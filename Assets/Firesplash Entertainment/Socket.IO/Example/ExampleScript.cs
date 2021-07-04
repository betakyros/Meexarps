using Firesplash.UnityAssets.SocketIO;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ExampleScript : MonoBehaviour
{
    public SocketIOCommunicator sioCom;
    public Text uiStatus, uiGreeting, uiPodName;

    [Serializable]
    struct ItsMeData
    {
        public string version;
    }

    [Serializable]
    struct ServerTechData
    {
        public string timestamp;
        public string podName;
    }

    // Start is called before the first frame update
    void Start()
    {
        //sioCom is assigned via inspector so no need to initialize it.
        //We just fetch the actual Socket.IO instance using its integrated Instance handle and subscribe to the connect event
        sioCom.Instance.On("connect", (string data) => {
            Debug.Log("LOCAL: Hey, we are connected!");
            uiStatus.text = "Socket.IO Connected. Doing work...";

            //NOTE: All those emitted and received events (except connect and disconnect) are made to showcase how this asset works. The technical handshake is done automatically.

            //First of all we knock at the servers door
            //EXAMPLE 1: Sending an event without payload data
            sioCom.Instance.Emit("KnockKnock");
        });

        //The server will respont to our knocking by askin who we are:
        //EXAMPLE 2: Listening for an event without payload
        sioCom.Instance.On("WhosThere", (string payload) =>
        {
            //We will always receive a payload object as Socket.IO does not distinguish. In case the server sent nothing (as it will do in this example) the object will be null.
            if (payload == null) 
                Debug.Log("RECEIVED a WhosThere event without payload data just as expected.");

            //As the server just asked for who we are, let's be polite and answer him.
            //EXAMPLE 3: Sending an event with payload data
            ItsMeData me = new ItsMeData()
            {
                version = Application.unityVersion
            };
            sioCom.Instance.Emit("ItsMe", JsonUtility.ToJson(me));

        });


        //The server will now receive our event and parse the data we sent. Then it will answer with two events.
        //EXAMPLE 4: Listening for an event with plain text payload
        sioCom.Instance.On("Welcome", (string payload) =>
        {
            Debug.Log("SERVER: " + payload);
            uiGreeting.text = payload;
        });


        //EXAMPLE 5: Listening for an event with JSON payload
        sioCom.Instance.On("TechData", (string payload) =>
        {
            ServerTechData srv = JsonUtility.FromJson<ServerTechData>(payload);
            Debug.Log("Received the POD name from the server. Upadting UI. Oh! It's " + srv.timestamp + " by the way.");
            uiPodName.text = "I talked to " + srv.podName;
        });


        //When the conversation is done, the server will close out connection.
        sioCom.Instance.On("disconnect", (string payload) => {
            if (payload.Equals("io server disconnect"))
            {
                Debug.Log("Disconnected from server.");
                uiStatus.text = "Finished. Server closed connection.";
            } 
            else
            {
                Debug.LogWarning("We have been unexpecteldy disconnected. This will cause an automatic reconnect. Reason: " + payload);
            }
        });


        //We are now ready to actually connect
        sioCom.Instance.Connect();
    }
}
