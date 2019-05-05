using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WouldYouRatherTimer : MonoBehaviour
{
    public Text timerText;
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
            timerText.text = Mathf.RoundToInt(15f - ((Time.time - startTime) % 60)) + "";
        }
    }

    public void SetTimerText(Text t)
    {
        timerText = t;
    }
}
