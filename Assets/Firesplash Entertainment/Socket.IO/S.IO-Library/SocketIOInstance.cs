using System;
using System.Collections.Generic;

namespace Firesplash.UnityAssets.SocketIO
{
    /// <summary>
    /// This is the class you will use to setup your events.
    /// It's the equivalent to JS socket.io's object returned from var con = io(...).
    /// You get an instance of this class by adding the SocketIOCommunicator to a GameObject and using it's "Instance" field.
    /// </summary>
    public class SocketIOInstance
    {
        /// <summary>
        /// DISCONNECTED means a disconnect happened upon request or a connection has never been attempted.
        /// CONNECTED is obvious
        /// ERROR means that connection should be established but it is not (check log output)
        /// RECONNECTING means that connection was established but got disconnected and the system is still trying to reconnect
        /// </summary>
        public enum SIOStatus { DISCONNECTED, CONNECTED, ERROR, RECONNECTING };

        public SIOStatus Status { get; internal set; } = SIOStatus.DISCONNECTED;

        protected string InstanceName;
        protected string GameObjectName;
        protected string targetAddress;
        protected bool enableAutoReconnect;
        protected SIOAuthPayload authPayload = null;

        public virtual string SocketID
        {
            get; internal set;
        }

        private Dictionary<string, List<SocketIOEvent>> eventCallbacks;

        /// <summary>
        /// This is the callback type for Socket.IO events
        /// </summary>
        /// <param name="data">The data payload of the transmitted event. Plain text or stringified JSON object.</param>
        public delegate void SocketIOEvent(string data);

        internal SocketIOInstance(string gameObjectName, string targetAddress, bool enableReconnect)
        {
            eventCallbacks = new Dictionary<string, List<SocketIOEvent>>();
            this.InstanceName = gameObjectName;
            this.GameObjectName = gameObjectName;
            this.targetAddress = targetAddress;
            this.enableAutoReconnect = enableReconnect;
        }

        protected void PrepareDestruction()
        {
            if (IsConnected()) Close();
        }
        ~SocketIOInstance()
        {
            Status = SIOStatus.DISCONNECTED;
            eventCallbacks = null;
        }

        public virtual bool IsConnected()
        {
			return Status == SIOStatus.CONNECTED;
        }

        /// <summary>
        /// Connect this Socket.IO instance using the stored parameters from last connect / component configuration
        /// </summary>
        public virtual void Connect()
        {
            Connect(this.targetAddress, this.enableAutoReconnect, this.authPayload);
        }

        /// <summary>
        /// Connect this Socket.IO instance using the component's set configuration but with (new) auth data
        /// </summary>
        /// <param name="authPayload">An instance of SIOAuthPayload to be sent upon (re-)connection. Can for example be used to send an authentication token.</param>
        public virtual void Connect(SIOAuthPayload authPayload)
        {
            Connect(targetAddress, this.enableAutoReconnect, authPayload);
        }

        /// <summary>
        /// Connect this Socket.IO instance to a new target (this even works after the initial connect)
        /// This method sends a previously used auth payload (if available)
        /// </summary>
        /// <param name="targetAddress">The server / IO address to connect to. Has to start with http:// or https:// (substitute ws with http or wss with https): http[s]://<Hostname>[:<Port>][/<path>]</param>
        /// <param name="enableReconnect">Shall we reconnect automatically on an unexpected connection loss?</param>
        public virtual void Connect(string targetAddress, bool enableReconnect)
        {
            Connect(targetAddress, this.enableAutoReconnect, this.authPayload);
        }

        /// <summary>
        /// Connect this Socket.IO instance to a new target (this even works after the initial connect)
        /// </summary>
        /// <param name="targetAddress">The server / IO address to connect to. Has to start with http:// or https:// (substitute ws with http or wss with https): http[s]://<Hostname>[:<Port>][/<path>]</param>
        /// <param name="enableReconnect">Shall we reconnect automatically on an unexpected connection loss?</param>
        /// <param name="authPayload">Null or an instance of SIOAuthPayload to be sent upon connection. Can for example be used to send an authentication token.</param>
        public virtual void Connect(string targetAddress, bool enableReconnect, SIOAuthPayload authPayload)
        {
            if (!targetAddress.StartsWith("http://") && !targetAddress.StartsWith("https://")) throw new UriFormatException("Socket.IO Address has to start with http:// or https:// if provided programmatically");

            this.targetAddress = targetAddress;
            this.enableAutoReconnect = enableReconnect;
            this.authPayload = authPayload;
        }

        public virtual void Close()
        {

        }

        public virtual void On(string EventName, SocketIOEvent Callback) {
            //Add callback internally
            if (!eventCallbacks.ContainsKey(EventName))
            {
                eventCallbacks.Add(EventName, new List<SocketIOEvent>());
            }
            eventCallbacks[EventName].Add(Callback);
        }

        public virtual void Off(string EventName, SocketIOEvent Callback)
        {
            if (eventCallbacks.ContainsKey(EventName)) {
                eventCallbacks[EventName].Remove(Callback);
            }
        }

        public virtual void Off(string EventName)
        {
            if (eventCallbacks.ContainsKey(EventName))
            {
                eventCallbacks.Remove(EventName);
            }
        }

        /// <summary>
        /// Called by the platform specific implementation
        /// </summary>
        /// <param name="EventName"></param>
        /// <param name="Data"></param>
        internal virtual void RaiseSIOEvent(string EventName, string Data)
        {
            if (eventCallbacks.ContainsKey(EventName))
            {
                foreach (SocketIOEvent cb in eventCallbacks[EventName])
                {
                    cb.Invoke(Data);
                }
            }
        }

        /// <summary>
        /// Emits a Socket.IO Event with payload
        /// </summary>
        /// <param name="EventName">The name of the event</param>
        /// <param name="Data">The payload (can for example be a serialized object)</param>
        /// <param name="DataIsPlainText">Use this parameter to explicitely state if the data is stringified JSON or a plain text string. Default: false = JSON object</param>
        public virtual void Emit(string EventName, string Data, bool DataIsPlainText)
        {

        }

        /// <summary>
        /// Emits a Socket.IO Event with payload
        /// If you are using JSON.NET, everything is fine. If not, consider using it (and set the HAS_JSON_NET flag) OR use the third parameter to specify the data type manually.
        /// </summary>
        /// <param name="EventName">The name of the event</param>
        /// <param name="Data">The payload (can for example be a serialized object)</param>
#if !HAS_JSON_NET
        [System.Obsolete("You are sending payload along an Emit without specifying the third parameter. -- This might cause unexpected results for complex objects or some plain text strings. Please consider using JSON.NET and set the HAS_JSON_NET flag or explicitely specify the third parameter to distinguish between plain text and JSON. Please referr to the documentation for more information abut this topic.")]
#endif
        public virtual void Emit(string EventName, string Data)
        {

        }

        /// <summary>
        /// Emits a Socket.IO Event without payload
        /// </summary>
        /// <param name="EventName">The name of the event</param>
        public virtual void Emit(string EventName)
        {

        }
    }
}
