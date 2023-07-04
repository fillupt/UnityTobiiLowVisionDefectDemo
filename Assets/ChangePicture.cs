
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class ChangePicture : MonoBehaviour
{
    public Sprite[] theIms;
    public Image defaultImage;
    public TMP_Text statusText;
    public VideoPlayer vp;
    private KeyCode[] keyCodes = {
         KeyCode.Alpha1,
         KeyCode.Alpha2,
         KeyCode.Alpha3,
         KeyCode.Alpha4,
         KeyCode.Alpha5,
         KeyCode.Alpha6,
         KeyCode.Alpha7,
         KeyCode.Alpha8,
         KeyCode.Alpha9,
         KeyCode.Alpha0,
         KeyCode.Minus,
         KeyCode.Equals
     };

    private bool hasStarted;
    public Button startBut;
    public GetGaze gg;
    public GameObject introCanvas;
    public SpriteRenderer VPlogo, VPLogoSmall, SOVSlogo;
    public ParticleSystem floaters;

    private void Start()
    {
        statusText.text = "LV Simulator V" + Application.version;
        startBut.onClick.AddListener(Begin);
        VPLogoSmall.enabled = false;
    }

    void Update()
    {
        if (hasStarted){
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyUp(keyCodes[i]) && theIms.Length > i) 
                {
                    vp.Stop();
                    defaultImage.enabled = true;
                    int numberPressed = i; //actual key is +1 and 0 ==10;
                    defaultImage.sprite = theIms[numberPressed];
                }
                // use tilde key to play the video
                if (Input.GetKeyUp(KeyCode.BackQuote))
                {
                    defaultImage.enabled = false;
                    vp.Play();
                }
            }
        }

        // reload the scene if pressing escape if SOVSlogo is not enabled, or quit application if it is
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (SOVSlogo.enabled)
                Application.Quit();
            else
                SceneManager.LoadScene(0);
        }
    }

    void Begin()
    {
        defaultImage.sprite = theIms[UnityEngine.Random.Range(0, theIms.Length)];
        floaters.Stop();
        floaters.Clear();
        introCanvas.SetActive(false);
        gg.enabled = true;
        VPlogo.enabled = false;
        SOVSlogo.enabled = false;
        VPLogoSmall.enabled = true;
        hasStarted = true;
    }
}
