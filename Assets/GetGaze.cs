using Tobii.Gaming;
using UnityEngine;
using Wilberforce.FinalVignette;
using UnityEngine.UI;

public class GetGaze : MonoBehaviour
{
    private Camera cam;
    public FinalVignetteCommandBuffer theVig;
    private Shader camShader;
    public GameObject noUserCanvas;
    public Color catColour;
    private bool noUser;
    private float maxScot = 1.99f;
    private float minScot = .01f;
    private float scotOuter, randomWetness;
    private float inactiveTimeOut = 1.5f;
    private float lastSeenTime;
    public float rateOfChange;
    public Image noUserImage, noEyesImage;
    public Color noUserColor, noEyesColor;
    private int currSim;
    private float maxCatAlpha = 0.99f;
    private float minCatAlpha = 0.1f;
    public Toggle aT, cT, gT ,oT;
    public float distortionSize;
    public float distortionAmount;
    public float distortRadius;
    public Image image;

    private void Start()
    {
        cam       = Camera.main;
        camShader = Shader.Find("Hidden/Wilberfoce/FinalVignette");
        scotOuter = theVig.VignetteOuterValueDistance;
        noUserCanvas.SetActive(false);
        lastSeenTime = Time.time;
    }

    void Update()
    {
        
        scotOuter = theVig.VignetteOuterValueDistance;
        // Set noUserImage and noEyesImage to red as default
        noUserColor = Color.red;
        noEyesColor = Color.red;
        
        if (Input.GetKeyUp(KeyCode.R)) //Reset
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            theVig.VignetteInnerColor.a = 0f;
            theVig.VignetteOuterColor.a = 0.0f;
            currSim = -1;
        }

        if (Input.GetKeyUp(KeyCode.G) && gT.isOn) //glaucoma
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 0;
            theVig.VignetteInnerColor   = Color.black;
            theVig.VignetteOuterColor   = new Color(0.1f, 0.1f, 0.1f, 1f);
            theVig.VignetteInnerColor.a = 0f; 
            theVig.VignetteOuterColor.a = 1f; 
            theVig.VignetteFalloff      = 1f;
            maxScot = 1.5f;
            minScot = .08f;
            theVig.VignetteOuterValueDistance = 1.5f;
            theVig.VignetteInnerValueDistance = 0f;
        }

        if (Input.GetKeyUp(KeyCode.C) && cT.isOn) //cataract
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 2;
            theVig.VignetteInnerColor   = new Color(.18f, .18f, .08f, 1f);
            theVig.VignetteOuterColor   = catColour;
            theVig.VignetteInnerColor.a = .5f; 
            theVig.VignetteOuterColor.a = .25f; 
            theVig.VignetteFalloff      = 1f;
            maxScot = 1.5f;
            minScot = .05f;
            theVig.VignetteOuterValueDistance = 1.5f;
        }

        if (Input.GetKeyUp(KeyCode.A) && aT.isOn)//AMD
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 1;
            theVig.VignetteInnerColor   = Color.black;
            theVig.VignetteOuterColor   = Color.black;
            theVig.VignetteInnerColor.a = 1f; 
            theVig.VignetteOuterColor.a = 0.02f;
            theVig.VignetteFalloff      = 2f;
            maxScot = 2f;
            minScot = .2f;
            theVig.VignetteOuterValueDistance = 0.2f;
            theVig.VignetteInnerValueDistance = 0.01f;
            randomWetness = Random.Range(0.1f, 0.4f);
        }

        if (Input.GetKeyUp(KeyCode.O) && oT.isOn) //oedema
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 4;
            theVig.VignetteInnerColor   = Color.gray;
            theVig.VignetteOuterColor   = Color.gray;
            theVig.VignetteInnerColor.a = .3f; 
            theVig.VignetteOuterColor.a = 0.01f;
            theVig.VignetteFalloff      = 1f;
            maxScot = 2f;
            minScot = .2f;
            theVig.VignetteOuterValueDistance = 0.2f;
            theVig.VignetteInnerValueDistance = 0.01f;
            randomWetness = Random.Range(0.1f, 0.4f);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (currSim ==0 && scotOuter > minScot)
            {              
                theVig.VignetteOuterValueDistance -= (rateOfChange * Time.deltaTime);
            }
            if ((currSim == 1 | currSim == 4) && scotOuter < maxScot) 
            {
                theVig.VignetteOuterValueDistance += (rateOfChange * Time.deltaTime);
                theVig.VignetteInnerValueDistance += (0.2f*rateOfChange * Time.deltaTime);
            }
            if (currSim == 2 && theVig.VignetteInnerColor.a < maxCatAlpha)
            {
                theVig.VignetteInnerColor.a += (rateOfChange * Time.deltaTime);
                theVig.VignetteOuterColor.a += (rateOfChange * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (currSim == 0  && scotOuter < maxScot)
            {
                theVig.VignetteOuterValueDistance += (rateOfChange * Time.deltaTime);
            }

            if ((currSim == 1 | currSim == 4) && scotOuter > minScot)
            {
                theVig.VignetteOuterValueDistance -= (rateOfChange * Time.deltaTime);
                theVig.VignetteInnerValueDistance -= (0.2f*rateOfChange * Time.deltaTime);
            }

            if (currSim == 2 && theVig.VignetteInnerColor.a > minCatAlpha)
            {
                theVig.VignetteInnerColor.a -= (rateOfChange * Time.deltaTime);
                theVig.VignetteOuterColor.a -= (rateOfChange * Time.deltaTime);
            }
        }
        
        if (TobiiAPI.IsConnected)
        {
            UserPresence userPresence = TobiiAPI.GetUserPresence();
            if (userPresence.IsUserPresent()) 
            {
                lastSeenTime = Time.time;
                noUserColor = Color.green;
                noUserCanvas.SetActive(false);
                cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
                GazePoint gazePoint = TobiiAPI.GetGazePoint();
                noUserColor = gazePoint.IsValid ? Color.green : Color.red;
                theVig.VignetteCenter.x = gazePoint.Screen.x / cam.pixelWidth;
                theVig.VignetteCenter.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
            else
            {
                // do a graduated release to no user mode
                if ((Time.time - lastSeenTime) > inactiveTimeOut/2f)
                    cam.GetComponent<FinalVignetteCommandBuffer>().enabled = false;

                if ((Time.time - lastSeenTime) > inactiveTimeOut)
                    noUserCanvas.SetActive(true);
            }
        }
        else // simulate with mouse if no Tobii connected
        {      
            theVig.VignetteCenter.x = Input.mousePosition.x / cam.pixelWidth;
            theVig.VignetteCenter.y = Input.mousePosition.y / cam.scaledPixelHeight;
        }

        // now distortion layer
        distortRadius = theVig.VignetteInnerValueDistance * 8f;

        if (currSim == 4)
            distortionSize = 0.09f;
        else
            distortionSize = 0.5f;

        // Calculate the region to distort based on the normalized position and distortion size
        Rect distortionRect = new Rect(theVig.VignetteCenter.x - distortionSize * 0.5f,
                                        theVig.VignetteCenter.y - distortionSize * 0.5f,
                                        distortionSize,
                                        distortionSize);
        distortionAmount = randomWetness + theVig.VignetteInnerValueDistance;
        // Apply distortion effect to the region
        if (currSim == 1 | currSim == 4) // AMD or Oedema
            ApplyDistortion(distortionRect, theVig.VignetteCenter, distortionAmount, distortRadius);
        else
            ApplyDistortion(distortionRect, theVig.VignetteCenter, 0f, distortRadius);
    }

    private void ApplyDistortion(Rect region, Vector2 distortionCenter, float amount, float radius)
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
        image.material = material;
    }
}