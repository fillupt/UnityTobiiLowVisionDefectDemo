using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomlyFlip : MonoBehaviour
{
private bool  WaitingFlip = true;

    void Update()
    {
        if (WaitingFlip)
            StartCoroutine(FlipImage());
    }

    IEnumerator FlipImage(){
        WaitingFlip = false;
        yield return new WaitForSeconds(Random.Range(5,15));
        GetComponent<Image>().transform.localScale = new Vector3(-GetComponent<Image>().transform.localScale.x, GetComponent<Image>().transform.localScale.y, GetComponent<Image>().transform.localScale.z);
        yield return new WaitForSeconds(Random.Range(1,3));
        GetComponent<Image>().transform.localScale = new Vector3(-GetComponent<Image>().transform.localScale.x, GetComponent<Image>().transform.localScale.y, GetComponent<Image>().transform.localScale.z);
        WaitingFlip = true;
    }
}
