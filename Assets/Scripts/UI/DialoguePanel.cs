using GameEnumDefines;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialoguePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _speaker;
    [SerializeField] private TMP_Text _body;
    [SerializeField] private Image _portrait;
    [SerializeField] private Button _next;
    [SerializeField] private TMP_Text _nextLabel;

    private GamePlay.InputManager _input;
    private InputAction _submit;
    private InputAction _cancel;
    private InputState _previousState;
    private DataManager _data;
    private string[] _lines;
    private int _index;
    private int _openedFrame;

    public bool IsOpen => _lines != null;

    private void Awake()
    {
        _input = FindFirstObjectByType<GamePlay.InputManager>();
        _data = FindFirstObjectByType<DataManager>();
        InputActionMap map = _input != null ? _input.GetActionMap(InputState.UI) : null;
        _submit = map?.FindAction("Submit");
        _cancel = map?.FindAction("Cancel");
        _next.onClick.AddListener(Advance);
    }

    public void Open(string speakerKey, Sprite portrait, string[] lines)
    {
        if (IsOpen || lines == null || lines.Length == 0)
        {
            return;
        }

        gameObject.SetActive(true);
        _lines = lines;
        _index = 0;
        _openedFrame = Time.frameCount;
        if (_input != null)
        {
            _previousState = _input.CurrentInputState;
            _input.SwitchInputState(InputState.UI);
        }
        transform.SetAsLastSibling();
        _speaker.text = _data.dataStorage.GetBBStringText(speakerKey);
        _portrait.sprite = portrait;
        _portrait.enabled = portrait != null;
        RefreshLine();
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == _openedFrame)
        {
            return;
        }

        if (_cancel?.WasPressedThisFrame() == true)
        {
            gameObject.SetActive(false);
        }
        else if (_submit?.WasPressedThisFrame() == true || Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            Advance();
        }
    }

    private void Advance()
    {
        if (!IsOpen)
        {
            return;
        }

        _index++;
        if (_index == _lines.Length)
        {
            gameObject.SetActive(false);
            return;
        }

        RefreshLine();
    }

    private void RefreshLine()
    {
        DataStorage data = _data.dataStorage;
        _body.text = data.GetBBStringText(_lines[_index]);
        _nextLabel.text = data.GetBBStringText(_index + 1 < _lines.Length ? "STR_Dialogue_Next" : "STR_Dialogue_Close");
    }

    private void OnDisable()
    {
        if (!IsOpen)
        {
            return;
        }

        _lines = null;
        if (_input != null && _input.CurrentInputState == InputState.UI && gameObject.scene.isLoaded)
        {
            _input.SwitchInputState(_previousState);
        }
    }
}
