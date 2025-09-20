using System;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Elements;
using _Project.Scripts.Services.Elements.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Services.Elements
{
    public class Element : MonoBehaviour, IDragTarget
    {
        private const float DragAlpha = 0.6f;
        private const float FullAlpha = 1f;

        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _image;

        private readonly Subject<Unit> _onDragBegin = new();
        private readonly Subject<Unit> _onDragEnd = new();

        private ElementConfiguration _config;
        private IElementContainer _owner;
        private int _slotIndex = -1;
        public IElementContainer Owner => _owner;
        public int SlotIndex => _slotIndex;
        public RectTransform Rect => _rectTransform;
        public ElementConfiguration Config => _config;

        public IObservable<Unit> OnDragBegin => _onDragBegin;
        public IObservable<Unit> OnDragEnd => _onDragEnd;
        
        public void Initialize(ElementConfiguration config, IElementContainer owner, int slotIndex = -1)
        {
            try
            {
                ValidateInitialization(config);

                _config = config;
                _owner = owner;
                _slotIndex = slotIndex;

                InitializeVisual(config);

                gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Element] Initialize failed: {ex}");
                throw;
            }
        }

        private void ValidateInitialization(ElementConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (_image == null || _rectTransform == null)
                throw new InvalidOperationException("Element is not wired: Image/RectTransform missing.");
        }

        private void InitializeVisual(ElementConfiguration config)
        {
            _image.sprite = config.Sprite;
            _image.raycastTarget = true;

            var baseColor = _image.color;
            _image.color = new Color(baseColor.r, baseColor.g, baseColor.b, FullAlpha);
            _rectTransform.localScale = Vector3.one;
        }
        
        public void OnBeginDrag()
        {
            try
            {
                if (_image != null)
                {
                    var color = _image.color;
                    _image.color = new Color(color.r, color.g, color.b, DragAlpha);
                }
                _owner?.OnElementPicked(this, _slotIndex);
                _onDragBegin.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Element] OnBeginDrag error: {ex}");
            }
        }
        
        public void OnEndDrag(bool droppedSuccessfully)
        {
            try
            {
                if (_image != null)
                {
                    var color = _image.color;
                    _image.color = new Color(color.r, color.g, color.b, FullAlpha);
                }
                _owner?.OnElementReleased(this, _slotIndex, droppedSuccessfully);
                _onDragEnd.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Element] OnEndDrag error: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _onDragBegin.OnCompleted();
                _onDragEnd.OnCompleted();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Element] OnDestroy completion warn: {ex}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        }
#endif
    }
}