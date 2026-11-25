using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemIcon : MonoBehaviour
{
    private const string ArtResourceRoot = "Art/";
    private const string DishFallbackPath = "UI/Cooking/Plating/Plating_Plate.png";

    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private bool countVisible = true;

    private DataManager dataManager;
    private string itemID;
    private int count;

    public string ItemID => itemID;
    public int Count => count;
    internal RectTransform ImageRect => itemImage != null ? itemImage.rectTransform : null;

    public bool CountVisible
    {
        get => countVisible;
        set
        {
            countVisible = value;
            if (countText != null)
                countText.gameObject.SetActive(value);
        }
    }

    private void Awake()
    {
        if (itemImage == null)
            itemImage = transform.Find("Item_Image")?.GetComponent<Image>();
        if (countText == null)
            countText = transform.Find("Item_Count")?.GetComponent<TMP_Text>();

        if (countText != null)
            countText.gameObject.SetActive(countVisible);
    }

    public void SetItem(string itemID, int count = 1)
    {
        this.itemID = itemID;
        this.count = count;

        if (countText != null)
            countText.text = count.ToString();

        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();

        if (itemImage != null)
        {
            itemImage.sprite = ResolveIcon(dataManager != null ? dataManager.dataStorage : null, itemID);
            itemImage.enabled = itemImage.sprite != null;
        }
    }

    internal static Sprite ResolveIcon(DataStorage data, string itemId)
    {
        if (data == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        if (data.ItemData != null && data.ItemData.TryGetValue(itemId, out var item) &&
            !string.IsNullOrEmpty(item.ICONPATH))
        {
            return LoadIconSprite(item.ICONPATH);
        }

        if (data.Recipe_CombineData != null)
        {
            foreach (DataStorage.Recipe_Combine recipe in data.Recipe_CombineData.Values)
            {
                if (recipe.PRODUCT_DISH_ID == itemId)
                {
                    return LoadIconSprite(recipe.ICONPATH) ?? LoadIconSprite(DishFallbackPath);
                }
            }
        }

        return null;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public static Sprite LoadIconSprite(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;

        string path = ArtResourceRoot + Path.ChangeExtension(iconPath, null).Replace('\\', '/');
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"ItemIcon: Sprite not found at Resources/{path}");

        return sprite;
    }
}
