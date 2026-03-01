using System;
using System.Collections.Generic;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// Lightweight event bus for decoupled module communication.
    /// Usage:
    ///   EventBus.Subscribe&lt;DamageEvent&gt;(OnDamage);
    ///   EventBus.Publish(new DamageEvent { ... });
    ///   EventBus.Unsubscribe&lt;DamageEvent&gt;(OnDamage);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        /// <summary>Subscribe a handler to an event type.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                _handlers[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        /// <summary>Unsubscribe a handler from an event type.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                    _handlers.Remove(type);
                else
                    _handlers[type] = result;
            }
        }

        /// <summary>Publish an event to all subscribers.</summary>
        public static void Publish<T>(T evt) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var handler))
            {
                ((Action<T>)handler).Invoke(evt);
            }
        }

        /// <summary>Remove all handlers. Call on scene teardown.</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
