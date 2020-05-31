using UnityEngine;
using UnityEditor;

public class ExceptionHandling : MonoBehaviour
{
    static bool isExceptionHandlingSetup;
    public static void SetupExceptionHandling()
    {
        if (!isExceptionHandlingSetup)
        {
            isExceptionHandlingSetup = true;
            Application.logMessageReceived += HandleException;
        }
    }

    static void HandleException(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            EditorUtility.DisplayDialog("ERROR", condition + "\n" + stackTrace,"ok");
        }
    }
}
