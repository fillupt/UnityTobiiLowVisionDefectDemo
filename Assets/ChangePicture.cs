using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System;
using UnityEngine.Events;
using TMPro;

public class ChangePicture : MonoBehaviour
{
    public Sprite[] theIms;
    public Image defaultImage;
    public Button foldPicBut, StartBut;
    public TMP_Text statusText;
    private bool imgLoaded, gtg;
    private GameObject men;
    public GetGaze gg;

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

    private void Start()
    {
        foldPicBut.onClick.AddListener(LoadImages);
        StartBut.onClick.AddListener(Begin);
        men = GameObject.Find("WelcomeMenu");
        statusText.text = "LV Simulator V" + Application.version;
    }

    private void Begin()
    {
        gtg = true;
        men.SetActive(false);
        gg.enabled = true;
        defaultImage.sprite = theIms[UnityEngine.Random.Range(0, theIms.Length)];
    }

    private void LoadImages()
    {
        string fold = EditorUtility.OpenFolderPanel("Select Image Directory", Application.dataPath, "");
        if (fold != "")
        {
            var fileInfo = Directory.GetFiles(fold, "*.png");
            theIms = new Sprite[fileInfo.Length];

            for (int i = 0; i < Math.Min(keyCodes.Length, fileInfo.Length); i++)
            {
                theIms[i] = LoadNewSprite(fileInfo[i]);
            }
            imgLoaded = true;
        }
        else 
        { 
            statusText.text = "No suitable images found"; 
            imgLoaded = false;
        }  
    }

    public Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f)
    {
        Texture2D SpriteTexture = LoadTexture(FilePath);
        Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);
        return NewSprite;
    }

    public Texture2D LoadTexture(string FilePath)
    {
        Texture2D Tex2D;
        byte[] FileData;

        if (File.Exists(FilePath))
        {
            FileData = File.ReadAllBytes(FilePath);
            Tex2D = new Texture2D(2, 2);           
            if (Tex2D.LoadImage(FileData))         
                return Tex2D;                
        }
        return null;                     
    }

    // Update is called once per frame
    void Update()
    {
        StartBut.interactable = imgLoaded;

        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyUp(keyCodes[i]) && theIms.Length > i) 
            {
                int numberPressed = i; //actual key is +1 and 0 ==10;
                defaultImage.sprite = theIms[numberPressed];
            }
        }
    }
}
