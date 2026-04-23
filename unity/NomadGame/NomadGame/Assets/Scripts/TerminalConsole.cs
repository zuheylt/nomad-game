using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

/// <summary>
/// In-game terminal. Toggle with backtick (`).
/// Attach to any GameObject — builds its own Canvas UI.
/// Commands: /help /status /claim /kill /treason /events /players /clear /wallet
/// </summary>
public class TerminalConsole : MonoBehaviour
{
    public static TerminalConsole Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameConfig config;
    [SerializeField] private VoxelWorld world;

    // Built at runtime
    private GameObject     _panel;
    private TMP_Text       _logText;
    private TMP_InputField _inputField;
    private ScrollRect     _scrollRect;

    private readonly List<string> _lines = new();
    private const int MaxLines = 100;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        _panel.SetActive(false);
        IsOpen = false;
    }

    void Start()
    {
        _inputField.onSubmit.AddListener(OnSubmit);
        ApiClient.Instance.OnEventsReceived += OnLiveEvent;
        Log("<color=#aaffaa>Nomad Terminal  —  type /help</color>");
    }

    void OnDestroy()
    {
        if (ApiClient.Instance != null)
            ApiClient.Instance.OnEventsReceived -= OnLiveEvent;
    }

    // ── Build UI ──────────────────────────────────────────────────────────

    void BuildUI()
    {
        // Find or create a Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : CreateCanvas();

        // Dark semi-transparent panel (left half of screen)
        _panel = new GameObject("TerminalPanel");
        _panel.transform.SetParent(canvasRoot, false);
        var panelRt = _panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0.6f);
        panelRt.offsetMin = new Vector2(10, 10);
        panelRt.offsetMax = new Vector2(-10, -10);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        // ScrollRect for log
        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(_panel.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0.08f);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(8, 4);
        scrollRt.offsetMax = new Vector2(-8, -8);

        _scrollRect = scrollGo.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _logText = content.AddComponent<TextMeshProUGUI>();
        _logText.fontSize   = 13;
        _logText.color      = Color.white;
        _logText.richText   = true;
        _logText.enableWordWrapping = true;

        _scrollRect.viewport = vpRt;
        _scrollRect.content  = contentRt;

        // Input field strip at bottom
        var inputGo = new GameObject("InputField");
        inputGo.transform.SetParent(_panel.transform, false);
        var inputRt = inputGo.AddComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0, 0);
        inputRt.anchorMax = new Vector2(1, 0.07f);
        inputRt.offsetMin = new Vector2(8, 4);
        inputRt.offsetMax = new Vector2(-8, -2);
        var inputImg = inputGo.AddComponent<Image>();
        inputImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        _inputField = inputGo.AddComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGo.transform, false);
        var taRt = textArea.AddComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(5, 2);
        taRt.offsetMax = new Vector2(-5, -2);
        textArea.AddComponent<RectMask2D>();

        var inputText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        inputText.transform.SetParent(textArea.transform, false);
        var itRt = inputText.GetComponent<RectTransform>();
        itRt.anchorMin = Vector2.zero;
        itRt.anchorMax = Vector2.one;
        itRt.offsetMin = itRt.offsetMax = Vector2.zero;
        inputText.fontSize = 13;
        inputText.color = Color.white;

        var placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        placeholder.transform.SetParent(textArea.transform, false);
        var phRt = placeholder.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;
        placeholder.fontSize = 13;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.text = "type a command...";

        _inputField.textViewport    = taRt;
        _inputField.textComponent   = inputText;
        _inputField.placeholder     = placeholder;
        _inputField.caretColor      = Color.white;
        _inputField.selectionColor  = new Color(0.3f, 0.6f, 1f, 0.4f);
    }

    Transform CreateCanvas()
    {
        var go = new GameObject("TerminalCanvas");
        var c  = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 99;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    // ── Toggle ───────────────────────────────────────────────────────────

    public void Toggle()
    {
        IsOpen = !IsOpen;
        _panel.SetActive(IsOpen);
        if (IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _inputField.ActivateInputField();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _inputField.DeactivateInputField();
        }
    }

    // ── Live event stream ────────────────────────────────────────────────

    void OnLiveEvent(GameEvent[] events)
    {
        if (events == null || events.Length == 0) return;
        var e = events[0];
        string color = e.eventType switch
        {
            "DEATH"              => "#ff6666",
            "TREASON_ASSIGNED"   => "#ffaa00",
            "TREASON_COMPLETED"  => "#ffcc00",
            "TILE_CLAIMED"       => "#66ff99",
            "CURRENCY_CREATED"   => "#66ccff",
            _                    => "#cccccc",
        };
        string icon = e.eventType switch
        {
            "DEATH"             => "💀",
            "TREASON_ASSIGNED"  => "🗡",
            "TREASON_COMPLETED" => "🤑",
            "TILE_CLAIMED"      => "🏕",
            "CURRENCY_CREATED"  => "💰",
            _                   => "📡",
        };
        string tx = e.txHash != null ? $"  tx:{e.txHash[..Math.Min(8, e.txHash.Length)]}.." : "";
        Log($"<color={color}>{icon} [{e.eventType}]{tx}</color>");
    }

    // ── Commands ─────────────────────────────────────────────────────────

    void OnSubmit(string input)
    {
        input = input.Trim();
        _inputField.text = "";
        _inputField.ActivateInputField();
        if (string.IsNullOrEmpty(input)) return;

        Log($"<color=#888888>> {input}</color>");
        string[] parts = input.Split(' ');
        string cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "/help":
                Log("  /status              alien countdown + prize pool");
                Log("  /claim               claim tile under your feet");
                Log("  /kill                kill yourself (on-chain TX + death screen)");
                Log("  /kill &lt;wallet&gt;      kill another player (on-chain TX)");
                Log("  /treason             assign treason mission to random player");
                Log("  /events              fetch latest 10 events");
                Log("  /players             list online players");
                Log("  /wallet              show your wallet address");
                Log("  /clear               clear terminal");
                break;

            case "/status":
                StartCoroutine(RunStatus());
                break;

            case "/claim":
                StartCoroutine(HandleClaim());
                break;

            case "/kill":
                string killTarget = parts.Length >= 2 ? parts[1] : config?.walletAddress;
                bool killingSelf  = killTarget == config?.walletAddress;
                Log($"<color=#ff6666>💀 Recording death of {killTarget[..Math.Min(10, killTarget?.Length ?? 0)]}... (on-chain TX)</color>");
                StartCoroutine(PostJson("/api/game/death",
                    $"{{\"victim\":\"{killTarget}\",\"killer\":\"{config?.walletAddress}\"}}",
                    json =>
                    {
                        string txHash = Extract(json, "txHash");
                        Log($"<color=#66ff99>✓ Death on-chain  TX: {txHash[..Math.Min(12, txHash.Length)]}..</color>");
                        if (killingSelf)
                        {
                            var pc = FindAnyObjectByType<PlayerController>();
                            pc?.Die(txHash);
                        }
                    },
                    err => Log($"<color=red>✗ {err}</color>")));
                break;

            case "/treason":
                Log("<color=#ffaa00>🗡 Assigning treason mission...</color>");
                StartCoroutine(PostJson("/api/game/treason/assign", "{}",
                    _ => Log("<color=#ffcc00>🤑 Treason assigned! Check the dashboard.</color>"),
                    err => Log($"<color=red>✗ {err}</color>")));
                break;

            case "/events":
                StartCoroutine(RunEvents());
                break;

            case "/players":
                StartCoroutine(RunPlayers());
                break;

            case "/wallet":
                Log($"<color=#66ccff>Wallet: {config?.walletAddress}</color>");
                break;

            case "/clear":
                _lines.Clear();
                _logText.text = "";
                break;

            default:
                Log($"<color=red>Unknown: {cmd}. Type /help</color>");
                break;
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    IEnumerator RunStatus()
    {
        Log("Fetching status...");
        GameStatus captured = null;
        void Handler(GameStatus s) { captured = s; }
        ApiClient.Instance.OnStatusReceived += Handler;
        yield return StartCoroutine(ApiClient.Instance.FetchStatus());
        ApiClient.Instance.OnStatusReceived -= Handler;

        if (captured != null)
        {
            long h = captured.secondsUntilAliens / 3600;
            long m = (captured.secondsUntilAliens % 3600) / 60;
            float mon = captured.prizePoolWei / 1e18f;
            Log($"<color=#ff6666>👽 Aliens arrive in: {h}h {m}m</color>");
            Log($"<color=#ffdd44>💰 Prize pool: {mon:F4} MON</color>");
            Log($"<color=#aaffaa>👥 Players online: {captured.playerCount}</color>");
        }
        else
            Log("<color=orange>Backend offline or no data yet.</color>");
    }

    IEnumerator HandleClaim()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) { Log("<color=red>No player found.</color>"); yield break; }

        int x = pc.CurrentTileX;
        int y = pc.CurrentTileY;

        if (world != null && world.IsClaimed(x, y))
        {
            Log($"<color=red>Tile ({x},{y}) already claimed.</color>");
            yield break;
        }
        Log($"Claiming tile ({x},{y})...");
        yield return StartCoroutine(ApiClient.Instance.ClaimTile(x, y,
            txHash => Log($"<color=#66ff99>✓ Claimed ({x},{y})  TX: {txHash[..Math.Min(12, txHash.Length)]}...</color>"),
            err    => Log($"<color=red>✗ {err}</color>")
        ));
    }

    IEnumerator RunEvents()
    {
        Log("Fetching events...");
        yield return GetJson("/api/events?limit=10", json =>
        {
            int count = 0;
            foreach (string seg in json.Split('{'))
            {
                if (!seg.Contains("eventType")) continue;
                string type = Extract(seg, "eventType");
                string ts   = Extract(seg, "timestamp");
                string tx   = Extract(seg, "txHash");
                Log($"  <color=#cccccc>[{type}] {TryParseTime(ts)}  {(tx.Length > 8 ? tx[..8] + ".." : tx)}</color>");
                if (++count >= 10) break;
            }
            if (count == 0) Log("  No events yet.");
        });
    }

    IEnumerator RunPlayers()
    {
        Log("Fetching players...");
        yield return GetJson("/api/game/players", json =>
        {
            int count = 0;
            foreach (string seg in json.Split('{'))
            {
                if (!seg.Contains("walletAddress")) continue;
                string wallet = Extract(seg, "walletAddress");
                Log($"  <color=#aaffaa>• {wallet[..Math.Min(10, wallet.Length)]}...</color>");
                count++;
            }
            if (count == 0) Log("  No players yet.");
            else Log($"  Total: {count}");
        });
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────

    IEnumerator GetJson(string path, Action<string> onDone)
    {
        string url = (config != null ? config.backendUrl : "http://localhost:8080") + path;
        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onDone?.Invoke(req.downloadHandler.text);
        else
            Log($"<color=red>✗ {req.error}</color>");
    }

    IEnumerator PostJson(string path, string body, Action<string> onSuccess, Action<string> onError)
    {
        string url = (config != null ? config.backendUrl : "http://localhost:8080") + path;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else
            onError?.Invoke(req.error);
    }

    // ── Misc helpers ──────────────────────────────────────────────────────

    public void Log(string line)
    {
        _lines.Add(line);
        if (_lines.Count > MaxLines) _lines.RemoveAt(0);
        _logText.text = string.Join("\n", _lines);
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
    }

    static string Extract(string segment, string key)
    {
        int i = segment.IndexOf($"\"{key}\"");
        if (i < 0) return "";
        int colon = segment.IndexOf(':', i);
        if (colon < 0) return "";
        int q1 = segment.IndexOf('"', colon + 1);
        if (q1 < 0) return "";
        int q2 = segment.IndexOf('"', q1 + 1);
        return q2 > q1 ? segment[(q1 + 1)..q2] : "";
    }

    static string TryParseTime(string iso)
    {
        if (DateTime.TryParse(iso, out var dt)) return dt.ToLocalTime().ToString("HH:mm:ss");
        return iso;
    }
}
