using System.Collections;
using UnityEngine;

/// <summary>
/// First-person controller. Attach Main Camera as a child of Player at (0, 1.6, 0).
/// WASD = move, mouse = look, E = claim tile underfoot, I = inventory, ` = terminal.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -20f;
    public float jumpForce = 6f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("References")]
    public Camera playerCamera;
    public VoxelWorld world;
    public GameConfig config;

    [Header("Backend sync")]
    public float moveSyncInterval = 0.5f;

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _cameraPitch;
    private float _syncTimer;
    private Vector3 _lastSentPos;
    private Vector3 _spawnPoint;

    public int  CurrentTileX { get; private set; }
    public int  CurrentTileY { get; private set; }
    public bool IsDead        { get; private set; }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _spawnPoint = transform.position;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        // Unlock cursor with Escape at any time
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (IsDead) return;

        bool terminalOpen = TerminalConsole.Instance != null && TerminalConsole.IsOpen;
        if (!terminalOpen)
        {
            if (Cursor.lockState == CursorLockMode.Locked) MouseLook();
            Move();
            DetectTileUnderfoot();
            HandleActions();
        }

        // Terminal toggle always available
        if (Input.GetKeyDown(KeyCode.BackQuote))
            TerminalConsole.Instance?.Toggle();

        SyncToBackend();
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _cameraPitch = Mathf.Clamp(_cameraPitch - mouseY, -maxLookAngle, maxLookAngle);
        if (playerCamera != null)
            playerCamera.transform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        _cc.Move(dir * moveSpeed * Time.deltaTime);

        if (_cc.isGrounded)
        {
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump")) _verticalVelocity = jumpForce;
        }
        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    void DetectTileUnderfoot()
    {
        float s = config != null ? config.tileSize : 1f;
        CurrentTileX = Mathf.RoundToInt(transform.position.x / s);
        CurrentTileY = Mathf.RoundToInt(transform.position.z / s);
        bool claimed = world != null && world.IsClaimed(CurrentTileX, CurrentTileY);
        UIManager.Instance?.ShowLandBanner(!claimed, CurrentTileX, CurrentTileY);
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (world != null && world.IsClaimed(CurrentTileX, CurrentTileY))
            {
                UIManager.Instance?.ShowStatus("This land is already claimed.");
            }
            else if (InventorySystem.Instance == null || !InventorySystem.Instance.IsBannerEquipped)
            {
                UIManager.Instance?.ShowStatus("Equip your Banner (slot 1) to claim land.");
            }
            else
            {
                UIManager.Instance?.OnTileClicked(CurrentTileX, CurrentTileY);
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
            InventoryUI.Instance?.ToggleStorage();
    }

    public void Die(string txHash = null)
    {
        if (IsDead) return;
        IsDead = true;
        UIManager.Instance?.ShowDeathScreen(txHash);
        StartCoroutine(RespawnAfterDelay(4f));
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsDead = false;
        _cc.enabled = false;
        transform.position = _spawnPoint + Vector3.up;
        _cc.enabled = true;
        _verticalVelocity = 0f;
        UIManager.Instance?.HideDeathScreen();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SyncToBackend()
    {
        _syncTimer += Time.deltaTime;
        if (_syncTimer < moveSyncInterval) return;
        _syncTimer = 0f;
        if (Vector3.Distance(transform.position, _lastSentPos) < 0.5f) return;
        _lastSentPos = transform.position;
        ApiClient.Instance?.SendMove(transform.position.x, transform.position.y, transform.position.z);
    }
}
