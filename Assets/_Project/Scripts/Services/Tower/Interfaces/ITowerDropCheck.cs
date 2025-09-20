using System.Collections.Generic;
using Code.Services.Elements;
using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface ITowerDropCheck
    {
        bool CanAdd(Element element, IReadOnlyList<Element> currentStack, RectTransform towerArea, out string reason);

    }
}