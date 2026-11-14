using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TabBar가 동적으로 만드는 탭 버튼 하나.
public class TabButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _label;

    public void Setup(string label, Action onClick)
    {
        _label.text = label;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetSelected(bool selected, Sprite normalSprite, Sprite selectedSprite)
    {
        Sprite sprite = selected ? selectedSprite : normalSprite;
        if (sprite != null)
        {
            _background.sprite = sprite;
        }
    }
}
