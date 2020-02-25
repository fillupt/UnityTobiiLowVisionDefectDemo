using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class startLVSim : MonoBehaviour
{
    public Button startBut;
    public LoadMovie lm;
    public ChangePicture cp;
    public GetGaze gg;
    public GameObject ws;
    public VideoPlayer vp;
    public Image defaultImage;
    public ParticleSystem floaters;

    void Start()
    {
        startBut.onClick.AddListener(Begin);
    }

    private void Update()
    {
        if (lm.movieLoaded || cp.imgLoaded)
        {
            startBut.interactable = true;
        }
    }
    // Update is called once per frame
    void Begin()
    {
        floaters.Stop();
        ws.SetActive(false);
        gg.enabled = true;

        if (lm.movieLoaded)
        {
            defaultImage.enabled = false;
            vp.Play();
        }
        else if (cp.imgLoaded)
        {     
            defaultImage.sprite = cp.theIms[UnityEngine.Random.Range(0, cp.theIms.Length)];
        }
    }
}
