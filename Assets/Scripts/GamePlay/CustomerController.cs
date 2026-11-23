using System;
using DG.Tweening;
using UnityEngine;
using GameEnumDefines;
using Interaction;

namespace GamePlay
{
    /// <summary>
    /// 손님 한 명의 상태(입장, 착석, 식사, 퇴장)와 인내심을 관리한다. 인내심은 게임 내 분 단위라 GameClock이 멈추면 같이 멈춘다.
    /// 앉아 있는 동안 플레이어가 상호작용하면 서빙을 시도한다.
    /// </summary>
    public class CustomerController : InteractComponent
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Ease _moveEase = Ease.Linear;

        [Header("Serve Feedback")]
        [SerializeField] private float _eatDuration = 1f;
        [SerializeField] private float _servedPunchScale = 0.2f;
        [SerializeField] private float _servedPunchDuration = 0.3f;
        [SerializeField] private float _rejectShakeStrength = 0.1f;
        [SerializeField] private float _rejectShakeDuration = 0.25f;

        private GameClock _clock;
        private OrderBoard _orderBoard;
        private CustomerSeat _seat;
        private Vector3 _exitPosition;
        private Tween _moveTween;
        private Tween _feedbackTween;
        private Sequence _eatSequence;
        private int _maxPatienceMinutes;
        private int _remainingPatienceMinutes;
        private int _lastMinute;
        private Vector3 _baseScale;

        public CustomerState State { get; private set; }
        public string CustomerId { get; private set; }
        public CustomerSeat Seat => _seat;
        public CustomerOrder Order { get; private set; }
        public float PatienceRatio => _maxPatienceMinutes > 0 ? (float)_remainingPatienceMinutes / _maxPatienceMinutes : 0f;
        public Sprite OrderIcon => _orderBoard != null && Order != null ? _orderBoard.GetOrderIcon(Order) : null;

        public event Action<CustomerController> OnLeft;
        public event Action<CustomerState> OnStateChanged;
        public event Action<float> OnPatienceChanged;

        protected override void Awake()
        {
            base.Awake();
            _baseScale = transform.localScale;
        }

        public void Init(DataStorage.Customer data, CustomerSeat seat, Vector3 exitPosition, GameClock clock, OrderBoard orderBoard)
        {
            CustomerId = data.ID;
            _orderBoard = orderBoard;
            _maxPatienceMinutes = Mathf.Max(1, data.PATIENCE_MINUTES);
            _remainingPatienceMinutes = _maxPatienceMinutes;
            _seat = seat;
            _exitPosition = exitPosition;
            _clock = clock;

            SetState(CustomerState.Entering);
            MoveTo(_seat.SitPosition, Sit);
        }

        public void Leave()
        {
            if (State == CustomerState.Leaving)
            {
                return;
            }

            UnsubscribeClock();
            KillActionTweens();
            CancelOrder();
            ReleaseSeat();
            SetState(CustomerState.Leaving);
            MoveTo(_exitPosition, FinishLeaving);
        }

        protected override void OnInteract()
        {
            if (State != CustomerState.Seated || _orderBoard == null)
            {
                return;
            }

            if (_orderBoard.TryServe(this) == ServeResult.Success)
            {
                Order = null;
                BeginEating();
                return;
            }

            PlayRejectFeedback();
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            KillActionTweens();
            UnsubscribeClock();
            CancelOrder();
            ReleaseSeat();
        }

        private void Sit()
        {
            _lastMinute = _clock.TotalMinutes;
            _clock.OnMinuteChanged += HandleMinuteChanged;

            if (_orderBoard != null)
            {
                Order = _orderBoard.IssueOrder(this);
            }

            SetState(CustomerState.Seated);
            OnPatienceChanged?.Invoke(PatienceRatio);
        }

        private void BeginEating()
        {
            UnsubscribeClock();
            KillActionTweens();
            SetState(CustomerState.Eating);

            _eatSequence = DOTween.Sequence()
                .Append(transform.DOPunchScale(Vector3.one * _servedPunchScale, _servedPunchDuration))
                .AppendInterval(_eatDuration)
                .OnComplete(Leave)
                .SetLink(gameObject);
        }

        private void PlayRejectFeedback()
        {
            _feedbackTween?.Kill(true);
            _feedbackTween = transform.DOShakePosition(_rejectShakeDuration, _rejectShakeStrength)
                .SetLink(gameObject);
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
            OnPatienceChanged?.Invoke(Mathf.Max(0f, PatienceRatio));
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

        private void SetState(CustomerState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
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

        // 식사 연출은 완료 처리(Kill(true))하면 OnComplete가 불려 Leave가 재귀되므로, 그냥 끊고 크기만 되돌린다.
        private void KillActionTweens()
        {
            if (_eatSequence != null && _eatSequence.IsActive())
            {
                _eatSequence.Kill();
                transform.localScale = _baseScale;
            }

            _feedbackTween?.Kill(true);
        }

        private void CancelOrder()
        {
            if (_orderBoard != null && Order != null)
            {
                _orderBoard.CancelOrder(this);
                Order = null;
            }
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
