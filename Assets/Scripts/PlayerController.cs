using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform _attractor;
    [SerializeField] Rigidbody _rb;
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float jumpStrength = 10f;
    [SerializeField] float acceleration = 20f;
    [SerializeField] Camera cam;
    Vector3 input;

    private void Awake()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        _rb.useGravity = false;   // we handle gravity manually
        //_rb.freezeRotation = true;
    }

    private void Update()
    {
        // Get WASD input
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        input = new Vector3(h, 0f, v).normalized;

        if(Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        AlignToAttractor();
        ApplyGravity();
        Move();
    }

    void AlignToAttractor()
    {
        Vector3 gravityUp = (_rb.position - _attractor.position).normalized;

        // Preserve current forward projected onto surface
        Vector3 forwardProjected =
            Vector3.ProjectOnPlane(transform.forward, gravityUp).normalized;

        // Safety fallback if forward becomes invalid
        if (forwardProjected.sqrMagnitude < 0.001f)
        {
            forwardProjected =
                Vector3.ProjectOnPlane(transform.right, gravityUp).normalized;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(forwardProjected, gravityUp);

        _rb.MoveRotation(
            Quaternion.Slerp(_rb.rotation, targetRotation, 10f * Time.fixedDeltaTime)
        );
    }


    void ApplyGravity()
    {
        Vector3 gravityDir = (_attractor.position - _rb.position).normalized;
        _rb.AddForce(gravityDir * 20f, ForceMode.Acceleration);
    }

    void Jump()
    {
        _rb.AddForce(_rb.transform.up* jumpStrength, ForceMode.Impulse);
    }

    void Move()
    {
        if (input.sqrMagnitude == 0)
            return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward = Vector3.ProjectOnPlane(camForward, _rb.transform.up).normalized;
        camRight = Vector3.ProjectOnPlane(camRight, _rb.transform.up).normalized;

        Vector3 moveDir = camForward * input.z + camRight * input.x;


        Vector3 targetVelocity = moveDir * moveSpeed;

        Vector3 velocityChange = targetVelocity - _rb.velocity;
        velocityChange -= Vector3.Project(velocityChange, _rb.transform.up);

        _rb.AddForce(velocityChange * acceleration, ForceMode.Acceleration);
    }
}
