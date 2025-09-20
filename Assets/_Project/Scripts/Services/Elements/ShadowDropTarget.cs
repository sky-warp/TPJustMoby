using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Services.Elements
{
    public sealed class ShadowDropTarget
    {
        private const string DragLayerName = "DragLayer";
        private const string GhostName = "DragGhost";

        public RectTransform RectTransform => _root;
        public bool IsActive => _root != null && _root.gameObject.activeSelf;

        private readonly RectTransform _layer; 
        private readonly Camera _uiCamera; 

        private RectTransform _root; 
        private Image _image; 

        [Inject]
        public ShadowDropTarget(
            [Inject(Id = "DragLayer", Optional = true)]
            RectTransform layer = null,
            [Inject(Id = "UICamera", Optional = true)]
            Camera uiCamera = null)
        {
            _layer = layer;
            _uiCamera = uiCamera;
            BuildSafe();
        }
        
        public void BeginFrom(RectTransform source, Vector2 startScreenPos)
        {
            if (source == null || _root == null) 
                return;

            try
            {
                CopyVisualFromSource(source);
                Show();
                MoveTo(startScreenPos);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShadowDropTarget] BeginFrom failed: {ex}");
                Hide();
            }
        }
        
        public void MoveTo(Vector2 screenPos)
        {
            if (_root == null || _root.parent == null)
                return;

            try
            {
                var parentRect = (RectTransform)_root.parent;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect, screenPos, GetCameraFor(parentRect), out var local))
                    return;

                _root.anchoredPosition = local;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShadowDropTarget] MoveTo failed: {ex}");
            }
        }
        
        public void Hide()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
        }
        
        private void BuildSafe()
        {
            try
            {
                var parent = _layer != null ? _layer : FindOrCreateDragLayer();
                _root = CreateGhostUnder(parent);
                _image = _root.GetComponent<Image>();
                ConfigureDefaultImage(_image);
                Hide();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShadowDropTarget] Build failed: {ex}");
            }
        }

        private RectTransform CreateGhostUnder(RectTransform parent)
        {
            if (parent == null) throw new InvalidOperationException("Parent layer is null.");
            var gameObject = new GameObject(GhostName, typeof(RectTransform), typeof(Image));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private void ConfigureDefaultImage(Image img)
        {
            if (img == null)
                return;
            img.raycastTarget = false;
            img.sprite = null;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.material = null;
        }

        private void CopyVisualFromSource(RectTransform source)
        {
            if (source.TryGetComponent(out Image srcImg))
            {
                _image.sprite = srcImg.sprite;
                _image.color = srcImg.color;
                _image.type = srcImg.type;
                _image.material = srcImg.material;
                _root.sizeDelta = srcImg.rectTransform.rect.size;
            }
            else
            {
                _image.sprite = null;
                _image.color = Color.white;
                _image.type = Image.Type.Simple;
                _image.material = null;
                _root.sizeDelta = source.rect.size;
            }

            _root.localScale = Vector3.one;
            _root.rotation = Quaternion.identity;
        }

        private void Show() => _root.gameObject.SetActive(true);

        private RectTransform FindOrCreateDragLayer()
        {
            var canvas = FindFirstCanvas() ?? CreateOverlayCanvas();
            var layer = canvas.transform.Find(DragLayerName) as RectTransform;
            if (layer == null)
            {
                var go = new GameObject(DragLayerName, typeof(RectTransform));
                layer = go.GetComponent<RectTransform>();
                layer.SetParent(canvas.transform, false);
                layer.anchorMin = Vector2.zero;
                layer.anchorMax = Vector2.one;
                layer.offsetMin = Vector2.zero;
                layer.offsetMax = Vector2.zero;
                layer.SetAsLastSibling();
            }

            return layer;
        }

        private static Canvas FindFirstCanvas()
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            return canvases != null && canvases.Length > 0 ? canvases[0] : null;
        }

        private static Canvas CreateOverlayCanvas()
        {
            Debug.LogWarning("[ShadowDropTarget] Canvas not found. Creating ScreenSpaceOverlay Canvas for DragLayer.");
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private Camera GetCameraFor(RectTransform parentRect)
        {
            if (_uiCamera != null) 
                return _uiCamera;
            var canvas = parentRect.GetComponentInParent<Canvas>();
            if (canvas == null) 
                return null;

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}