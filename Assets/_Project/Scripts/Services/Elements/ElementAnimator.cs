using System;
using System.Collections.Generic;
using _Project.Scripts.Services.Elements.Interfaces;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Services.Elements
{
    public sealed class ElementAnimator : IElementAnimator
    {
        private const float JumpPower = 100f;
        private const int JumpNum = 1;
        private const float JumpDur = 0.36f;
        private const float AddPulseDur = 0.06f;
        
        private const float MissScaleUpDur = 0.06f;
        private const float MissScaleDownDur = 0.16f;

        private const float RemoveBounceIn = 0.12f;
        private const float RemoveBounceOut = 0.08f;
        private const float RemoveFlyDur = 0.35f;
        private const float RemoveScaleTo = 0f;
        private const float CollapseMoveDur = 0.15f;
        
      
        public UniTask PlayAdd(RectTransform element, Vector2 toPos)
        {
            if (!IsRectValid(element)) 
                return UniTask.CompletedTask;

            try
            {
                element.DOKill();
                ResetTransform(element);

                var seq = DOTween.Sequence()
                    .Append(element.DOJumpAnchorPos(toPos, JumpPower, JumpNum, JumpDur).SetEase(Ease.OutQuad))
                    .Append(element.DOScale(1.03f, AddPulseDur).SetLoops(2, LoopType.Yoyo));

                seq.OnComplete(() =>
                {
                    element.anchoredPosition = toPos;
                    element.localScale = Vector3.one;
                    element.rotation = Quaternion.identity;
                });

                return seq.AsyncWaitForCompletion().AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementAnimator] PlayAdd error: {ex}");
                return UniTask.CompletedTask;
            }
        }
        
        public UniTask PlayMiss(RectTransform proxy)
        {
            if (!IsRectValid(proxy))
                return UniTask.CompletedTask;

            try
            {
                proxy.DOComplete();
                var seq = DOTween.Sequence()
                    .Append(proxy.DOScale(1.1f, MissScaleUpDur))
                    .Append(proxy.DOScale(0f, MissScaleDownDur).SetEase(Ease.InBack));

                return seq.AsyncWaitForCompletion().AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementAnimator] PlayMiss error: {ex}");
                return UniTask.CompletedTask;
            }
        }
        
        public UniTask PlayRemove(RectTransform element, Vector2 toPos)
        {
            if (!IsRectValid(element)) return UniTask.CompletedTask;

            try
            {
                element.DOKill();
                element.localScale = Vector3.one;

                float spin = UnityEngine.Random.Range(90f, 140f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);

                var seq = DOTween.Sequence()
                    .Append(element.DOScale(0.92f, RemoveBounceIn).SetEase(Ease.OutQuad))
                    .Append(element.DOScale(1f, RemoveBounceOut).SetEase(Ease.OutBack))
                    .Append(element.DOAnchorPos(toPos, RemoveFlyDur).SetEase(Ease.InQuad))
                    .Join(element.DOScale(RemoveScaleTo, RemoveFlyDur).SetEase(Ease.InCubic))
                    .Join(element.DORotate(new Vector3(0f, 0f, spin), RemoveFlyDur, RotateMode.FastBeyond360));

                return seq.AsyncWaitForCompletion().AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementAnimator] PlayRemove error: {ex}");
                return UniTask.CompletedTask;
            }
        }
        
        public UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 toPos)> moves)
        {
            if (moves == null || moves.Count == 0)
                return UniTask.CompletedTask;

            try
            {
                var seq = DOTween.Sequence();

                for (int i = 0; i < moves.Count; i++)
                {
                    var (rect, to) = moves[i];
                    if (!IsRectValid(rect)) continue;

                    rect.DOComplete();
                    seq.Join(rect.DOAnchorPos(to, CollapseMoveDur));
                }

                return seq.AsyncWaitForCompletion().AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElementAnimator] PlayCollapse error: {ex}");
                return UniTask.CompletedTask;
            }
        }
        
        private static bool IsRectValid(RectTransform rect)
        {
            return rect != null && rect.gameObject != null && rect.gameObject.activeInHierarchy;
        }

        private static void ResetTransform(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.rotation = Quaternion.identity;
        }
    }
}