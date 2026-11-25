using GameEnumDefines;
using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(110)]
public sealed class DaySummaryUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _orders;
    [SerializeField] private TMP_Text _served;
    [SerializeField] private TMP_Text _missed;
    [SerializeField] private TMP_Text _earned;
    [SerializeField] private TMP_Text _dayLabel;
    [SerializeField] private Button _nextDay;
    [SerializeField] private Button _lobby;

    private DayStats _stats;
    private GameClock _clock;
    private InputManager _input;
    private GameSession _session;
    private DataStorage _data;
    private InputState _previousInput;
    private float _previousTimeScale;
    private bool _isOpen;

    private void Start()
    {
        _stats = FindFirstObjectByType<DayStats>();
        _clock = FindFirstObjectByType<GameClock>();
        _input = FindFirstObjectByType<InputManager>();
        _session = FindFirstObjectByType<GameSession>();
        _data = FindFirstObjectByType<DataManager>()?.dataStorage;
        if (_stats == null || _clock == null)
        {
            return;
        }

        _stats.OnDaySummary += Open;
        _clock.OnDayStarted += RefreshDayLabel;
        RefreshDayLabel(_clock.Day);
        _nextDay.onClick.AddListener(NextDay);
        _lobby.onClick.AddListener(ReturnToLobby);
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDaySummary -= Open;
        }
        if (_clock != null)
        {
            _clock.OnDayStarted -= RefreshDayLabel;
        }
        RestorePlay();
    }

    private void Open(DayStats stats)
    {
        if (_isOpen || !_clock.IsDayEnded)
        {
            return;
        }

        _isOpen = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (_input != null)
        {
            _previousInput = _input.CurrentInputState;
            _input.SwitchInputState(InputState.UI);
        }

        _title.text = Format("STR_DaySummary_Title", _clock.Day);
        _orders.text = Format("STR_DaySummary_Orders", stats.OrderedCount);
        _served.text = Format("STR_DaySummary_Served", stats.ServedCount);
        _missed.text = Format("STR_DaySummary_Missed", stats.MissedCount);
        _earned.text = Format("STR_DaySummary_Earned", stats.EarnedGold);
        _lobby.gameObject.SetActive(_session != null);
        _panel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(_nextDay.gameObject);
        GameAudio.Play(SoundCue.PanelOpen);
    }

    private void NextDay()
    {
        if (!_isOpen)
        {
            return;
        }

        Close();
        _clock.StartNextDay();
    }

    private void ReturnToLobby()
    {
        if (_isOpen && _session != null && _session.TryReturnToLobby())
        {
            Close();
        }
    }

    private void Close()
    {
        RestorePlay();
        _panel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        GameAudio.Play(SoundCue.PanelClose);
    }

    private void RestorePlay()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        Time.timeScale = _previousTimeScale;
        if (_input != null && _input.CurrentInputState == InputState.UI)
        {
            _input.SwitchInputState(_previousInput);
        }
    }

    private string Format(string id, int value) => string.Format(_data.GetBBStringText(id), value);

    private void RefreshDayLabel(int day)
    {
        if (_dayLabel != null)
        {
            _dayLabel.text = Format("STR_DaySummary_Date", day);
        }
    }
}
