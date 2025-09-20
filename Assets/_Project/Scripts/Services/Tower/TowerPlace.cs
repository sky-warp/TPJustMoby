using System;
using System.Collections.Generic;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Elements;
using _Project.Scripts.Services.Elements.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Tower.Interfaces;
using Code.Services.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Tower
{
    public sealed class TowerPlace : MonoBehaviour, IDropTarget, IElementContainer
    {
        [Header("Area")] [SerializeField] private RectTransform _towerArea;

        [Header("Drop corridor (over-the-top)")] [SerializeField, Range(0f, 1f)]
        private float _topSnapPercent = 0.7f;

        [SerializeField, Min(0f)] private float _sideSlackPx = 8f;

        private readonly List<Element> _stack = new();

        private ITowerDropCheck _dropCheck;
        private ITowerPlacementProvider _placement;
        private ITowerAnimation _animation;
        private ElementsPool _pool;
        private IInputService _input;
        private SignalBus _signalBus;

        public IReadOnlyList<Element> Stack => _stack;
        public RectTransform Area => _towerArea;

        [Inject]
        public void Construct(
            ITowerDropCheck dropCheck,
            ITowerPlacementProvider placement,
            ITowerAnimation animation,
            ElementsPool pool,
            IInputService input,
            SignalBus bus)
        {
            _dropCheck = dropCheck;
            _placement = placement;
            _animation = animation;
            _pool = pool;
            _input = input;
            _signalBus = bus;
        }

        public void ClearInstant()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                try
                {
                    _pool.Despawn(_stack[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TowerPlace] Despawn failed: {ex}");
                }
            }

            _stack.Clear();
        }

        public void SpawnAtInstant(ElementConfiguration config, Vector2 anchoredPos)
        {
            try
            {
                var element = _pool.Spawn();
                element.transform.SetParent(_towerArea, false);
                element.Initialize(config, this, -1);
                element.Rect.anchoredPosition = anchoredPos;
                _stack.Add(element);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPlace] SpawnAtInstant failed: {ex}");
            }
        }

        public bool CanDrop(IDragTarget drag)
        {
            if (drag is not Element element) return false;

            if (IsElementFromTower(element))
                return false;

            if (!PassesHeightLimit(element))
                return false;

            return _stack.Count == 0 || IsPointerInTopCorridor();
        }

        public void OnDrop(IDragTarget drag)
        {
            if (drag is not Element source)
                return;

            try
            {
                var target = CalculateTargetPosition(source);
                var copy = SpawnCopyUnderTower(source.Config);

                if (_stack.Count == 0)
                    HandleDropOnEmptyStack(copy, target);
                else
                    HandleDropOnExistingStack(source, copy, target);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPlace] OnDrop failed: {ex}");
            }
        }

        public void OnElementPicked(Element element, int slotIndex)
        {
        }

        public void OnElementReleased(Element element, int slotIndex, bool success)
        {
        }

        public async UniTask RemoveElementAsync(Element target)
        {
            int idx = _stack.IndexOf(target);
            if (idx < 0)
                return;

            _stack.RemoveAt(idx);

            var moves = BuildCollapseMoves();

            await _animation.PlayCollapse(moves);

            try
            {
                _pool.Despawn(target);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPlace] Despawn removed element failed: {ex}");
            }

            _signalBus.TryFire(new TowerChanged());
        }

        private bool IsElementFromTower(Element element) => element.Owner is TowerPlace || element.SlotIndex < 0;

        private bool PassesHeightLimit(Element element)
        {
            if (_dropCheck.CanAdd(element, _stack, _towerArea, out var reason))
                return true;

            if (reason == "height_limit")
                _signalBus.Fire(new HeightLimitReached());

            return false;
        }

        private bool IsPointerInTopCorridor()
        {
            var topRect = _stack[^1].Rect;

            var camera = GetCanvasCamera(_towerArea);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                topRect, _input.CursorPosition.Value, camera, out var localPoint);

            var topRectRect = topRect.rect;
            float height = topRectRect.height;

            var expanded = new Rect(
                topRectRect.xMin - _sideSlackPx,
                topRectRect.yMin,
                topRectRect.width + _sideSlackPx * 2f,
                topRectRect.height + height * _topSnapPercent
            );

            return expanded.Contains(localPoint);
        }

        private Vector2 CalculateTargetPosition(Element source)
        {
            float size = source.Rect.rect.height;

            if (_stack.Count == 0)
                return CalculateBottomPlacement(size);

            var topRect = _stack[^1].Rect;
            return _placement.CalculateAddPosition(_towerArea, topRect, size);
        }

        private Vector2 CalculateBottomPlacement(float size)
        {
            var camera = GetCanvasCamera(_towerArea);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _towerArea,
                _input.CursorPosition.Value,
                camera,
                out var local);

            float half = size * 0.5f;
            float areaHalfW = _towerArea.rect.width * 0.5f;
            float x = Mathf.Clamp(local.x, -areaHalfW + half, areaHalfW - half);

            float bottomY = -_towerArea.rect.height * 0.5f + half;

            return new Vector2(x, bottomY);
        }

        private Element SpawnCopyUnderTower(ElementConfiguration config)
        {
            var copy = _pool.Spawn();
            copy.transform.SetParent(_towerArea, false);
            copy.Initialize(config, this, -1);
            return copy;
        }

        private void HandleDropOnEmptyStack(Element copy, Vector2 target)
        {
            copy.Rect.anchoredPosition = target;

            _stack.Add(copy);
            _signalBus.Fire(new ElementPlaced());
            _signalBus.TryFire(new TowerChanged());
        }

        private void HandleDropOnExistingStack(Element source, Element copy, Vector2 target)
        {
            float rectHeight = copy.Rect.rect.height;
            var start = new Vector2(target.x, target.y - rectHeight * 0.6f);
            copy.Rect.anchoredPosition = start;

            _stack.Add(copy);
            _signalBus.Fire(new ElementPlaced());

            PlayAddAndNotifyAsync(copy, target).Forget();
        }

        private async UniTask PlayAddAndNotifyAsync(Element copy, Vector2 target)
        {
            try
            {
                await _animation.PlayAdd(copy, target);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPlace] PlayAdd failed: {ex}");
            }
            finally
            {
                _signalBus.TryFire(new TowerChanged());
            }
        }


        private List<(RectTransform rect, Vector2 to)> BuildCollapseMoves()
        {
            var moves = new List<(RectTransform rect, Vector2 to)>(_stack.Count);

            float areaHalfH = _towerArea.rect.height * 0.5f;
            float prevCenterY = 0f;
            float prevH = 0f;

            for (int i = 0; i < _stack.Count; i++)
            {
                var rect = _stack[i].Rect;
                float h = rect.rect.height;

                float x = rect.anchoredPosition.x;
                float y = i == 0
                    ? -areaHalfH + h * 0.5f
                    : prevCenterY + (prevH + h) * 0.5f;

                moves.Add((rect, new Vector2(x, y)));
                prevCenterY = y;
                prevH = h;
            }

            return moves;
        }


        private static Camera GetCanvasCamera(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            if (!canvas) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_towerArea == null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        Debug.LogWarning("[TowerPlace] TowerArea is not assigned.");
                };
            }
        }
#endif
    }
}