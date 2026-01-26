using System.Collections.Generic;
using System;

namespace YYZ
{
    public interface IEvent{}

    public static class EventBus
    {
        static Dictionary<Type, List<Delegate>> handlerMap = new();

        public static void Subscribe<T>(Action<T> action) where T: IEvent
        {
            Type type = typeof(T);
            if(!handlerMap.TryGetValue(type, out var handlers))
            {
                handlers = handlerMap[type] = new();
            }
            if(!handlers.Contains(action))
            {
                handlers.Add(action);
            }
        }

        public static void Unsubscribe<T>(Action<T> action) where T: IEvent
        {
            Type type = typeof(T);
            if(!handlerMap.TryGetValue(type, out var handlers))
            {
                return;
            }
            if(handlers.Contains(action))
            {
                handlers.Remove(action);
            }
        }

        public static void Publish<T>(T eventData) where T: IEvent
        {
            Type type = typeof(T);
            if(!handlerMap.TryGetValue(type, out var handlers))
            {
                return;
            }
            // TODO: shallow copy handlers to prevent exception the handlers is changed when processing handler?
            foreach(var handler in handlers)
            {
                if(handler is Action<T> action)
                {
                    action(eventData);
                }
            }
        }
    }
}

