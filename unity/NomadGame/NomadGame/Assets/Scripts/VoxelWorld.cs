using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates the 20×20 land grid and colors tiles by owner.
/// Attach to an empty GameObject named "World".
/// </summary>
public class VoxelWorld : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private Material tileMaterial;

    private readonly Dictionary<(int, int), GameObject> _tiles = new();
    private readonly Dictionary<(int, int), string> _owners = new();

    void Start()
    {
        GenerateGrid();
        ApiClient.Instance.OnTilesReceived += ApplyOwnership;
        InvokeRepeating(nameof(RefreshTiles), 5f, 5f);
    }

    void OnDestroy()
    {
        if (ApiClient.Instance != null)
            ApiClient.Instance.OnTilesReceived -= ApplyOwnership;
    }

    void GenerateGrid()
    {
        int min = config.gridMin;
        int max = config.gridMax;
        float s = config.tileSize;
        float h = config.tileHeight;

        for (int x = min; x <= max; x++)
        {
            for (int y = min; y <= max; y++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Tile_{x}_{y}";
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(x * s, 0f, y * s);
                go.transform.localScale = new Vector3(s * 0.95f, h, s * 0.95f);

                var mat = new Material(tileMaterial);
                mat.color = config.unclaimedColor;
                go.GetComponent<Renderer>().material = mat;

                var tc = go.AddComponent<TileClickHandler>();
                tc.X = x;
                tc.Y = y;

                _tiles[(x, y)] = go;
            }
        }
    }

    void ApplyOwnership(WorldTile[] tiles)
    {
        _owners.Clear();
        foreach (var tile in tiles)
        {
            if (!string.IsNullOrEmpty(tile.ownerWallet))
                _owners[(tile.id.x, tile.id.y)] = tile.ownerWallet;
        }
        RefreshColors();
    }

    void RefreshColors()
    {
        string myWallet = config.walletAddress.ToLowerInvariant();
        foreach (var kvp in _tiles)
        {
            var key = kvp.Key;
            var go = kvp.Value;
            Color color;
            if (_owners.TryGetValue(key, out string owner))
            {
                color = owner.ToLowerInvariant() == myWallet
                    ? config.ownTileColor
                    : WalletToColor(owner);
            }
            else
            {
                color = config.unclaimedColor;
            }
            go.GetComponent<Renderer>().material.color = color;
        }
    }

    void RefreshTiles() => StartCoroutine(ApiClient.Instance.FetchTiles());

    public bool IsClaimed(int x, int y) => _owners.ContainsKey((x, y));

    public void MarkClaimed(int x, int y, string wallet)
    {
        _owners[(x, y)] = wallet;
        RefreshColors();
    }

    static Color WalletToColor(string wallet)
    {
        int hash = 0;
        foreach (char c in wallet) hash = c + ((hash << 5) - hash);
        float h = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(h, 0.7f, 0.6f);
    }
}
