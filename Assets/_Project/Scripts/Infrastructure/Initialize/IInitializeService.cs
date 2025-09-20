using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Initialize
{
    public interface IInitializeService
    {
        UniTask InitializeAsync();
    }
}