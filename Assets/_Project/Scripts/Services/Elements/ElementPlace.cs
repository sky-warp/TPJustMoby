using System;
using System.Collections.Generic;
using Code.Services.Elements;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Services.Elements
{
    public class ElementPlace : ElementsBox
    {
        [Header("UI")] 
        [SerializeField] private RectTransform _slotsRoot; 
        [SerializeField] private ScrollRect _scroll;

        [Header("Data")] 
        [SerializeField] private GameConfiguration _gameConfig; 
        private readonly Dictionary<int, Image> _slotShadows = new(); 
        private readonly Dictionary<int, Element> _slotElements = new();
        private ElementsPool _pool;

        [Inject]
        public void Construct(ElementsPool pool) => _pool = pool;

        private void Start()
        {
            try
            {
                ValidateSerializedFields();
                InitializeSlots();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementPlace] Start failed: {ex}");
            }
        }

        private void InitializeSlots()
        {
            var configs = _gameConfig.Configurations;
            for (int index = 0; index < configs.Count; index++)
            {
                CreateSlotElement(index, configs[index]);
            }
        }
        
        public void RegisterShadow(int slotIndex, Image shadow)
        {
            if (shadow == null) 
                return;
            _slotShadows[slotIndex] = shadow;
            shadow.gameObject.SetActive(false);
        }

        public override void OnElementPicked(Element element, int slotIndex)
        {
            try
            {
                ToggleScroll(false);
                if (slotIndex < 0) 
                    return;
                ShowShadow(slotIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementPlace] OnElementPicked error: {ex}");
            }
        }

        public override void OnElementReleased(Element element, int slotIndex, bool success)
        {
            try
            {
                ToggleScroll(true);
                if (slotIndex < 0) 
                    return;
                
                HideShadow(slotIndex);

                if (success)
                {
                    _pool.Despawn(element);
                    _slotElements.Remove(slotIndex);

                    var config = element.Config;
                    CreateSlotElement(slotIndex, config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementPlace] OnElementReleased error: {ex}");
            }
        }
        
        private void CreateSlotElement(int slotIndex, ElementConfiguration config)
        {
            try
            {
                var element = _pool.Spawn();
                AttachUnderSlotsRoot(element.transform);
                element.Initialize(config, this, slotIndex);
                _slotElements[slotIndex] = element;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementPlace] CreateSlotElement[{slotIndex}] failed: {ex}");
            }
        }

        private void AttachUnderSlotsRoot(Transform t)
        {
            if (_slotsRoot == null) 
                throw new InvalidOperationException("SlotsRoot is not assigned.");
            t.SetParent(_slotsRoot, false);
        }

        private void ToggleScroll(bool enabled)
        {
            if (_scroll != null) 
                _scroll.enabled = enabled;
        }

        private void ShowShadow(int slotIndex)
        {
            if (_slotShadows.TryGetValue(slotIndex, out var shadow))
                shadow.gameObject.SetActive(true);
        }

        private void HideShadow(int slotIndex)
        {
            if (_slotShadows.TryGetValue(slotIndex, out var shadow))
                shadow.gameObject.SetActive(false);
        }

        private void ValidateSerializedFields()
        {
            if (_gameConfig == null) throw new InvalidOperationException("GameConfiguration is not assigned.");
            if (_slotsRoot == null) throw new InvalidOperationException("SlotsRoot is not assigned.");
            if (_pool == null) throw new InvalidOperationException("ElementsPool is not injected.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_slotsRoot == null || _gameConfig == null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    Debug.LogWarning("[ElementPlace] Missing serialized references (GameConfiguration/SlotsRoot).");
                };
            }
        }
#endif
    }
}