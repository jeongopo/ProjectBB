using System;
using UnityEngine;
using GameEnumDefines;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace GamePlay
{
    [Serializable]
    public struct DayPhaseSetting
    {
        [SerializeField] private DayPhase _phase;
        [SerializeField] private int _startHour;
        [SerializeField] private float _realSecondsPerGameHour;

        public DayPhase Phase => _phase;
        public int StartHour => _startHour;
        public float RealSecondsPerGameHour => _realSecondsPerGameHour;

        public DayPhaseSetting(DayPhase phase, int startHour, float realSecondsPerGameHour)
        {
            _phase = phase;
            _startHour = startHour;
            _realSecondsPerGameHour = realSecondsPerGameHour;
        }
    }

    /// <summary>
    /// 게임 내 시간과 일차를 관리한다. 하루는 시작 시각에서 종료 시각까지 흐르고, 구간(DayPhase)별로 흐르는 속도가 다르다.
    /// </summary>
    [AddComponentMenu("GamePlay/GameClock")]
    [DisallowMultipleComponent]
    public class GameClock : MonoBehaviour
    {
        private const int MinutesPerHour = 60;

        [Header("References")]
        [SerializeField] private GameManager _gameManager = default;
        [SerializeField] private InputManager _inputManager = default;

        [Header("Day")]
        [SerializeField] private int _dayStartHour = 0;
        [SerializeField] private int _dayEndHour = 24;

        [Header("Phases")]
        // StartHour 오름차순으로 넣어야 한다. 현재 시각 이하의 마지막 StartHour를 가진 구간이 현재 구간이 된다.
        [SerializeField] private DayPhaseSetting[] _phases =
        {
            new DayPhaseSetting(DayPhase.Hunting, 0, 60f),
            new DayPhaseSetting(DayPhase.Business, 18, 120f),
        };

        [Header("Options")]
        [SerializeField] private bool _pauseDuringMinigame = true;

        [Header("Debug")]
        [SerializeField] private float _debugFastForwardMultiplier = 20f;

        private int _day = 1;
        private float _totalMinutes;
        private int _lastNotifiedMinute;
        private int _phaseIndex;
        private bool _isDayEnded;
        private float _speedMultiplier = 1f;

        public int Day => _day;
        public int TotalMinutes => (int)_totalMinutes;
        public int Hour => TotalMinutes / MinutesPerHour;
        public int Minute => TotalMinutes % MinutesPerHour;
        public DayPhase CurrentPhase => _phases[_phaseIndex].Phase;
        public bool IsDayEnded => _isDayEnded;
        public bool IsRunning => !_isDayEnded && !IsPausedByState();

        public event Action<int> OnMinuteChanged;
        public event Action<int> OnHourChanged;
        public event Action<DayPhase> OnPhaseChanged;
        public event Action<int> OnDayStarted;
        public event Action<int> OnDayEnded;

        private void Start()
        {
            if (_phases == null || _phases.Length == 0)
            {
                Debug.LogError("GameClock: 시간 구간(Phases)이 설정되지 않았습니다.", this);
                enabled = false;
                return;
            }

            BeginDay(1, _dayStartHour * MinutesPerHour);
        }

        private void Update()
        {
            HandleDebugInput();

            if (!IsRunning)
            {
                return;
            }

            AdvanceTime(Time.deltaTime);
        }

        public void StartNextDay()
        {
            BeginDay(_day + 1, _dayStartHour * MinutesPerHour);
        }

        public void ReadFrom(SaveData data)
        {
            if (data._currentDay <= 0)
            {
                BeginDay(1, _dayStartHour * MinutesPerHour);
                return;
            }

            int minutes = Mathf.Clamp(data._currentTime, _dayStartHour * MinutesPerHour, _dayEndHour * MinutesPerHour - 1);
            BeginDay(data._currentDay, minutes);
        }

        public void WriteTo(SaveData data)
        {
            data._currentDay = _day;
            data._currentTime = TotalMinutes;
        }

        internal void WriteNextDayTo(SaveData data)
        {
            data._currentDay = _day + 1;
            data._currentTime = _dayStartHour * MinutesPerHour;
        }

        private void BeginDay(int day, int startMinutes)
        {
            _day = Mathf.Max(1, day);
            _isDayEnded = false;
            _totalMinutes = startMinutes;
            _lastNotifiedMinute = startMinutes;
            _phaseIndex = GetPhaseIndex(startMinutes);

            OnDayStarted?.Invoke(_day);
            OnMinuteChanged?.Invoke(startMinutes);
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void AdvanceTime(float deltaSeconds)
        {
            float secondsPerHour = Mathf.Max(0.01f, _phases[_phaseIndex].RealSecondsPerGameHour);
            AdvanceTo(_totalMinutes + deltaSeconds * MinutesPerHour / secondsPerHour * _speedMultiplier);
        }

        private void AdvanceTo(float minutes)
        {
            float endMinutes = _dayEndHour * MinutesPerHour;
            bool reachedEnd = minutes >= endMinutes;
            _totalMinutes = Mathf.Min(minutes, endMinutes);
            if (reachedEnd)
            {
                _isDayEnded = true;
            }

            NotifyTimeChanged();

            if (reachedEnd)
            {
                OnDayEnded?.Invoke(_day);
            }
        }

        private void NotifyTimeChanged()
        {
            int minute = TotalMinutes;
            if (minute == _lastNotifiedMinute)
            {
                return;
            }

            int previousHour = _lastNotifiedMinute / MinutesPerHour;
            _lastNotifiedMinute = minute;
            OnMinuteChanged?.Invoke(minute);

            int currentHour = minute / MinutesPerHour;
            for (int hour = previousHour + 1; hour <= currentHour; hour++)
            {
                OnHourChanged?.Invoke(hour);
            }

            int phaseIndex = GetPhaseIndex(minute);
            if (phaseIndex != _phaseIndex)
            {
                _phaseIndex = phaseIndex;
                OnPhaseChanged?.Invoke(CurrentPhase);
            }
        }

        private int GetPhaseIndex(int minutes)
        {
            int hour = minutes / MinutesPerHour;
            int index = 0;
            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i].StartHour <= hour)
                {
                    index = i;
                }
            }

            return index;
        }

        private bool IsPausedByState()
        {
            if (_gameManager != null && _gameManager.CurrentGameState != GameState.Gameplay)
            {
                return true;
            }

            if (_pauseDuringMinigame && _inputManager != null && _inputManager.CurrentInputState == InputState.Minigame)
            {
                return true;
            }

            return false;
        }

        private void HandleDebugInput()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _speedMultiplier = Mathf.Approximately(_speedMultiplier, 1f) ? _debugFastForwardMultiplier : 1f;
            }
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame && CanAdvanceDebugTime())
            {
                AdvanceTo(_dayEndHour * MinutesPerHour);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void AdvanceOneHourForTesting()
        {
            if (CanAdvanceDebugTime())
            {
                AdvanceTo(_totalMinutes + MinutesPerHour);
            }
        }

        private bool CanAdvanceDebugTime() => isActiveAndEnabled && IsRunning && Time.timeScale > 0f &&
            (_inputManager == null || _inputManager.CurrentInputState == InputState.Default);
#endif
    }
}
