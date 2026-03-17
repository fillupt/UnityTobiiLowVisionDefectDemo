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
    public StartMouseMode smm;
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
    float lastSeenTime;
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
    private void Start()
    {
        noUserCanvas.SetActive(false);
        lastSeenTime = Time.time;
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
            }

            if (TryGetSelectedCondition(out VisionCondition selectedCondition))
            {
                ActivateCondition(selectedCondition);
                ShowSeveritySlider();
            }
        }
        
        bool hasTimedOut = false;

        if (TobiiAPI.IsConnected)
        {
            UserPresence userPresence = TobiiAPI.GetUserPresence();
            if (userPresence.IsUserPresent()) 
            {
                enableShader = true;
                lastSeenTime = Time.time;
                GazePoint gazePoint = TobiiAPI.GetGazePoint();
                shaderCentre.x = gazePoint.Screen.x / cam.pixelWidth;
                shaderCentre.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
            else
            {
                // do a graduated release to no user mode
                if ((Time.time - lastSeenTime) > inactiveTimeOut/2f)
                    enableShader = false;

                if ((Time.time - lastSeenTime) > inactiveTimeOut)
                    hasTimedOut = true;
            }
        }
        else if (smm.mouseModeOn){
                enableShader = true;
                lastSeenTime = Time.time;
                shaderCentre.x = Mathf.Clamp01(Input.mousePosition.x / cam.pixelWidth);
                shaderCentre.y = Mathf.Clamp01(Input.mousePosition.y / cam.pixelHeight);
        }
        else // simulate with mouse if no Tobii connected
        {
            // simulate user not present by holding right mouse button
            if (!Input.GetMouseButton(1))
            {
                //enableShader = true;
                lastSeenTime = Time.time;
                shaderCentre.x = Mathf.Clamp01(Input.mousePosition.x / cam.pixelWidth);
                shaderCentre.y = Mathf.Clamp01(Input.mousePosition.y / cam.pixelHeight);
            }
            else
            {
                // do a graduated release to no user mode
                if ((Time.time - lastSeenTime) > inactiveTimeOut/2f)
                    enableShader = false;

                if ((Time.time - lastSeenTime) > inactiveTimeOut)
                    hasTimedOut = true;
            }      

        }
        bool ShowNoUser = hasTimedOut || smm.mouseModeOn;
        noUserCanvas.SetActive(ShowNoUser);
        
        if (cp.hasStarted)
            ApplyShaderParameters();
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
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity + Mathf.RoundToInt(change), 0, 100);
                ShowSeveritySlider();
            }

            if (Input.GetKey(KeyCode.UpArrow)) // decrease severity
            {
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity - Mathf.RoundToInt(change), 0, 100);
                ShowSeveritySlider();
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
