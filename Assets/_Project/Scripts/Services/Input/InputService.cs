using System;
using _Project.Scripts.Infrastructure.Initialize;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace _Project.Scripts.Services.Input
{
    public class InputService : IInputService, IInitializeService, IDisposable
    {
        public IReadOnlyReactiveProperty<Vector2> CursorPosition => _cursorPosition;
        public IObservable<Unit> CursorDown => _cursorDown;
        public IObservable<Unit> CursorUp => _cursorUp;
        public IObservable<Vector2> CursorMove => _cursorMove;

        private readonly ReactiveProperty<Vector2> _cursorPosition = new();
        private readonly Subject<Unit> _cursorDown = new();
        private readonly Subject<Unit> _cursorUp = new();
        private readonly Subject<Vector2> _cursorMove = new();

        private CompositeDisposable _subscriptions;
        private bool _disposed;

        public UniTask InitializeAsync()
        {
            _subscriptions = new CompositeDisposable();
            SetupUpdateLoop();
            return UniTask.CompletedTask;
        }
        
        private void SetupUpdateLoop()
        {
            Observable.EveryUpdate()
                .Subscribe(_ => SafeUpdateLoop())
                .AddTo(_subscriptions);
        }

        private void SafeUpdateLoop()
        {
            try
            {
                if (!ProcessTouchInput())
                    ProcessMouseInput();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputService] Update loop failed: {ex}");
            }
        }
        
        private bool ProcessTouchInput()
        {
            if (UnityEngine.Input.touchCount <= 0)
                return false;

            var touch = UnityEngine.Input.GetTouch(0);
            UpdateCursorPosition(touch.position);

            if (touch.phase == TouchPhase.Began)
                EmitDown();
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                EmitUp();

            return true;
        }

        private void ProcessMouseInput()
        {
            UpdateCursorPosition(UnityEngine.Input.mousePosition);

            if (UnityEngine.Input.GetMouseButtonDown(0))
                EmitDown();
            if (UnityEngine.Input.GetMouseButtonUp(0))
                EmitUp();
        }


        private void UpdateCursorPosition(Vector2 pos)
        {
            if (pos == _cursorPosition.Value)
                return;
            _cursorPosition.Value = pos;
            _cursorMove.OnNext(pos);
        }

        private void EmitDown() => _cursorDown.OnNext(Unit.Default);
        private void EmitUp() => _cursorUp.OnNext(Unit.Default);
        
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                _subscriptions?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputService] Dispose subscriptions failed: {ex}");
            }

            CompleteSafely(_cursorDown);
            CompleteSafely(_cursorUp);
            CompleteSafely(_cursorMove);

            try
            {
                _cursorPosition?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InputService] Dispose cursor position warn: {ex}");
            }
        }

        private static void CompleteSafely(IObserver<Unit> subject)
        {
            (subject as ISubject<Unit>)?.OnCompleted();
        }

        private static void CompleteSafely(IObserver<Vector2> subject)
        {
            (subject as ISubject<Vector2>)?.OnCompleted();
        }
    }
}