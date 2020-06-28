// Smooth towards the target
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FadeSplashScreen : MonoBehaviour
{
    public Canvas canvas;
    public float originalWidth;
    public float originalHeight;
    public float originalAlpha;
    public float originalFontSize;
    public Vector2 originalMinAnchor;
    public Vector2 originalMaxAnchor;

    // Time taken for the transition.
    float duration;
    float startTime;

    void Start()
    {
    }
    

    public void Setup(float d)
    {
        duration = d;
        RectTransform myRectTransform = gameObject.GetComponent<RectTransform>();

        startTime = Time.time;
        originalAlpha = gameObject.GetComponentInChildren<Image>().color.a;

        canvas = gameObject.GetComponentInParent<Canvas>();
    }

    void Update()
    {
        Image[] myImages = gameObject.GetComponentsInChildren<Image>();
        // Calculate the fraction of the total duration that has passed.
        float timePassed = Time.time - startTime;

        float curentAlpha;
        if (timePassed < duration)
        {
            float t = timePassed / duration;

            curentAlpha = Mathf.SmoothStep(originalAlpha, 0f, t);
            foreach(Image myImage in myImages)
            {
                myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, curentAlpha);
            }
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(this);
        }
    }
}

