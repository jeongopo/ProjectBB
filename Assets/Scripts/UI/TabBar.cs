using System;
using System.Collections.Generic;
using UnityEngine;

// 탭 버튼을 동적으로 추가하는 공용 탭 바. 탭 위치는 컨테이너의 레이아웃 그룹이 정한다.
public class TabBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _container;
    [SerializeField] private TabButtonView _tabPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _selectedSprite;

    [Header("Initial Tabs")]
    [SerializeField] private List<string> _initialLabels = new List<string>();

    private readonly List<TabButtonView> _tabs = new List<TabButtonView>();

    public event Action<int> OnTabSelected;

    public int SelectedIndex { get; private set; } = -1;
    public int TabCount => _tabs.Count;

    private void Awake()
    {
        if (_container == null)
        {
            _container = (RectTransform)transform;
        }

        if (_initialLabels.Count > 0)
        {
            SetTabs(_initialLabels);
        }
    }

    public void SetTabs(IReadOnlyList<string> labels)
    {
        ClearTabs();
        for (int i = 0; i < labels.Count; i++)
        {
            AddTab(labels[i]);
        }
        Select(_tabs.Count > 0 ? 0 : -1);
    }

    public void AddTab(string label)
    {
        int index = _tabs.Count;
        TabButtonView tab = Instantiate(_tabPrefab, _container);
        tab.Setup(label, () => Select(index));
        tab.SetSelected(false, _normalSprite, _selectedSprite);
        _tabs.Add(tab);

        if (SelectedIndex < 0)
        {
            Select(index);
        }
    }

    public void Select(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            SelectedIndex = -1;
            return;
        }

        SelectedIndex = index;
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].SetSelected(i == index, _normalSprite, _selectedSprite);
        }
        OnTabSelected?.Invoke(index);
    }

    private void ClearTabs()
    {
        foreach (TabButtonView tab in _tabs)
        {
            if (tab != null)
            {
                Destroy(tab.gameObject);
            }
        }
        _tabs.Clear();
        SelectedIndex = -1;
    }
}
