using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ItemIcon itemIcon;
    [SerializeField] private Image _dimOverlay;
    private Button _button;
    private Image _background;
    private Sprite _normal;
    private Sprite _outline;
    private Action<InventoryListItem> _clicked;
    private Action<InventoryListItem> _hovered;
    private Action<InventoryListItem> _exited;

    internal SavedItemStack Stack { get; private set; }
    internal RectTransform TooltipAnchor => itemIcon != null && itemIcon.ImageRect != null
        ? itemIcon.ImageRect
        : (RectTransform)transform;

    private void Awake()
    {
        if (itemIcon == null)
            itemIcon = GetComponentInChildren<ItemIcon>(true);
        _button = GetComponent<Button>();
        _background = GetComponent<Image>();
        _normal = _background != null ? _background.sprite : null;
        if (_button != null)
        {
            ColorBlock colors = _button.colors;
            colors.disabledColor = Color.white;
            _button.colors = colors;
            _button.onClick.AddListener(Click);
        }
    }

    internal void Bind(Sprite outline, Action<InventoryListItem> clicked,
        Action<InventoryListItem> hovered, Action<InventoryListItem> exited)
    {
        _outline = outline;
        _clicked = clicked;
        _hovered = hovered;
        _exited = exited;
    }

    internal void SetItem(SavedItemStack stack, bool selectable, bool selected, bool dimmed)
    {
        bool changedItem = Stack.ItemId != stack.ItemId || Stack.Grade != stack.Grade;
        bool refreshIcon = changedItem || Stack.Count != stack.Count;
        if (changedItem)
        {
            _exited?.Invoke(this);
        }
        Stack = stack;
        SetSelection(selectable, selected, dimmed);
        if (itemIcon == null || !refreshIcon)
            return;

        itemIcon.SetItem(stack.ItemId, stack.Count);
        itemIcon.CountVisible = true;
        itemIcon.Show();
    }

    public void Clear()
    {
        _exited?.Invoke(this);
        Stack = default;
        SetSelection(false, false, false);
        if (itemIcon == null)
            return;

        itemIcon.Hide();
    }

    private void SetSelection(bool selectable, bool selected, bool dimmed)
    {
        if (_button != null)
        {
            _button.interactable = selectable;
        }
        if (_background != null)
        {
            _background.sprite = selected && _outline != null ? _outline : _normal;
        }
        if (_dimOverlay != null)
        {
            _dimOverlay.enabled = dimmed;
        }
    }

    private void Click() => _clicked?.Invoke(this);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Stack.Count > 0)
        {
            _hovered?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData) => _exited?.Invoke(this);
}
