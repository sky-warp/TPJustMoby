using System;
using Code.Services.Elements;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Elements
{
    public class ElementsPool : MonoMemoryPool<Element>
    {
        protected override void OnSpawned(Element item)
        {
            base.OnSpawned(item);

            try
            {
                if (item == null)
                    return;

                item.gameObject.SetActive(true);

                var rect = item.Rect;
                if (rect != null)
                {
                    ResetRect(rect);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementsPool] OnSpawned error: {ex}");
            }
        }

        protected override void OnDespawned(Element item)
        {
            try
            {
                if (item != null)
                {
                    if (item.Rect != null)
                        item.Rect.localScale = Vector3.one;

                    item.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementsPool] OnDespawned error: {ex}");
            }
            finally
            {
                base.OnDespawned(item);
            }
        }

        private static void ResetRect(RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}