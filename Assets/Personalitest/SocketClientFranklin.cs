using UnityEngine;
using System;
using System.Threading.Tasks;
using Firesplash.UnityAssets.SocketIO;
using Newtonsoft.Json.Linq;

public class SocketClientFranklin : MonoBehaviour
{
    public SocketIOCommunicator socketIoCommunicator;

    public SocketIOCommunicator getSocketIoCommunicator()
    {
        return socketIoCommunicator;
    }

    // Start is called before the first frame update
    void Start()
    {
        socketIoCommunicator.Instance.On("connect", (String data) =>
        {
            Debug.Log("Connected");
            JObject msg = new JObject();
            msg.Add("action", "init");

            socketIoCommunicator.Instance.Emit("computerMessage", JToken.FromObject(msg).ToString());
        });

        socketIoCommunicator.Instance.On("disconnect", (String data) =>
        {
            Debug.Log("Disconnected");
        });
    }

    //target = -1 is a broadcast
    public void SendWebSocketMessage(string message)
    {
        //await WaitForConnection();
        socketIoCommunicator.Instance.Emit("computerMessage", message);
    }
    public async Task<bool> WaitForConnection()
    {
        
        while (Application.isPlaying && (socketIoCommunicator == null || socketIoCommunicator.Instance == null || !socketIoCommunicator.Instance.IsConnected()))
        {
            Debug.Log("waiting 1s for socket to open");
            await Task.Delay(1000);
        }
        
        return true;
    }

    public string GetConnectionUrl()
    {
        return socketIoCommunicator.socketIOAddress;
    }

    private void OnApplicationQuit()
    {
        Debug.Log("closing application");
        socketIoCommunicator.Instance.Close();
    }
}