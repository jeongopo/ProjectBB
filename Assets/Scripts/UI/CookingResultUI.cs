using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CookingResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _itemName;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _gradeIcon;
    [SerializeField] private Sprite _dishFallback;

    internal void Show(DataStorage data, string itemId, string fallbackName, Sprite grade)
    {
        string name = fallbackName;
        Sprite sprite = ItemIcon.ResolveIcon(data, itemId);
        if (data?.ItemData != null && !string.IsNullOrEmpty(itemId) &&
            data.ItemData.TryGetValue(itemId, out DataStorage.Item item))
        {
            name = string.IsNullOrEmpty(item.DISPLAY_NAME) ? item.NAME : data.GetBBStringText(item.DISPLAY_NAME);
        }

        _title.text = data.GetBBStringText("STR_Cooking_Complete");
        _itemName.text = string.IsNullOrEmpty(name) ? itemId : name;
        _itemIcon.sprite = sprite != null ? sprite : _dishFallback;
        _itemIcon.enabled = _itemIcon.sprite != null;
        _gradeIcon.sprite = grade;
        _gradeIcon.enabled = grade != null;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    internal void Hide() => gameObject.SetActive(false);
}
