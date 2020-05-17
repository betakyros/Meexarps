// Smooth towards the target
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CameraZoom : MonoBehaviour
{
    public Canvas canvas;
    public float originalWidth;
    public float originalHeight;
    public float originalAlpha;
    public float originalFontSize;
    public Vector2 originalMinAnchor;
    public Vector2 originalMaxAnchor;

    // Time taken for the transition.
    float duration = 1.0f;
    float pauseDuration = 12.0f;
    float maxFontSize = 40;
    bool gentleShake;
    bool resetAnchors;
    bool isMovie;

    float startTime;
    Vector3 originalPosition;

    void Start()
    {
    }

    public void Setup(float initialGrowDuration, float initialPauseDuration, bool setAsLastSibling, bool setGentleShake, bool setResetAnchors)
    {
        Setup(initialGrowDuration, initialPauseDuration, setAsLastSibling, setGentleShake, setResetAnchors, false);
    }

    public void Setup(float initialGrowDuration, float initialPauseDuration, bool setAsLastSibling, bool setGentleShake, bool setResetAnchors, bool setIsMovie)
    {
        isMovie = setIsMovie;

        RectTransform myRectTransform = gameObject.GetComponent<RectTransform>();

        duration = initialGrowDuration;
        pauseDuration = initialPauseDuration;
        originalWidth = myRectTransform.rect.width;
        originalHeight = myRectTransform.rect.height;
        originalMinAnchor = myRectTransform.anchorMin;
        originalMaxAnchor = myRectTransform.anchorMax;
        startTime = Time.time;
        originalPosition = gameObject.transform.position;
        if(!isMovie)
        {
            originalAlpha = gameObject.GetComponent<Image>().color.a;
            originalFontSize = gameObject.GetComponentInChildren<Text>().fontSize;
        }
        canvas = gameObject.GetComponentInParent<Canvas>();
        gentleShake = setGentleShake;
        resetAnchors = setResetAnchors;
        if (setAsLastSibling)
        {
            GetComponent<RectTransform>().SetAsLastSibling();
        }
        if (gentleShake)
        {
            GetComponent<GentleShake>().SetOriginalPosition(canvas.transform.position);
        }
        if (resetAnchors)
        {
            //remove the anchors so that sizeDelta will be a value instead of a scale factor
            myRectTransform.anchorMax = new Vector2(0f, 0f);
            myRectTransform.anchorMin = new Vector2(0f, 0f);
        }
    }

    void Update()
    {
        RectTransform myPanelTransform = GetComponent<RectTransform>();
        Image myImage = gameObject.GetComponent<Image>();
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        // Calculate the fraction of the total duration that has passed.
        float timePassed = Time.time - startTime;

        float currentWidth;
        float currentHeight;
        float currentX;
        float currentY;
        float curentAlpha;
        float currentFontSize;

        //growing
        if (timePassed < duration)
        {
            float t = timePassed / duration;

            currentWidth = Mathf.SmoothStep(originalWidth, canvasWidth, t);
            currentHeight = Mathf.SmoothStep(originalHeight, canvasHeight, t);

            currentX = Mathf.SmoothStep(originalPosition.x, canvas.transform.position.x, t);
            currentY = Mathf.SmoothStep(originalPosition.y, canvas.transform.position.y, t);

            if (!isMovie)
            {
                curentAlpha = Mathf.SmoothStep(originalAlpha, 1f, t);
                currentFontSize = Mathf.SmoothStep(originalFontSize, maxFontSize, t);
                myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, curentAlpha);
                gameObject.GetComponentInChildren<Text>().fontSize = Mathf.RoundToInt(currentFontSize);
            }
            myPanelTransform.sizeDelta = new Vector2(currentWidth, currentHeight);
            myPanelTransform.position = new Vector3(currentX, currentY, 0);
        }
        //paused
        else if(timePassed < (duration + pauseDuration))
        {
            //do nothing
        }
        else if(timePassed < (duration * 2 + pauseDuration))
        {
            float t = (timePassed - (duration + pauseDuration)) / duration;

            currentWidth = Mathf.SmoothStep(canvasWidth, originalWidth, t);
            currentHeight = Mathf.SmoothStep(canvasHeight, originalHeight, t);


            currentX = Mathf.SmoothStep(canvas.transform.position.x, originalPosition.x, t);
            currentY = Mathf.SmoothStep(canvas.transform.position.y, originalPosition.y, t);

            if (!isMovie)
            {
                curentAlpha = Mathf.SmoothStep(1f, originalAlpha, t);
                currentFontSize = Mathf.SmoothStep(maxFontSize, originalFontSize, t);
                myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, curentAlpha);
                gameObject.GetComponentInChildren<Text>().fontSize = Mathf.RoundToInt(currentFontSize);
            }

            myPanelTransform.sizeDelta = new Vector2(currentWidth, currentHeight);
            myPanelTransform.position = new Vector3(currentX, currentY, 0);
        }
        else
        {
            SetToDefaults(myPanelTransform);
        }

    }

    void OnDisable()
    {
        RectTransform myPanelTransform = GetComponent<RectTransform>();

        SetToDefaults(myPanelTransform);
    }

    void SetToDefaults(RectTransform myPanelTransform)
    {

        if (!isMovie)
        {
            Image myImage = gameObject.GetComponent<Image>();

            myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, originalAlpha);
            gameObject.GetComponentInChildren<Text>().fontSize = Mathf.RoundToInt(originalFontSize);
        }

        myPanelTransform.sizeDelta = new Vector2(originalWidth, originalHeight);
        myPanelTransform.position = originalPosition;
        if (gentleShake)
        {
            GetComponent<GentleShake>().SetOriginalPosition(originalPosition);
        }
        if (resetAnchors)
        {
            myPanelTransform.anchorMax = originalMaxAnchor;
            myPanelTransform.anchorMin = originalMinAnchor;
            //unclear why i have to do this
            myPanelTransform.position = originalPosition;
        }
        Destroy(this);
    }
}

