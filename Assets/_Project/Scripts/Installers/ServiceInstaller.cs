using _Project.Scripts.Infrastructure.Initialize;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.DragAndDrop;
using _Project.Scripts.Services.Elements;
using _Project.Scripts.Services.Elements.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Saves;
using _Project.Scripts.Services.Tower;
using _Project.Scripts.Services.Tower.Interfaces;
using _Project.Scripts.Services.Zones;
using Code.Services.Elements;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace _Project.Scripts.Installers
{
    public class ServiceInstaller : MonoInstaller
    {
        [SerializeField] private Element elementPrefab;
        [SerializeField] private GameConfiguration gameConfig;

        public override void InstallBindings()
        {
            try
            {
                ValidateSerializedFields();

                BindSignals();
                BindConfigs();
                BindServices();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceInstaller] InstallBindings failed: {ex}");
                throw;
            }
        }

        private void BindServices()
        {
            Container.Bind<InitializeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputService>().AsSingle();
            Container.BindInterfacesAndSelfTo<DragAndDropService>().AsSingle();
            Container.Bind<IElementAnimator>().To<ElementAnimator>().AsSingle();
            Container.Bind<ITowerDropCheck>().To<TowerDropCheckDefault>().AsSingle();
            Container.Bind<ITowerPlacementProvider>().To<TowerPlacementProviderDefault>().AsSingle();
            Container.Bind<ITowerAnimation>().To<TowerAnimation>().AsSingle();
            Container.Bind<TowerPlace>().FromComponentInHierarchy().AsSingle();
            Container.Bind<RemoveZone>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ShadowDropTarget>().AsSingle();
            Container.Bind<EventSystem>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IDataStorage>().To<FileStorage>().AsSingle();
            Container.BindInterfacesAndSelfTo<TowerPersistenceService>().AsSingle();
            Container.BindMemoryPool<Element, ElementsPool>().FromComponentInNewPrefab(elementPrefab)
                .UnderTransformGroup("ElementsPool");
        }

        private void BindConfigs()
        {
            Container.BindInstance(gameConfig).AsSingle();
        }

        private void BindSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<DragStarted>().OptionalSubscriber();;
            Container.DeclareSignal<DragEnded>().OptionalSubscriber();;
            Container.DeclareSignal<TowerChanged>();
            Container.DeclareSignal<HeightLimitReached>();
            Container.DeclareSignal<ElementPlaced>();
            Container.DeclareSignal<ElementMissed>();
            Container.DeclareSignal<ElementDeleted>();
        }

        private void ValidateSerializedFields()
        {
            if (elementPrefab == null)
                throw new System.InvalidOperationException("Element prefab is not assigned.");
            if (gameConfig == null)
                throw new System.InvalidOperationException("GameConfiguration is not assigned.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (elementPrefab == null || gameConfig == null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    Debug.LogWarning("[ServiceInstaller] Some serialized fields are not assigned in Inspector.");
                };
            }
        }
#endif
    }
}