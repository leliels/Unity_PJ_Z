using System.Collections;
using BlockPuzzle.Core;
using BlockPuzzle.Save;
using UnityEngine;

namespace BlockPuzzle.Feedback
{
    public class FeedbackManager : Singleton<FeedbackManager>
    {
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private float _defaultDuration = 0.45f;
        [SerializeField] private float _defaultAmplitude = 0.12f;
        [SerializeField] private float _frequency = 32f;

        private Coroutine _shakeCoroutine;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
                DontDestroyOnLoad(gameObject);
        }

        public void SetShakeTarget(Transform target)
        {
            _shakeTarget = target;
        }

        public void PlayLight()
        {
            Trigger(0.25f, 0.06f, false);
        }

        public void PlayMedium()
        {
            Trigger(_defaultDuration, _defaultAmplitude, true);
        }

        public void Trigger(float duration, float amplitude, bool mobileVibrate = true)
        {
            var settings = SaveManager.Instance != null ? SaveManager.Instance.GetSettings() : null;
            if (settings != null && !settings.vibrationEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            if (mobileVibrate)
                Handheld.Vibrate();
#endif

            Transform target = ResolveShakeTarget();
            if (target == null) return;

            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeCoroutine(target, Mathf.Max(0.01f, duration), Mathf.Max(0f, amplitude)));
        }

        private Transform ResolveShakeTarget()
        {
            if (_shakeTarget != null) return _shakeTarget;
            var boardScaleRoot = GameObject.Find("BoardScaleRoot");
            if (boardScaleRoot != null)
            {
                _shakeTarget = boardScaleRoot.transform;
                return _shakeTarget;
            }
            return null;
        }

        private IEnumerator ShakeCoroutine(Transform target, float duration, float amplitude)
        {
            Vector3 origin = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float strength = amplitude * (1f - t);
                float x = Mathf.Sin(elapsed * _frequency) * strength;
                float y = Mathf.Cos(elapsed * _frequency * 1.37f) * strength;
                target.localPosition = origin + new Vector3(x, y, 0f);
                yield return null;
            }

            if (target != null)
                target.localPosition = origin;
            _shakeCoroutine = null;
        }
    }
}
