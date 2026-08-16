using System.IO;
using UnityEngine;
using UnityEngine.UI;

// Item.xml 데이터를 아이콘으로 보여주는 공용 UI 컴포넌트. 사냥/인벤토리 등에서 공용으로 사용.
public class ItemIcon : MonoBehaviour
{
    private const string ArtResourceRoot = "Art/";

    [SerializeField] private Image itemImage;

    private DataManager dataManager;
    private string itemID;
    private int count;

    public string ItemID => itemID;
    public int Count => count;

    private void Awake()
    {
        if (itemImage == null)
            itemImage = transform.Find("Item_Image")?.GetComponent<Image>();
    }

    public void SetItem(string itemID, int count = 0)
    {
        this.itemID = itemID;
        this.count = count;

        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();

        if (dataManager == null)
        {
            // 비활성 오브젝트까지 포함해서 다시 찾아보면 "아예 없음"과 "있는데 꺼져있음"을 구분할 수 있음
            var inactiveDataManager = FindFirstObjectByType<DataManager>(FindObjectsInactive.Include);
            if (inactiveDataManager != null)
                Debug.LogWarning($"ItemIcon: DataManager found but inactive/disabled on '{inactiveDataManager.gameObject.name}'. Enable it in the scene.");
            else
                Debug.LogWarning("ItemIcon: No DataManager exists in the currently loaded scene(s) at all.");
            return;
        }

        if (dataManager.dataStorage?.ItemData == null)
        {
            Debug.LogWarning("ItemIcon: dataStorage.ItemData is null - DataManager.Start() (XML load) hasn't run yet, or Resources/XML/Item.xml failed to load.");
            return;
        }

        if (!dataManager.dataStorage.ItemData.TryGetValue(itemID, out var itemRow))
        {
            string knownIDs = string.Join(", ", dataManager.dataStorage.ItemData.Keys);
            Debug.LogWarning($"ItemIcon: Item data not found for ID '{itemID}'. Known IDs=[{knownIDs}]");
            return;
        }

        if (itemImage != null)
            itemImage.sprite = LoadIconSprite(itemRow.ICONPATH);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static Sprite LoadIconSprite(string iconPath)
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
