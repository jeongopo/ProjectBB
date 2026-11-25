using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class InventoryTooltip
{
    private readonly RectTransform _canvas;
    private readonly RectTransform _box;
    private readonly TMP_Text _label;

    internal InventoryTooltip(Canvas canvas)
    {
        _canvas = (RectTransform)canvas.rootCanvas.transform;
        _box = (RectTransform)new GameObject("Inventory Tooltip", typeof(RectTransform)).transform;
        _box.SetParent(_canvas, false);
        _box.pivot = new Vector2(0f, 1f);
        Image background = _box.gameObject.AddComponent<Image>();
        background.color = new Color32(50, 30, 23, 246);
        background.raycastTarget = false;

        RectTransform textRect = (RectTransform)new GameObject("Item Name", typeof(RectTransform)).transform;
        textRect.SetParent(_box, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(7f, 3f);
        textRect.offsetMax = new Vector2(-7f, -3f);
        _label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/PFStartDust 3");
        if (font != null)
        {
            _label.font = font;
        }
        _label.fontSize = 14f;
        _label.color = new Color32(255, 227, 178, 255);
        _label.alignment = TextAlignmentOptions.Center;
        _label.raycastTarget = false;
        Hide();
    }

    internal void Show(string name, Vector2 position, Camera camera)
    {
        if (string.IsNullOrEmpty(name) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, position, camera, out Vector2 local))
        {
            Hide();
            return;
        }

        _box.SetAsLastSibling();
        _box.gameObject.SetActive(true);
        _label.text = name;
        Rect area = _canvas.rect;
        Vector2 textSize = _label.GetPreferredValues(name, Mathf.Min(250f, area.width - 14f), float.PositiveInfinity);
        _box.sizeDelta = new Vector2(Mathf.Ceil(textSize.x) + 14f, Mathf.Ceil(textSize.y) + 8f);
        float x = Mathf.Clamp(local.x + 12f, area.xMin, area.xMax - _box.sizeDelta.x);
        float y = Mathf.Clamp(local.y + 18f + _box.sizeDelta.y, area.yMin + _box.sizeDelta.y, area.yMax);
        _box.localPosition = new Vector3(Mathf.Round(x), Mathf.Round(y), 0f);
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
}
