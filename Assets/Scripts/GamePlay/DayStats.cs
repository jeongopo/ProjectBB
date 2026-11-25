using System;
using DG.Tweening;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 하루 동안의 주문, 서빙, 놓친 손님, 수입을 집계한다. 하루가 시작되면 초기화한다.
    /// </summary>
    [AddComponentMenu("GamePlay/DayStats")]
    [DisallowMultipleComponent]
    public class DayStats : MonoBehaviour
    {
        [SerializeField] private GameClock _clock = default;

        private Tween _summaryTween;

        public int OrderedCount { get; private set; }
        public int ServedCount { get; private set; }
        public int MissedCount { get; private set; }
        public int EarnedGold { get; private set; }

        public event Action OnChanged;
        public event Action<DayStats> OnDaySummary;

        private void OnEnable()
        {
            if (_clock == null)
            {
                _clock = FindFirstObjectByType<GameClock>();
            }

            if (_clock == null)
            {
                return;
            }

            _clock.OnDayStarted += HandleDayStarted;
            _clock.OnDayEnded += HandleDayEnded;
        }

        private void OnDisable()
        {
            _summaryTween?.Kill();

            if (_clock == null)
            {
                return;
            }

            _clock.OnDayStarted -= HandleDayStarted;
            _clock.OnDayEnded -= HandleDayEnded;
        }

        public void RecordOrdered()
        {
            OrderedCount++;
            OnChanged?.Invoke();
        }

        public void RecordServed(int gold)
        {
            ServedCount++;
            EarnedGold += gold;
            OnChanged?.Invoke();
        }

        public void RecordMissed()
        {
            MissedCount++;
            OnChanged?.Invoke();
        }

        private void HandleDayStarted(int day)
        {
            OrderedCount = 0;
            ServedCount = 0;
            MissedCount = 0;
            EarnedGold = 0;
            OnChanged?.Invoke();
        }

        // 마감 시 남은 손님이 떠나며 놓친 손님으로 집계되므로, 같은 이벤트 처리가 끝난 뒤에 요약을 낸다.
        private void HandleDayEnded(int day)
        {
            _summaryTween?.Kill();
            _summaryTween = DOVirtual.DelayedCall(0f, PublishSummary).SetLink(gameObject);
        }

        private void PublishSummary()
        {
            Debug.Log($"[DayStats] 주문 {OrderedCount} / 서빙 {ServedCount} / 놓침 {MissedCount} / 수입 {EarnedGold}");
            OnDaySummary?.Invoke(this);
        }
    }
}
