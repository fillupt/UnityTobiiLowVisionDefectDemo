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
    private Color vigColor = Color.gray;
    float inactiveTimeOut = 1.5f;
    float lastSeenTime, distortionSize, distortionAmount, distortRadius, randomWetness;
    int currSim;
    float vigAlpha, vigSize;
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
                distortionSize = 1.0f;
                distortRadius = 0f;
                vigColor   = new Color(0.2f, 0.2f, 0.2f, 1f);
                vigInvert = false;
                vigAlpha = 0f; 
                vigSize = 1.0f;
            }

            if (Input.GetKeyUp(KeyCode.C) && cT.isOn) //cataract
            {
                enableShader = true;
                currSim = 2;
                vigColor = catColour;
                distortionSize = 0.116f;
                distortRadius = 0.08f;
                distortionAmount = distortRadius;
                vigInvert = true;
                vigAlpha = 0f; 
                vigSize = 0.7f;
            }

            if (Input.GetKeyUp(KeyCode.A) && aT.isOn)//AMD
            {
                enableShader = true;
                currSim = 1;
                distortionSize = 0.116f;
                vigColor   = Color.gray;
                vigInvert = true;
                vigAlpha = 0f; 
                vigSize = 0.05f;
                randomWetness = Random.Range(0.4f, 0.6f);
                distortRadius = vigSize;
                distortionAmount = randomWetness;

            }

            if (Input.GetKeyUp(KeyCode.O) && oT.isOn) //oedema
            {
                enableShader = true;
                currSim = 4;
                distortionSize = 0.116f;
                distortRadius = 0.08f;
                distortionAmount = distortRadius;
                vigColor   = Color.gray;
                vigInvert = true;
                vigAlpha = 0f; 
                vigSize = 0.001f;
                randomWetness = Random.Range(0.6f, 0.9f);
            }

            if (Input.GetKey(KeyCode.DownArrow)) // get better
            {
                float change = rateOfChange * Time.deltaTime * -1f;
                if (currSim ==0) // glaucoma
                {              
                    vigSize = Mathf.Clamp(vigSize + change, 0.1f, 1.0f);
                    //vigAlpha = Mathf.Clamp(vigSize/1.2f, 0, 0.5f);
                }
                if (currSim == 1) // AMD 
                {
                    vigSize = Mathf.Clamp(vigSize - change, 0.1f, .7f);
                    vigAlpha = Mathf.Clamp(vigSize, 0f, 0.6f);
                    distortRadius = vigSize;
                    //distortionAmount = distortRadius * randomWetness;
                }
                if (currSim == 2 ) //cataract
                {
                    vigSize = Mathf.Clamp(vigSize - change/2f, 0.7f, 1.0f); 
                    vigAlpha = Mathf.Clamp(vigAlpha - change, 0f, .6f);
                }
                if ( currSim == 4)  // Oedema
                {
                    distortRadius = Mathf.Clamp(distortRadius - change, 0.08f, 0.7f);
                    distortionAmount = distortRadius;
                    vigSize = Mathf.Clamp(Mathf.Pow(distortRadius,4f), .001f, 0.3f);
                }
            }

            if (Input.GetKey(KeyCode.UpArrow)) // get worse
            {
                float change = rateOfChange * Time.deltaTime;
                if (currSim == 0 )
                {
                    vigSize = Mathf.Clamp(vigSize + change, 0.1f, 1.0f);
                }

                if (currSim == 1 )
                {
                    vigSize = Mathf.Clamp(vigSize - change, 0.05f, .7f);
                    vigAlpha = Mathf.Clamp(vigSize, 0f, 0.6f);
                    distortRadius = vigSize;
                }

                if (currSim == 2)
                {
                    vigSize = Mathf.Clamp(vigSize - change/2f, 0.7f, 1.0f); 
                    vigAlpha = Mathf.Clamp(vigAlpha - change, 0f, .6f);
                }
                if ( currSim == 4)  // Oedema
                {
                    distortRadius = Mathf.Clamp(distortRadius - change, 0.08f, 0.7f);
                    distortionAmount = distortRadius;
                    vigSize = Mathf.Clamp(distortRadius-0.5f, .001f, 1.0f);
                }
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
            ApplyDistortion(distortionRect, shaderCentre, distortionAmount, distortRadius, vigColor, vigAlpha, vigSize, vigInvert, enableShader);
    }

    private void ApplyDistortion(Rect region, Vector2 distortionCenter, float amount, float radius, Color vignetteColor, float vignetteAlpha, float vignetteSize, bool reverseVignette, bool enableShader)
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

        // Set the vignette parameters in the material properties
        material.SetColor("_VignetteColor", vignetteColor);
        material.SetFloat("_VignetteAlpha", vignetteAlpha);
        material.SetFloat("_VignetteSize", vignetteSize);
        material.SetFloat("_ReverseVignette", reverseVignette ? 1f : 0f);
        material.SetFloat("_EnableShader", enableShader ? 1f : 0f);
        image.material = material;
    }
}
