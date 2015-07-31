using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    public struct Event<T0>
    {
        T0 now;
        Next<Event<T0>> wait;

        public Event(T0 now)
            : this()
        {
            this.now = now;
        }

        public bool HasValue
        {
            get { return wait == null; }
        }
        public T0 Now
        {
            get
            {
                if (!HasValue) throw new ArgumentException("Event hasn't happened.");
                return now;
            }
        }
        public Next<Event<T0>> Wait
        {
            get
            {
                if (HasValue) throw new ArgumentException("Event has happened.");
                return Wait;
            }
        }

        public Event<T1> Select<T1>(Func<T0, T1> selector)
        {
            return HasValue ? new Event<T1> { now = selector(now) }:
                              new Event<T1> { wait = wait.Select(x => x.Select(selector)) };
        }

        public Event<T1> SelectMany<T1>(Func<T0, Event<T1>> selector)
        {
            return HasValue ? selector(now):
                              new Event<T1> { wait = wait.Select(x => x.SelectMany(selector)) };
        }

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

        public Event<T0> Join(Event<T0> other)
        {
            var x = this;
            return x.HasValue     ? x:
                   other.HasValue ? other:
                                    new Event<T0> { wait = Next.Delay(() => x.wait.Force().Join(other.wait.Force())) };
        }
    }
}
