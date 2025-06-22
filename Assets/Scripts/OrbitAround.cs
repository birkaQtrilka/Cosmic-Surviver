using UnityEngine;

public class OrbitAround : MonoBehaviour
{
    public Transform Target;
    public float Speed = 10f;
    public float Radius = 5f;
    public Vector3 OrbitAxis = Vector3.up;

    private float _angle;

    void Start()
    {
        if (Target == null) return;

        // Initialize angle based on current position
        Vector3 offset = transform.position - Target.position;
        _angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        Radius = new Vector2(offset.x, offset.z).magnitude;
    }

    void FixedUpdate()
    {
        if (Target == null) return;

        _angle += Speed * Time.fixedDeltaTime;

        Quaternion rotation = Quaternion.AngleAxis(_angle, OrbitAxis);
        Vector3 offset = rotation * Vector3.right * Radius;

        transform.position = Target.position + offset;
    }
}
