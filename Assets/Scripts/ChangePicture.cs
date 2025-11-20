
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
    private int currentImageIndex = 0;
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

    public bool hasStarted;
    public Button startBut;
    public GetGaze gg;
    public GameObject introCanvas, runtimeCanvas;
    public AudioSource audioSource;
    private void Start()
    {
        statusText.text = "LV Simulator V" + Application.version;
        startBut.onClick.AddListener(Begin);
        
        // Configure video player audio properly
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.controlledAudioTrackCount = 1;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, audioSource);
        
        // Prepare video player
        vp.Prepare();
        vp.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Video is ready to play with audio
        Debug.Log("Video prepared and ready");
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
                    currentImageIndex = i; //actual key is +1 and 0 ==10;
                    defaultImage.sprite = theIms[currentImageIndex];
                }
                // use tilde key to play the video
                if (Input.GetKeyUp(KeyCode.BackQuote))
                {
                    defaultImage.enabled = false;
                    if (!vp.isPrepared)
                        vp.Prepare();
                    vp.Play();
                }
            }

            // Navigate images with left/right arrow keys
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                vp.Stop();
                defaultImage.enabled = true;
                currentImageIndex = (currentImageIndex + 1) % theIms.Length;
                defaultImage.sprite = theIms[currentImageIndex];
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                vp.Stop();
                defaultImage.enabled = true;
                currentImageIndex = (currentImageIndex - 1 + theIms.Length) % theIms.Length;
                defaultImage.sprite = theIms[currentImageIndex];
            }
        }

        // Use Escape key to quit or go back to intro
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (introCanvas.activeSelf)
                Application.Quit();
            else{
                gg.ResetShader();
                hasStarted = false;
                runtimeCanvas.SetActive(false);
                introCanvas.SetActive(true);
            }
        }
    }

    void Begin()
    {
        currentImageIndex = UnityEngine.Random.Range(0, theIms.Length);
        defaultImage.sprite = theIms[currentImageIndex];
        introCanvas.SetActive(false);
        runtimeCanvas.SetActive(true);
        gg.enabled = true;
        hasStarted = true;
    }
}
