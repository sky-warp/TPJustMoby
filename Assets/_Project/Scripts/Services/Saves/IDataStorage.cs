using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Services.Saves
{
    public interface IDataStorage
    {
        UniTask<T> LoadState<T>() where T : class, new();
        UniTask SaveState<T>(T state) where T : class;
    }
}