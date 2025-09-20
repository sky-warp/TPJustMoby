using _Project.Scripts.Services.Elements.Interfaces;
using Code.Services.Elements;
using UnityEngine;

namespace _Project.Scripts.Services.Elements
{
    public abstract class ElementsBox : MonoBehaviour, IElementContainer
    {
        public abstract void OnElementPicked(Element element, int slotIndex);
        public abstract void OnElementReleased(Element element, int slotIndex, bool success);
    }
}