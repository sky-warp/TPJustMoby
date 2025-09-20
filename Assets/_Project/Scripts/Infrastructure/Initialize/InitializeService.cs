using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Initialize
{
    public class InitializeService : IInitializeService
    {
        private readonly List<IInitializeService> _initializables;

        public InitializeService(List<IInitializeService> initializables)
        {
            _initializables = initializables;
        }

        public async UniTask InitializeAsync()
        {
            foreach (var init in _initializables)
            {
                if (ReferenceEquals(init, this))
                    continue;
                await init.InitializeAsync();
            }
        }
    }

}