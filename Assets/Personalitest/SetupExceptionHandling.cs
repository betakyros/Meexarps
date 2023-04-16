using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class ExceptionHandling
{
    private static GameObject errorPanel;
    private static SocketClientFranklin socket;
    private static main m;

    static bool isExceptionHandlingSetup;
    public static void SetupExceptionHandling(GameObject ePanel, SocketClientFranklin s, main ma)
    {
        if (!isExceptionHandlingSetup)
        {
            errorPanel = ePanel;
            isExceptionHandlingSetup = true;
            Application.logMessageReceived += HandleException;
            socket = s;
            m = ma;
        }
    }

    static void HandleException(string condition, string stackTrace, LogType type)
    {

        if (type == LogType.Exception)
        {
            m.printActionList();
            m.printState();

            errorPanel.SetActive(true);
            Text errorText = errorPanel.GetComponentInChildren<Text>();
            errorText.text = "This message will dissapear in 10 seconds. Please screenshot and send to the developers.\nERROR: " + condition + "\n" + stackTrace;
            Debug.Log("ERROR: " + condition + "\n" + stackTrace);

            JObject msg = new JObject();
            msg.Add("action", "log");
            msg.Add("message", errorText.text);
            msg.Add("context", "error");

            socket.getSocketIoCommunicator().Instance.Emit("computerMessage", JToken.FromObject(msg).ToString(), false);
        }
    }
}
