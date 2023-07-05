using UnityEngine;
using UnityEngine.UI;

public class ImageDistortion : MonoBehaviour
{
    public float distortionSize = 0.1f;
    public float distortionAmount = 0.5f;
    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
    }

private void Update()
{
    // Convert mouse position to normalized screen coordinates
    Vector2 normalizedMousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);

    if (RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, normalizedMousePos))
    {
        print("Mouse is over the image.");
        // Convert mouse position from screen space to canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, Input.mousePosition, null, out Vector2 localPoint);

        // Normalize the position within the image
        Vector2 normalizedPos = new Vector2((localPoint.x + image.rectTransform.rect.width * 0.5f) / image.rectTransform.rect.width,
                                            (localPoint.y + image.rectTransform.rect.height * 0.5f) / image.rectTransform.rect.height);

        // Calculate the region to distort based on the normalized position and distortion size
        Rect distortionRect = new Rect(normalizedPos.x - distortionSize * 0.5f,
                                       normalizedPos.y - distortionSize * 0.5f,
                                       distortionSize,
                                       distortionSize);

        // Calculate the distortion center in normalized screen coordinates
        Vector2 distortionCenter = new Vector2(distortionRect.x / image.rectTransform.rect.width, distortionRect.y / image.rectTransform.rect.height);

        // Apply distortion effect to the region
        ApplyDistortion(distortionRect, distortionCenter, distortionAmount);
        print(distortionCenter.x + " " + distortionCenter.y);
    }
}






private void ApplyDistortion(Rect region, Vector2 distortionCenter, float amount)
{
    // Get the material assigned to the image component
    Material material = image.material;

    // Calculate the distortion center in screen space based on the cursor position
    //Vector2 mousePosition = Input.mousePosition;
    //Vector2 distortionCenter = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);

    // Convert the distortion center from screen space to texture space
    Vector2 distortionCenterTextureSpace = new Vector2(distortionCenter.x * region.width + region.x,
                                                       distortionCenter.y * region.height + region.y);

    // Set the distortion center and parameters in the material properties
    material.SetVector("_DistortionCenter", distortionCenter);
    material.SetFloat("_DistortionSize", Mathf.Min(region.width, region.height));
    material.SetFloat("_DistortionAmount", amount);
    image.material = material;
}



}
