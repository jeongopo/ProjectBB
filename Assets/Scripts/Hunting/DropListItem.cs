using TMPro;
using UnityEngine;

// Hunting_Select 패널의 드랍 목록(Dropbox content) 스크롤에 채워지는 항목. 아이템 아이콘/이름/드랍개수/확률을 표시.
public class DropListItem : MonoBehaviour
{
    [SerializeField] private ItemIcon itemIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dropRangeText;
    [SerializeField] private TMP_Text dropRatioText;

    public void SetData(DataStorage.Drop dropRow, DataStorage.Item itemRow, float ratio)
    {
        if (itemIcon != null)
        {
            itemIcon.SetItem(dropRow.ITEMID);
            itemIcon.Show();
        }

        if (nameText != null)
            nameText.text = itemRow != null ? itemRow.NAME : dropRow.ITEMID;

        if (dropRangeText != null)
            dropRangeText.text = $"{dropRow.MIN}~{dropRow.MAX}";

        if (dropRatioText != null)
            dropRatioText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
    }
}
