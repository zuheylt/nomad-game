using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Attached to each slot GameObject. Renders item and handles click-to-swap.
/// </summary>
public class SlotUI : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public bool IsHotbar;
    [HideInInspector] public int  Index;

    private Image     _background;
    private Image     _icon;
    private TMP_Text  _countText;
    private Image     _selection;

    // Colors
    private static readonly Color SlotBg       = new(0.12f, 0.12f, 0.12f, 0.95f);
    private static readonly Color SlotBorder    = new(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Color SelectedColor = new(1f, 1f, 1f, 0.6f);
    private static readonly Color EmptyIcon     = new(0f, 0f, 0f, 0f);

    public void Build()
    {
        // Background
        _background = gameObject.AddComponent<Image>();
        _background.color = SlotBg;

        // Border via outline child
        var border = new GameObject("Border").AddComponent<Image>();
        border.transform.SetParent(transform, false);
        border.color = SlotBorder;
        border.rectTransform.anchorMin = Vector2.zero;
        border.rectTransform.anchorMax = Vector2.one;
        border.rectTransform.offsetMin = new Vector2(-2, -2);
        border.rectTransform.offsetMax = new Vector2(2, 2);
        border.transform.SetAsFirstSibling();

        // Item icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(transform, false);
        _icon = iconGo.AddComponent<Image>();
        _icon.rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
        _icon.rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
        _icon.rectTransform.offsetMin = Vector2.zero;
        _icon.rectTransform.offsetMax = Vector2.zero;
        _icon.color = EmptyIcon;

        // Count text
        var countGo = new GameObject("Count");
        countGo.transform.SetParent(transform, false);
        _countText = countGo.AddComponent<TextMeshProUGUI>();
        _countText.fontSize = 11;
        _countText.alignment = TextAlignmentOptions.BottomRight;
        _countText.rectTransform.anchorMin = Vector2.zero;
        _countText.rectTransform.anchorMax = Vector2.one;
        _countText.rectTransform.offsetMin = new Vector2(2, 2);
        _countText.rectTransform.offsetMax = new Vector2(-2, -2);
        _countText.color = Color.white;

        // Selection highlight (hotbar only)
        if (IsHotbar)
        {
            var selGo = new GameObject("Selection");
            selGo.transform.SetParent(transform, false);
            _selection = selGo.AddComponent<Image>();
            _selection.color = EmptyIcon;
            _selection.rectTransform.anchorMin = Vector2.zero;
            _selection.rectTransform.anchorMax = Vector2.one;
            _selection.rectTransform.offsetMin = Vector2.zero;
            _selection.rectTransform.offsetMax = Vector2.zero;
        }
    }

    public void Refresh(Slot slot, bool selected = false)
    {
        if (slot == null || slot.IsEmpty)
        {
            _icon.color = EmptyIcon;
            _countText.text = "";
        }
        else
        {
            _icon.color = slot.Item.Color;
            _countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
        }

        if (_selection != null)
            _selection.color = selected ? SelectedColor : EmptyIcon;
    }

    // Click: pick up or swap with held item (simple click-to-move)
    private static bool  _holding;
    private static bool  _holdingFromHotbar;
    private static int   _holdingIndex;

    public void OnPointerClick(PointerEventData _)
    {
        if (!_holding)
        {
            var slot = IsHotbar
                ? InventorySystem.Instance.Hotbar[Index]
                : InventorySystem.Instance.Storage[Index];
            if (slot.IsEmpty) return;

            _holding = true;
            _holdingFromHotbar = IsHotbar;
            _holdingIndex = Index;
        }
        else
        {
            InventorySystem.Instance.SwapSlots(_holdingFromHotbar, _holdingIndex, IsHotbar, Index);
            _holding = false;
        }
    }
}
