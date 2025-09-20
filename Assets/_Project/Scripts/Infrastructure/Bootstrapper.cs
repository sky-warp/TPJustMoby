using _Project.Scripts.Infrastructure.Initialize;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Infrastructure
{
    public class Bootstrapper : MonoBehaviour
    {
        [Inject] InitializeService  _initializer;

        private void Start() => _initializer.InitializeAsync().Forget();
    }
}