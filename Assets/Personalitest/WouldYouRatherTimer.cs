﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WouldYouRatherTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float startTime;
    //this is just the display. must match         InvokeRepeating("SendWouldYouRather", 0f, 1f);
    private float duration = 20f;

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
            timerText.SetText(Mathf.RoundToInt(duration - ((Time.time - startTime))) + "");
        }
    }

    public void SetTimerText(TextMeshProUGUI t)
    {
        timerText = t;
    }

    public void SetTimerDuration(float f)
    {
        duration = f;
    }
}
