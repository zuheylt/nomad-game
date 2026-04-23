using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central HUD manager. Handles alien timer, event feed, claim panel,
/// land banner, inventory panel, and status toasts.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private GameConfig config;
    [SerializeField] private VoxelWorld world;

    [Header("HUD Text")]
    [SerializeField] private TMP_Text alienTimerText;
    [SerializeField] private TMP_Text prizePoolText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text walletText;
    [SerializeField] private TMP_Text statusText;

    [Header("Event Feed")]
    [SerializeField] private Transform eventFeedParent;
    [SerializeField] private TMP_Text eventRowPrefab;
    private const int MaxEventRows = 6;

    [Header("Claim Panel")]
    [SerializeField] private GameObject claimPanel;
    [SerializeField] private TMP_Text claimCoordText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button cancelButton;

    [Header("Land Banner")]
    [SerializeField] private GameObject landBanner;
    [SerializeField] private TMP_Text landBannerText;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshair;

    private long _alienArrivalTime;
    private int _pendingClaimX;
    private int _pendingClaimY;
    private GameObject      _deathScreen;
    private TMPro.TMP_Text  _deathTxText;
    private TMPro.TMP_Text  _deathCountdownText;
    private bool            _deathScreenActive;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (walletText) walletText.text = $"Wallet: {ShortenAddress(config.walletAddress)}";
        claimPanel.SetActive(false);
        landBanner.SetActive(false);

        if (claimButton)
            claimButton.onClick.AddListener(OnClaimConfirmed);
        else
            Debug.LogError("UIManager: claimButton not assigned in Inspector!");

        if (cancelButton)
            cancelButton.onClick.AddListener(() =>
            {
                claimPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            });
        else
            Debug.LogError("UIManager: cancelButton not assigned in Inspector!");

        BuildDeathScreen();

        ApiClient.Instance.OnStatusReceived += OnStatus;
        ApiClient.Instance.OnEventsReceived += OnEvents;
    }

    void OnDestroy()
    {
        if (ApiClient.Instance == null) return;
        ApiClient.Instance.OnStatusReceived -= OnStatus;
        ApiClient.Instance.OnEventsReceived -= OnEvents;
    }

    void Update()
    {
        if (_alienArrivalTime > 0 && alienTimerText)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long left = Math.Max(0, _alienArrivalTime - now);
            long h = left / 3600;
            long m = (left % 3600) / 60;
            long s = left % 60;
            alienTimerText.text = $"{h:D2}:{m:D2}:{s:D2}";
            alienTimerText.color = left < 3600 ? Color.red : Color.white;
        }
    }

    // ── Backend callbacks ────────────────────────────────────────────────

    void OnStatus(GameStatus status)
    {
        _alienArrivalTime = status.alienArrivalTime;
        float mon = status.prizePoolWei / 1e18f;
        if (prizePoolText)   prizePoolText.text   = $"Prize: {mon:F3} MON";
        if (playerCountText) playerCountText.text = $"Players: {status.playerCount}";
    }

    void OnEvents(GameEvent[] events)
    {
        if (eventFeedParent == null || eventRowPrefab == null) return;
        foreach (Transform child in eventFeedParent) Destroy(child.gameObject);

        int count = Mathf.Min(events.Length, MaxEventRows);
        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(eventRowPrefab, eventFeedParent);
            var ev = events[i];
            string icon = ev.eventType switch
            {
                "TILE_CLAIMED"      => "🏕",
                "CURRENCY_CREATED"  => "💰",
                "DEATH"             => "💀",
                "TREASON_ASSIGNED"  => "🗡",
                "TREASON_COMPLETED" => "🤑",
                _                   => "📡",
            };
            row.text = $"{icon} {ev.eventType}  {TryParseTime(ev.timestamp)}";
        }
    }

    // ── Land Banner ──────────────────────────────────────────────────────

    public void ShowLandBanner(bool unclaimed, int x, int y)
    {
        if (landBanner == null) return;
        if (unclaimed)
        {
            landBanner.SetActive(true);
            if (landBannerText) landBannerText.text = $"[E]  Claim this land  ({x}, {y})";
        }
        else
        {
            landBanner.SetActive(false);
        }
    }

    // ── Death Screen ─────────────────────────────────────────────────────

    void BuildDeathScreen()
    {
        // Own canvas so it's never affected by other canvas sorting
        var canvasGo = new GameObject("DeathCanvas");
        var c = canvasGo.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 200;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        DontDestroyOnLoad(canvasGo);

        _deathScreen = new GameObject("DeathScreen");
        _deathScreen.transform.SetParent(canvasGo.transform, false);
        var rt = _deathScreen.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _deathScreen.AddComponent<Image>().color = new Color(0.55f, 0f, 0f, 0.88f);

        TMPro.TextMeshProUGUI MakeTmp(string name, float y0, float y1, float size)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(_deathScreen.transform, false);
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize  = size;
            tmp.color     = Color.white;
            var r = tmp.rectTransform;
            r.anchorMin = new Vector2(0.1f, y0);
            r.anchorMax = new Vector2(0.9f, y1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return tmp;
        }

        var title = MakeTmp("Title", 0.56f, 0.76f, 72f);
        title.text      = "YOU DIED";
        title.fontStyle = TMPro.FontStyles.Bold;

        _deathTxText        = MakeTmp("TxHash",    0.45f, 0.55f, 13f);
        _deathTxText.color  = new Color(1f, 0.6f, 0.6f, 1f);

        _deathCountdownText = MakeTmp("Countdown", 0.36f, 0.46f, 24f);

        _deathScreen.SetActive(false);
        _deathScreenActive = false;
    }

    public void ShowDeathScreen(string txHash = null)
    {
        if (_deathScreen == null) BuildDeathScreen();
        if (_deathScreenActive) return;          // already showing — don't restart
        _deathScreenActive = true;

        if (_deathTxText != null)
            _deathTxText.text = txHash != null ? $"TX: {txHash[..Math.Min(20, txHash.Length)]}..." : "";

        _deathScreen.SetActive(true);
        StartCoroutine(DeathCountdown(4));
    }

    IEnumerator DeathCountdown(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (_deathCountdownText) _deathCountdownText.text = $"Respawning in {i}...";
            yield return new WaitForSeconds(1f);
        }
    }

    public void HideDeathScreen()
    {
        _deathScreenActive = false;
        if (_deathScreen != null) _deathScreen.SetActive(false);
    }

    // ── Inventory (delegates to InventorySystem) ─────────────────────────

    public void AddItem(string item, int amount = 1)
    {
        InventorySystem.Instance?.AddItem(item, amount);
    }

    // ── Tile claim flow ──────────────────────────────────────────────────

    public void OnTileClicked(int x, int y)
    {
        if (world != null && world.IsClaimed(x, y))
        {
            ShowStatus("Tile already claimed.");
            return;
        }
        _pendingClaimX = x;
        _pendingClaimY = y;
        if (claimCoordText) claimCoordText.text = $"Claim tile ({x}, {y})?";
        claimPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnClaimConfirmed()
    {
        claimPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        claimButton.interactable = false;
        ShowStatus("Submitting TX...");
        StartCoroutine(ApiClient.Instance.ClaimTile(
            _pendingClaimX, _pendingClaimY,
            txHash =>
            {
                world?.MarkClaimed(_pendingClaimX, _pendingClaimY, config.walletAddress);
                ShowStatus($"Claimed! TX: {txHash[..10]}...");
                AddItem("Stone", 5);
                claimButton.interactable = true;
            },
            err =>
            {
                ShowStatus($"Error: {err}");
                claimButton.interactable = true;
            }
        ));
    }

    // ── Status toast ─────────────────────────────────────────────────────

    public void ShowStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        CancelInvoke(nameof(ClearStatus));
        Invoke(nameof(ClearStatus), 5f);
    }

    void ClearStatus() { if (statusText) statusText.text = ""; }

    // ── Helpers ──────────────────────────────────────────────────────────

    static string ShortenAddress(string addr) =>
        addr != null && addr.Length > 10 ? $"{addr[..6]}...{addr[^4..]}" : addr ?? "";

    static string TryParseTime(string iso)
    {
        if (DateTime.TryParse(iso, out var dt)) return dt.ToLocalTime().ToString("HH:mm:ss");
        return "";
    }
}
