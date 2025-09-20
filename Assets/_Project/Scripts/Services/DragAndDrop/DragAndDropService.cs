using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Initialize;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Elements;
using _Project.Scripts.Services.Elements.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Tower;
using Code.Services.Elements;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace _Project.Scripts.Services.DragAndDrop
{
    public sealed class DragAndDropService : IInitializeService, IDisposable
    {
        private const float HoldTimeDefault = 0.04f;
        private const float HoldTimeBottom = 0.02f;
        private const float HoldTimeTower = 0.00f;

        private float _activeHoldTime;
        private float HoldMaxMove => _eventSystem != null ? _eventSystem.pixelDragThreshold : 16f;
        private float HoldMaxMoveSqr => HoldMaxMove * HoldMaxMove;

        private readonly IInputService _inputService;
        private readonly SignalBus _signalBus;
        private readonly EventSystem _eventSystem;
        private readonly ShadowDropTarget _ghost;
        private readonly IElementAnimator _animator;

        private IDragTarget _source;
        private Vector2 _startPos;
        private bool _isHolding;
        private float _startTime;
        private bool _isDisposed;

        private CompositeDisposable _subscriptions;
        private static readonly List<RaycastResult> RaycastBuffer = new(16);

        public DragAndDropService(
            IInputService inputService,
            SignalBus signalBus,
            ShadowDropTarget ghost,
            IElementAnimator animator,
            [Inject(Optional = true)] EventSystem eventSystem)
        {
            _inputService = inputService;
            _signalBus = signalBus;
            _ghost = ghost;
            _animator = animator;
            _eventSystem = eventSystem ?? EventSystem.current;
        }
        
        public UniTask InitializeAsync()
        {
            _subscriptions = new CompositeDisposable();

            _inputService.CursorDown
                .Subscribe(_ =>
                {
                    try
                    {
                        TryBegin(_inputService.CursorPosition.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DnD] TryBegin failed: {ex}");
                        CancelHold();
                    }
                })
                .AddTo(_subscriptions);

            _inputService.CursorMove
                .Subscribe(cursorPos =>
                {
                    try
                    {
                        OnMove(cursorPos);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DnD] OnMove failed: {ex}");
                        CancelHold();
                        _ghost.Hide();
                    }
                })
                .AddTo(_subscriptions);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    try
                    {
                        if (_isHolding && !_ghost.IsActive)
                        {
                            var elapsed = Time.unscaledTime - _startTime;
                            if (elapsed >= HoldTimeDefault)
                                StartDragAsync(_inputService.CursorPosition.Value).Forget();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DnD] Hold tick failed: {ex}");
                        CancelHold();
                    }
                })
                .AddTo(_subscriptions);

            _inputService.CursorUp
                .Subscribe(_ =>
                {
                    try
                    {
                        EndAsync(_inputService.CursorPosition.Value).Forget();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DnD] EndAsync failed: {ex}");
                        CancelHold();
                        _ghost.Hide();
                    }
                })
                .AddTo(_subscriptions);

            return UniTask.CompletedTask;
        }
        
        private void TryBegin(Vector2 startPos)
        {
            if (_ghost.IsActive) return;

            _source = RaycastFor<IDragTarget>(startPos);
            if (_source == null) return;

            _activeHoldTime = HoldTimeBottom;
            if (_source is Element element && element.Owner is TowerPlace)
                _activeHoldTime = HoldTimeTower;

            if (_activeHoldTime <= 0f)
            {
                StartDragAsync(startPos).Forget();
                return;
            }

            _isHolding = true;
            _startTime = Time.unscaledTime;
            _startPos = startPos;
        }
        
        private void OnMove(Vector2 currentPos)
        {
            if (_ghost.IsActive)
            {
                _ghost.MoveTo(currentPos);
                return;
            }

            if (_isHolding)
            {
                var movedSqr = (currentPos - _startPos).sqrMagnitude;
                
                if (movedSqr >= HoldMaxMoveSqr)
                {
                    CancelHold();
                }
            }
        }
        
        private UniTask StartDragAsync(Vector2 pos)
        {
            _isHolding = false;
            if (_source == null) return UniTask.CompletedTask;

            try
            {
                _ghost.BeginFrom(_source.Rect, pos);
                _source.OnBeginDrag();
                _signalBus.TryFire(new DragStarted { PointerId = 0 });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DnD] StartDragAsync failed: {ex}");
                CancelHold();
                _ghost.Hide();
            }

            return UniTask.CompletedTask;
        }
        
        private async UniTask EndAsync(Vector2 pos)
        {
            if (!_ghost.IsActive)
            {
                CancelHold();
                return;
            }

            bool success = false;
            try
            {
                var dropTarget = RaycastFor<IDropTarget>(pos);
                success = dropTarget != null && dropTarget.CanDrop(_source);

                if (success)
                {
                    dropTarget.OnDrop(_source);
                }
                else
                {
                    if (dropTarget == null)
                    {
                        _signalBus.Fire(new ElementMissed());
                        await _animator.PlayMiss(_ghost.RectTransform);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DnD] EndAsync drop failed: {ex}");
            }
            finally
            {
                try
                {
                    _ghost.Hide();
                    _source?.OnEndDrag(success);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DnD] Cleanup failed: {ex}");
                }
                finally
                {
                    _source = null;
                    _signalBus.TryFire(new DragEnded { PointerId = 0 });
                }
            }
        }

        private void CancelHold()
        {
            _isHolding = false;
            _source = null;
        }
        
        private T RaycastFor<T>(Vector2 screenPos) where T : class
        {
            if (_eventSystem == null) return null;

            RaycastBuffer.Clear();
            var pointerData = new PointerEventData(_eventSystem) { position = screenPos };
            _eventSystem.RaycastAll(pointerData, RaycastBuffer);

            for (int index = 0; index < RaycastBuffer.Count; index++)
            {
                var hitGo = RaycastBuffer[index].gameObject;
                var found = FindInterfaceInParents<T>(hitGo);
                if (found != null)
                    return found;
            }

            return null;
        }
        
        private static T FindInterfaceInParents<T>(GameObject go) where T : class
        {
            var tr = go.transform;
            while (tr != null)
            {
                var behaviours = tr.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is T match)
                        return match;
                }

                tr = tr.parent;
            }

            return null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _subscriptions?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DnD] Dispose failed: {ex}");
            }
        }
    }
}