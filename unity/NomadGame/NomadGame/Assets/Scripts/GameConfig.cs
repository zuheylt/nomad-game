using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Nomad/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Backend")]
    public string backendUrl = "http://localhost:8080";

    [Header("Player")]
    public string walletAddress = "0xYourWalletAddressHere";
    public string playerName = "Nomad";

    [Header("World")]
    public int gridMin = -10;
    public int gridMax = 10;
    public float tileSize = 1f;
    public float tileHeight = 0.2f;

    [Header("Colors")]
    public Color unclaimedColor = new Color(0.1f, 0.12f, 0.2f);
    public Color ownTileColor = new Color(0.2f, 0.8f, 0.4f);
}
