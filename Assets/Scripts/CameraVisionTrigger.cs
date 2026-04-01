using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CameraVisionTrigger : MonoBehaviour
{
    [Header("Vision Settings")]
    [Tooltip("The distance of the raycast (how far can you see through a wall?)")]
    public float visionDepth = 50f;

    private Camera cam;
    private BoxCollider boxCol;

    private void Start()
    {
        cam = GetComponent<Camera>();
        boxCol = GetComponent<BoxCollider>();

        // box collider must be true
        boxCol.isTrigger = true;
    }

    private void Update()
    {
        SyncColliderWithCamera();
    }

    /// <summary>
    /// Based on the camera mode, automatically reshape box collider size
    /// </summary>
    private void SyncColliderWithCamera()
    {
        float width = 0f;
        float height = 0f;

        if (cam.orthographic)
        {
            height = cam.orthographicSize * 2f;
            width = height * cam.aspect;
        }
        else
        {
            // Height = 2 * Distance * Tan(FOV / 2)
            height = 2.0f * visionDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            width = height * cam.aspect;
        }

        // reshape Box Collider
        boxCol.size = new Vector3(width, height, visionDepth);

        // move box collider to forward by half of depth
        boxCol.center = new Vector3(0, 0, visionDepth / 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<FadableWall>(out FadableWall wall))
        {
            wall.SetTransparentState(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<FadableWall>(out FadableWall wall))
        {
            wall.SetTransparentState(false);
        }
    }
}