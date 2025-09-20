using System.Collections.Generic;
using Code.Services.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface ITowerAnimation
    {
        UniTask PlayAdd(Element element, Vector2 to);
        UniTask PlayRemove(Element element, Vector2 to);
        UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 to)> moves);
    }
}