using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangePicture : MonoBehaviour
{
    public Sprite[] theIms;
    public Image image;
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
         KeyCode.Alpha0
     };


    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyUp(keyCodes[i]) && theIms.Length > i) 
            {
                int numberPressed = i; //actual key is +1 and 0 ==10;
                image.sprite = theIms[numberPressed];
            }
        }
    }
}
