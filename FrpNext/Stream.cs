using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    /// <summary>
    /// A stream of values.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    public struct Stream<T0>
    {
        /// <summary>
        /// The stream's current value.
        /// </summary>
        public T0 Value { get; internal set; }

        /// <summary>
        /// The stream's next value.
        /// </summary>
        public Next<Stream<T0>> Next { get; internal set; }

        /// <summary>
        /// Map the stream values to another stream of values.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Stream<T1> Select<T1>(Func<T0, T1> selector)
        {
            return new Stream<T1>
            {
                Value = selector(Value),
                Next = Next.Select(x => x.Select(selector)),
            };
        }

        /// <summary>
        /// Combine with another stream.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="other"></param>
        /// <returns></returns>
        public Stream<Tuple<T0, T1>> Zip<T1>(Stream<T1> other)
        {
            return new Stream<Tuple<T0, T1>>
            {
                Value = Tuple.Create(Value, other.Value),
                Next = Next.Zip(other.Next).Select(x => x.Item1.Zip(x.Item2)),
            };
        }
    }

    /// <summary>
    /// Extensions on the core stream type.
    /// </summary>
    public static class Streams
    {
        /// <summary>
        /// Generate a stream of values.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="step"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        public static Stream<T1> Generate<T0, T1>(Func<T0, Tuple<T1, Next<T0>>> step, T0 arg0)
        {
            var tmp = step(arg0);
            return new Stream<T1>
            {
                Value = tmp.Item1,
                Next = tmp.Item2.Select(x => Generate(step, x)),
            };
        }

        /// <summary>
        /// Unpack a combined stream.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="react"></param>
        /// <returns></returns>
        public static Tuple<Stream<T0>, Stream<T1>> Unzip<T0, T1>(this Stream<Tuple<T0, T1>> react)
        {
            var next = react.Next.Select(x => x.Unzip());
            return Tuple.Create(
                new Stream<T0> { Value = react.Value.Item1, Next = next.Select(x => x.Item1), },
                new Stream<T1> { Value = react.Value.Item2, Next = next.Select(x => x.Item2), });
        }
    }
}
