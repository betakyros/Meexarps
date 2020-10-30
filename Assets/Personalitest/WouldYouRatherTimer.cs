using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WouldYouRatherTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float startTime;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if(null != timerText)
        {
            timerText.SetText(Mathf.RoundToInt(20f - ((Time.time - startTime) % 60)) + "");
        }
    }

    public void SetTimerText(TextMeshProUGUI t)
    {
        timerText = t;
    }
}
