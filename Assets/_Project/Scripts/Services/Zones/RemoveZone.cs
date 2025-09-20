using System;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Elements.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Tower;
using Code.Services.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Zones
{
    public sealed class RemoveZone : MonoBehaviour, IDropTarget, ICanvasRaycastFilter
    {
        [Header("Raycast")] [SerializeField] private RectTransform _raycastRect;
        [SerializeField] private PolygonCollider2D _polygon;

        private TowerPlace _tower;
        private IElementAnimator _animator;
        private IInputService _input;
        private SignalBus _signalBus;
        private Canvas _canvas;

        [Inject]
        public void Construct(TowerPlace tower, IElementAnimator animator, IInputService input, SignalBus signalBus)
        {
            _tower = tower;
            _animator = animator;
            _input = input;
            _signalBus = signalBus;
        }

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        public bool CanDrop(IDragTarget drag) => drag is Element e && IsFromTower(e);

        public async void OnDrop(IDragTarget drag)
        {
            if (drag is not Element element)
                return;

            try
            {
                await HandleRemoveAsync(element);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoveZone] OnDrop failed: {ex}");
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
        {
            try
            {
                return IsInsidePolygon(screenPos, eventCamera);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoveZone] Raycast filter error: {ex}");
                return true;
            }
        }

        private static bool IsFromTower(Element e) => e.Owner is TowerPlace || e.SlotIndex < 0;

        private async UniTask HandleRemoveAsync(Element element)
        {
            var localInTower = ScreenToLocalInTower(_input.CursorPosition.Value);
            await _animator.PlayRemove(element.Rect, localInTower);
            await _tower.RemoveElementAsync(element);
            _signalBus.Fire(new ElementDeleted());
        }

        private Vector2 ScreenToLocalInTower(Vector2 screenPos)
        {
            var area = _tower.Area;
            var cam = GetCanvasCamera(area);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, cam, out var local))
                return Vector2.zero;

            return local;
        }

        private bool IsInsidePolygon(Vector2 screenPos, Camera eventCamera)
        {
            if (_polygon == null)
                return true;

            var rect = _raycastRect ? _raycastRect : (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPos, eventCamera, out var world))
                return false;

            var point2D = new Vector2(world.x, world.y);
            return _polygon.OverlapPoint(point2D);
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
            if (_polygon == null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        Debug.LogWarning("[RemoveZone] PolygonCollider2D is not assigned.");
                };
            }
        }
#endif
    }
}