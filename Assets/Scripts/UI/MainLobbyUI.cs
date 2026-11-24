using System.Collections.Generic;
using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainLobbyUI : MonoBehaviour
{
    [SerializeField] private RawImage _illustration;
    [SerializeField] private GameObject _menu;
    [SerializeField] private GameObject _confirmation;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private Button _newGame;
    [SerializeField] private Button _continue;
    [SerializeField] private Button _quit;
    [SerializeField] private Button _confirm;
    [SerializeField] private Button _cancel;
    [SerializeField] private EventSystem _eventSystem;

    private Dictionary<string, DataStorage.BBString> _strings;

    private void Start()
    {
        _strings = DataManager.LoadDefineData<DataStorage.BBString>("BBString");
        _newGame.onClick.AddListener(NewGame);
        _continue.onClick.AddListener(ContinueGame);
        _quit.onClick.AddListener(Quit);
        _confirm.onClick.AddListener(ConfirmNewGame);
        _cancel.onClick.AddListener(CancelNewGame);

        _continue.interactable = SaveSystem.TryLoad(out _);
        LinkVertical(_confirm, _cancel);
        if (_continue.interactable)
        {
            LinkVertical(_newGame, _continue, _quit);
        }
        else
        {
            LinkVertical(_newGame, _quit);
        }

        _eventSystem.SetSelectedGameObject(_newGame.gameObject);
        UpdateIllustrationCrop();
        GameAudio.StartMusic();
    }

    private void OnDestroy()
    {
        _newGame?.onClick.RemoveListener(NewGame);
        _continue?.onClick.RemoveListener(ContinueGame);
        _quit?.onClick.RemoveListener(Quit);
        _confirm?.onClick.RemoveListener(ConfirmNewGame);
        _cancel?.onClick.RemoveListener(CancelNewGame);
    }

    private void ConfirmNewGame() => OpenGame(null);

    private void OnRectTransformDimensionsChange()
    {
        UpdateIllustrationCrop();
    }

    private void UpdateIllustrationCrop()
    {
        if (_illustration == null || _illustration.texture == null)
        {
            return;
        }

        Rect viewport = ((RectTransform)transform).rect;
        if (viewport.width <= 0f || viewport.height <= 0f)
        {
            return;
        }

        Texture art = _illustration.texture;
        float imageRatio = (float)art.width / art.height;
        float viewportRatio = viewport.width / viewport.height;
        float width = Mathf.Min(1f, viewportRatio / imageRatio);
        float height = Mathf.Min(1f, imageRatio / viewportRatio);
        _illustration.uvRect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
    }

    private void NewGame()
    {
        if (!SaveSystem.TryLoad(out _))
        {
            OpenGame(null);
            return;
        }

        GameAudio.Play(SoundCue.Select);
        _status.text = string.Empty;
        _menu.SetActive(false);
        _confirmation.SetActive(true);
        _eventSystem.SetSelectedGameObject(_cancel.gameObject);
    }

    private void CancelNewGame()
    {
        _confirmation.SetActive(false);
        _menu.SetActive(true);
        _eventSystem.SetSelectedGameObject(_newGame.gameObject);
        GameAudio.Play(SoundCue.PanelClose);
    }

    private void ContinueGame()
    {
        if (SaveSystem.TryLoad(out SaveData data))
        {
            OpenGame(data);
        }
        else
        {
            _continue.interactable = false;
            LinkVertical(_newGame, _quit);
            _eventSystem.SetSelectedGameObject(_newGame.gameObject);
            _status.text = GetText("STR_Lobby_LoadFailed");
        }
    }

    private void OpenGame(SaveData data)
    {
        GameAudio.Play(SoundCue.Select);
        if (!GameSession.OpenGameplay(data))
        {
            _status.text = GetText("STR_Lobby_SceneFailed");
        }
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private string GetText(string id) => _strings.TryGetValue(id, out DataStorage.BBString row) ? row.BODY : id;

    private static void LinkVertical(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                selectOnDown = buttons[(i + 1) % buttons.Length],
            };
        }
    }

}
