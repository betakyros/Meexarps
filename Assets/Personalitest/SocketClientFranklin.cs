using UnityEngine;
using NativeWebSocket;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

public class SocketClientFranklin : MonoBehaviour
{
    static WebSocket websocket { get; set; }

    public WebSocket getWebSocket()
    {
        return websocket;
    }

    // Start is called before the first frame update
    async void Start()
    {
//        websocket = new WebSocket("ws://localhost:8080");
        websocket = new WebSocket("wss://meexarps-server.herokuapp.com");
//    https://meexarps-server.herokuapp.com/
        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            /*
            Debug.Log("OnMessage!");
            Debug.Log(bytes);
            */
            // getting the message as a string
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("OnMessage! " + message);
        };

        // waiting for messages
        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    //target = -1 is a broadcast
    public async void SendWebSocketMessage(string message)
    {
        await WaitForConnection();
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
    }
    public async Task<bool> WaitForConnection()
    {
        while (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.Log("waiting 1s for socket to open");
            await Task.Delay(1000);
        }
        return true;
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}