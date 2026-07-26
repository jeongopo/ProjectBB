using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hunting_Select 패널의 가로 리스트에 채워지는 사냥터 항목. 클릭 시 자신의 huntingID로 콜백을 호출.
public class HuntingSelectItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private string huntingID;
    private Action<string> onSelected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void SetData(string id, string nameK, Action<string> onSelectedCallback)
    {
        huntingID = id;
        onSelected = onSelectedCallback;

        if (nameText != null)
            nameText.text = nameK;
    }

    private void HandleClick()
    {
        onSelected?.Invoke(huntingID);
    }
}
