using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class InventoryTooltip
{
    private const float MinWidth = 52f;
    private const float MaxWidth = 106f;
    private const float Height = 38f;
    private const float Margin = 6f;
    private readonly RectTransform _canvas;
    private readonly RectTransform _box;
    private readonly TMP_Text _name;
    private readonly Vector3[] _corners = new Vector3[4];

    internal InventoryTooltip(Canvas canvas)
    {
        _canvas = (RectTransform)canvas.rootCanvas.transform;
        _box = CreateRect("InventoryTooltipPanel", _canvas, new Vector2(MinWidth, Height), Vector2.zero);
        _box.pivot = new Vector2(1f, 0.5f);
        Image background = _box.gameObject.AddComponent<Image>();
        background.color = new Color32(50, 30, 23, 246);
        background.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        _name = CreateLabel("ItemName", font, new Vector2(MinWidth - 12f, 19f), new Vector2(0f, 8f));
        _name.enableAutoSizing = true;
        _name.fontSizeMin = 10f;
        _name.fontSizeMax = 14f;
        _name.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform coin = CreateRect("GoldIcon", _box, new Vector2(12f, 12f), new Vector2(-7f, -8f));
        Image icon = coin.gameObject.AddComponent<Image>();
        icon.sprite = Resources.Load<Sprite>("Art/UI/Common/HUD_Icon_Coin");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TMP_Text price = CreateLabel("Price", font, new Vector2(10f, 16f), new Vector2(7f, -8f));
        price.text = "5";
        Hide();
    }

    internal void Show(string name, RectTransform icon)
    {
        if (string.IsNullOrEmpty(name) || icon == null)
        {
            Hide();
            return;
        }

        _name.text = name;
        float width = Mathf.Clamp(Mathf.Ceil(_name.GetPreferredValues(name).x) + 12f, MinWidth, MaxWidth);
        _box.sizeDelta = new Vector2(width, Height);
        _name.rectTransform.sizeDelta = new Vector2(width - 12f, 19f);

        icon.GetWorldCorners(_corners);
        float iconLeft = _canvas.InverseTransformPoint(_corners[0]).x;
        float iconRight = _canvas.InverseTransformPoint(_corners[2]).x;
        float iconCenter = _canvas.InverseTransformPoint(icon.TransformPoint(icon.rect.center)).y;
        Rect area = _canvas.rect;
        float rightSide = iconRight + Margin + width;
        float leftSide = iconLeft - Margin;
        float x = rightSide <= area.xMax - Margin ? rightSide : leftSide;
        x = Mathf.Clamp(x, area.xMin + width + Margin, area.xMax - Margin);
        float y = Mathf.Clamp(iconCenter, area.yMin + Height * 0.5f + Margin, area.yMax - Height * 0.5f - Margin);
        _box.localPosition = new Vector3(Mathf.Round(x), Mathf.Round(y), 0f);
        _box.SetAsLastSibling();
        _box.gameObject.SetActive(true);
    }

    internal void Hide()
    {
        if (_box != null)
        {
            _box.gameObject.SetActive(false);
        }
    }

    internal void Destroy()
    {
        if (_box != null)
        {
            Object.Destroy(_box.gameObject);
        }
    }

    private TMP_Text CreateLabel(string name, TMP_FontAsset font, Vector2 size, Vector2 position)
    {
        RectTransform rect = CreateRect(name, _box, size, position);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }
        label.fontSize = 14f;
        label.color = new Color32(255, 227, 178, 255);
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
}
