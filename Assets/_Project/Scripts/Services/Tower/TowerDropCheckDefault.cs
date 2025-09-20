using System.Collections.Generic;
using _Project.Scripts.Services.Tower.Interfaces;
using Code.Services.Elements;
using UnityEngine;

namespace _Project.Scripts.Services.Tower
{
    public sealed class TowerDropCheckDefault : ITowerDropCheck
    {
        private const string ReasonHeightLimit = "height_limit";
        private const string ReasonInvalidInput = "invalid_input";

        public bool CanAdd(Element element, IReadOnlyList<Element> stack, RectTransform towerArea, out string reason)
        {
            if (!ValidateInputs(element, towerArea, out reason))
                return false;

            float elementSize = GetElementHeight(element);
            float halfAreaHeight = towerArea.rect.height * 0.5f;

            float nextCenterY = ComputeNextCenterY(stack, elementSize, halfAreaHeight);
            float nextTopEdge = nextCenterY + elementSize * 0.5f;

            if (ExceedsTopBound(nextTopEdge, halfAreaHeight))
            {
                reason = ReasonHeightLimit;
                return false;
            }

            reason = null;
            return true;
        }

        private static bool ValidateInputs(Element element, RectTransform area, out string reason)
        {
            if (element == null || element.Rect == null || area == null)
            {
                reason = ReasonInvalidInput;
                return false;
            }

            reason = null;
            return true;
        }

        private static float GetElementHeight(Element element) => element.Rect.rect.height;

        private static float ComputeNextCenterY(IReadOnlyList<Element> stack, float newSize, float halfAreaHeight)
        {
            if (stack == null || stack.Count == 0)
            {
                return -halfAreaHeight + newSize * 0.5f;
            }

            var topRect = stack[^1].Rect;
            float topHeight = topRect.rect.height;

            return topRect.anchoredPosition.y + (topHeight + newSize) * 0.5f;
        }

        private static bool ExceedsTopBound(float nextTopEdge, float halfAreaHeight) =>
            nextTopEdge > halfAreaHeight + Mathf.Epsilon;
    }
}