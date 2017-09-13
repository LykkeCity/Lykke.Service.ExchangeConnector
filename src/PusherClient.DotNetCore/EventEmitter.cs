using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PusherClient.DotNetCore
{
    public class EventEmitter
    {
        private readonly Dictionary<string, List<Action<dynamic>>> eventListeners = new Dictionary<string, List<Action<dynamic>>>();
        private readonly List<Action<string, dynamic>> generalListeners = new List<Action<string, dynamic>>();

        public void Bind(string eventName, Action<dynamic> listener)
        {
            if(eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName].Add(listener);
            }
            else
            {
                List<Action<dynamic>> listeners = new List<Action<dynamic>>();
                listeners.Add(listener);
                eventListeners.Add(eventName, listeners);
            }
        }

        public void BindAll(Action<string, dynamic> listener)
        {
            generalListeners.Add(listener);
        }

        public void Unbind(string eventName)
        {
            eventListeners.Remove(eventName);
        }

        public void Unbind(string eventName, Action<dynamic> listener)
        {
            if(eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName].Remove(listener);
            }
        }

        public void UnbindAll()
        {
            eventListeners.Clear();
            generalListeners.Clear();
        }

        internal void EmitEvent(string eventName, string data)
        {
            var obj = JsonConvert.DeserializeObject<dynamic>(data);

            // Emit to general listeners regardless of event type
            foreach (var action in generalListeners)
            {
                action(eventName, obj);
            }

            if (eventListeners.ContainsKey(eventName))
            {
                foreach (var action in eventListeners[eventName])
                {
                    action(obj);
                }
            }

        }
    }
}