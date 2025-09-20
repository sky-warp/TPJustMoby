namespace _Project.Scripts.Services.DragAndDrop.Interfaces
{
    public interface IDropTarget
    {
        void OnDrop(IDragTarget drag);
        bool CanDrop(IDragTarget drag);
    }
}