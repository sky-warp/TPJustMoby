using System;
using UniRx;
using UnityEngine;

namespace _Project.Scripts.Services.Input
{
    public interface IInputService
    {
        IReadOnlyReactiveProperty<Vector2> CursorPosition { get; }
        IObservable<Unit> CursorDown { get; }
        IObservable<Unit> CursorUp { get; }
        IObservable<Vector2> CursorMove { get; }
        
    }
}