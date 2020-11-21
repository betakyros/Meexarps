using UnityEngine;
using System.Collections;

public class GentleShake : MonoBehaviour
{
    private Transform myTransform;
    

    public Vector3 originalPos;
    public float bobAmountVertical;
    public float bobAmountHorizontal;
    public float bobSpeed;

    private float maxX;
    private float minX;
    private float currentX;

    private float maxY;
    private float minY;
    private float currentY;

    public float rotationAmount;
    public float rotationSpeed;
    private float originalRotationZ;
    private float minRotationZ;
    private float maxRotationZ;
    private float currentRotationZ;

    void Start()
    {
        RectTransform rectTransform = (RectTransform)transform;

        originalPos = rectTransform.anchoredPosition;
        maxX = bobAmountHorizontal;
        minX = - bobAmountHorizontal;
        maxY = bobAmountVertical;
        minY = - bobAmountVertical;
        currentX = maxX;
        currentY = maxY;

        originalRotationZ = transform.rotation.eulerAngles.z;
        minRotationZ = originalRotationZ - rotationAmount;
        minRotationZ = 360f + minRotationZ;        
        maxRotationZ = originalRotationZ + rotationAmount;
        //maxRotationZ += 360f;
        currentRotationZ = maxRotationZ;
    }

    void Update()
    {
        RectTransform rectTransform = (RectTransform)transform;
        if (rectTransform.anchoredPosition.x <= minX * .99f)
        {
            maxX = Random.Range(originalPos.x, originalPos.x + bobAmountHorizontal);
            currentX = maxX;
        }
        else if (rectTransform.anchoredPosition.x >= maxX*.99f)
        {
            minX = Random.Range(originalPos.x - bobAmountHorizontal, originalPos.x);
            currentX = minX;
        }

        if (rectTransform.anchoredPosition.y <= minY * .99f)
        {
            maxY = Random.Range(originalPos.y, originalPos.y + bobAmountVertical);
            currentY = maxY;
        }
        else if (rectTransform.anchoredPosition.y >= maxY * .99f)
        {
            minY = Random.Range(originalPos.y - bobAmountVertical, originalPos.y);
            currentY = minY;
        }


        if ((transform.rotation.eulerAngles.z % 360f) <= minRotationZ + 1f && transform.rotation.eulerAngles.z > 180f)
        {
            maxRotationZ = Random.Range(originalRotationZ, originalRotationZ + rotationAmount);
            currentRotationZ = maxRotationZ;
        }
        else if (transform.rotation.eulerAngles.z % 360f >= maxRotationZ - 1f && transform.rotation.eulerAngles.z < 180f)
        {
            minRotationZ = Random.Range(originalRotationZ - rotationAmount, originalRotationZ) + 360f;
            currentRotationZ = minRotationZ;
        }
        // Rotate the cube by converting the angles into a quaternion.
        Quaternion target = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, currentRotationZ);
        rectTransform.anchoredPosition = Vector2.MoveTowards(rectTransform.anchoredPosition, new Vector2(currentX, currentY), bobSpeed);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed);
    }

    public void SetOriginalPosition(Vector3 newOriginalPos)
    {
        if(newOriginalPos != originalPos)
        {
            originalPos = newOriginalPos;
            maxX = originalPos.x + bobAmountHorizontal;
            minX = originalPos.x - bobAmountHorizontal;
            maxY = originalPos.y + bobAmountVertical;
            minY = originalPos.y - bobAmountVertical;
            currentX = maxX;
            currentY = maxY;
        }
    }
}