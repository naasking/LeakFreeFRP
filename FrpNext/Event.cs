using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    /// <summary>
    /// An event that may happen at some future time.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    public struct Event<T0>
    {
        T0 now;
        Next<Event<T0>> wait;

        public Event(T0 now)
            : this()
        {
            this.now = now;
        }

        /// <summary>
        /// True if this event happened.
        /// </summary>
        public bool HasValue
        {
            get { return wait == null; }
        }

        /// <summary>
        /// The value computed by the event.
        /// </summary>
        public T0 Now
        {
            get
            {
                if (!HasValue) throw new ArgumentException("Event hasn't happened.");
                return now;
            }
        }

        /// <summary>
        /// The next scheduled event.
        /// </summary>
        public Next<Event<T0>> Wait
        {
            get
            {
                if (HasValue) throw new ArgumentException("Event has happened.");
                return Wait;
            }
        }

        /// <summary>
        /// Map an event to another event type.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Event<T1> Select<T1>(Func<T0, T1> selector)
        {
            return HasValue ? new Event<T1> { now = selector(now) }:
                              new Event<T1> { wait = wait.Select(x => x.Select(selector)) };
        }

        /// <summary>
        /// Generate a sequence of events.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Event<T1> SelectMany<T1>(Func<T0, Event<T1>> selector)
        {
            return HasValue ? selector(now):
                              new Event<T1> { wait = wait.Select(x => x.SelectMany(selector)) };
        }

        /// <summary>
        /// Generate a sequence of events.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="selector"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Event<T2> SelectMany<T1, T2>(Func<T0, Event<T1>> selector, Func<T0, T1, T2> result)
        {
            if (HasValue)
            {
                var now = this.now;
                var inner = selector(now);
                return HasValue ? new Event<T2> { now = result(now, inner.now) }:
                                  new Event<T2> { wait = inner.wait.Select(x => new Event<T2> { now = result(now, x.now) }) };
            }
            else
            {
                return new Event<T2> { wait = wait.Select(x => x.SelectMany(selector, result)) };
            }
        }

        /// <summary>
        /// Return the first event that resolved to a value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Event<T0> Join(Event<T0> other)
        {
            var x = this;
            return x.HasValue     ? x:
                   other.HasValue ? other:
                                    new Event<T0> { wait = Next.Delay(() => x.wait.Force().Join(other.wait.Force())) };
        }
    }
}
