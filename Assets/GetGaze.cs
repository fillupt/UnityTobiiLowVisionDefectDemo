using System.Collections;
using Tobii.Gaming;
using UnityEngine;
using Wilberforce.FinalVignette;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    private float scotOuter;
    public float rateOfChange;
    public bool simAbs;
    public Image noUserImage, noEyesImage;
    public Color noUserColor, noEyesColor;
    private int currSim;
    private float maxCatAlpha = 0.99f;
    private float minCatAlpha = 0.1f;
    public Toggle aT, cT, gT ,sT;

    private void Start()
    {
        cam       = Camera.main;
        camShader = Shader.Find("Hidden/Wilberfoce/FinalVignette");
        scotOuter = theVig.VignetteOuterValueDistance;
        noUserCanvas.SetActive(false);
    }

    void Update()
    {
        scotOuter = theVig.VignetteOuterValueDistance;
        simAbs = noUser;
        // Set noUserImage and noEyesImage to red as default
        noUserColor = Color.red;
        noEyesColor = Color.red;
   

        
        if (Input.GetKeyUp(KeyCode.R)) //Reset
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            theVig.VignetteInnerColor.a = 0f;
            theVig.VignetteOuterColor.a = 0.0f;
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
            minScot = .05f;
            theVig.VignetteOuterValueDistance = 1.5f;
        }

        if (Input.GetKeyUp(KeyCode.C) && cT.isOn) //cataract
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 2;
            theVig.VignetteInnerColor   = new Color(.29f, .29f, .15f, 1f);
            theVig.VignetteOuterColor   = catColour;
            theVig.VignetteInnerColor.a = .3f; 
            theVig.VignetteOuterColor.a = .1f; 
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
            theVig.VignetteOuterColor   = Color.gray;
            theVig.VignetteInnerColor.a = 1f; 
            theVig.VignetteOuterColor.a = 0.05f;
            theVig.VignetteFalloff      = 2f;
            maxScot = 2f;
            minScot = .2f;
            theVig.VignetteOuterValueDistance = 0.2f;
            theVig.VignetteInnerValueDistance = 0.01f;
        }

    if (Input.GetKeyUp(KeyCode.S) && sT.isOn) //scotoma
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
            currSim = 0;
            theVig.VignetteInnerColor   = Color.red;
            theVig.VignetteOuterColor   = Color.red;
            theVig.VignetteInnerColor.a = 0f; 
            theVig.VignetteOuterColor.a = 1f; 
            theVig.VignetteFalloff      = 1f;
            maxScot = 1.5f;
            minScot = .05f;
            theVig.VignetteOuterValueDistance = 1.5f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (currSim ==0 && scotOuter > minScot)
            {              
                theVig.VignetteOuterValueDistance -= (rateOfChange * Time.deltaTime);
            }
            if (currSim == 1 && scotOuter < maxScot) 
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

            if (currSim == 1 && scotOuter > minScot)
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
                noUserColor = Color.green;
                noUserCanvas.SetActive(false);
                cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
                noUser = false;
                GazePoint gazePoint = TobiiAPI.GetGazePoint();
                noUserColor = gazePoint.IsValid ? Color.green : Color.red;
                theVig.VignetteCenter.x = gazePoint.Screen.x / cam.pixelWidth;
                theVig.VignetteCenter.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
            else
            {
                if (!noUser)
                    StartCoroutine("StartAbsentMode"); // just run once per loss
                noUser = true;

            }
        }
        else // simulate with mouse if no Tobii connected
        {
            theVig.VignetteCenter.x = Input.mousePosition.x / cam.pixelWidth;
            theVig.VignetteCenter.y = Input.mousePosition.y / cam.pixelHeight;
        }
    }

    private IEnumerator StartAbsentMode()
    {
        cam.GetComponent<FinalVignetteCommandBuffer>().enabled = false;
        yield return new WaitForSeconds(1f);
        noUserCanvas.SetActive(true);
    }

}