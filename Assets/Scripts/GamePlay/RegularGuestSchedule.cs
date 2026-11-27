using System.Collections;
using UnityEngine;

namespace GamePlay
{
    public sealed class RegularGuestSchedule : MonoBehaviour
    {
        [Header("Guest")]
        [SerializeField] private string _guestId;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _dialogueKey;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private Sprite _worldSprite;
        [SerializeField] private string _recipeId;

        [Header("Schedule")]
        [SerializeField, Min(0)] private int _visitDay;
        [SerializeField, Range(0, 1439)] private int _arrivalMinute = 840;
        [SerializeField, Range(0, 1439)] private int _orderMinute = 900;
        [SerializeField, Range(1, 1440)] private int _departureMinute = 960;

        [Header("Scene")]
        [SerializeField] private CustomerController _customerPrefab;
        [SerializeField] private CustomerSeat _seat;
        [SerializeField] private Transform _entrance;

        private GameClock _clock;
        private OrderBoard _orders;
        private GameSession _session;
        private DataStorage _strings;
        private DialoguePanel _dialogue;
        private RegularGuestPanel _panel;
        private CustomerController _visitor;
        private int _orderDay;
        private int _arrivalDay;
        private int _affinity;
        private bool _talking;
        private bool _ready;

        internal bool CanTalk => !string.IsNullOrEmpty(_dialogueKey) && _dialogue != null && !_dialogue.IsOpen;

        private IEnumerator Start()
        {
            yield return null;
            _clock = FindFirstObjectByType<GameClock>();
            _orders = FindFirstObjectByType<OrderBoard>();
            _session = FindFirstObjectByType<GameSession>();
            _strings = FindFirstObjectByType<DataManager>()?.dataStorage;
            _dialogue = FindFirstObjectByType<DialoguePanel>(FindObjectsInactive.Include);
            _panel = FindFirstObjectByType<RegularGuestPanel>();
            _affinity = _session != null ? _session.GetGuestAffinity(_guestId) : 0;
            _ready = _clock != null && _orders != null && _seat != null && _entrance != null &&
                _customerPrefab != null && !string.IsNullOrEmpty(_guestId);
            if (_ready && isActiveAndEnabled)
            {
                _clock.OnMinuteChanged += HandleMinuteChanged;
                RefreshVisit(true);
            }
        }

        private void OnEnable()
        {
            if (_ready)
            {
                _clock.OnMinuteChanged += HandleMinuteChanged;
                RefreshVisit(true);
            }
        }

        private void OnDisable()
        {
            if (_clock != null)
            {
                _clock.OnMinuteChanged -= HandleMinuteChanged;
            }

            if (_talking && _dialogue != null)
            {
                _dialogue.Close();
            }

            if (_visitor != null)
            {
                _visitor.OnLeft -= HandleLeft;
                _visitor.OnOrderChanged -= UpdateCard;
                Destroy(_visitor.gameObject);
                _visitor = null;
            }

            if (_panel != null)
            {
                _panel.RemoveGuest(_guestId);
            }
        }

        internal void Talk()
        {
            if (CanTalk && _dialogue.Open(_nameKey, _ => _dialogue.Close(), FinishTalking))
            {
                _talking = true;
                _dialogue.ShowLine(_dialogueKey, _portrait, false);
            }
        }

        internal void RecordServed()
        {
            _affinity = _session != null ? _session.IncreaseGuestAffinity(_guestId) : _affinity + 1;
            UpdateCard();
        }

        private void FinishTalking()
        {
            _talking = false;
            if (isActiveAndEnabled)
            {
                RefreshVisit(false);
            }
        }

        private void HandleMinuteChanged(int _) => RefreshVisit(false);

        private void RefreshVisit(bool placeDirectly)
        {
            if (_talking)
            {
                return;
            }

            int day = _clock.Day;
            int minute = _clock.TotalMinutes;
            bool scheduled = !_clock.IsDayEnded && (_visitDay == 0 || _visitDay == day) &&
                minute >= _arrivalMinute && minute < _departureMinute;
            if (!scheduled || (_visitor != null && _arrivalDay != day))
            {
                if (_visitor != null)
                {
                    _visitor.Leave();
                }
                return;
            }

            if (_visitor == null && !_seat.IsOccupied)
            {
                Spawn(day, placeDirectly);
            }

            if (_visitor != null && _orderDay != day && minute >= _orderMinute && !string.IsNullOrEmpty(_recipeId) &&
                _visitor.TryIssueRegularOrder(_recipeId))
            {
                _orderDay = day;
            }
        }

        private void Spawn(int day, bool placeDirectly)
        {
            CustomerController visitor = Instantiate(_customerPrefab, _entrance.position, Quaternion.identity);
            if (!_seat.TryReserve(visitor))
            {
                Destroy(visitor.gameObject);
                return;
            }

            _visitor = visitor;
            _arrivalDay = day;
            if (_worldSprite != null && visitor.InteractionVisuals.Length > 0)
            {
                visitor.InteractionVisuals[0].sprite = _worldSprite;
            }
            visitor.OnLeft += HandleLeft;
            visitor.OnOrderChanged += UpdateCard;
            visitor.InitRegular(this, _guestId, _seat, _entrance.position, _clock, _orders, placeDirectly);
            GameAudio.Play(SoundCue.CustomerArrival);
            UpdateCard();
        }

        private void HandleLeft(CustomerController visitor)
        {
            visitor.OnLeft -= HandleLeft;
            visitor.OnOrderChanged -= UpdateCard;
            _visitor = null;
            if (_panel != null)
            {
                _panel.RemoveGuest(_guestId);
            }
        }

        private void UpdateCard()
        {
            if (_panel == null || _visitor == null)
            {
                return;
            }

            string affinity = string.Format(_strings.GetBBStringText("STR_RegularGuest_Affinity"), _affinity);
            _panel.ShowGuest(_guestId, _strings.GetBBStringText(_nameKey), _portrait, affinity, _visitor.OrderIcon);
        }
    }
}
