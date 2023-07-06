using Tobii.Gaming;
using UnityEngine;
using UnityEngine.UI;

public class GetGaze : MonoBehaviour
{

    public GameObject noUserCanvas;
    public Color catColour;
    public Toggle aT, cT, gT ,oT;
    public Image image;
    public float rateOfChange;
    public Color vigColor;
    float inactiveTimeOut = 1.5f;
    float lastSeenTime, distortionSize, distortionAmount, distortRadius, randomWetness;
    int currSim;
    float vigAlpha, vigSize;
    bool vigInvert;
    private Camera cam;
    
    private void Start()
    {
        cam       = Camera.main;
        noUserCanvas.SetActive(false);
        lastSeenTime = Time.time;
        ResetShader();
    }

    private void ResetShader(){
        currSim = -1;
        distortionSize = 1.0f;
        distortRadius = 0f;
        vigInvert = true;
        vigAlpha = 1f;
    }

    void Update()
    {
        
        
        if (Input.GetKeyUp(KeyCode.R)) //Reset
        {
            ResetShader();
        }

        if (Input.GetKeyUp(KeyCode.G) && gT.isOn) //glaucoma
        {
            currSim = 0;
            distortionSize = 1.0f;
            distortRadius = 0f;
            vigColor   = Color.black;
            vigInvert = false;
            vigAlpha = 0.5f; 
            vigSize = 1.0f;
        }

        if (Input.GetKeyUp(KeyCode.C) && cT.isOn) //cataract
        {
            currSim = 2;
            vigColor = catColour;
        }

        if (Input.GetKeyUp(KeyCode.A) && aT.isOn)//AMD
        {
            currSim = 1;
            distortionSize = 0.116f;
            vigColor   = Color.black;
            vigInvert = true;
            vigAlpha = 0f; 
            vigSize = 0.05f;
            randomWetness = Random.Range(0.4f, 0.6f);
            distortRadius = vigSize;
            distortionAmount = randomWetness;

        }

        if (Input.GetKeyUp(KeyCode.O) && oT.isOn) //oedema
        {
            currSim = 4;
            distortionSize = 0.116f;
            distortRadius = 0.08f;
            vigColor   = Color.gray;
            vigInvert = true;
            vigAlpha = 0f; 
            vigSize = 0.001f;
            randomWetness = Random.Range(0.6f, 0.9f);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            float change = rateOfChange * Time.deltaTime * -1f;
            if (currSim ==0) // glaucoma
            {              
                vigSize = Mathf.Clamp(vigSize + change, 0.1f, 1.0f);
                vigAlpha = vigSize/2f;
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
                //theVig.VignetteInnerColor.a += (rateOfChange * Time.deltaTime);
                //theVig.VignetteOuterColor.a += (rateOfChange * Time.deltaTime);
            }
            if ( currSim == 4)  // Oedema
            {
                distortRadius = Mathf.Clamp(distortRadius - change, 0.08f, 0.7f);
                distortionAmount = distortRadius;
                vigSize = Mathf.Clamp(distortRadius-0.5f, .001f, 1.0f);

            }
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            float change = rateOfChange * Time.deltaTime;
            if (currSim == 0 )
            {
                vigSize = Mathf.Clamp(vigSize + change, 0.1f, 1.0f);
                vigAlpha = vigSize/2f;
            }

            if (currSim == 1 )
            {
                vigSize = Mathf.Clamp(vigSize - change, 0.1f, .7f);
                vigAlpha = Mathf.Clamp(vigSize, 0f, 0.6f);
                distortRadius = vigSize;
                //distortionAmount = distortRadius * randomWetness;
            }

            if (currSim == 2)
            {
                //theVig.VignetteInnerColor.a -= (rateOfChange * Time.deltaTime);
                //theVig.VignetteOuterColor.a -= (rateOfChange * Time.deltaTime);
            }
            if ( currSim == 4)  // Oedema
            {
                distortRadius = Mathf.Clamp(distortRadius - change, 0.08f, 0.7f);
                distortionAmount = distortRadius;
                vigSize = Mathf.Clamp(distortRadius-0.5f, .001f, 1.0f);
            }
        }
        
        if (TobiiAPI.IsConnected)
        {
            UserPresence userPresence = TobiiAPI.GetUserPresence();
            if (userPresence.IsUserPresent()) 
            {
                lastSeenTime = Time.time;
                noUserCanvas.SetActive(false);
                //cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
                GazePoint gazePoint = TobiiAPI.GetGazePoint();
                //theVig.VignetteCenter.x = gazePoint.Screen.x / cam.pixelWidth;
                //theVig.VignetteCenter.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
            else
            {
                // do a graduated release to no user mode
                if ((Time.time - lastSeenTime) > inactiveTimeOut/2f)
                    //cam.GetComponent<FinalVignetteCommandBuffer>().enabled = false;

                if ((Time.time - lastSeenTime) > inactiveTimeOut)
                    noUserCanvas.SetActive(true);
            }
        }
        else // simulate with mouse if no Tobii connected
        {      
            //theVig.VignetteCenter.x = Input.mousePosition.x / cam.pixelWidth;
            //theVig.VignetteCenter.y = Input.mousePosition.y / cam.scaledPixelHeight;
        }

        // Calculate the region to distort based on the normalized position and distortion size
        Rect distortionRect = new Rect(Input.mousePosition.x / cam.pixelWidth - distortionSize * 0.5f,
                                       Input.mousePosition.y / cam.scaledPixelHeight - distortionSize * 0.5f,
                                        distortionSize,
                                        distortionSize);
        // Apply distortion effect to the region
        // get mouse position as normalised screen space
        Vector2 mousePos = Input.mousePosition;
        mousePos.x = mousePos.x / cam.pixelWidth;
        mousePos.y = mousePos.y / cam.scaledPixelHeight;
        
        ApplyDistortion(distortionRect, mousePos, distortionAmount, distortRadius, Color.gray, vigAlpha, vigSize, vigInvert);
    }

 private void ApplyDistortion(Rect region, Vector2 distortionCenter, float amount, float radius, Color vignetteColor, float vignetteAlpha, float vignetteSize, bool reverseVignette)
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

    image.material = material;
}

}
        // properties
/*      float2 _DistortionCenter;
        float _DistortionSize;
        float _DistortionAmount;
        float _DistortionRadius;
        float _BlurStrength;
        fixed4 _VignetteColor;
        float _VignetteAlpha;
        float _VignetteSize;
        float _ReverseVignette; */