using System;
using System.Collections.Generic;
using DG.Tweening;
using GameEnumDefines;
using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class AudioOptionsUI : MonoBehaviour
{
    private static readonly Color Ink = new Color32(71, 41, 28, 255);
    private static readonly Color Paper = new Color32(244, 218, 167, 255);
    private static readonly Color GaugeFill = new Color32(193, 122, 61, 255);
    private static readonly Color GaugeThumb = new Color32(246, 184, 105, 255);
    private InputManager _input;
    private Dictionary<string, DataStorage.BBString> _strings;
    private TMP_FontAsset _font;
    private Sprite _buttonSprite;
    private Sprite _trackSprite;
    private GameObject _canvasRoot;
    private GameObject _modal;
    private CanvasGroup _modalGroup;
    private Button _openButton;
    private Slider _musicSlider;
    private Tween _fade;
    private float _previousTimeScale;
    private bool _isOpen;

    internal void Initialize(InputManager input)
    {
        _input = input;
        _strings = DataManager.LoadDefineData<DataStorage.BBString>("BBString");
        _font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        _buttonSprite = LoadSprite("UI_Button_Normal");
        _trackSprite = LoadSprite("UI_Slider_Back");

        RectTransform root = CreateRect("Audio Options Canvas", null, Vector2.one, Vector2.zero);
        _canvasRoot = root.gameObject;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_canvasRoot, gameObject.scene);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 450);
        scaler.matchWidthOrHeight = 0.5f;
        root.gameObject.AddComponent<GraphicRaycaster>();

        _openButton = CreateButton(root, "STR_Options_Title", new Vector2(76, 26), Vector2.zero, Open);
        RectTransform opener = (RectTransform)_openButton.transform;
        opener.anchorMin = opener.anchorMax = new Vector2(0, 1);
        opener.pivot = new Vector2(0, 1);
        opener.anchoredPosition = new Vector2(12, -12);

        RectTransform backdrop = CreateRect("Options Backdrop", root, Vector2.zero, Vector2.zero);
        Stretch(backdrop);
        AddImage(backdrop, null, new Color(0.08f, 0.045f, 0.03f, 0.70f), true);
        _modal = backdrop.gameObject;
        _modalGroup = _modal.AddComponent<CanvasGroup>();
        RectTransform panel = CreateRect("Options Panel", backdrop, new Vector2(412, 244), Vector2.zero);
        Sprite window = LoadSprite("UI_Window_Back");
        AddImage(panel, window, window == null ? Paper : Color.white);
        CreateLabel(panel, GetText("STR_Options_Title"), 24, new Vector2(330, 32), new Vector2(0, 88));

        _musicSlider = CreateVolumeRow(panel, AudioChannel.Music, "STR_Options_Music", 44);
        CreateVolumeRow(panel, AudioChannel.World, "STR_Options_World", 4, SoundCue.CustomerArrival);
        CreateVolumeRow(panel, AudioChannel.UI, "STR_Options_UI", -36, SoundCue.Select);
        CreateButton(panel, "STR_Options_Close", new Vector2(88, 28), new Vector2(0, -87), Close);
        _modal.SetActive(false);
        UpdateOpenButton();
    }

    private void Update()
    {
        if (_modal == null)
        {
            return;
        }

        if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
        UpdateOpenButton();
    }

    private void UpdateOpenButton()
    {
        bool canOpen = !_isOpen && (_input == null || _input.CurrentInputState == InputState.Default);
        if (_openButton.gameObject.activeSelf != canOpen)
        {
            _openButton.gameObject.SetActive(canOpen);
        }
    }

    private void Open()
    {
        if (_isOpen || (_input != null && _input.CurrentInputState != InputState.Default))
        {
            return;
        }

        _isOpen = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _input?.SwitchInputState(InputState.UI);
        _modal.SetActive(true);
        _modalGroup.alpha = 0f;
        _fade?.Kill();
        _fade = _modalGroup.DOFade(1f, 0.16f).SetUpdate(true).SetLink(gameObject);
        EventSystem.current?.SetSelectedGameObject(_musicSlider.gameObject);
        UpdateOpenButton();
        GameAudio.Play(SoundCue.PanelOpen);
    }

    private void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        Dismiss();
        GameAudio.Play(SoundCue.PanelClose);
    }

    private void Dismiss()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        _fade?.Kill();
        _fade = null;
        Time.timeScale = _previousTimeScale;
        if (_input != null && _input.CurrentInputState == InputState.UI)
        {
            _input.SwitchInputState(InputState.Default);
        }

        EventSystem.current?.SetSelectedGameObject(null);
        _modal.SetActive(false);
        PlayerPrefs.Save();
        UpdateOpenButton();
    }

    private void OnEnable()
    {
        if (_canvasRoot != null)
        {
            _canvasRoot.SetActive(true);
        }
    }

    private void OnDisable()
    {
        Dismiss();
        if (_canvasRoot != null)
        {
            _canvasRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_canvasRoot != null)
        {
            Destroy(_canvasRoot);
        }
    }

    private Slider CreateVolumeRow(RectTransform panel, AudioChannel channel, string label, float y, SoundCue? preview = null)
    {
        TMP_Text name = CreateLabel(panel, GetText(label), 15, new Vector2(100, 28), new Vector2(-125, y));
        name.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform track = CreateRect(channel.ToString(), panel, new Vector2(136, 28), new Vector2(-2, y));
        AddImage(track, null, Color.clear, true);
        RectTransform back = CreateRect("Track", track, new Vector2(128, 12), Vector2.zero);
        AddImage(back, _trackSprite, _trackSprite == null ? Ink : Color.white);
        // Slider가 자식의 앵커를 갱신하므로 채움 높이는 부모에서 제한한다.
        RectTransform fillArea = CreateRect("Fill Area", track, new Vector2(114, 6), Vector2.zero);
        RectTransform fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.zero);
        Stretch(fill);
        AddImage(fill, null, GaugeFill);
        RectTransform handleArea = CreateRect("Handle Area", track, new Vector2(114, 0), Vector2.zero);
        RectTransform handle = CreateRect("Handle", handleArea, new Vector2(8, 14), Vector2.zero);
        Image knob = AddImage(handle, null, Ink, true);
        RectTransform cap = CreateRect("Handle Cap", handle, Vector2.zero, Vector2.zero);
        Stretch(cap);
        cap.offsetMin = Vector2.one;
        cap.offsetMax = -Vector2.one;
        AddImage(cap, null, GaugeThumb);
        Slider slider = track.gameObject.AddComponent<Slider>();
        slider.targetGraphic = knob;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;
        int volume = Mathf.RoundToInt(GameAudio.GetVolume(channel) * 100f);
        slider.SetValueWithoutNotify(volume);
        TMP_Text amount = CreateLabel(panel, $"{volume}%", 15, new Vector2(44, 28), new Vector2(91, y));
        slider.onValueChanged.AddListener(value =>
        {
            GameAudio.SetVolume(channel, value / 100f);
            amount.text = $"{value:0}%";
        });
        if (preview.HasValue)
        {
            SoundCue cue = preview.Value;
            CreateButton(panel, "STR_Options_Preview", new Vector2(56, 25), new Vector2(151, y), () => GameAudio.Play(cue));
        }

        return slider;
    }

    private Button CreateButton(RectTransform parent, string labelId, Vector2 size, Vector2 position, Action onClick)
    {
        RectTransform rect = CreateRect(labelId, parent, size, position);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = AddImage(rect, _buttonSprite, _buttonSprite == null ? Paper : Color.white, true);
        CreateLabel(rect, GetText(labelId), 15, size, Vector2.zero);
        button.onClick.AddListener(() => onClick());
        return button;
    }

    private TMP_Text CreateLabel(RectTransform parent, string value, float size, Vector2 dimensions, Vector2 position)
    {
        RectTransform rect = CreateRect("Label", parent, dimensions, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null)
        {
            label.font = _font;
        }

        label.text = value;
        label.fontSize = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private string GetText(string id) => _strings.TryGetValue(id, out DataStorage.BBString row) ? row.BODY : id;

    private static Sprite LoadSprite(string name) => Resources.Load<Sprite>($"Art/UI/Common/{name}");

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast = false)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }
}
