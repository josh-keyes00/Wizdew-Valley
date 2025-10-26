using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smooth = 10f;

    private void LateUpdate()
    {
        if (!target) return;
        Vector3 pos = transform.position;
        pos = Vector3.Lerp(pos, new Vector3(target.position.x, target.position.y, pos.z), smooth * Time.deltaTime);
        transform.position = pos;
    }
}
