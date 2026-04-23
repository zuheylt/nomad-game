using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// All HTTP calls to the Nomad backend. Attach to a persistent GameObject.
/// </summary>
public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [SerializeField] private GameConfig config;

    public event Action<GameEvent[]> OnEventsReceived;
    public event Action<GameStatus> OnStatusReceived;
    public event Action<WorldTile[]> OnTilesReceived;

    private float _eventPollTimer;
    private float _statusPollTimer;
    private const float EventPollInterval = 3f;
    private const float StatusPollInterval = 10f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(RegisterPlayer());
        StartCoroutine(FetchTiles());
    }

    void Update()
    {
        _eventPollTimer += Time.deltaTime;
        _statusPollTimer += Time.deltaTime;

        if (_eventPollTimer >= EventPollInterval)
        {
            _eventPollTimer = 0f;
            StartCoroutine(FetchEvents());
        }
        if (_statusPollTimer >= StatusPollInterval)
        {
            _statusPollTimer = 0f;
            StartCoroutine(FetchStatus());
        }
    }

    // ── Player ──────────────────────────────────────────────────────────

    public IEnumerator RegisterPlayer()
    {
        string body = $"{{\"wallet\":\"{config.walletAddress}\"}}";
        yield return Post("/api/game/players/register", body, _ => { });
        StartCoroutine(FetchStatus());
    }

    public void SendMove(float x, float y, float z)
    {
        string body = $"{{\"wallet\":\"{config.walletAddress}\",\"x\":{x:F2},\"y\":{y:F2},\"z\":{z:F2}}}";
        StartCoroutine(Post("/api/game/players/move", body, _ => { }));
    }

    // ── World ────────────────────────────────────────────────────────────

    public IEnumerator FetchTiles()
    {
        int min = config.gridMin;
        int max = config.gridMax;
        yield return Get($"/api/world/tiles?x1={min}&y1={min}&x2={max}&y2={max}", json =>
        {
            var wrapper = JsonUtility.FromJson<WorldTileArray>("{\"items\":" + json + "}");
            OnTilesReceived?.Invoke(wrapper.items);
        });
    }

    public IEnumerator ClaimTile(int x, int y, Action<string> onTxHash, Action<string> onError)
    {
        string body = $"{{\"wallet\":\"{config.walletAddress}\",\"x\":{x},\"y\":{y}}}";
        yield return Post("/api/world/claim", body, json =>
        {
            var resp = JsonUtility.FromJson<ClaimResponse>(json);
            if (!string.IsNullOrEmpty(resp.txHash))
                onTxHash?.Invoke(resp.txHash);
            else
                onError?.Invoke(resp.error ?? "Unknown error");
        }, onError);
    }

    // ── Events & Status ──────────────────────────────────────────────────

    private IEnumerator FetchEvents()
    {
        yield return Get("/api/events?limit=10", json =>
        {
            var wrapper = JsonUtility.FromJson<GameEventArray>("{\"items\":" + json + "}");
            OnEventsReceived?.Invoke(wrapper.items);
        });
    }

    private IEnumerator FetchStatus()
    {
        yield return Get("/api/game/status", json =>
        {
            var status = JsonUtility.FromJson<GameStatus>(json);
            OnStatusReceived?.Invoke(status);
        });
    }

    // ── HTTP helpers ─────────────────────────────────────────────────────

    private IEnumerator Get(string path, Action<string> onSuccess)
    {
        using var req = UnityWebRequest.Get(config.backendUrl + path);
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else
            Debug.LogWarning($"GET {path} failed: {req.error}");
    }

    private IEnumerator Post(string path, string jsonBody, Action<string> onSuccess, Action<string> onError = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
        using var req = new UnityWebRequest(config.backendUrl + path, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else
        {
            Debug.LogWarning($"POST {path} failed: {req.error}");
            onError?.Invoke(req.error);
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────

    [Serializable] public class WorldTileArray { public WorldTile[] items; }
    [Serializable] public class GameEventArray  { public GameEvent[] items; }
    [Serializable] public class ClaimResponse   { public string txHash; public string error; }
}

// ── Data models (must be serializable for JsonUtility) ───────────────────────

[Serializable]
public class WorldTile
{
    public TileId id;
    public string ownerWallet;
    public string buildingType;
    public int resourceLevel;
}

[Serializable]
public class TileId
{
    public int x;
    public int y;
}

[Serializable]
public class GameEvent
{
    public int id;
    public string txHash;
    public string eventType;
    public string timestamp;
}

[Serializable]
public class GameStatus
{
    public long alienArrivalTime;
    public long secondsUntilAliens;
    public long prizePoolWei;
    public int playerCount;
}
