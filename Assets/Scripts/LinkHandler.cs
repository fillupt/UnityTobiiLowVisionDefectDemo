using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class LinkHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TMP_Text pTextMeshPro = GetComponent<TMP_Text>();
        // Use null for the camera if your Canvas is set to Screen Overlay mode
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, eventData.position, null); 

        if (linkIndex != -1)
        {
            // A link was clicked, get the link info
            TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

            // Open the URL specified in the link tag's ID
            Application.OpenURL(linkInfo.GetLinkID());
        }
    }
}
