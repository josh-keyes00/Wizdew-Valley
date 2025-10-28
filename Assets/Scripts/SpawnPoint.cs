using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Must match the Gateway's spawnId that leads here.")]
    public string id = "Default";

    [Header("Gizmo")]
    public Color gizmoColor = Color.green;
    public Vector2 gizmoSize = new Vector2(0.5f, 0.5f);

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, (Vector3)gizmoSize);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, id);
#endif
    }
}
