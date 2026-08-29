using UnityEngine;

public class InventoryListItem : MonoBehaviour
{
    [SerializeField] private ItemIcon itemIcon;

    private void Awake()
    {
        if (itemIcon == null)
            itemIcon = GetComponentInChildren<ItemIcon>(true);
    }

    public void SetItem(string itemID, int count)
    {
        if (itemIcon == null)
            return;

        itemIcon.SetItem(itemID, count);
        itemIcon.Show();
    }

    public void Clear()
    {
        if (itemIcon == null)
            return;

        itemIcon.Hide();
    }
}
