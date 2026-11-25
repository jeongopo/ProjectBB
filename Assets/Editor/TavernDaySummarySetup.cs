using System.Collections.Generic;
using GamePlay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class TavernDaySummarySetup
{
    private const string UiPrefabPath = "Assets/Prefabs/UI.prefab";
    private const string SlotPrefabPath = "Assets/Prefabs/InventoryListItem.prefab";
    private static readonly Color Ink = new Color32(68, 42, 29, 255);

    internal static void Connect()
    {
        SaveUiPrefab();
        SaveSlotPrefab();
        GameObject oldRoot = GameObject.Find("Day Summary");
        if (oldRoot != null && oldRoot.transform.parent == null && oldRoot.GetComponent<DaySummaryUI>() != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }
        ConnectGoldLabel();
    }

    private static void SaveUiPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
        try
        {
            bool changed = false;
            Transform systemMessage = root.transform.Find("SystemMessage");
            if (systemMessage != null)
            {
                systemMessage.name = "SystemMessagePanel";
                changed = true;
            }
            DaySummaryUI summary = root.GetComponentInChildren<DaySummaryUI>(true);
            if (summary == null)
            {
                summary = AddSummary(root.transform);
                changed = true;
            }
            changed |= NormalizeSummaryNames(summary);
            Transform cookingPanel = root.transform.Find("CookingPanel");
            if (cookingPanel == null)
            {
                throw new System.InvalidOperationException("UI 프리팹에 CookingPanel이 없습니다.");
            }
            if (cookingPanel.GetComponentInChildren<CookingResultUI>(true) == null)
            {
                AddCookingResult(cookingPanel);
                changed = true;
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath, out bool saved);
                if (!saved)
                {
                    throw new System.InvalidOperationException("UI 프리팹을 저장할 수 없습니다.");
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SaveSlotPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SlotPrefabPath);
        try
        {
            if (root.transform.Find("DimOverlay") != null)
            {
                return;
            }
            RectTransform rect = Create("DimOverlay", root.transform, Vector2.zero, Vector2.zero);
            Stretch(rect);
            Image dim = AddImage(rect, null, new Color32(20, 15, 13, 150));
            dim.raycastTarget = false;
            dim.enabled = false;
            SerializedObject settings = new SerializedObject(root.GetComponent<InventoryListItem>());
            Assign(settings, "_dimOverlay", dim);
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath, out bool saved);
            if (!saved)
            {
                throw new System.InvalidOperationException("인벤토리 슬롯 프리팹을 저장할 수 없습니다.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static DaySummaryUI AddSummary(Transform ui)
    {
        Dictionary<string, DataStorage.BBString> strings = DataManager.LoadDefineData<DataStorage.BBString>("BBString");
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        Sprite window = Resources.Load<Sprite>("Art/UI/Common/UI_Window_Back");
        Sprite button = Resources.Load<Sprite>("Art/UI/Common/UI_Button_Normal");

        RectTransform root = Create("DaySummaryPanel", ui, Vector2.zero, Vector2.zero);
        Stretch(root);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30;
        root.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform shade = Create("SummaryBackdrop", root, Vector2.zero, Vector2.zero);
        Stretch(shade);
        AddImage(shade, null, new Color32(34, 21, 16, 190));
        RectTransform panel = Create("Window", shade, new Vector2(360f, 252f), Vector2.zero);
        AddImage(panel, window, window != null ? Color.white : new Color32(247, 220, 167, 255));

        DaySummaryUI controller = root.gameObject.AddComponent<DaySummaryUI>();
        SerializedObject settings = new SerializedObject(controller);
        Assign(settings, "_panel", shade.gameObject);
        foreach (TMP_Text label in ui.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name == "RightTop_DateLabel")
            {
                Assign(settings, "_dayLabel", label);
                break;
            }
        }
        Assign(settings, "_title", AddLabel(panel, font, "SummaryTitle", string.Format(strings["STR_DaySummary_Title"].BODY, 1), 84f, 25f));
        Assign(settings, "_orders", AddLabel(panel, font, "OrderCountLabel", string.Format(strings["STR_DaySummary_Orders"].BODY, 0), 43f, 16f));
        Assign(settings, "_served", AddLabel(panel, font, "ServedCountLabel", string.Format(strings["STR_DaySummary_Served"].BODY, 0), 17f, 16f));
        Assign(settings, "_missed", AddLabel(panel, font, "MissedCountLabel", string.Format(strings["STR_DaySummary_Missed"].BODY, 0), -9f, 16f));
        Assign(settings, "_earned", AddLabel(panel, font, "RevenueLabel", string.Format(strings["STR_DaySummary_Earned"].BODY, 0), -36f, 18f));
        Assign(settings, "_nextDay", AddButton(panel, font, button, "NextDayButton", strings["STR_DaySummary_Next"].BODY, 62f));
        Assign(settings, "_lobby", AddButton(panel, font, button, "ReturnToLobbyButton", strings["STR_DaySummary_Lobby"].BODY, -62f));
        settings.ApplyModifiedPropertiesWithoutUndo();
        shade.gameObject.SetActive(false);
        return controller;
    }

    private static bool NormalizeSummaryNames(DaySummaryUI summary)
    {
        SerializedObject settings = new SerializedObject(summary);
        bool changed = RenameBoundComponent(settings, "_title", "SummaryTitle");
        changed |= RenameBoundComponent(settings, "_orders", "OrderCountLabel");
        changed |= RenameBoundComponent(settings, "_served", "ServedCountLabel");
        changed |= RenameBoundComponent(settings, "_missed", "MissedCountLabel");
        changed |= RenameBoundComponent(settings, "_earned", "RevenueLabel");
        changed |= RenameBoundComponent(settings, "_nextDay", "NextDayButton");
        changed |= RenameBoundComponent(settings, "_lobby", "ReturnToLobbyButton");
        return changed;
    }

    private static bool RenameBoundComponent(SerializedObject settings, string field, string name)
    {
        if (!(settings.FindProperty(field).objectReferenceValue is Component component))
        {
            return false;
        }

        bool changed = component.gameObject.name != name;
        if (changed)
        {
            component.gameObject.name = name;
        }
        if (component is Button button)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.gameObject.name != "Label")
            {
                label.gameObject.name = "Label";
                changed = true;
            }
        }
        return changed;
    }

    private static void AddCookingResult(Transform cookingPanel)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        Sprite window = Resources.Load<Sprite>("Art/UI/Common/UI_Window_Back");
        RectTransform panel = Create("CookingResultPanel", cookingPanel, new Vector2(212f, 142f), Vector2.zero);
        AddImage(panel, window, window != null ? Color.white : new Color32(247, 220, 167, 255));
        CookingResultUI result = panel.gameObject.AddComponent<CookingResultUI>();
        SerializedObject settings = new SerializedObject(result);
        TMP_Text title = AddLabel(panel, font, "CompletionTitle", string.Empty, 46f, 21f);
        title.rectTransform.sizeDelta = new Vector2(180f, 31f);
        TMP_Text name = AddLabel(panel, font, "CompletedItemName", string.Empty, -44f, 16f);
        name.rectTransform.sizeDelta = new Vector2(182f, 31f);
        Image icon = AddImage(Create("CompletedItemIcon", panel, new Vector2(50f, 50f), Vector2.zero), null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Image grade = AddImage(Create("PreparationGrade", panel, new Vector2(39f, 29f), new Vector2(63f, 4f)), null, Color.white);
        grade.preserveAspect = true;
        grade.raycastTarget = false;
        Assign(settings, "_title", title);
        Assign(settings, "_itemName", name);
        Assign(settings, "_itemIcon", icon);
        Assign(settings, "_gradeIcon", grade);
        Assign(settings, "_dishFallback", Resources.Load<Sprite>("Art/UI/Cooking/Plating/Plating_Plate"));
        settings.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
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

    private static Button AddButton(RectTransform parent, TMP_FontAsset font, Sprite sprite, string name, string title, float x)
    {
        RectTransform rect = Create(name, parent, new Vector2(104f, 29f), new Vector2(x, -86f));
        Image image = AddImage(rect, sprite, sprite != null ? Color.white : new Color32(231, 180, 104, 255));
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = AddLabel(rect, font, "Label", title, 0f, 15f);
        text.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static TMP_Text AddLabel(RectTransform parent, TMP_FontAsset font, string name, string title, float y, float size)
    {
        RectTransform rect = Create(name, parent, new Vector2(284f, 32f), new Vector2(0f, y));
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

    private static RectTransform Create(string name, Transform parent, Vector2 size, Vector2 offset)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
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

    private static void Assign(SerializedObject settings, string name, Object value)
    {
        settings.FindProperty(name).objectReferenceValue = value;
    }
}
