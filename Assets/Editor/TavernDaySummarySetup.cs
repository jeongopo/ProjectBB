using System.Collections.Generic;
using GamePlay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class TavernDaySummarySetup
{
    private static readonly Color Ink = new Color32(68, 42, 29, 255);

    internal static void Connect()
    {
        ConnectGoldLabel();
        if (Object.FindFirstObjectByType<DaySummaryUI>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        Dictionary<string, DataStorage.BBString> strings = DataManager.LoadDefineData<DataStorage.BBString>("BBString");
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        Sprite window = Resources.Load<Sprite>("Art/UI/Common/UI_Window_Back");
        Sprite button = Resources.Load<Sprite>("Art/UI/Common/UI_Button_Normal");

        RectTransform root = Create("Day Summary", null, Vector2.zero, Vector2.zero);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 450f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform shade = Create("Summary Panel", root, Vector2.zero, Vector2.zero);
        shade.anchorMin = Vector2.zero;
        shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        AddImage(shade, null, new Color32(34, 21, 16, 190));
        RectTransform panel = Create("Window", shade, new Vector2(360f, 252f), Vector2.zero);
        AddImage(panel, window, window != null ? Color.white : new Color32(247, 220, 167, 255));

        DaySummaryUI controller = root.gameObject.AddComponent<DaySummaryUI>();
        SerializedObject settings = new SerializedObject(controller);
        Assign(settings, "_panel", shade.gameObject);
        Assign(settings, "_dayLabel", FindHudLabel("RightTop_DateLabel"));
        Assign(settings, "_title", AddLabel(panel, font, string.Format(strings["STR_DaySummary_Title"].BODY, 1), 84f, 25f));
        Assign(settings, "_orders", AddLabel(panel, font, string.Format(strings["STR_DaySummary_Orders"].BODY, 0), 43f, 16f));
        Assign(settings, "_served", AddLabel(panel, font, string.Format(strings["STR_DaySummary_Served"].BODY, 0), 17f, 16f));
        Assign(settings, "_missed", AddLabel(panel, font, string.Format(strings["STR_DaySummary_Missed"].BODY, 0), -9f, 16f));
        Assign(settings, "_earned", AddLabel(panel, font, string.Format(strings["STR_DaySummary_Earned"].BODY, 0), -36f, 18f));
        Assign(settings, "_nextDay", AddButton(panel, font, button, strings["STR_DaySummary_Next"].BODY, 62f));
        Assign(settings, "_lobby", AddButton(panel, font, button, strings["STR_DaySummary_Lobby"].BODY, -62f));
        settings.ApplyModifiedPropertiesWithoutUndo();
        shade.gameObject.SetActive(false);
    }

    private static void ConnectGoldLabel()
    {
        CurrencyWallet wallet = Object.FindFirstObjectByType<CurrencyWallet>();
        if (wallet == null)
        {
            return;
        }

        TMP_Text text = FindHudLabel("RightTop_MoneyLabel");
        if (text == null)
        {
            return;
        }

        if (!text.TryGetComponent(out CurrencyLabel label))
        {
            label = Undo.AddComponent<CurrencyLabel>(text.gameObject);
        }
        SerializedObject settings = new SerializedObject(label);
        Assign(settings, "_text", text);
        Assign(settings, "_wallet", wallet);
        settings.ApplyModifiedProperties();
    }

    private static TMP_Text FindHudLabel(string name)
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.gameObject.name == name)
            {
                return text;
            }
        }
        return null;
    }

    private static Button AddButton(RectTransform parent, TMP_FontAsset font, Sprite sprite, string title, float x)
    {
        RectTransform rect = Create(title, parent, new Vector2(104f, 29f), new Vector2(x, -86f));
        Image image = AddImage(rect, sprite, sprite != null ? Color.white : new Color32(231, 180, 104, 255));
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = AddLabel(rect, font, title, 0f, 15f);
        text.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static TMP_Text AddLabel(RectTransform parent, TMP_FontAsset font, string title, float y, float size)
    {
        RectTransform rect = Create(title, parent, new Vector2(284f, 32f), new Vector2(0f, y));
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }
        label.text = title;
        label.fontSize = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color tint)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        return image;
    }

    private static RectTransform Create(string name, RectTransform parent, Vector2 size, Vector2 offset)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
        return rect;
    }

    private static void Assign(SerializedObject settings, string name, Object value)
    {
        settings.FindProperty(name).objectReferenceValue = value;
    }
}
