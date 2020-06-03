using UnityEngine;
using UnityEngine.UI;
public class ExceptionHandling
{
    private static GameObject errorPanel;

    static bool isExceptionHandlingSetup;
    public static void SetupExceptionHandling(GameObject ePanel)
    {
        if (!isExceptionHandlingSetup)
        {
            errorPanel = ePanel;
            isExceptionHandlingSetup = true;
            Application.logMessageReceived += HandleException;
        }
    }

    static void HandleException(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            errorPanel.SetActive(true);
            Text errorText = errorPanel.GetComponentInChildren<Text>();
            errorText.text = "ERROR: " + condition + "\n" + stackTrace;
            Debug.Log("ERROR: " + condition + "\n" + stackTrace);
        }
    }
}
