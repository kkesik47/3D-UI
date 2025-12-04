using UnityEngine;

public class FaceCameraBillboard : MonoBehaviour
{
    public Transform target;
    public bool onlyYaw = true; 
    public float smooth = 12f; 

    void LateUpdate()
    {
        if (!target) target = Camera.main ? Camera.main.transform : null;
        if (!target) return;

        // Direction from camera to panel (so panel faces the camera)
        Vector3 dir = transform.position - target.position;
        if (onlyYaw) dir.y = 0f;
        if (dir.sqrMagnitude < 0.00001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
