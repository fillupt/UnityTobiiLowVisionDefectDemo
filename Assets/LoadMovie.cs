using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Video;

public class LoadMovie : MonoBehaviour
{
    public Button movBut;
    public TMP_Text statusText;
    public Image theScreen;
    public GameObject welcomeScreen;
    public GetGaze gg;
    public bool movieLoaded = false;

    void Start()
    {
        movBut.onClick.AddListener(LoadDefaultMovie);
    }

    // private void LoadMovies()
    // {
    //     string movFile = EditorUtility.OpenFilePanel("Select Movie File", Application.dataPath + "/movie", "mp4");
    //     if (movFile != "")
    //     {           
    //         theScreen.GetComponent<VideoPlayer>().url = movFile;
    //         movieLoaded = true;
    //         statusText.text = "Movie ready";

    //     }
    //     else
    //     {
    //         statusText.text = "No suitable movie found";
    //         movieLoaded = false;
    //     }
    // }

    private void LoadDefaultMovie()
    { 
        //theScreen.GetComponent<VideoPlayer>().url = Application.dataPath + "/movie/" + "VirtualPatientDemoReel.mp4";
        movieLoaded = true;
        statusText.text = "Movie ready";
    }
}