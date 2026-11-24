using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public static class MainLobbySetup
{
    private const string ScenePath = "Assets/Scenes/MainLobby.unity";

    [MenuItem("Tools/Lobby/Place UI In MainLobby")]
    public static void Place()
    {
        Scene previous = UnitySceneManager.GetActiveScene();
        Scene scene = UnitySceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<MainLobbyUI>(true) != null)
                {
                    Debug.Log("[Lobby] 이미 배치된 UI를 유지합니다.");
                    return;
                }
            }

            new Layout().Place(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("MainLobby 씬을 저장할 수 없습니다.");
            }
            Debug.Log("[Lobby] MainLobby 씬에 UI를 저장했습니다.");
        }
        finally
        {
            if (openedHere && !scene.isDirty)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (previous.isLoaded)
            {
                UnitySceneManager.SetActiveScene(previous);
            }
        }
    }

    private static void Set(SerializedObject obj, string field, UnityEngine.Object value)
    {
        obj.FindProperty(field).objectReferenceValue = value;
    }

    private sealed class Layout
    {
        private static readonly Color Parchment = new Color32(246, 220, 176, 255);
        private static readonly Color Ink = new Color32(68, 42, 29, 255);
        private readonly Dictionary<string, DataStorage.BBString> _strings =
            DataManager.LoadDefineData<DataStorage.BBString>("BBString");
        private readonly TMP_FontAsset _font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        private readonly Sprite _buttonSprite = Resources.Load<Sprite>("Art/UI/Common/UI_Button_Normal");
        private readonly Sprite _pressedSprite = Resources.Load<Sprite>("Art/UI/Common/UI_Button_Pressed");
        private readonly Sprite _disabledSprite = Resources.Load<Sprite>("Art/UI/Common/UI_Button_Disabled");

        internal void Place(Scene scene)
        {
            Texture art = Resources.Load<Texture>("Art/Title/TavernLobby");
            if (art == null)
            {
                throw new InvalidOperationException("TavernLobby 배경 텍스처를 가져올 수 없습니다.");
            }

            GameObject rootObject = new GameObject("Main Lobby", typeof(RectTransform));
            UnitySceneManager.MoveGameObjectToScene(rootObject, scene);
            MainLobbyUI controller = rootObject.AddComponent<MainLobbyUI>();
            Canvas canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = rootObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 450);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            rootObject.AddComponent<GraphicRaycaster>();

            GameObject eventObject = new GameObject("Lobby Event System");
            UnitySceneManager.MoveGameObjectToScene(eventObject, scene);
            EventSystem eventSystem = eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>();

            RectTransform root = (RectTransform)canvas.transform;
            RectTransform background = CreateRect(root, "Tavern Illustration");
            Stretch(background);
            RawImage illustration = background.gameObject.AddComponent<RawImage>();
            illustration.texture = art;
            illustration.raycastTarget = false;

            RectTransform strip = CreateRect(root, "Menu Backdrop");
            strip.anchorMin = Vector2.zero;
            strip.anchorMax = new Vector2(0, 1);
            strip.pivot = new Vector2(0, 0.5f);
            strip.sizeDelta = new Vector2(304, 0);
            AddImage(strip, null, new Color32(33, 20, 16, 205));

            RectTransform content = CreateRect(strip, "Lobby Content");
            content.sizeDelta = new Vector2(260, 366);
            Label(content, "STR_Lobby_Title", Parchment, 44, 128, 56);
            Label(content, "STR_Lobby_Subtitle", new Color32(225, 180, 120, 255), 17, 88, 28);
            RectTransform divider = CreateRect(content, "Divider", new Vector2(110, 1), new Vector2(0, 58));
            AddImage(divider, null, new Color32(178, 121, 66, 255));

            RectTransform menu = CreateRect(content, "Main Menu");
            Button newGame = CreateButton(menu, "STR_Lobby_NewGame", 23);
            Button continueGame = CreateButton(menu, "STR_Lobby_Continue", -21);
            Button quit = CreateButton(menu, "STR_Lobby_Quit", -65);
            TMP_Text status = Label(content, null, Parchment, 13, -116, 26);

            RectTransform confirmation = CreateRect(content, "New Game Confirmation");
            TMP_Text prompt = Label(confirmation, "STR_Lobby_Replace", Parchment, 15, 23, 45);
            prompt.textWrappingMode = TextWrappingModes.Normal;
            Button confirm = CreateButton(confirmation, "STR_Lobby_NewGame", -27);
            Button cancel = CreateButton(confirmation, "STR_Lobby_Cancel", -71);
            confirmation.gameObject.SetActive(false);
            eventSystem.firstSelectedGameObject = newGame.gameObject;

            SerializedObject settings = new SerializedObject(controller);
            Set(settings, "_illustration", illustration);
            Set(settings, "_menu", menu.gameObject);
            Set(settings, "_confirmation", confirmation.gameObject);
            Set(settings, "_status", status);
            Set(settings, "_newGame", newGame);
            Set(settings, "_continue", continueGame);
            Set(settings, "_quit", quit);
            Set(settings, "_confirm", confirm);
            Set(settings, "_cancel", cancel);
            Set(settings, "_eventSystem", eventSystem);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private Button CreateButton(RectTransform parent, string id, float y)
        {
            RectTransform rect = CreateRect(parent, id, new Vector2(184, 36), new Vector2(0, y));
            Image image = AddImage(rect, _buttonSprite, _buttonSprite == null ? Parchment : Color.white, true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (_pressedSprite != null && _disabledSprite != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = _pressedSprite,
                    selectedSprite = _pressedSprite,
                    pressedSprite = _pressedSprite,
                    disabledSprite = _disabledSprite,
                };
            }

            Label(rect, id, Ink, 16, 0, 30);
            return button;
        }

        private TMP_Text Label(RectTransform parent, string id, Color color, float fontSize, float y, float height)
        {
            RectTransform rect = CreateRect(parent, "Label", new Vector2(250, height), new Vector2(0, y));
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                label.font = _font;
            }

            label.text = id == null ? string.Empty : GetText(id);
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private string GetText(string id) => _strings.TryGetValue(id, out DataStorage.BBString row) ? row.BODY : id;

        private static RectTransform CreateRect(RectTransform parent, string name, Vector2 size = default, Vector2 offset = default)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
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
}
