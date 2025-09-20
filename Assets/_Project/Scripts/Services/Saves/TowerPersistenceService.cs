using System;
using System.Linq;
using _Project.Scripts.Infrastructure.Initialize;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.Elements;
using _Project.Scripts.Services.Tower;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Saves
{
    public sealed class TowerPersistenceService : IInitializeService, IDisposable
    {
        private readonly IDataStorage _storage;
        private readonly TowerPlace _tower;
        private readonly SignalBus _signalBus;
        private readonly GameConfiguration _gameConfig;

        private Action<TowerChanged> _onTowerChanged;
        private bool _disposed;

        [Inject]
        public TowerPersistenceService(
            IDataStorage storage,
            TowerPlace tower,
            SignalBus signalBus,
            GameConfiguration gameCfg)
        {
            _storage = storage;
            _tower = tower;
            _signalBus = signalBus;
            _gameConfig = gameCfg;
        }

        public async UniTask InitializeAsync()
        {
            try
            {
                ValidateDeps();
                await LoadAndRebuildAsync();
                SetupSubscriptions();
                HookAppEvents();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPersistenceService] InitializeAsync failed: {ex}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_onTowerChanged != null)
                    _signalBus.TryUnsubscribe(_onTowerChanged);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPersistenceService] Unsubscribe failed: {ex}");
            }
            finally
            {
                UnhookAppEvents();
            }
        }

        private void ValidateDeps()
        {
            if (_tower == null) throw new InvalidOperationException("TowerPlace is not injected.");
            if (_storage == null) throw new InvalidOperationException("IDataStorage is not injected.");
            if (_signalBus == null) throw new InvalidOperationException("SignalBus is not injected.");
            if (_gameConfig == null) throw new InvalidOperationException("GameConfiguration is not injected.");
        }

        private async UniTask LoadAndRebuildAsync()
        {
            var state = await _storage.LoadState<TowerState>();
            if (state?.Elements == null || state.Elements.Count == 0)
                return;

            await RebuildFromStateAsync(state);
        }

        private void SetupSubscriptions()
        {
            _onTowerChanged = _ => SaveNowAsync().Forget();
            _signalBus.Subscribe(_onTowerChanged);
        }

        private void HookAppEvents()
        {
            Application.quitting += OnQuitting;
            Application.focusChanged += OnFocusChanged;
        }

        private void UnhookAppEvents()
        {
            Application.quitting -= OnQuitting;
            Application.focusChanged -= OnFocusChanged;
        }

        private TowerState BuildState()
        {
            var state = new TowerState();
            foreach (var element in _tower.Stack)
            {
                var pos = element.Rect.anchoredPosition;
                state.Elements.Add(new TowerElementState
                {
                    ConfigId = element.Config.Id,
                    X = pos.x,
                    Y = pos.y
                });
            }

            return state;
        }

        private async UniTask SaveNowAsync()
        {
            try
            {
                var snapshot = BuildState();
                await _storage.SaveState(snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPersistenceService] Save failed: {ex}");
            }
        }

        private void SaveNowSync()
        {
            try
            {
                SaveNowAsync().Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPersistenceService] Save (sync) dispatch failed: {ex}");
            }
        }

        private async UniTask RebuildFromStateAsync(TowerState state)
        {
            try
            {
                _tower.ClearInstant();

                var configMap =
                    _gameConfig.Configurations.ToDictionary(elementConfiguration => elementConfiguration.Id, c => c);

                foreach (var it in state.Elements)
                {
                    if (!configMap.TryGetValue(it.ConfigId, out var cfg))
                    {
                        Debug.LogWarning($"[TowerPersistenceService] Config '{it.ConfigId}' not found. Skipping.");
                        continue;
                    }

                    _tower.SpawnAtInstant(cfg, new Vector2(it.X, it.Y));
                    await UniTask.Yield();
                }

                _signalBus.TryFire(new TowerChanged());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TowerPersistenceService] Rebuild failed: {ex}");
            }
        }
        
        private void OnQuitting() => SaveNowSync();

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                SaveNowSync();
        }
    }
}