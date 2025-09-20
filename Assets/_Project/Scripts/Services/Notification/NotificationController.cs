using System;
using _Project.Scripts.Installers.Structures;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Notification
{
    public class NotificationController : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private TMP_Text _outputText;

        [Header("Timings")] [SerializeField] private float _fadeInSeconds = 0.15f;
        [SerializeField] private float _holdSeconds = 1.4f;
        [SerializeField] private float _fadeOutSeconds = 0.15f;
        [SerializeField] private bool _ignoreTimeScale = true;

        private SignalBus _signalBus;
        private Sequence _sequence;

        private Action<ElementPlaced> _onPlacedHandler;
        private Action<ElementMissed> _onMissedHandler;
        private Action<ElementDeleted> _onDeletedHandler;
        private Action<HeightLimitReached> _onHeightHandler;

        [Inject]
        public void Construct(SignalBus bus) => _signalBus = bus;

        private void Awake()
        {
            try
            {
                InitializeTextSafely();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationController] Awake failed: {ex}");
            }
        }

        private void OnEnable()
        {
            try
            {
                ValidateSerializedFields();
                SubscribeSignals();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationController] OnEnable failed: {ex}");
            }
        }

        private void OnDisable()
        {
            try
            {
                UnsubscribeSignals();
                StopAnimation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationController] OnDisable failed: {ex}");
            }
        }

        private void Show(string text)
        {
            if (_outputText == null || string.IsNullOrEmpty(text))
                return;

            try
            {
                StopAnimation();
                PrepareOutput(text);
                PlayShowAnimation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationController] Show failed: {ex}");
                _outputText.text = string.Empty;
            }
        }

        private void SubscribeSignals()
        {
            if (_signalBus == null)
                return;

            _onPlacedHandler = _ => Show(MessageNotifications.Get(MessageNotifications.Placed));
            _onMissedHandler = _ => Show(MessageNotifications.Get(MessageNotifications.Missed));
            _onDeletedHandler = _ => Show(MessageNotifications.Get(MessageNotifications.Deleted));
            _onHeightHandler = _ => Show(MessageNotifications.Get(MessageNotifications.HeightLimit));

            _signalBus.Subscribe(_onPlacedHandler);
            _signalBus.Subscribe(_onMissedHandler);
            _signalBus.Subscribe(_onDeletedHandler);
            _signalBus.Subscribe(_onHeightHandler);
        }

        private void UnsubscribeSignals()
        {
            if (_signalBus == null)
                return;

            if (_onPlacedHandler != null) _signalBus.TryUnsubscribe(_onPlacedHandler);
            if (_onMissedHandler != null) _signalBus.TryUnsubscribe(_onMissedHandler);
            if (_onDeletedHandler != null) _signalBus.TryUnsubscribe(_onDeletedHandler);
            if (_onHeightHandler != null) _signalBus.TryUnsubscribe(_onHeightHandler);
        }

        private void InitializeTextSafely()
        {
            if (_outputText != null)
                _outputText.text = string.Empty;
        }

        private void PrepareOutput(string text)
        {
            _outputText.gameObject.SetActive(true);
            _outputText.enabled = true;
            _outputText.text = text;

            var color = _outputText.color;
            color.a = 0f;
            _outputText.color = color;
        }

        private void PlayShowAnimation()
        {
            _sequence = DOTween.Sequence().SetUpdate(_ignoreTimeScale);

            _sequence.Append(_outputText.DOFade(1f, _fadeInSeconds));
            if (_holdSeconds > 0f)
                _sequence.AppendInterval(_holdSeconds);
            _sequence.Append(_outputText.DOFade(0f, _fadeOutSeconds));

            _sequence.OnComplete(() =>
            {
                _outputText.text = string.Empty;
                _sequence = null;
            });
        }

        private void StopAnimation()
        {
            if (_sequence == null) return;
            try
            {
                _sequence.Kill();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NotificationController] Kill warn: {ex}");
            }
            finally
            {
                _sequence = null;
            }
        }

        private void ValidateSerializedFields()
        {
            if (_outputText == null)
                throw new InvalidOperationException("OutputText is not assigned.");
        }
    }
}