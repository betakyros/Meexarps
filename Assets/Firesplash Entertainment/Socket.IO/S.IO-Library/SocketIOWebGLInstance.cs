using System;
using System.Collections.Generic;

#if HAS_JSON_NET
using Newtonsoft.Json;
#endif

namespace Firesplash.UnityAssets.SocketIO.Internal
{
#if UNITY_WEBGL
    internal class SocketIOWebGLInstance : SocketIOInstance
    {
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CreateSIOInstance(string instanceName, string gameObjectName, string targetAddress, int enableReconnect, string authPayload);
		
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void DestroySIOInstance(string instanceName);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CloseSIOInstance(string instanceName);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SIOEmitWithData(string instanceName, string eventName, string data, int parseAsJSON);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SIOEmitNoData(string instanceName, string eventName);

        public override string SocketID
        {
            get; internal set;
        }

        internal SocketIOWebGLInstance(string gameObjectName, string targetAddress, bool enableReconnect) : base(gameObjectName, targetAddress, enableReconnect)
        {
            SocketIOManager.LogDebug("Preparing WebGL-Based Socket.IO instance for " + gameObjectName);
            this.GameObjectName = gameObjectName;
            this.InstanceName = this.GameObjectName + "_" + System.Guid.NewGuid().ToString("N");
            //CreateSIOInstance(this.InstanceName, this.GameObjectName, targetAddress, enableReconnect ? 1 : 0);
        }
		
		~SocketIOWebGLInstance() {
            PrepareDestruction(); //This makes sure that we cleanly disconnect instead of forcefully dropping connection
			DestroySIOInstance(InstanceName);
		}

        public override void Connect(string targetAddress, bool enableReconnect, SIOAuthPayload authPayload)
        {
            base.Connect(targetAddress, enableReconnect, authPayload);

            string payload = "";
            if (this.authPayload != null) payload = this.authPayload.GetPayloadJSON();

            SocketIOManager.LogDebug("Creating and connecting WebGL-Based Socket.IO instance for " + this.GameObjectName);
            CreateSIOInstance(InstanceName, this.GameObjectName, targetAddress, enableReconnect ? 1 : 0, payload);
        }

        public override void Close()
        {
            CloseSIOInstance(InstanceName);
            base.Close();
        }

        public override void On(string EventName, SocketIOEvent Callback)
        {
            //Now register the callback to the base class's dictionary
            base.On(EventName, Callback);
        }

        public override void Off(string EventName)
        {
            //Now register the callback to the base class's dictionary
            base.Off(EventName);
        }

        public override void Emit(string EventName, string Data, bool DataIsPlainText)
        {
            SIOEmitWithData(InstanceName, EventName, Data, DataIsPlainText ? 0 : 1);
        }

#if !HAS_JSON_NET
        [Obsolete("Emitting data without specifying DataIsPlainText is no longer supported without Json.Net", true)]
#endif
        public override void Emit(string EventName, string Data)
        {
            bool handleJSONAsPlainText = false;
            try
            {
#if HAS_JSON_NET
                Newtonsoft.Json.Linq.JObject.Parse(Data);
#else
                //UnityEngine.JsonUtility.FromJson(Data, null);
#endif
            }
            catch (Exception)
            {
                //We re-use the bool. This happens if the "Data" object contains no valid json data
                handleJSONAsPlainText = true;
            }
            SIOEmitWithData(InstanceName, EventName, Data, handleJSONAsPlainText ? 0 : 1);
        }

        public override void Emit(string EventName)
        {
            SIOEmitNoData(InstanceName, EventName);
        }


        internal void UpdateSIOStatus(int statusCode)
        {
            if (statusCode < 0 || statusCode > 2) return;
            Status = (SIOStatus)statusCode;
        }


        internal void UpdateSIOSocketID(string currentSocketID)
        {
            SocketID = currentSocketID;
        }
    }
#endif
}
