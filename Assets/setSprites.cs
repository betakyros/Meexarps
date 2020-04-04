using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class setSprites : MonoBehaviour
{
    public Sprite myIdleSprite;
    public Image myImage;

    void LateUpdate()
    {
        myImage.overrideSprite = myIdleSprite;
        myImage.sprite = myIdleSprite;
    }
}
