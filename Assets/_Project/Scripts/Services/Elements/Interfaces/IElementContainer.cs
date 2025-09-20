using Code.Services.Elements;

namespace _Project.Scripts.Services.Elements.Interfaces
{
    public interface IElementContainer
    {
        void OnElementPicked(Element element, int slotIndex);
        void OnElementReleased(Element element, int slotIndex, bool success);
    }
}