using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hunting_Select 패널의 가로 리스트에 채워지는 사냥터 항목. 클릭 시 자신의 huntingID로 콜백을 호출.
public class HuntingSelectItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform dropListContent;
    [SerializeField] private DropListItem dropListItemPrefab;

    private string huntingID;
    private Action<string> onSelected;
    private readonly List<DropListItem> spawnedDropItems = new List<DropListItem>();

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void SetData(string id, string nameK, string[] dropIDs, DataManager dataManager, Action<string> onSelectedCallback)
    {
        huntingID = id;
        onSelected = onSelectedCallback;

        if (nameText != null)
            nameText.text = nameK;

        PopulateDropList(dropIDs, dataManager);
    }

    // dropIDs에 해당하는 Drop 데이터를 찾아 DropListItem으로 스크롤 목록을 채움. 확률은 dropIDs 내 RATE 합 대비 비율로 표시.
    private void PopulateDropList(string[] dropIDs, DataManager dataManager)
    {
        foreach (var item in spawnedDropItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawnedDropItems.Clear();

        if (dropListContent == null || dropListItemPrefab == null || dropIDs == null
            || dataManager?.dataStorage.DropData == null)
            return;

        var dropRows = new List<DataStorage.Drop>();
        float totalRate = 0f;
        foreach (var dropID in dropIDs)
        {
            if (dataManager.dataStorage.DropData.TryGetValue(dropID, out var dropRow))
            {
                dropRows.Add(dropRow);
                totalRate += dropRow.RATE;
            }
        }

        foreach (var dropRow in dropRows)
        {
            DropListItem dropItem = Instantiate(dropListItemPrefab, dropListContent);
            dataManager.dataStorage.ItemData.TryGetValue(dropRow.ITEMID, out var itemRow);
            float ratio = totalRate > 0f ? dropRow.RATE / totalRate : 0f;
            dropItem.SetData(dropRow, itemRow, ratio);
            spawnedDropItems.Add(dropItem);
        }
    }

    private void HandleClick()
    {
        onSelected?.Invoke(huntingID);
    }
}
