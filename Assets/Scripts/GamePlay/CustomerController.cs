using System;
using DG.Tweening;
using UnityEngine;
using GameEnumDefines;

namespace GamePlay
{
    /// <summary>
    /// 손님 한 명의 상태(입장, 착석, 퇴장)와 인내심을 관리한다. 인내심은 게임 내 분 단위라 GameClock이 멈추면 같이 멈춘다.
    /// </summary>
    public class CustomerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Ease _moveEase = Ease.Linear;

        private GameClock _clock;
        private CustomerSeat _seat;
        private Vector3 _exitPosition;
        private Tween _moveTween;
        private int _maxPatienceMinutes;
        private int _remainingPatienceMinutes;
        private int _lastMinute;

        public CustomerState State { get; private set; }
        public string CustomerId { get; private set; }
        public CustomerSeat Seat => _seat;
        public float PatienceRatio => _maxPatienceMinutes > 0 ? (float)_remainingPatienceMinutes / _maxPatienceMinutes : 0f;

        public event Action<CustomerController> OnLeft;

        public void Init(DataStorage.Customer data, CustomerSeat seat, Vector3 exitPosition, GameClock clock)
        {
            CustomerId = data.ID;
            _maxPatienceMinutes = Mathf.Max(1, data.PATIENCE_MINUTES);
            _remainingPatienceMinutes = _maxPatienceMinutes;
            _seat = seat;
            _exitPosition = exitPosition;
            _clock = clock;

            State = CustomerState.Entering;
            MoveTo(_seat.SitPosition, Sit);
        }

        public void Leave()
        {
            if (State == CustomerState.Leaving)
            {
                return;
            }

            State = CustomerState.Leaving;
            UnsubscribeClock();
            ReleaseSeat();
            MoveTo(_exitPosition, FinishLeaving);
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            UnsubscribeClock();
            ReleaseSeat();
        }

        private void Sit()
        {
            State = CustomerState.Seated;
            _lastMinute = _clock.TotalMinutes;
            _clock.OnMinuteChanged += HandleMinuteChanged;
        }

        private void HandleMinuteChanged(int totalMinutes)
        {
            int elapsed = totalMinutes - _lastMinute;
            _lastMinute = totalMinutes;
            if (elapsed <= 0)
            {
                return;
            }

            _remainingPatienceMinutes -= elapsed;
            if (_remainingPatienceMinutes <= 0)
            {
                Leave();
            }
        }

        private void FinishLeaving()
        {
            OnLeft?.Invoke(this);
            Destroy(gameObject);
        }

        private void MoveTo(Vector3 destination, TweenCallback onArrived)
        {
            _moveTween?.Kill();

            float duration = Vector3.Distance(transform.position, destination) / Mathf.Max(0.01f, _moveSpeed);
            _moveTween = transform.DOMove(destination, duration)
                .SetEase(_moveEase)
                .SetLink(gameObject)
                .OnComplete(onArrived);
        }

        private void ReleaseSeat()
        {
            if (_seat != null)
            {
                _seat.Release(this);
            }
        }

        private void UnsubscribeClock()
        {
            if (_clock != null)
            {
                _clock.OnMinuteChanged -= HandleMinuteChanged;
            }
        }
    }
}
