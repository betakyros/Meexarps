using System.Collections.Generic;
using System.Text;

namespace Firesplash.UnityAssets.SocketIO
{
    /// <summary>
    /// Creates an object to be sent while connecting to the server.
    /// This can be used to authenticate against the server.
    /// </summary>
    public class SIOAuthPayload
    {
        Dictionary<string, object> payloadData = new Dictionary<string, object>();

        /// <summary>
        /// Adds an integer typed value to the payload.
        /// </summary>
        /// <param name="key">The name of this object (on the server side this will go socket.handshake.auth.<b>__HERE__</b></param>
        /// <param name="value">The value of this object</param>
        public void AddElement(string key, int value)
        {
            AddElementToList(key, value);
        }

        /// <summary>
        /// Adds a float typed value to the payload.
        /// </summary>
        /// <param name="key">The name of this object (on the server side this will go socket.handshake.auth.<b>__HERE__</b></param>
        /// <param name="value">The value of this object</param>
        public void AddElement(string key, float value)
        {
            AddElementToList(key, value);
        }

        /// <summary>
        /// Adds a double typed value to the payload.
        /// </summary>
        /// <param name="key">The name of this object (on the server side this will go socket.handshake.auth.<b>__HERE__</b></param>
        /// <param name="value">The value of this object</param>
        public void AddElement(string key, double value)
        {
            AddElementToList(key, value);
        }

        /// <summary>
        /// Adds a string typed value to the payload.
        /// </summary>
        /// <param name="key">The name of this object (on the server side this will go socket.handshake.auth.<b>__HERE__</b></param>
        /// <param name="value">The value of this object</param>
        public void AddElement(string key, string value)
        {
            AddElementToList(key, value);
        }

        /// <summary>
        /// Adds a boolean typed value to the payload.
        /// </summary>
        /// <param name="key">The name of this object (on the server side this will go socket.handshake.auth.<b>__HERE__</b></param>
        /// <param name="value">The value of this object</param>
        public void AddElement(string key, bool value)
        {
            AddElementToList(key, value);
        }

        private void AddElementToList(string key, object value)
        {
            if (payloadData.ContainsKey(key)) payloadData.Remove(key);
            payloadData.Add(key, value);
        }


        /// <summary>
        /// Removes a previously added element from the payload.
        /// </summary>
        /// <param name="key">The name of the object to be removed</param>
        /// <returns>True if the object existed and has been removed, false otherwise</param>
        public bool RemoveElement(string key)
        {
            if (payloadData.ContainsKey(key))
            {
                return payloadData.Remove(key);
            }
            return false;
        }

        /// <summary>
        /// Clears out all previously set payload data from this object
        /// </summary>
        public void Clear()
        {
            payloadData.Clear();
        }

        internal string GetPayloadJSON()
        {
            if (payloadData.Count == 0) return "null";

            StringBuilder json = new StringBuilder();
            json.Append("{");
            bool isFirst = true;
            foreach (KeyValuePair<string, object> element in payloadData)
            {
                if (!isFirst) json.Append(",");

                json.Append("\"" + element.Key + "\":");
                if (element.Value.GetType().Equals(typeof(int)) || element.Value.GetType().Equals(typeof(float)) || element.Value.GetType().Equals(typeof(double)))
                {
                    json.Append(element.Value);
                } 
                else if (element.Value.GetType().Equals(typeof(string)))
                {
                    json.Append("\"" + ((string)element.Value).Replace("\\", "\\\\").Replace("\b", "\\b").Replace("\n", "\\n").Replace("\f", "\\f").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\"", "\\\"") + "\"");
                } 
                else if (element.Value.GetType().Equals(typeof(bool)))
                {
                    json.Append(((bool)element.Value ? "true" : "false"));
                } 
                else
                {
                    json.Append("null");
                }
                isFirst = false;
            }
            json.Append("}");

            return json.ToString();
        }
    }
}
