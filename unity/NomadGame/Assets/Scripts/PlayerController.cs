using UnityEngine;

/// <summary>
/// WASD + mouse-look character controller. Camera follows from above and behind.
/// Attach to the Player GameObject (with a CharacterController component).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float gravity = -20f;

    [Header("Camera")]
    public Transform cameraTarget;
    public float cameraDistance = 12f;
    public float cameraHeight = 10f;
    public float cameraSmoothing = 5f;

    [Header("Backend sync")]
    public float moveSyncInterval = 0.5f;

    private CharacterController _cc;
    private Vector3 _velocity;
    private Camera _cam;
    private float _syncTimer;
    private Vector3 _lastSentPos;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = Camera.main;
    }

    void Update()
    {
        Move();
        SyncToBackend();
    }

    void LateUpdate()
    {
        FollowCamera();
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, 0f, v).normalized;
        if (dir.magnitude > 0.1f)
        {
            Vector3 move = dir * moveSpeed;
            _cc.Move(move * Time.deltaTime);
        }

        if (_cc.isGrounded && _velocity.y < 0) _velocity.y = -2f;
        if (Input.GetButtonDown("Jump") && _cc.isGrounded) _velocity.y = 6f;
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void FollowCamera()
    {
        if (_cam == null) return;
        Vector3 target = transform.position + new Vector3(0f, cameraHeight, -cameraDistance);
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, target, cameraSmoothing * Time.deltaTime);
        _cam.transform.LookAt(transform.position + Vector3.up);
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
