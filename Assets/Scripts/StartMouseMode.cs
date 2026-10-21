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
        ApplyMouseMode(!mouseModeOn);
    }

    public void SetForceMouseModeAvailable(bool available)
    {
        if (!available && mouseModeOn)
        {
            ApplyMouseMode(false);
        }
    }

    void ApplyMouseMode(bool enabled)
    {
        mouseModeOn = enabled;
        buttonText.text = mouseModeOn ? "Stop Force Mouse Mode" : "Force Mouse Mode";
        instructionsText.enabled = !mouseModeOn;
        backgroundImage.rectTransform.sizeDelta = new Vector2(backgroundImage.rectTransform.sizeDelta.x, mouseModeOn ? 100 : 270);
        backgroundImage.rectTransform.anchoredPosition = new Vector2(backgroundImage.rectTransform.anchoredPosition.x, mouseModeOn ? -466 : -367);
    }
}
