using Tobii.Gaming;
using UnityEngine;
using UnityEngine.UI;

public class GetGaze : MonoBehaviour
{

    public GameObject noUserCanvas;
    public StartMouseMode smm;
    public Color catColour;
    public Toggle aT, cT, gT ,oT;
    public Image image;
    public float rateOfChange;
    
    // Separate materials for each condition
    public Material glaucomaMaterial;
    public Material amdMaterial;
    public Material cataractMaterial;
    public Material oedemaMaterial;
    
    private Color vigColor = Color.gray;
    float inactiveTimeOut = 1.5f;
    float lastSeenTime, distortionSize;
    int currSim;
    int diseaseSeverity = 0; // 0-100 disease severity
    bool vigInvert, enableShader;
    private Camera cam;
    public ChangePicture cp;
    public Vector2 shaderCentre;
    private void Start()
    {
        noUserCanvas.SetActive(false);
        lastSeenTime = Time.time;
        cam          = Camera.main;
        ResetShader();
    }

    public void ResetShader(){
        shaderCentre = new Vector2(0.5f, 0.5f);
        currSim = -1;
        enableShader = false;
        diseaseSeverity = 0;
    }

    void Update()
    {
        
        if (cp.hasStarted){
            if (Input.GetKeyUp(KeyCode.R)) //Reset
            {
                ResetShader();
            }

            if (Input.GetKeyUp(KeyCode.G) && gT.isOn) //glaucoma
            {
                enableShader = true;
                currSim = 0;
                image.material = glaucomaMaterial;
                vigColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                diseaseSeverity = 10; // Start at 10%
            }

            if (Input.GetKeyUp(KeyCode.C) && cT.isOn) //cataract
            {
                enableShader = true;
                currSim = 2;
                image.material = cataractMaterial;
                vigColor = catColour;
                diseaseSeverity = 10; // Start at 10%
            }

            if (Input.GetKeyUp(KeyCode.A) && aT.isOn)//AMD
            {
                enableShader = true;
                currSim = 1;
                image.material = amdMaterial;
                vigColor = Color.gray;
                diseaseSeverity = 10; // Start at 10%
            }

            if (Input.GetKeyUp(KeyCode.O) && oT.isOn) //oedema
            {
                enableShader = true;
                currSim = 4;
                image.material = oedemaMaterial;
                diseaseSeverity = 10; // Start at 10%
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
        // Calculate the region to distort based on the normalized position and distortion size
        Rect distortionRect = new Rect(shaderCentre.x - distortionSize * 0.5f,
                                       shaderCentre.y - distortionSize * 0.5f,
                                        distortionSize,
                                        distortionSize);
        
        if (cp.hasStarted)
            ApplyShaderParameters();
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
        if (currSim == 0) // Glaucoma
        {
            mat.SetColor("_VignetteColor", vigColor);
        }
        else if (currSim == 1) // AMD
        {
            mat.SetColor("_VignetteColor", vigColor);
        }
        else if (currSim == 2) // Cataract
        {
            mat.SetColor("_CataractColor", vigColor);
        }
        
        // Always set severity for cataract material in case of reset
        if (mat == cataractMaterial)
        {
            mat.SetFloat("_DiseaseSeverity", currSim == 2 ? diseaseSeverity / 100f : 0f);
        }
    }
    
    void FixedUpdate()
    {
        // Handle severity adjustment in fixed timestep for smooth, frame-rate independent control
        // rateOfChange is now interpreted as "severity units per second"
        if (cp.hasStarted)
        {
            if (Input.GetKey(KeyCode.DownArrow)) // increase severity
            {
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity + Mathf.RoundToInt(change), 0, 100);
            }

            if (Input.GetKey(KeyCode.UpArrow)) // decrease severity
            {
                float change = rateOfChange * Time.fixedDeltaTime;
                diseaseSeverity = Mathf.Clamp(diseaseSeverity - Mathf.RoundToInt(change), 0, 100);
            }
        }
    }
}
