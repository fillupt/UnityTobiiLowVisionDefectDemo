using System.Collections;
using Tobii.Gaming;
using UnityEngine;
using Wilberforce.FinalVignette;
using UnityEngine.UI;

public class GetGaze : MonoBehaviour
{
    private Camera cam;
    public FinalVignetteCommandBuffer theVig;
    private Shader camShader;
    public Color catColour;
    private bool noUser;
    private float maxScot = 1.99f;
    private float minScot = .01f;
    private float scotOuter;
    public float rateOfChange;
    public bool simAbs;
    private int currSim;
    private float maxCatAlpha = 0.99f;
    private float minCatAlpha = 0.1f;
    public Toggle aT, cT, gT;

    private void Start()
    {
        cam       = Camera.main;
        camShader = Shader.Find("Hidden/Wilberfoce/FinalVignette");
        scotOuter = theVig.VignetteOuterValueDistance;
    }

    void Update()
    {
        scotOuter = theVig.VignetteOuterValueDistance;

        if (simAbs)
        {  noUser = true;  }   else { noUser = false; }

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
            theVig.VignetteOuterColor   = Color.black;
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
            theVig.VignetteInnerColor   = catColour;
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
            maxScot = 1.99f;
            minScot = .2f;
            theVig.VignetteOuterValueDistance = 0.5f;
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
            }
            if (currSim == 2 && theVig.VignetteInnerColor.a < maxCatAlpha)
            {
                theVig.VignetteInnerColor.a += (rateOfChange * Time.deltaTime);
                theVig.VignetteOuterColor.a += (rateOfChange * Time.deltaTime);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {  
            Application.Quit();
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
                cam.GetComponent<FinalVignetteCommandBuffer>().enabled = true;
                noUser = false;
                GazePoint gazePoint = TobiiAPI.GetGazePoint();
                HeadPose headPose = TobiiAPI.GetHeadPose();
                theVig.VignetteCenter.x = gazePoint.Screen.x / cam.pixelWidth;
                theVig.VignetteCenter.y = gazePoint.Screen.y / cam.scaledPixelHeight;
            }
            else
            {
                noUser = true;
                StartCoroutine("StartAbsentMode");
            }
        }
        else
        {
            theVig.VignetteCenter.x = Input.mousePosition.x / cam.pixelWidth;
            theVig.VignetteCenter.y = Input.mousePosition.y / cam.pixelHeight;
        }
    }

    private IEnumerator StartAbsentMode()
    {
        yield return new WaitForSeconds(1.5f);
        if (noUser)
        {
            cam.GetComponent<FinalVignetteCommandBuffer>().enabled = false;
        }
    }
}