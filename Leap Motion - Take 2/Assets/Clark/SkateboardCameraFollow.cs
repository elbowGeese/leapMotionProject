using UnityEngine;

public class SkateboardCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.1f)] private float followSpeed = 12f;

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    private void Awake()
    {
        if (target == null) return;

        Quaternion inverseRotation = Quaternion.Inverse(target.rotation);
        positionOffset = inverseRotation * (transform.position - target.position);
        rotationOffset = inverseRotation * transform.rotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + target.rotation * positionOffset;
        Quaternion desiredRotation = target.rotation * rotationOffset;
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
    }
}