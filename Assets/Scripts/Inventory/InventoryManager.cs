using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameEnumDefines;
using GamePlay;

public class InventoryManager : MonoBehaviour
{
    private const int MaxSlots = 40;
    private const string ToggleActionName = "Inventory";

    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeButton;

    [Header("Slots")]
    [SerializeField] private Transform slotParent;
    [SerializeField] private InventoryListItem slotPrefab;

    private class ItemStack
    {
        public string ItemID;
        public int Grade; // 재료 등급(★). 날것은 0
        public int Count;
    }

    private readonly List<ItemStack> itemStacks = new List<ItemStack>();
    private readonly List<InventoryListItem> slots = new List<InventoryListItem>();

    private GamePlay.InputManager inputManager;
    private InputAction toggleAction;
    private DataManager _dataManager;
    private InventoryTooltip _tooltip;
    private InventoryListItem _hoveredSlot;
    private bool _inCooking;
    private bool _selectionLocked;
    private float _normalPanelX;
    private Predicate<SavedItemStack> _ingredientFilter;
    private Action<SavedItemStack?> _ingredientChanged;
    private SavedItemStack? _ingredient;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();
        _dataManager = FindFirstObjectByType<DataManager>();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        CreateSlots();
        SubscribeToggleAction();
    }

    private void OnDestroy()
    {
        _tooltip?.Destroy();
        if (toggleAction != null)
            toggleAction.started -= OnToggleInventory;
    }

    private void CreateSlots()
    {
        if (slotParent == null || slotPrefab == null)
            return;

        Sprite outline = Resources.Load<Sprite>("Art/UI/Common/UI_Slot_Selected");
        for (int i = 0; i < MaxSlots; i++)
        {
            InventoryListItem slot = Instantiate(slotPrefab, slotParent);
            slot.Bind(outline, SelectIngredient, ShowTooltip, HideTooltip);
            slot.Clear();
            slots.Add(slot);
        }
    }

    private void SubscribeToggleAction()
    {
        if (inputManager == null)
            return;

        // Default 액션맵은 Default/Popup 상태 모두에서 항상 켜져 있으므로 한 번만 구독하면 됨
        toggleAction = inputManager.GetActionMap(InputState.Default)?.FindAction(ToggleActionName);

        if (toggleAction != null)
            toggleAction.started += OnToggleInventory;
    }

    private void OnToggleInventory(InputAction.CallbackContext ctx)
    {
        ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (_inCooking)
        {
            return;
        }
        RefreshSlots();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            GameAudio.Play(SoundCue.PanelOpen);
        }
        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Popup);
    }

    public void CloseInventory()
    {
        if (_inCooking)
        {
            return;
        }
        HideTooltip(_hoveredSlot);
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(false);
            GameAudio.Play(SoundCue.PanelClose);
        }
        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Default);
    }

    internal void OpenForCooking(Predicate<SavedItemStack> filter, Action<SavedItemStack?> changed)
    {
        if (_inCooking || inventoryPanel == null)
        {
            return;
        }

        _inCooking = true;
        _ingredientFilter = filter;
        _ingredientChanged = changed;
        if (inventoryPanel.transform is RectTransform rect)
        {
            _normalPanelX = rect.anchoredPosition.x;
            rect.anchoredPosition = new Vector2(250f, rect.anchoredPosition.y);
        }
        if (closeButton != null)
        {
            closeButton.interactable = false;
        }
        RefreshSlots();
        inventoryPanel.SetActive(true);
    }

    internal void SetCookingSelectionLocked(bool locked)
    {
        if (!_inCooking)
        {
            return;
        }
        _selectionLocked = locked;
        RefreshSlots();
    }

    internal void CloseAfterCooking()
    {
        if (!_inCooking)
        {
            return;
        }

        HideTooltip(_hoveredSlot);
        _inCooking = false;
        _selectionLocked = false;
        _ingredient = null;
        _ingredientFilter = null;
        _ingredientChanged = null;
        if (inventoryPanel.transform is RectTransform rect)
        {
            rect.anchoredPosition = new Vector2(_normalPanelX, rect.anchoredPosition.y);
        }
        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
        inventoryPanel.SetActive(false);
    }

    private void SelectIngredient(InventoryListItem slot)
    {
        if (!_inCooking || _selectionLocked || slot.Stack.Count <= 0 || _ingredientFilter?.Invoke(slot.Stack) != true)
        {
            return;
        }

        _ingredient = slot.Stack;
        _ingredientChanged?.Invoke(_ingredient);
        RefreshSlots();
        GameAudio.Play(SoundCue.Select);
    }

    private void ShowTooltip(InventoryListItem slot, PointerEventData pointer)
    {
        if (_tooltip == null)
        {
            Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }
            _tooltip = new InventoryTooltip(canvas);
        }

        _hoveredSlot = slot;
        string name = GetItemName(slot.Stack.ItemId);
        _tooltip.Show(name, pointer.position, pointer.enterEventCamera);
    }

    private void HideTooltip(InventoryListItem slot)
    {
        if (_hoveredSlot == slot)
        {
            _hoveredSlot = null;
            _tooltip?.Hide();
        }
    }

    private string GetItemName(string itemId)
    {
        DataStorage data = _dataManager != null ? _dataManager.dataStorage : null;
        if (data?.ItemData != null && data.ItemData.TryGetValue(itemId, out DataStorage.Item item))
        {
            return string.IsNullOrEmpty(item.DISPLAY_NAME) ? item.NAME : data.GetBBStringText(item.DISPLAY_NAME);
        }

        if (data?.Recipe_CombineData != null)
        {
            foreach (DataStorage.Recipe_Combine recipe in data.Recipe_CombineData.Values)
            {
                if (recipe.PRODUCT_DISH_ID == itemId)
                {
                    return recipe.DISPLAY_NAME;
                }
            }
        }
        return itemId;
    }

    public void AddItem(string itemID, int count, int grade = 0)
    {
        TryAddItem(itemID, count, grade);
    }

    public bool TryAddItem(string itemID, int count, int grade = 0)
    {
        if (string.IsNullOrEmpty(itemID) || count <= 0)
        {
            return false;
        }

        ItemStack stack = itemStacks.Find(s => s.ItemID == itemID && s.Grade == grade);
        if (stack != null)
        {
            stack.Count += count;
        }
        else if (itemStacks.Count < MaxSlots)
        {
            itemStacks.Add(new ItemStack { ItemID = itemID, Grade = grade, Count = count });
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Inventory full ({MaxSlots}/{MaxSlots}), cannot add new item '{itemID}'");
            return false;
        }

        if (inventoryPanel != null && inventoryPanel.activeSelf)
            RefreshSlots();

        return true;
    }

    // 등급 구분 없이 합산한 보유 수량
    internal List<SavedItemStack> CaptureStacks()
    {
        List<SavedItemStack> stacks = new List<SavedItemStack>(itemStacks.Count);
        foreach (ItemStack stack in itemStacks)
        {
            stacks.Add(new SavedItemStack { ItemId = stack.ItemID, Grade = stack.Grade, Count = stack.Count });
        }

        return stacks;
    }

    internal void RestoreStacks(SaveData data)
    {
        itemStacks.Clear();
        if (data.ItemStacks != null)
        {
            foreach (SavedItemStack stack in data.ItemStacks)
            {
                AddItem(stack.ItemId, stack.Count, stack.Grade);
            }
        }
        else if (data._playerInventory != null)
        {
            foreach (string itemId in data._playerInventory)
            {
                AddItem(itemId, 1);
            }
        }

        RefreshSlots();
    }

    internal bool TryConvertOne(string inputId, int inputGrade, string outputId, int outputGrade)
    {
        ItemStack input = itemStacks.Find(stack => stack.ItemID == inputId && stack.Grade == inputGrade);
        if (input == null || input.Count <= 0 || string.IsNullOrEmpty(outputId))
        {
            return false;
        }

        ItemStack output = itemStacks.Find(stack => stack.ItemID == outputId && stack.Grade == outputGrade);
        if (output == input)
        {
            return true;
        }
        if (output == null && itemStacks.Count >= MaxSlots && input.Count > 1)
        {
            return false;
        }

        input.Count--;
        if (input.Count == 0)
        {
            itemStacks.Remove(input);
        }
        if (output == null)
        {
            itemStacks.Add(new ItemStack { ItemID = outputId, Grade = outputGrade, Count = 1 });
        }
        else
        {
            output.Count++;
        }
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            RefreshSlots();
        }
        return true;
    }

    internal bool TryCraft(PlatingRecipe recipe)
    {
        List<SavedItemStack> consumed = new List<SavedItemStack>();
        int freed = 0;
        foreach (string itemId in recipe.DistinctItemIDs)
        {
            int remaining = recipe.GetRequiredCount(itemId);
            List<int> grades = GetOwnedGrades(itemId);
            for (int i = grades.Count - 1; i >= 0 && remaining > 0; i--)
            {
                int grade = grades[i];
                int owned = GetCount(itemId, grade);
                int take = Mathf.Min(remaining, owned);
                if (take == owned)
                {
                    freed++;
                }
                consumed.Add(new SavedItemStack { ItemId = itemId, Grade = grade, Count = take });
                remaining -= take;
            }

            if (remaining > 0)
            {
                return false;
            }
        }

        bool hasOutput = !string.IsNullOrEmpty(recipe.outputItemID);
        if (hasOutput && itemStacks.Count - freed >= MaxSlots && GetCount(recipe.outputItemID, 0) == 0)
        {
            return false;
        }

        foreach (SavedItemStack stack in consumed)
        {
            RemoveItem(stack.ItemId, stack.Grade, stack.Count);
        }
        if (hasOutput)
        {
            AddItem(recipe.outputItemID, 1);
        }
        return true;
    }

    public int GetCount(string itemID)
    {
        int total = 0;
        foreach (ItemStack stack in itemStacks)
        {
            if (stack.ItemID == itemID)
                total += stack.Count;
        }
        return total;
    }

    public int GetCount(string itemID, int grade)
    {
        ItemStack stack = itemStacks.Find(s => s.ItemID == itemID && s.Grade == grade);
        return stack != null ? stack.Count : 0;
    }

    // itemID를 보유한 등급 목록 (등급 내림차순)
    public List<int> GetOwnedGrades(string itemID)
    {
        List<int> grades = new List<int>();
        foreach (ItemStack stack in itemStacks)
        {
            if (stack.ItemID == itemID && stack.Count > 0)
                grades.Add(stack.Grade);
        }
        grades.Sort((a, b) => b.CompareTo(a));
        return grades;
    }

    public bool RemoveItem(string itemID, int grade, int count = 1)
    {
        if (string.IsNullOrEmpty(itemID) || count <= 0)
            return false;

        ItemStack stack = itemStacks.Find(s => s.ItemID == itemID && s.Grade == grade);
        if (stack == null || stack.Count < count)
            return false;

        stack.Count -= count;
        if (stack.Count == 0)
            itemStacks.Remove(stack);

        if (inventoryPanel != null && inventoryPanel.activeSelf)
            RefreshSlots();
        return true;
    }

    private void RefreshSlots()
    {
        if (_ingredient.HasValue && GetCount(_ingredient.Value.ItemId, _ingredient.Value.Grade) == 0)
        {
            _ingredient = null;
            _ingredientChanged?.Invoke(null);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < itemStacks.Count)
            {
                ItemStack item = itemStacks[i];
                SavedItemStack stack = new SavedItemStack { ItemId = item.ItemID, Grade = item.Grade, Count = item.Count };
                bool selected = _ingredient?.ItemId == stack.ItemId && _ingredient?.Grade == stack.Grade;
                bool selectable = _inCooking && !_selectionLocked && _ingredientFilter?.Invoke(stack) == true;
                slots[i].SetItem(stack, selectable, selected);
            }
            else
            {
                slots[i].Clear();
            }
        }
    }
}
