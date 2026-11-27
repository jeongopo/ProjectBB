using System;
using System.Collections.Generic;
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
        private const float IgnoreAvoidanceSeconds = 1.5f;
        private const float FrontDotThreshold = 0.3f;

        private static readonly List<CustomerController> ActiveCustomers = new List<CustomerController>();
        private static int _nextPriority;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [Tooltip("먼저 입장한 손님이 앞에 이 거리 안에 있으면 늦게 입장한 손님이 멈춰서 양보한다.")]
        [SerializeField] private float _personalSpace = 0.8f;
        [Tooltip("양보하며 이만큼 기다려도 비켜주지 않으면 그 손님 칸을 피해 가는 경로를 다시 찾는다.")]
        [SerializeField] private float _repathAfterSeconds = 0.4f;
        [Tooltip("이만큼 기다리면 교착으로 보고 잠시 양보를 무시하고 지나간다.")]
        [SerializeField] private float _maxWaitSeconds = 4f;

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
        private Vector3 _destination;
        private Action _onArrived;
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _detourBuffer = new List<Vector3>();
        private readonly HashSet<Vector3Int> _tempBlocked = new HashSet<Vector3Int>();
        private int _pathIndex;
        private int _priority;
        private bool _isMoving;
        private bool _triedRepath;
        private float _blockedTimer;
        private float _ignoreAvoidanceTimer;
        private Tween _feedbackTween;
        private Sequence _eatSequence;
        private int _maxPatienceMinutes;
        private int _remainingPatienceMinutes;
        private int _lastMinute;
        private Vector3 _baseScale;
        private RegularGuestSchedule _regular;

        public CustomerState State { get; private set; }
        public string CustomerId { get; private set; }
        public CustomerSeat Seat => _seat;
        public CustomerOrder Order { get; private set; }
        public bool IsRegular => _regular != null;
        public override bool CanInteract => base.CanInteract && State == CustomerState.Seated && (Order != null || _regular?.CanTalk == true);
        public override string PromptKey => Order != null ? "STR_Interact_Serve" : "STR_Interact_Talk";
        public float PatienceRatio => _maxPatienceMinutes > 0 ? (float)_remainingPatienceMinutes / _maxPatienceMinutes : 0f;
        public Sprite OrderIcon => _orderBoard != null && Order != null ? _orderBoard.GetOrderIcon(Order) : null;

        public event Action<CustomerController> OnLeft;
        public event Action<CustomerState> OnStateChanged;
        public event Action<float> OnPatienceChanged;
        public event Action OnOrderChanged;

        protected override void Awake()
        {
            base.Awake();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            ActiveCustomers.Add(this);
        }

        private void OnDisable()
        {
            ActiveCustomers.Remove(this);
        }

        private void Update()
        {
            if (_isMoving)
            {
                FollowPath();
            }
        }

        public void Init(DataStorage.Customer data, CustomerSeat seat, Vector3 exitPosition, GameClock clock, OrderBoard orderBoard)
        {
            _maxPatienceMinutes = Mathf.Max(1, data.PATIENCE_MINUTES);
            _remainingPatienceMinutes = _maxPatienceMinutes;
            BeginVisit(data.ID, seat, exitPosition, clock, orderBoard, false);
        }

        internal void InitRegular(RegularGuestSchedule regular, string id, CustomerSeat seat, Vector3 entrance, GameClock clock, OrderBoard orderBoard, bool alreadyArrived)
        {
            _regular = regular;
            BeginVisit(id, seat, entrance, clock, orderBoard, alreadyArrived);
        }

        private void BeginVisit(string id, CustomerSeat seat, Vector3 exitPosition, GameClock clock, OrderBoard orderBoard, bool alreadyArrived)
        {
            CustomerId = id;
            _orderBoard = orderBoard;
            _seat = seat;
            _exitPosition = exitPosition;
            _clock = clock;
            _priority = _nextPriority++;

            SetState(CustomerState.Entering);
            if (alreadyArrived)
            {
                transform.position = _seat.SitPosition;
                Sit();
            }
            else
            {
                BeginMove(_seat.SitPosition, Sit);
            }
        }

        internal bool TryIssueRegularOrder(string recipeId)
        {
            if (!IsRegular || State != CustomerState.Seated || Order != null || _orderBoard == null)
            {
                return false;
            }

            Order = _orderBoard.IssueOrder(this, recipeId);
            if (Order == null)
            {
                return false;
            }

            OnOrderChanged?.Invoke();
            return true;
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
            BeginMove(_exitPosition, FinishLeaving);
        }

        protected override void OnInteract()
        {
            if (State != CustomerState.Seated || _orderBoard == null)
            {
                return;
            }

            if (Order == null)
            {
                _regular?.Talk();
                return;
            }

            if (_orderBoard.TryServe(this) == ServeResult.Success)
            {
                Order = null;
                OnOrderChanged?.Invoke();
                _regular?.RecordServed();
                BeginEating();
                return;
            }

            PlayRejectFeedback();
        }

        private void OnDestroy()
        {
            KillActionTweens();
            UnsubscribeClock();
            CancelOrder();
            ReleaseSeat();
        }

        private void Sit()
        {
            if (!IsRegular)
            {
                _lastMinute = _clock.TotalMinutes;
                _clock.OnMinuteChanged += HandleMinuteChanged;
                Order = _orderBoard?.IssueOrder(this);
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
                .OnComplete(FinishEating)
                .SetLink(gameObject);
        }

        private void FinishEating()
        {
            if (IsRegular)
            {
                SetState(CustomerState.Seated);
            }
            else
            {
                Leave();
            }
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

        private void BeginMove(Vector3 destination, Action onArrived)
        {
            _destination = destination;
            _onArrived = onArrived;
            _pathIndex = 0;
            _triedRepath = false;
            _blockedTimer = 0f;
            _ignoreAvoidanceTimer = 0f;

            if (!CustomerPathfinder.TryFindPath(transform.position, destination, null, _path))
            {
                // 바닥 영역 밖이거나 길이 막혀 있으면 직선으로 간다.
                Debug.LogWarning($"[Customer] {name}: 경로를 찾지 못해 직선으로 이동합니다.", this);
                _path.Clear();
                _path.Add(destination);
            }

            _isMoving = true;
        }

        // 양보하며 멈추고 설 수 있어야 해서 트윈 대신 매 프레임 직접 이동한다.
        private void FollowPath()
        {
            if (_ignoreAvoidanceTimer > 0f)
            {
                _ignoreAvoidanceTimer -= Time.deltaTime;
            }

            Vector3 position = transform.position;
            Vector3 toTarget = _path[_pathIndex] - position;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;

            if (_ignoreAvoidanceTimer <= 0f)
            {
                CustomerController blocker = FindBlocker(position, direction);
                if (blocker != null)
                {
                    WaitForBlocker(blocker);
                    return;
                }
            }

            _blockedTimer = 0f;
            _triedRepath = false;

            float step = _moveSpeed * Time.deltaTime;
            if (distance > step)
            {
                transform.position = position + direction * step;
                return;
            }

            transform.position = _path[_pathIndex];
            _pathIndex++;
            if (_pathIndex >= _path.Count)
            {
                Arrive();
            }
        }

        // 먼저 입장한 손님(우선순위 숫자가 작은 쪽)에게만 양보하므로, 서로 마주 봐도 한 명은 반드시 먼저 지나간다.
        private CustomerController FindBlocker(Vector3 position, Vector3 direction)
        {
            if (direction == Vector3.zero)
            {
                return null;
            }

            float sqrSpace = _personalSpace * _personalSpace;
            foreach (CustomerController other in ActiveCustomers)
            {
                // 앉거나 식사 중인 손님은 의자 칸 안에 있고, 그 칸은 경로가 이미 피한다.
                if (other == this || !other._isMoving || other._priority > _priority)
                {
                    continue;
                }

                Vector3 offset = other.transform.position - position;
                if (offset.sqrMagnitude > sqrSpace)
                {
                    continue;
                }

                if (Vector2.Dot(direction, offset.normalized) >= FrontDotThreshold)
                {
                    return other;
                }
            }

            return null;
        }

        private void WaitForBlocker(CustomerController blocker)
        {
            _blockedTimer += Time.deltaTime;

            if (_blockedTimer >= _maxWaitSeconds)
            {
                _ignoreAvoidanceTimer = IgnoreAvoidanceSeconds;
                _blockedTimer = 0f;
                _triedRepath = false;
                return;
            }

            if (_blockedTimer >= _repathAfterSeconds && !_triedRepath)
            {
                _triedRepath = true;
                RepathAround(blocker);
            }
        }

        private void RepathAround(CustomerController blocker)
        {
            PlacementGrid placement = PlacementGrid.Instance;
            if (placement == null || placement.Grid == null)
            {
                return;
            }

            Vector3Int blockerCell = placement.Grid.WorldToCell(blocker.transform.position);
            blockerCell.z = 0;
            _tempBlocked.Clear();
            _tempBlocked.Add(blockerCell);

            // 돌아갈 길이 없으면 경로를 그대로 두고 계속 기다린다.
            if (CustomerPathfinder.TryFindPath(transform.position, _destination, _tempBlocked, _detourBuffer))
            {
                _path.Clear();
                _path.AddRange(_detourBuffer);
                _pathIndex = 0;
            }
        }

        private void Arrive()
        {
            _isMoving = false;
            Action onArrived = _onArrived;
            _onArrived = null;
            onArrived?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_isMoving)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 previous = transform.position;
            for (int i = _pathIndex; i < _path.Count; i++)
            {
                Gizmos.DrawLine(previous, _path[i]);
                previous = _path[i];
            }
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
                OnOrderChanged?.Invoke();
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
