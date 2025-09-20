using _Project.Scripts.Services.Tower.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Services.Tower
{
    public sealed class TowerPlacementProviderDefault : ITowerPlacementProvider
    {
        public Vector2 CalculateAddPosition(RectTransform area, RectTransform top, float size)
        {
            float half = size * 0.5f;
            float areaHalfW = area.rect.width * 0.5f;

            if (top == null)
            {
                float bottomY = -area.rect.height * 0.5f + half;
                return new Vector2(0f, bottomY);
            }

            float lastH = top.rect.height;
            float baseX = top.anchoredPosition.x;
            float baseY = top.anchoredPosition.y + (lastH + size) * 0.5f;

            float x = Mathf.Clamp(baseX + Random.Range(-half, +half), -areaHalfW + half, areaHalfW - half);
            return new Vector2(x, baseY);
        }
    }
}