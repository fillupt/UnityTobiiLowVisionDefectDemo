using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        GetComponent<SpriteRenderer>().flipX = !GetComponent<SpriteRenderer>().flipX;
        yield return new WaitForSeconds(Random.Range(1,3));
        GetComponent<SpriteRenderer>().flipX = !GetComponent<SpriteRenderer>().flipX;
        WaitingFlip = true;
    }
}
