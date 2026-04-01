using UnityEngine;

public class FadableWall : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("When being masked, the final wall transparent value")]
    public float targetAlpha = 0.3f;
    [Tooltip("Fading speed")]
    public float fadeSpeed = 5f;

    private Material wallMaterial;

    private bool shouldBeTransparent = false;

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            wallMaterial = rend.material;
        }
    }

    private void Update()
    {
        if (wallMaterial == null) return;

        float currentTargetAlpha = shouldBeTransparent ? targetAlpha : 1f;

        Color currentColor = wallMaterial.color;
        currentColor.a = Mathf.Lerp(currentColor.a, currentTargetAlpha, fadeSpeed * Time.deltaTime);
        wallMaterial.color = currentColor;
    }

    /// <summary>
    /// Call by CameraVisionTrigger script
    /// </summary>
    /// <param name="isTransparent">true: transparent, false: become solid</param>
    public void SetTransparentState(bool isTransparent)
    {
        shouldBeTransparent = isTransparent;
    }
}