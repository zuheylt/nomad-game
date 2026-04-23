using UnityEngine;

/// <summary>
/// Added to each tile cube. Notifies UIManager when the player clicks a tile.
/// </summary>
public class TileClickHandler : MonoBehaviour
{
    public int X;
    public int Y;

    void OnMouseDown()
    {
        UIManager.Instance?.OnTileClicked(X, Y);
    }
}
