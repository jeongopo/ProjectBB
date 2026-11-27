using System;
using System.Collections.Generic;
using GameEnumDefines;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialoguePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _speaker;
    [SerializeField] private TMP_Text _body;
    [SerializeField] private Image _portrait;
    [SerializeField] private Button _next;
    [SerializeField] private TMP_Text _nextLabel;

    private readonly List<Button> _choices = new List<Button>();
    private readonly List<TMP_Text> _choiceLabels = new List<TMP_Text>();
    private GamePlay.InputManager _input;
    private InputAction _submit;
    private InputAction _cancel;
    private InputAction _navigate;
    private InputState _previousState;
    private DataManager _data;
    private Action<int> _advance;
    private Action _closed;
    private Vector2 _bodyPosition;
    private Vector2 _bodySize;
    private int _choiceCount;
    private int _selectedChoice;
    private int _openedFrame;
    private int _advancedFrame = -1;
    private bool _navigationHeld;
    private bool _waitingForGameplay;
    private CanvasGroup _canvasGroup;
    private Image _backdrop;

    public bool IsOpen => _advance != null;

    private void Awake()
    {
        _input = FindFirstObjectByType<GamePlay.InputManager>();
        _data = FindFirstObjectByType<DataManager>();
        InputActionMap map = _input != null ? _input.GetActionMap(InputState.UI) : null;
        _submit = map?.FindAction("Submit");
        _cancel = map?.FindAction("Cancel");
        _navigate = map?.FindAction("Navigate");
        _bodyPosition = _body.rectTransform.anchoredPosition;
        _bodySize = _body.rectTransform.sizeDelta;
        _body.richText = true;
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _backdrop = GetComponent<Image>();
        _next.onClick.AddListener(() => Advance(-1));
    }

    internal bool Open(string speakerKey, Action<int> advance, Action closed)
    {
        if (IsOpen)
        {
            return false;
        }

        gameObject.SetActive(true);
        _advance = advance;
        _closed = closed;
        _openedFrame = Time.frameCount;
        if (_input != null)
        {
            _previousState = _input.CurrentInputState;
            _input.SwitchInputState(InputState.UI);
        }

        EventSystem.current?.SetSelectedGameObject(null);
        transform.SetAsLastSibling();
        _speaker.text = GetText(speakerKey);
        return true;
    }

    internal void Close() => gameObject.SetActive(false);

    internal void SetSpeaker(string speakerKey) => _speaker.text = GetText(speakerKey);

    internal void ShowLine(string textKey, Sprite portrait, bool hasNext)
    {
        SetChoiceCount(0);
        _body.rectTransform.anchoredPosition = _bodyPosition;
        _body.rectTransform.sizeDelta = _bodySize;
        SetContent(textKey, portrait);
        _next.gameObject.SetActive(true);
        _nextLabel.text = GetText(hasNext ? "STR_Dialogue_Next" : "STR_Dialogue_Close");
    }

    internal void ShowChoices(string textKey, Sprite portrait, string[] labels)
    {
        SetChoiceCount(labels.Length);
        _next.gameObject.SetActive(false);
        _body.rectTransform.anchoredPosition = new Vector2(_bodyPosition.x, 32f);
        _body.rectTransform.sizeDelta = new Vector2(_bodySize.x, 20f);
        SetContent(textKey, portrait);

        float spacing = Mathf.Min(22f, 84f / Mathf.Max(1, labels.Length - 1));
        for (int i = 0; i < labels.Length; i++)
        {
            RectTransform rect = (RectTransform)_choices[i].transform;
            rect.sizeDelta = new Vector2(_bodySize.x, spacing - 2f);
            rect.anchoredPosition = new Vector2(_bodyPosition.x, 10f - i * spacing);
            _choiceLabels[i].text = GetText(labels[i]);
        }
        RefreshChoiceSelection();
    }

    internal void WaitForEvent()
    {
        SetChoiceCount(0);
        _next.gameObject.SetActive(false);
    }

    internal void WaitForGameplay(string textKey, Sprite portrait)
    {
        ShowLine(textKey, portrait, true);
        WaitForEvent();
        _waitingForGameplay = true;
        _canvasGroup.blocksRaycasts = false;
        if (_backdrop != null)
        {
            _backdrop.enabled = false;
        }
        if (_input != null)
        {
            _input.SwitchInputState(InputState.Default);
        }
    }

    internal void ResumeEventDialogue()
    {
        if (!_waitingForGameplay)
        {
            return;
        }

        _waitingForGameplay = false;
        _canvasGroup.blocksRaycasts = true;
        if (_backdrop != null)
        {
            _backdrop.enabled = true;
        }
        if (_input != null)
        {
            _input.SwitchInputState(InputState.UI);
        }
    }

    private void SetContent(string textKey, Sprite portrait)
    {
        _body.text = GetText(textKey);
        _portrait.sprite = portrait;
        _portrait.enabled = portrait != null;
    }

    private string GetText(string key) => _data?.dataStorage.GetBBStringText(key) ?? key;

    private void SetChoiceCount(int count)
    {
        while (_choices.Count < count)
        {
            int index = _choices.Count;
            Button button = Instantiate(_next, _next.transform.parent);
            button.name = $"ChoiceButton_{index + 1:00}";
            button.onClick.AddListener(() => Advance(index));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            RectTransform textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            _choices.Add(button);
            _choiceLabels.Add(label);
        }

        _choiceCount = count;
        _selectedChoice = 0;
        for (int i = 0; i < _choices.Count; i++)
        {
            _choices[i].gameObject.SetActive(i < count);
        }
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == _openedFrame)
        {
            return;
        }

        if (_waitingForGameplay)
        {
            bool canCancel = _input == null || _input.CurrentInputState == InputState.Default;
            if (canCancel && (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                Gamepad.current?.buttonEast.wasPressedThisFrame == true))
            {
                Close();
            }
            return;
        }

        if (_cancel?.WasPressedThisFrame() == true)
        {
            Close();
            return;
        }

        float vertical = _navigate?.ReadValue<Vector2>().y ?? 0f;
        bool navigating = Mathf.Abs(vertical) > 0.5f;
        if (_choiceCount > 0 && navigating && !_navigationHeld)
        {
            int step = vertical < 0f ? 1 : -1;
            _selectedChoice = (_selectedChoice + step + _choiceCount) % _choiceCount;
            RefreshChoiceSelection();
        }
        _navigationHeld = navigating;

        if (_submit?.WasPressedThisFrame() == true || Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            if (_choiceCount > 0)
            {
                Advance(_selectedChoice);
            }
            else if (_next.gameObject.activeSelf)
            {
                Advance(-1);
            }
        }
    }

    private void RefreshChoiceSelection()
    {
        for (int i = 0; i < _choiceCount; i++)
        {
            _choices[i].image.color = i == _selectedChoice
                ? new Color32(130, 80, 47, 255)
                : new Color32(65, 40, 29, 255);
        }
    }

    private void Advance(int choice)
    {
        if (!IsOpen || Time.frameCount == _openedFrame || Time.frameCount == _advancedFrame)
        {
            return;
        }

        _advancedFrame = Time.frameCount;
        _advance(choice);
    }

    private void OnDisable()
    {
        if (!IsOpen)
        {
            return;
        }

        _advance = null;
        _navigationHeld = false;
        _waitingForGameplay = false;
        _canvasGroup.blocksRaycasts = true;
        if (_backdrop != null)
        {
            _backdrop.enabled = true;
        }
        Action closed = _closed;
        _closed = null;
        closed?.Invoke();
        if (_input != null && _input.CurrentInputState == InputState.UI && gameObject.scene.isLoaded)
        {
            _input.SwitchInputState(_previousState);
        }
    }
}
