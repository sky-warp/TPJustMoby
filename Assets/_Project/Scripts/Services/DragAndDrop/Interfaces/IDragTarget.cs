using UnityEngine;

namespace _Project.Scripts.Services.DragAndDrop.Interfaces
{
    public interface IDragTarget
    {
        RectTransform Rect { get; }
        void OnBeginDrag();
        void OnEndDrag(bool droppedSuccessfully);
    }
}