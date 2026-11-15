using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TabBar가 동적으로 만드는 탭 버튼 하나. 선택되지 않은 탭은 윗변이 낮아져 선택된 탭이 도드라져 보인다.
public class TabButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _body;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _unselectedInset = 3f;

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

        _body.offsetMax = new Vector2(0f, selected ? 0f : -_unselectedInset);
    }
}
