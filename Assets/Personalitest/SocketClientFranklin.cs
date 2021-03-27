using Socket.Quobject.SocketIoClientDotNet.Client;
using UnityEngine;

public class SocketClientFranklin : MonoBehaviour
{
    private QSocket socket;

    // Start is called before the first frame update
    public void Connect()
    {
        socket = IO.Socket("http://localhost:3000");
        socket.On(QSocket.EVENT_CONNECT, () => {
            Debug.Log("Connected");
            socket.Emit("chat", "test");
            socket.Emit("fas", "afsdfsa");
            socket.Emit("afsddfsa", "asfdfdsafdas");
            socket.Emit("afdsfadsadfsadfs", "dfasfadsfadsfd");
            socket.Disconnect();
        });

        socket.On("chat", data => {
            Debug.Log("data : " + data);
        });
        
    }
    private void OnDestroy()
    {
        socket.Disconnect();
    }
}
