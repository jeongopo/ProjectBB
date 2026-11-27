using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEnumDefines;
using Interaction;

namespace GamePlay
{
    /// <summary>
    /// 영업 구간 동안 일정 간격(게임 내 분)으로 손님을 입장시키고, 하루가 끝나면 남은 손님을 내보낸다.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameClock _clock = default;
        [SerializeField] private CustomerController _customerPrefab = default;
        [SerializeField] private Transform _entrance = default;
        [SerializeField] private OrderBoard _orderBoard = default;

        [Header("Spawn")]
        [SerializeField] private int _spawnIntervalMinutes = 10;

        [Header("Test")]
        [Tooltip("0 이상이면 영업 구간과 상관없이 이 시각부터 스폰한다. 테스트용이라 실제 빌드에서는 -1로 끈다.")]
        [SerializeField] private int _testSpawnStartHour = 1;
        [Tooltip("TestScene의 첫 손님을 진입 직후 입장시킨다. 에디터와 개발 빌드에서만 적용한다.")]
        [SerializeField] private bool _spawnImmediatelyForTesting = true;

        private readonly List<CustomerController> _activeCustomers = new List<CustomerController>();
        private readonly List<CustomerSeat> _freeSeats = new List<CustomerSeat>();
        private CustomerSeat[] _seats;
        private DataManager _dataManager;
        private List<DataStorage.Customer> _customerPool;
        private bool _isBusinessOpen;
        private bool _wasOpen;
        private bool _ready;
        private int _minutesUntilSpawn;
        private int _lastMinute;

        private IEnumerator Start()
        {
            _seats = FindObjectsByType<CustomerSeat>(FindObjectsSortMode.None);
            _dataManager = FindFirstObjectByType<DataManager>();
            MainCharacter player = FindFirstObjectByType<MainCharacter>();
            if (player != null)
            {
                foreach (Furniture table in Furniture.ActiveFurniture)
                {
                    if (table.gameObject.scene == gameObject.scene && table.name.StartsWith("DiningTable", System.StringComparison.Ordinal))
                    {
                        TableServingPoint.Install(table, _seats, player.transform);
                    }
                }
            }

            if (_orderBoard == null)
            {
                _orderBoard = FindFirstObjectByType<OrderBoard>();
            }

            // 데이터 로드와 이어하기의 시계 복원이 끝난 뒤 처음 입장시킨다.
            yield return null;
            if (_clock == null || !_clock.isActiveAndEnabled)
            {
                yield break;
            }

            _ready = true;
            _lastMinute = _clock.TotalMinutes;
            if (IsImmediateTestEnabled() && IsOpen())
            {
                _wasOpen = true;
                _minutesUntilSpawn = Mathf.Max(1, _spawnIntervalMinutes);
                TrySpawn();
            }
        }

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

            _lastMinute = _clock.TotalMinutes;
            _clock.OnMinuteChanged += HandleMinuteChanged;
            _clock.OnPhaseChanged += HandlePhaseChanged;
            _clock.OnDayStarted += HandleDayStarted;
            _clock.OnDayEnded += HandleDayEnded;
        }

        private void OnDisable()
        {
            if (_clock == null)
            {
                return;
            }

            _clock.OnMinuteChanged -= HandleMinuteChanged;
            _clock.OnPhaseChanged -= HandlePhaseChanged;
            _clock.OnDayStarted -= HandleDayStarted;
            _clock.OnDayEnded -= HandleDayEnded;
        }

        private void HandlePhaseChanged(DayPhase phase)
        {
            _isBusinessOpen = phase == DayPhase.Business;
        }

        private void HandleDayStarted(int day)
        {
            _lastMinute = _clock.TotalMinutes;
            _wasOpen = false;
        }

        private void HandleDayEnded(int day)
        {
            _isBusinessOpen = false;

            foreach (CustomerController customer in _activeCustomers.ToArray())
            {
                customer.Leave();
            }
        }

        private void HandleMinuteChanged(int totalMinutes)
        {
            int elapsed = totalMinutes - _lastMinute;
            _lastMinute = totalMinutes;
            if (!_ready)
            {
                return;
            }

            bool isOpen = IsOpen();
            if (isOpen && !_wasOpen)
            {
                _minutesUntilSpawn = 0;
            }

            _wasOpen = isOpen;
            if (!isOpen || elapsed <= 0)
            {
                return;
            }

            _minutesUntilSpawn -= elapsed;
            while (_minutesUntilSpawn <= 0)
            {
                TrySpawn();
                _minutesUntilSpawn += Mathf.Max(1, _spawnIntervalMinutes);
            }
        }

        private bool IsOpen()
        {
            if (_clock.IsDayEnded)
            {
                return false;
            }

            if (_isBusinessOpen)
            {
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _testSpawnStartHour >= 0 && (IsImmediateTestEnabled() || _clock.Hour >= _testSpawnStartHour);
#else
            return false;
#endif
        }

        private bool IsImmediateTestEnabled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _spawnImmediatelyForTesting && _testSpawnStartHour >= 0 && gameObject.scene.name == "TestScene";
#else
            return false;
#endif
        }

        private void TrySpawn()
        {
            if (_customerPrefab == null || _entrance == null)
            {
                return;
            }

            DataStorage.Customer data = PickCustomerData();
            CustomerSeat seat = PickFreeSeat();
            if (data == null || seat == null)
            {
                return;
            }

            CustomerController customer = Instantiate(_customerPrefab, _entrance.position, Quaternion.identity);
            if (!seat.TryReserve(customer))
            {
                Destroy(customer.gameObject);
                return;
            }

            customer.OnLeft += HandleCustomerLeft;
            _activeCustomers.Add(customer);
            customer.Init(data, seat, _entrance.position, _clock, _orderBoard);
            GameAudio.Play(SoundCue.CustomerArrival);
        }

        private void HandleCustomerLeft(CustomerController customer)
        {
            customer.OnLeft -= HandleCustomerLeft;
            _activeCustomers.Remove(customer);
        }

        private DataStorage.Customer PickCustomerData()
        {
            if (_customerPool == null)
            {
                Dictionary<string, DataStorage.Customer> table = _dataManager != null ? _dataManager.dataStorage?.CustomerData : null;
                if (table == null || table.Count == 0)
                {
                    return null;
                }

                _customerPool = new List<DataStorage.Customer>(table.Values);
            }

            return _customerPool[Random.Range(0, _customerPool.Count)];
        }

        private CustomerSeat PickFreeSeat()
        {
            _freeSeats.Clear();
            foreach (CustomerSeat seat in _seats)
            {
                if (seat != null && !seat.IsOccupied && !seat.ReservedForRegulars)
                {
                    _freeSeats.Add(seat);
                }
            }

            return _freeSeats.Count > 0 ? _freeSeats[Random.Range(0, _freeSeats.Count)] : null;
        }
    }
}
