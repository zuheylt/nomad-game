using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// All item definitions. Add new items here.
/// </summary>
public class ItemData
{
    public string Name;
    public Color Color;
    public int MaxStack;
    public bool IsTool;
    public string Emoji;

    // ── Item registry ────────────────────────────────────────────────────
    public static readonly ItemData Banner   = new() { Name = "Banner",  Color = new Color(0.95f, 0.80f, 0.1f), MaxStack = 1,  IsTool = true,  Emoji = "🚩" };
    public static readonly ItemData Stone    = new() { Name = "Stone",   Color = new Color(0.55f, 0.55f, 0.55f), MaxStack = 64, IsTool = false, Emoji = "🪨" };
    public static readonly ItemData Wood     = new() { Name = "Wood",    Color = new Color(0.60f, 0.40f, 0.20f), MaxStack = 64, IsTool = false, Emoji = "🪵" };
    public static readonly ItemData Iron     = new() { Name = "Iron",    Color = new Color(0.75f, 0.80f, 0.85f), MaxStack = 64, IsTool = false, Emoji = "⚙" };
    public static readonly ItemData Food     = new() { Name = "Food",    Color = new Color(0.90f, 0.50f, 0.20f), MaxStack = 64, IsTool = false, Emoji = "🍖" };

    public static readonly Dictionary<string, ItemData> All = new()
    {
        { "Banner", Banner }, { "Stone", Stone }, { "Wood", Wood },
        { "Iron", Iron }, { "Food", Food }
    };

    public static ItemData Get(string name) =>
        All.TryGetValue(name, out var d) ? d : null;
}

/// <summary>
/// One inventory slot — item + count. Null item = empty slot.
/// </summary>
public class Slot
{
    public ItemData Item;
    public int Count;

    public bool IsEmpty => Item == null || Count <= 0;

    public void Clear() { Item = null; Count = 0; }

    public void Add(ItemData item, int amount = 1)
    {
        if (IsEmpty) { Item = item; Count = 0; }
        Count += amount;
    }
}
