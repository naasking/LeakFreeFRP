using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    /// <summary>
    /// An exception describing an invalid forcing.
    /// </summary>
    public sealed class TimingException : Exception
    {
        public TimingException(int scheduledTick, int actualTick)
            : base("Value was evaluated on the wrong timing.")
        {
        }
        /// <summary>
        /// The clock tick at which the value was supposed to be forced.
        /// </summary>
        public int ScheduledTick { get; private set; }

        /// <summary>
        /// The actual clock tick the value was forced.
        /// </summary>
        public int ActualTick { get; private set; }
    }

    /// <summary>
    /// The base class for values that are lazily evaluated on a timing schedule.
    /// </summary>
    public abstract class Next : IDisposable
    {
        internal int time;
        public abstract void Dispose();

        /// <summary>
        /// Construct a scheduled value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="create"></param>
        /// <returns></returns>
        public static Next<T> Delay<T>(Func<T> create)
        {
            return new Next<T>(create);
        }
    }

    /// <summary>
    /// A schedule-evaluated value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Next<T> : Next
    {
        internal T value;
        internal Func<T> code;

        static Func<T> error = () => { throw new Exception("Invalidly forced thunk!"); };

        /// <summary>
        /// Construct a value computed at a later time.
        /// </summary>
        /// <param name="eval"></param>
        public Next(Func<T> eval)
        {
            this.code = eval;
            this.time = 1 + Runtime.clock;
            Runtime.thunks.Add(this);
        }

        /// <summary>
        /// Map a scheduled value to another type.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Next<T1> Select<T1>(Func<T, T1> selector)
        {
            return Delay(() => selector(this.Force()));
        }

        /// <summary>
        /// Combine two scheduled values.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="other"></param>
        /// <returns></returns>
        public Next<Tuple<T, T1>> Zip<T1>(Next<T1> other)
        {
            return Delay(() => Tuple.Create(this.Force(), other.Force()));
        }

        /// <summary>
        /// Dispose of a scheduled value. Any subsequent access will throw an error.
        /// </summary>
        public override void Dispose()
        {
            value = default(T);
            code = error;
        }
    }

    /// <summary>
    /// The class used to schedule and evaluate values.
    /// </summary>
    public static class Runtime
    {
        internal static int clock;
        internal static List<Next> thunks = new List<Next>();

        /// <summary>
        /// Unpack a zipped value.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public static Tuple<Next<T0>, Next<T1>> Unzip<T0, T1>(this Next<Tuple<T0, T1>> next)
        {
            return Tuple.Create(next.Select(x => x.Item1), next.Select(x => x.Item2));
        }

        /// <summary>
        /// Apply a scheduled argument to a scheduled function.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        public static Next<T1> Apply<T0, T1>(this Next<Func<T0, T1>> func, Next<T0> arg0)
        {
            return Next.Delay(() => func.Force()(arg0.Force()));
        }

        /// <summary>
        /// Advance the schedule by one clock tick.
        /// </summary>
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

        /// <summary>
        /// Force a scheduled value to resolve to a concrete value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
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
