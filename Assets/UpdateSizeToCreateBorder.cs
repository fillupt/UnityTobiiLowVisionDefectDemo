using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateSizeToCreateBorder : MonoBehaviour
{
    // On start, this script updates the size of the attached gameobject to be n pixel larger than the reference gameobject

    public float borderSizeInPixels = 10.0f;
    public GameObject referenceGameObject;

    void Start()
    {
        if (referenceGameObject != null)
        {
            RectTransform referenceRect = referenceGameObject.GetComponent<RectTransform>();
            RectTransform thisRect = GetComponent<RectTransform>();

            if (referenceRect != null && thisRect != null)
            {
                // Calculate new size
                Vector2 newSize = new Vector2(
                    referenceRect.rect.width + borderSizeInPixels,
                    referenceRect.rect.height + borderSizeInPixels
                );

                // Update this gameobject's size
                thisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
                thisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
            }
            else
            {
                Debug.LogError("RectTransform component missing on one of the gameobjects.");
            }
        }
        else
        {
            Debug.LogError("Reference gameobject is not assigned.");
        }
    }


}
