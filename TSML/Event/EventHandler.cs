using System;
using UnityEngine;

namespace TSML.Event
{
    public delegate void EventListener(Event e);

    public class EventHandler : MonoBehaviour
    {
        public static event EventListener Listeners;

        public static void Register(EventListener listener)
        {
            Listeners += listener;
        }

        public static void OnEvent(Event e)
        {
            Listeners?.Invoke(e);
        }
    }
}
