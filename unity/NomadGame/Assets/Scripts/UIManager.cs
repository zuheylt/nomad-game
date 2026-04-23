using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all HUD elements: alien timer, prize pool, event feed, tile claim flow.
/// Requires TextMeshPro. Attach to a Canvas GameObject.
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

    private long _alienArrivalTime;
    private int _pendingClaimX;
    private int _pendingClaimY;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        walletText.text = $"Wallet: {ShortenAddress(config.walletAddress)}";
        claimPanel.SetActive(false);

        claimButton.onClick.AddListener(OnClaimConfirmed);
        cancelButton.onClick.AddListener(() => claimPanel.SetActive(false));

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
        if (_alienArrivalTime > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long left = Mathf.Max(0, (int)(_alienArrivalTime - now));
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
        prizePoolText.text = $"Prize: {mon:F3} MON";
        playerCountText.text = $"Players: {status.playerCount}";
    }

    void OnEvents(GameEvent[] events)
    {
        foreach (Transform child in eventFeedParent)
            Destroy(child.gameObject);

        int count = Mathf.Min(events.Length, MaxEventRows);
        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(eventRowPrefab, eventFeedParent);
            var ev = events[i];
            string icon = ev.eventType switch
            {
                "TILE_CLAIMED"       => "🏕",
                "CURRENCY_CREATED"   => "💰",
                "DEATH"              => "💀",
                "TREASON_ASSIGNED"   => "🗡",
                "TREASON_COMPLETED"  => "🤑",
                _                    => "📡",
            };
            string time = TryParseTime(ev.timestamp);
            row.text = $"{icon} {ev.eventType}  {time}";
        }
    }

    // ── Tile claim flow ──────────────────────────────────────────────────

    public void OnTileClicked(int x, int y)
    {
        if (world.IsClaimed(x, y))
        {
            ShowStatus("Tile already claimed.");
            return;
        }
        _pendingClaimX = x;
        _pendingClaimY = y;
        claimCoordText.text = $"Claim tile ({x}, {y})?";
        claimPanel.SetActive(true);
    }

    void OnClaimConfirmed()
    {
        claimPanel.SetActive(false);
        claimButton.interactable = false;
        ShowStatus("Submitting TX...");
        StartCoroutine(ApiClient.Instance.ClaimTile(
            _pendingClaimX, _pendingClaimY,
            txHash =>
            {
                world.MarkClaimed(_pendingClaimX, _pendingClaimY, config.walletAddress);
                ShowStatus($"Claimed! TX: {txHash[..10]}...");
                claimButton.interactable = true;
            },
            err =>
            {
                ShowStatus($"Error: {err}");
                claimButton.interactable = true;
            }
        ));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    void ShowStatus(string msg)
    {
        statusText.text = msg;
        CancelInvoke(nameof(ClearStatus));
        Invoke(nameof(ClearStatus), 5f);
    }

    void ClearStatus() => statusText.text = "";

    static string ShortenAddress(string addr) =>
        addr.Length > 10 ? $"{addr[..6]}...{addr[^4..]}" : addr;

    static string TryParseTime(string iso)
    {
        if (System.DateTime.TryParse(iso, out var dt))
            return dt.ToLocalTime().ToString("HH:mm:ss");
        return "";
    }
}
