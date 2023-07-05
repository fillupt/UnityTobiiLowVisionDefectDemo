using UnityEngine;
using UnityEngine.UI;

public class ImageDistortion : MonoBehaviour
{
    public float distortionSize;
    public float distortionAmount;
    public float distortRadius;
    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
    }

private void Update()
{
    // Convert mouse position to normalized screen coordinates
    Vector2 normalizedMousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);

    // Convert mouse position from screen space to canvas space
    RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, Input.mousePosition, null, out Vector2 localPoint);

    // Calculate the region to distort based on the normalized position and distortion size
    Rect distortionRect = new Rect(normalizedMousePos.x - distortionSize * 0.5f,
                                    normalizedMousePos.y - distortionSize * 0.5f,
                                    distortionSize,
                                    distortionSize);

    // Calculate the distortion center in normalized screen coordinates, with both x and y dimensions inverted
    Vector2 distortionCenter = normalizedMousePos;//new Vector2(1f-normalizedMousePos.x, 1f-normalizedMousePos.y);

    // Apply distortion effect to the region
    ApplyDistortion(distortionRect, distortionCenter, distortionAmount, distortRadius);
}






private void ApplyDistortion(Rect region, Vector2 distortionCenter, float amount, float radius)
{
    // Get the material assigned to the image component
    Material material = image.material;

    // Convert the distortion center from screen space to texture space
    Vector2 distortionCenterTextureSpace = new Vector2(distortionCenter.x * region.width + region.x,
                                                       distortionCenter.y * region.height + region.y);

    // Set the distortion center and parameters in the material properties
    material.SetVector("_DistortionCenter", distortionCenterTextureSpace);
    material.SetFloat("_DistortionSize", Mathf.Min(region.width, region.height));
    material.SetFloat("_DistortionAmount", amount);
    material.SetFloat("_DistortionRadius", radius);
    image.material = material;
}


}
