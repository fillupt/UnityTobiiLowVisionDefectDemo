using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StartMouseMode : MonoBehaviour
{
    public Image backgroundImage;
    public TMP_Text buttonText, instructionsText;
    public Button button;
    public bool mouseModeOn;
    void Start()
    {
        button.onClick.AddListener(ToggleMouseMode);
    }

    void ToggleMouseMode()
    {
        mouseModeOn = !mouseModeOn;
        buttonText.text = mouseModeOn ? "Stop Mouse Mode" : "Start Mouse Mode";
        instructionsText.enabled = !mouseModeOn;
        // When mousemode is off, set the size of the background image to 550 x 270, and when it is on, set it to 550 x 100
        backgroundImage.rectTransform.sizeDelta = new Vector2(backgroundImage.rectTransform.sizeDelta.x, mouseModeOn ? 100 : 270);
        // Change the y-position of the backgroundImage from -367 to -466  when in mouse mode, but keeping the width of the image the same
        backgroundImage.rectTransform.anchoredPosition = new Vector2(backgroundImage.rectTransform.anchoredPosition.x, mouseModeOn ? -466 : -367);
    }
}
