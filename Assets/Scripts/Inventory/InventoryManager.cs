using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameEnumDefines;

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
        public int Count;
    }

    private readonly List<ItemStack> itemStacks = new List<ItemStack>();
    private readonly List<InventoryListItem> slots = new List<InventoryListItem>();

    private GamePlay.InputManager inputManager;
    private InputAction toggleAction;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<GamePlay.InputManager>();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        CreateSlots();
        SubscribeToggleAction();
    }

    private void OnDestroy()
    {
        if (toggleAction != null)
            toggleAction.started -= OnToggleInventory;
    }

    private void CreateSlots()
    {
        if (slotParent == null || slotPrefab == null)
            return;

        for (int i = 0; i < MaxSlots; i++)
        {
            InventoryListItem slot = Instantiate(slotPrefab, slotParent);
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
        RefreshSlots();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Popup);
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (inputManager != null)
            inputManager.SwitchInputState(InputState.Default);
    }

    public void AddItem(string itemID, int count)
    {
        if (string.IsNullOrEmpty(itemID) || count <= 0)
            return;

        ItemStack stack = itemStacks.Find(s => s.ItemID == itemID);
        if (stack != null)
        {
            stack.Count += count;
        }
        else if (itemStacks.Count < MaxSlots)
        {
            itemStacks.Add(new ItemStack { ItemID = itemID, Count = count });
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Inventory full ({MaxSlots}/{MaxSlots}), cannot add new item '{itemID}'");
            return;
        }

        if (inventoryPanel != null && inventoryPanel.activeSelf)
            RefreshSlots();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < itemStacks.Count)
                slots[i].SetItem(itemStacks[i].ItemID, itemStacks[i].Count);
            else
                slots[i].Clear();
        }
    }
}
