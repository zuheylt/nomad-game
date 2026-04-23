using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds and renders the Minecraft-style inventory.
/// Attach to an empty child of Canvas named "InventoryRoot".
/// No prefabs needed — builds everything in code.
/// Press I to open/close. Press 1-9 or scroll wheel to select hotbar.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Sizing")]
    public int   SlotSize      = 56;
    public int   SlotPadding   = 4;
    public int   HotbarY       = 60;   // pixels from bottom

    // Generated UI
    private GameObject _hotbarPanel;
    private GameObject _storagePanel;
    private GameObject _equippedLabel;
    private SlotUI[]   _hotbarSlots   = new SlotUI[InventorySystem.HotbarSize];
    private SlotUI[]   _storageSlots  = new SlotUI[InventorySystem.StorageSize];

    private bool _storageOpen;

    private static readonly Color PanelBg = new(0.16f, 0.14f, 0.12f, 0.97f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildHotbar();
        BuildStorage();
        BuildEquippedLabel();
        _storagePanel.SetActive(false);
        InventorySystem.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        // 1-9 hotbar selection
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                InventorySystem.Instance.SelectHotbar(i);

        // Scroll wheel
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0.01f)  InventorySystem.Instance.ScrollHotbar(-1);
        if (scroll < -0.01f) InventorySystem.Instance.ScrollHotbar(1);

        // I to toggle storage
        if (Input.GetKeyDown(KeyCode.I)) ToggleStorage();
    }

    public void ToggleStorage()
    {
        _storageOpen = !_storageOpen;
        _storagePanel.SetActive(_storageOpen);
        Cursor.lockState = _storageOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = _storageOpen;
    }

    // ── Build UI ─────────────────────────────────────────────────────────

    void BuildHotbar()
    {
        int totalWidth = InventorySystem.HotbarSize * (SlotSize + SlotPadding) - SlotPadding;

        _hotbarPanel = MakePanel(transform, totalWidth + 16, SlotSize + 16,
                                 new Vector2(0.5f, 0f), new Vector2(0, HotbarY));

        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            var slot = MakeSlot(_hotbarPanel.transform, i, true);
            _hotbarSlots[i] = slot;
        }
    }

    void BuildStorage()
    {
        int cols = InventorySystem.HotbarSize;
        int rows = InventorySystem.StorageRows;
        int totalWidth  = cols * (SlotSize + SlotPadding) - SlotPadding + 16;
        int totalHeight = rows * (SlotSize + SlotPadding) - SlotPadding + 16;

        // Position storage panel just above hotbar
        float storageY = HotbarY + SlotSize + 16 + SlotPadding * 2 + 10;
        _storagePanel = MakePanel(transform, totalWidth, totalHeight,
                                  new Vector2(0.5f, 0f), new Vector2(0, storageY));

        for (int i = 0; i < InventorySystem.StorageSize; i++)
        {
            var slot = MakeSlot(_storagePanel.transform, i, false);
            _storageSlots[i] = slot;
        }
    }

    void BuildEquippedLabel()
    {
        var go = new GameObject("EquippedLabel");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, HotbarY + SlotSize + 22);
        rt.sizeDelta = new Vector2(300, 28);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.fontSize   = 15;
        tmp.color      = Color.white;
        _equippedLabel = go;
    }

    // ── Slot builder ─────────────────────────────────────────────────────

    SlotUI MakeSlot(Transform parent, int index, bool isHotbar)
    {
        var go = new GameObject($"Slot_{index}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(SlotSize, SlotSize);

        int cols = InventorySystem.HotbarSize;
        int col  = index % cols;
        int row  = index / cols;
        float x  = 8 + col * (SlotSize + SlotPadding) + SlotSize * 0.5f;
        float y  = -8 - row * (SlotSize + SlotPadding) - SlotSize * 0.5f;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);

        // Needs GraphicRaycaster-compatible canvas
        go.AddComponent<CanvasRenderer>();

        var ui = go.AddComponent<SlotUI>();
        ui.IsHotbar = isHotbar;
        ui.Index    = index;
        ui.Build();
        return ui;
    }

    // ── Panel factory ────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, int w, int h, Vector2 anchor, Vector2 pos)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = PanelBg;
        return go;
    }

    // ── Refresh ──────────────────────────────────────────────────────────

    void Refresh()
    {
        var inv = InventorySystem.Instance;

        for (int i = 0; i < InventorySystem.HotbarSize; i++)
            _hotbarSlots[i].Refresh(inv.Hotbar[i], i == inv.SelectedHotbarIndex);

        for (int i = 0; i < InventorySystem.StorageSize; i++)
            _storageSlots[i].Refresh(inv.Storage[i]);

        // Equipped label
        if (_equippedLabel != null)
        {
            var tmp = _equippedLabel.GetComponent<TextMeshProUGUI>();
            var slot = inv.EquippedSlot;
            tmp.text = slot.IsEmpty ? "" : $"{slot.Item.Emoji}  {slot.Item.Name}";
            tmp.color = inv.IsBannerEquipped ? new Color(0.95f, 0.80f, 0.1f) : Color.white;
        }
    }
}
