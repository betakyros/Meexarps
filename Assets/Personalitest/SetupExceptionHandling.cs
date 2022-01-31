using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class ExceptionHandling
{
    private static GameObject errorPanel;
    private static SocketClientFranklin socket;

    static bool isExceptionHandlingSetup;
    public static void SetupExceptionHandling(GameObject ePanel, SocketClientFranklin s)
    {
        if (!isExceptionHandlingSetup)
        {
            errorPanel = ePanel;
            isExceptionHandlingSetup = true;
            Application.logMessageReceived += HandleException;
            socket = s;
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

            JObject msg = new JObject();
            msg.Add("action", "log");
            msg.Add("message", errorText.text);
            msg.Add("context", "error");

            socket.getSocketIoCommunicator().Instance.Emit("computerMessage", JToken.FromObject(msg).ToString());
        }
    }
}
