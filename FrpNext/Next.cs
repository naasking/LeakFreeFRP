using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    public sealed class TimingException : Exception
    {
        public TimingException(int scheduledTick, int actualTick)
            : base("Value was evaluated on the wrong timing.")
        {
        }
        public int ScheduledTick { get; private set; }
        public int ActualTick { get; private set; }
    }

    public abstract class Next : IDisposable
    {
        internal int time;
        public abstract void Dispose();
    }

    public class Next<T> : Next
    {
        internal T value;
        internal Func<T> code;

        static Func<T> error = () => { throw new Exception("Invalidly forced thunk!"); };

        public Next(Func<T> eval)
        {
            this.code = eval;
            this.time = 1 + Runtime.clock;
            Runtime.thunks.Add(this);
        }

        public Next<T1> Select<T1>(Func<T, T1> selector)
        {
            return Runtime.Delay(() => selector(this.Force()));
        }

        public Next<Tuple<T, T1>> Zip<T1>(Next<T1> other)
        {
            return Runtime.Delay(() => Tuple.Create(this.Force(), other.Force()));
        }

        public override void Dispose()
        {
            value = default(T);
            code = error;
        }
    }

    public static class Runtime
    {
        internal static int clock;
        internal static List<Next> thunks = new List<Next>();

        public static void Unzip<T0, T1>(this Next<Tuple<T0, T1>> next, out Next<T0> next0, out Next<T1> next1)
        {
            next0 = next.Select(x => x.Item1);
            next1 = next.Select(x => x.Item2);
        }
        public static Tuple<Next<T0>, Next<T1>> Unzip<T0, T1>(this Next<Tuple<T0, T1>> next)
        {
            return Tuple.Create(next.Select(x => x.Item1), next.Select(x => x.Item2));
        }
        public static T Fix<T>(Func<Next<T>, T> extract)
        {
            return extract(Delay(() => Fix(extract)));
        }
        public static Next<T1> Apply<T0, T1>(this Next<Func<T0, T1>> func, Next<T0> arg0)
        {
            return Delay(() => func.Force()(arg0.Force()));
        }
        public static Next<T> Delay<T>(Func<T> create)
        {
            return new Next<T>(create);
        }

        // runtime
        public static void Tick()
        {
            ++clock;

            // cleanup thunks
            var newthunks = new List<Next>(thunks.Count);
            foreach (var x in thunks)
            {
                if (x.time >= clock)
                    newthunks.Add(x);
                else
                    x.Dispose();
            }
            thunks = newthunks;
        }

        public static T Force<T>(this Next<T> next)
        {
            if (next.time != clock)
                throw new TimingException(next.time, clock);
            // force the delayed evaluation:
            if (next.code != null)
            {
                next.value = next.code();
                next.code = null;
            }
            return next.value;
        }
    }
}
