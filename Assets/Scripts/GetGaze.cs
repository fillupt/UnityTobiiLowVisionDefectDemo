using Tobii.Gaming;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GetGaze : MonoBehaviour
{
    private enum VisionCondition
    {
        None = -1,
        Glaucoma = 0,
        AMD = 1,
        Cataract = 2,
        DiabeticRetinopathy = 3,
        Oedema = 4
    }

    public GameObject noUserCanvas;
    public CanvasGroup noUserCanvasGroup;
    public Text noUserBannerText;
    public GameObject forceMouseModeButton;
    public GameObject overlayBackground;
    public GameObject overlayInstructions;
    public StartMouseMode smm;
    public float overlayFadeDuration = 0.35f;

    const string NoTrackerDetectedMessage = "No eye tracker detected — gaze follows mouse";
    const string ExplicitMouseModeMessage = "Force mouse mode — eye tracker overridden";
    const string SignalLostMessage = "No eye tracking signal — look at the screen";
    const string TrackerDisconnectedMessage = "Eye tracker disconnected — using mouse";
    public Color catColour;
    public Image image;
    public float rateOfChange;
    
    // Separate materials for each condition
    public Material glaucomaMaterial;
    public Material amdMaterial;
    public Material cataractMaterial;
    public Material oedemaMaterial;
    public Material diabetesMaterial;
    public BlurPrepassRenderer blurRenderer;
    
    private Color vigColor = Color.gray;
    float inactiveTimeOut = 1.5f;
    float lastActivityTime;
    Vector3 lastMousePosition;
    const float MouseMoveThresholdPixels = 2f;
    static readonly float MouseMoveThresholdSq = MouseMoveThresholdPixels * MouseMoveThresholdPixels;
    float overlayTargetAlpha;
    VisionCondition currentCondition = VisionCondition.None;
    int diseaseSeverity = 0; // 0-100 disease severity
    bool enableShader;
    private Camera cam;
    public ChangePicture cp;
    public Vector2 shaderCentre;
    
    // Severity slider UI
    public GameObject severitySliderPanel;
    public Slider severitySlider;
    public TMP_Text severityConditionText;
    public CanvasGroup severityCanvasGroup;
    private float sliderDisplayTimer = 0f;
    private float sliderDisplayDuration = 1f;
    private float sliderFadeDuration = 0.5f;
    private bool trackerEverConnected;
    private bool hadValidEyeSignal;

    private void Start()
    {
        if (noUserCanvas != null)
        {
            noUserCanvas.SetActive(false);
            if (noUserCanvasGroup == null)
            {
                noUserCanvasGroup = noUserCanvas.GetComponent<CanvasGroup>();
            }

            if (noUserCanvasGroup != null)
            {
                noUserCanvasGroup.alpha = 0f;
                noUserCanvasGroup.blocksRaycasts = false;
                noUserCanvasGroup.interactable = false;
            }

            Transform overlayRoot = noUserCanvas.transform;
            if (noUserBannerText == null)
            {
                noUserBannerText = noUserCanvas.GetComponentInChildren<Text>(true);
            }

            if (forceMouseModeButton == null)
            {
                Transform buttonTransform = overlayRoot.Find("mouseClickBut");
                if (buttonTransform != null)
                {
                    forceMouseModeButton = buttonTransform.gameObject;
                }
            }

            if (overlayBackground == null)
            {
                Transform backgroundTransform = overlayRoot.Find("noUserBackgroundIm");
                if (backgroundTransform != null)
                {
                    overlayBackground = backgroundTransform.gameObject;
                }
            }

            if (overlayInstructions == null)
            {
                Transform instructionsTransform = overlayRoot.Find("instrucTMPText");
                if (instructionsTransform != null)
                {
                    overlayInstructions = instructionsTransform.gameObject;
                }
            }
        }
        lastActivityTime = Time.time;
        lastMousePosition = Input.mousePosition;
        cam          = Camera.main;
        if (blurRenderer == null)
        {
            blurRenderer = GetComponent<BlurPrepassRenderer>();
            if (blurRenderer == null)
            {
                blurRenderer = gameObject.AddComponent<BlurPrepassRenderer>();
            }
        }
        if (severitySliderPanel != null)
        {
            severitySliderPanel.SetActive(false);
        }
        ResetShader();
    }

    public void RegisterUserActivity()
    {
        lastActivityTime = Time.time;
    }

    public void ResetShader(){
        shaderCentre = new Vector2(0.5f, 0.5f);
        currentCondition = VisionCondition.None;
        enableShader = false;
        diseaseSeverity = 0;

        if (blurRenderer != null && image != null && image.material != null)
        {
            blurRenderer.ClearBlur(image.material);
        }
    }

    void Update()
    {
        // Handle slider display timer and fade out
        if (sliderDisplayTimer > 0)
        {
            sliderDisplayTimer -= Time.deltaTime;
            
            if (severityCanvasGroup != null)
            {
                if (sliderDisplayTimer <= 0)
                {
                    // Fully faded out, hide panel
                    severitySliderPanel.SetActive(false);
                    severityCanvasGroup.alpha = 1f; // Reset alpha for next display
                }
                else if (sliderDisplayTimer <= sliderFadeDuration)
                {
                    // Fade out phase
                    severityCanvasGroup.alpha = sliderDisplayTimer / sliderFadeDuration;
                }
                else
                {
                    // Fully visible phase
                    severityCanvasGroup.alpha = 1f;
                }
            }
            else if (sliderDisplayTimer <= 0 && severitySliderPanel != null)
            {
                // Fallback if no canvas group
                severitySliderPanel.SetActive(false);
            }
        }
        
        if (cp.hasStarted)
        {
            if (Input.GetKeyUp(KeyCode.R)) //Reset
            {
                ResetShader();
                RegisterUserActivity();
            }

            if (TryGetSelectedCondition(out VisionCondition selectedCondition))
            {
                ActivateCondition(selectedCondition);
                ShowSeveritySlider();
                RegisterUserActivity();
            }
        }
        
        bool isConnected = TobiiAPI.IsConnected;

        if (isConnected)
        {
            trackerEverConnected = true;
        }

        if (isConnected && !smm.mouseModeOn)
        {
            UserPresence userPresence = TobiiAPI.GetUserPresence();
            GazePoint gazePoint = TobiiAPI.GetGazePoint();
            bool hasValidSignal = userPresence.IsUserPresent() && gazePoint.IsRecent();

            if (hasValidSignal)
            {
                hadValidEyeSignal = true;
                RegisterUserActivity();
                shaderCentre.x = gazePoint.Screen.x / cam.pixelWidth;
                shaderCentre.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
        }
        else if (smm.mouseModeOn)
        {
            UpdateMouseGazeDrive();
        }
        else if (!Input.GetMouseButton(1))
        {
            UpdateMouseGazeDrive();
        }

        bool usingEyeTracking = isConnected && !smm.mouseModeOn;
        float timeout = usingEyeTracking ? inactiveTimeOut : inactiveTimeOut * 2f;
        bool overlayIdle = (Time.time - lastActivityTime) > timeout;
        UpdateTrackingOverlay(isConnected, overlayIdle);
        ApplyInactivityEffects(overlayIdle);

        if (cp.hasStarted)
            ApplyShaderParameters();
    }

    private void UpdateMouseGazeDrive()
    {
        Vector3 mousePosition = Input.mousePosition;
        if ((mousePosition - lastMousePosition).sqrMagnitude > MouseMoveThresholdSq)
        {
            RegisterUserActivity();
            lastMousePosition = mousePosition;
        }

        shaderCentre.x = Mathf.Clamp01(mousePosition.x / cam.pixelWidth);
        shaderCentre.y = Mathf.Clamp01(mousePosition.y / cam.pixelHeight);
    }

    private void ApplyInactivityEffects(bool overlayIdle)
    {
        // Hide the effect only once the overlay has fully faded in (or if there is nothing to show)
        bool overlayFullyShown = noUserCanvasGroup == null
            ? overlayTargetAlpha > 0f
            : overlayTargetAlpha <= 0f || noUserCanvasGroup.alpha >= 0.999f;
        enableShader = !(overlayIdle && overlayFullyShown)
            && (currentCondition != VisionCondition.None || hadValidEyeSignal || smm.mouseModeOn);
    }

    private void UpdateTrackingOverlay(bool isConnected, bool overlayIdle)
    {
        if (cp == null || !cp.hasStarted || noUserCanvas == null)
        {
            if (noUserCanvas != null)
            {
                noUserCanvas.SetActive(false);
            }

            return;
        }

        bool showForceMouseButton = isConnected || trackerEverConnected;
        smm.SetForceMouseModeAvailable(showForceMouseButton);

        // Keep the overlay while the pointer is over the button so it can be clicked
        bool hoveringButton = noUserCanvasGroup != null && noUserCanvasGroup.alpha > 0.5f
            && forceMouseModeButton != null && forceMouseModeButton.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)forceMouseModeButton.transform, Input.mousePosition);
        overlayIdle |= hoveringButton;

        string message = null;
        bool showBanner = overlayIdle && TryGetBannerMessage(isConnected, smm.mouseModeOn, out message);
        bool showOverlayContent = overlayIdle && (showBanner || showForceMouseButton);
        overlayTargetAlpha = showOverlayContent ? 1f : 0f;

        noUserCanvas.SetActive(true);
        ApplyOverlayFade();

        if (noUserCanvasGroup != null && noUserCanvasGroup.alpha <= 0.001f && overlayTargetAlpha <= 0f)
        {
            return;
        }

        if (forceMouseModeButton != null)
        {
            forceMouseModeButton.SetActive(showForceMouseButton);
        }

        if (noUserBannerText != null)
        {
            noUserBannerText.gameObject.SetActive(showBanner);
            if (showBanner)
            {
                noUserBannerText.text = message;
            }
        }

        bool showInstructionPanel = showBanner && !smm.mouseModeOn;
        if (overlayBackground != null)
        {
            overlayBackground.SetActive(showInstructionPanel);
        }

        if (overlayInstructions != null)
        {
            overlayInstructions.SetActive(showInstructionPanel);
        }
    }

    private void ApplyOverlayFade()
    {
        if (noUserCanvasGroup == null)
        {
            return;
        }

        float fadeSpeed = overlayFadeDuration > 0f ? Time.deltaTime / overlayFadeDuration : 1f;
        noUserCanvasGroup.alpha = Mathf.MoveTowards(noUserCanvasGroup.alpha, overlayTargetAlpha, fadeSpeed);
        bool visible = noUserCanvasGroup.alpha > 0.01f;
        noUserCanvasGroup.blocksRaycasts = visible;
        noUserCanvasGroup.interactable = visible;
    }

    private bool TryGetBannerMessage(bool isConnected, bool forceMouseModeOn, out string message)
    {
        message = null;

        if (forceMouseModeOn && (isConnected || trackerEverConnected))
        {
            message = ExplicitMouseModeMessage;
            return true;
        }

        if (isConnected)
        {
            if (hadValidEyeSignal)
            {
                message = SignalLostMessage;
                return true;
            }

            return false;
        }

        if (trackerEverConnected)
        {
            message = TrackerDisconnectedMessage;
            return true;
        }

        message = NoTrackerDetectedMessage;
        return true;
    }

    private bool TryGetSelectedCondition(out VisionCondition selectedCondition)
    {
        selectedCondition = VisionCondition.None;

        // Preserve existing priority by processing keys in the original order.
        if (Input.GetKeyUp(KeyCode.G))
        {
            selectedCondition = VisionCondition.Glaucoma;
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            selectedCondition = VisionCondition.Cataract;
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            selectedCondition = VisionCondition.AMD;
        }
        if (Input.GetKeyUp(KeyCode.O))
        {
            selectedCondition = VisionCondition.Oedema;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            selectedCondition = VisionCondition.DiabeticRetinopathy;
        }

        return selectedCondition != VisionCondition.None;
    }

    private void ActivateCondition(VisionCondition condition)
    {
        enableShader = true;
        currentCondition = condition;
        diseaseSeverity = 0;

        Material conditionMaterial = GetConditionMaterial(condition);
        if (conditionMaterial != null)
        {
            image.material = conditionMaterial;
        }

        switch (condition)
        {
            case VisionCondition.Glaucoma:
                vigColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                break;
            case VisionCondition.AMD:
                vigColor = Color.gray;
                break;
            case VisionCondition.Cataract:
                vigColor = catColour;
                break;
        }
    }

    private Material GetConditionMaterial(VisionCondition condition)
    {
        switch (condition)
        {
            case VisionCondition.Glaucoma:
                return glaucomaMaterial;
            case VisionCondition.AMD:
                return amdMaterial;
            case VisionCondition.Cataract:
                return cataractMaterial;
            case VisionCondition.DiabeticRetinopathy:
                return diabetesMaterial;
            case VisionCondition.Oedema:
                return oedemaMaterial;
            default:
                return null;
        }
    }

    private string GetConditionDisplayName(VisionCondition condition)
    {
        switch (condition)
        {
            case VisionCondition.Glaucoma:
                return "Glaucoma";
            case VisionCondition.AMD:
                return "AMD";
            case VisionCondition.Cataract:
                return "Cataract";
            case VisionCondition.DiabeticRetinopathy:
                return "Diabetic Retinopathy";
            case VisionCondition.Oedema:
                return "Oedema";
            default:
                return "None";
        }
    }

    private void ApplyShaderParameters()
    {
        Material mat = image.material;
        if (mat == null) return;

        // Common parameters
        mat.SetVector("_GazeCenter", shaderCentre);
        mat.SetFloat("_EnableShader", enableShader ? 1f : 0f);
        mat.SetFloat("_DiseaseSeverity", diseaseSeverity / 100f); // Normalize to 0-1

        // Condition-specific color parameters
        switch (currentCondition)
        {
            case VisionCondition.Glaucoma:
            case VisionCondition.AMD:
                mat.SetColor("_VignetteColor", vigColor);
                break;
            case VisionCondition.Cataract:
                mat.SetColor("_CataractColor", vigColor);
                break;
        }
        
        // Always set severity for cataract material in case of reset.
        // Remap [0,100] -> [0.05,1.0] so the shader's minimum floor (Range(0.05,1)) is
        // respected and a mild effect is visible even at slider position 0.
        if (mat == cataractMaterial)
        {
            mat.SetFloat("_DiseaseSeverity", currentCondition == VisionCondition.Cataract ? Mathf.Lerp(0.05f, 1.0f, diseaseSeverity / 100f) : 0f);
        }

        ApplyConditionBlur(mat);
    }

    private void ApplyConditionBlur(Material mat)
    {
        if (blurRenderer == null)
        {
            return;
        }

        float severity = diseaseSeverity / 100f;
        Texture sourceTexture = image != null && image.mainTexture != null ? image.mainTexture : Texture2D.whiteTexture;

        switch (currentCondition)
        {
            case VisionCondition.Cataract:
            {
                // Mirror the [0.05, 1.0] remap used when setting _DiseaseSeverity.
                float effectiveSeverity = Mathf.Lerp(0.05f, 1.0f, severity);
                int passes = Mathf.RoundToInt(Mathf.Lerp(1f, 5f, effectiveSeverity));
                float amount = effectiveSeverity;
                float radius = Mathf.Lerp(0.35f, 0.75f, effectiveSeverity);
                blurRenderer.ApplyBlur(mat, sourceTexture, amount, radius, passes);
                break;
            }
            case VisionCondition.AMD:
            {
                float t = Mathf.Clamp01((severity - 0.30f) / 0.70f);
                int passes = t > 0f ? Mathf.RoundToInt(Mathf.Lerp(1f, 3f, t)) : 0;
                float amount = t * 0.50f;
                float radius = Mathf.Lerp(0.15f, 0.30f, severity);
                blurRenderer.ApplyBlur(mat, sourceTexture, amount, radius, passes);
                break;
            }
            case VisionCondition.Oedema:
            {
                float t = Mathf.Clamp01((severity - 0.50f) / 0.50f);
                int passes = t > 0f ? Mathf.RoundToInt(Mathf.Lerp(1f, 2f, t)) : 0;
                float amount = t * 0.35f;
                float radius = Mathf.Lerp(0.10f, 0.20f, severity);
                blurRenderer.ApplyBlur(mat, sourceTexture, amount, radius, passes);
                break;
            }
            default:
                blurRenderer.ClearBlur(mat);
                break;
        }
    }
    
    void FixedUpdate()
    {
        // Handle severity adjustment in fixed timestep for smooth, frame-rate independent control
        // rateOfChange is now interpreted as "severity units per second"
        if (cp.hasStarted && currentCondition != VisionCondition.None) // Only allow adjustment when a condition is selected
        {
            if (Input.GetKey(KeyCode.DownArrow)) // increase severity
            {
                int previousSeverity = diseaseSeverity;
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity + Mathf.RoundToInt(change), 0, 100);
                if (diseaseSeverity != previousSeverity)
                {
                    ShowSeveritySlider();
                    RegisterUserActivity();
                }
            }

            if (Input.GetKey(KeyCode.UpArrow)) // decrease severity
            {
                int previousSeverity = diseaseSeverity;
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity - Mathf.RoundToInt(change), 0, 100);
                if (diseaseSeverity != previousSeverity)
                {
                    ShowSeveritySlider();
                    RegisterUserActivity();
                }
            }
        }
    }
    
    void ShowSeveritySlider()
    {
        if (severitySliderPanel == null || severitySlider == null) return;
        
        // Show the panel
        severitySliderPanel.SetActive(true);
        sliderDisplayTimer = sliderDisplayDuration + sliderFadeDuration; // Add fade duration to total time
        
        // Reset alpha to fully visible
        if (severityCanvasGroup != null)
        {
            severityCanvasGroup.alpha = 1f;
        }
        
        // Update slider value
        severitySlider.value = diseaseSeverity;
        
        // Update condition name
        if (severityConditionText != null)
        {
            severityConditionText.text = GetConditionDisplayName(currentCondition);
        }
    }
}
