using System;
using UnityEngine;

/// <summary>
/// Data layer for the inventory. 9 hotbar slots + 27 storage slots = 36 total.
/// Access via InventorySystem.Instance.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    public const int HotbarSize   = 9;
    public const int StorageRows  = 3;
    public const int StorageSize  = StorageRows * HotbarSize; // 27
    public const int TotalSlots   = HotbarSize + StorageSize; // 36

    public Slot[] Hotbar  { get; private set; } = new Slot[HotbarSize];
    public Slot[] Storage { get; private set; } = new Slot[StorageSize];

    public int SelectedHotbarIndex { get; private set; } = 0;
    public Slot EquippedSlot => Hotbar[SelectedHotbarIndex];
    public bool IsBannerEquipped => !EquippedSlot.IsEmpty && EquippedSlot.Item == ItemData.Banner;

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitSlots();
        GiveStartingItems();
    }

    void InitSlots()
    {
        for (int i = 0; i < HotbarSize; i++)  Hotbar[i]  = new Slot();
        for (int i = 0; i < StorageSize; i++) Storage[i] = new Slot();
    }

    void GiveStartingItems()
    {
        // Banner always starts in hotbar slot 0
        Hotbar[0].Add(ItemData.Banner, 1);
        Hotbar[1].Add(ItemData.Stone,  10);
        Hotbar[2].Add(ItemData.Wood,   10);
        Storage[0].Add(ItemData.Food,  5);
        Storage[1].Add(ItemData.Iron,  3);
    }

    // ── Hotbar selection ─────────────────────────────────────────────────

    public void SelectHotbar(int index)
    {
        SelectedHotbarIndex = Mathf.Clamp(index, 0, HotbarSize - 1);
        OnInventoryChanged?.Invoke();
    }

    public void ScrollHotbar(int delta)
    {
        int next = (SelectedHotbarIndex + delta + HotbarSize) % HotbarSize;
        SelectHotbar(next);
    }

    // ── Item management ──────────────────────────────────────────────────

    public bool AddItem(string itemName, int amount = 1)
    {
        var item = ItemData.Get(itemName);
        if (item == null) return false;

        // Try stacking in hotbar first, then storage
        if (TryStack(Hotbar, item, amount)) { OnInventoryChanged?.Invoke(); return true; }
        if (TryStack(Storage, item, amount)) { OnInventoryChanged?.Invoke(); return true; }
        // Fill empty slot
        if (TryFillEmpty(Hotbar, item, amount)) { OnInventoryChanged?.Invoke(); return true; }
        if (TryFillEmpty(Storage, item, amount)) { OnInventoryChanged?.Invoke(); return true; }
        return false; // inventory full
    }

    public void SwapSlots(bool fromHotbar, int fromIdx, bool toHotbar, int toIdx)
    {
        Slot from = fromHotbar ? Hotbar[fromIdx] : Storage[fromIdx];
        Slot to   = toHotbar   ? Hotbar[toIdx]   : Storage[toIdx];
        (from.Item, to.Item)   = (to.Item, from.Item);
        (from.Count, to.Count) = (to.Count, from.Count);
        OnInventoryChanged?.Invoke();
    }

    static bool TryStack(Slot[] slots, ItemData item, int amount)
    {
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.Item == item && slot.Count + amount <= item.MaxStack)
            { slot.Count += amount; return true; }
        return false;
    }

    static bool TryFillEmpty(Slot[] slots, ItemData item, int amount)
    {
        foreach (var slot in slots)
            if (slot.IsEmpty) { slot.Add(item, amount); return true; }
        return false;
    }
}
