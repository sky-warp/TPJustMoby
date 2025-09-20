using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface ITowerPlacementProvider
    {
        Vector2 CalculateAddPosition(RectTransform towerArea, RectTransform topRect, float elementSize);

    }
}