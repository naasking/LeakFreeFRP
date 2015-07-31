using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrpNext
{
    public struct Stream<T0>
    {
        public T0 Value { get; internal set; }
        public Next<Stream<T0>> Next { get; internal set; }

        public Stream<T1> Select<T1>(Func<T0, T1> selector)
        {
            return new Stream<T1>
            {
                Value = selector(Value),
                Next = Next.Select(x => x.Select(selector)),
            };
        }

        public Stream<Tuple<T0, T1>> Zip<T1>(Stream<T1> other)
        {
            return new Stream<Tuple<T0, T1>>
            {
                Value = Tuple.Create(Value, other.Value),
                Next = Next.Zip(other.Next).Select(x => x.Item1.Zip(x.Item2)),
            };
        }
    }
    public static class Streams
    {
        public static Stream<T1> Unfold<T0, T1>(Func<T0, Tuple<T1, Next<T0>>> thunk, T0 arg0)
        {
            var tmp = thunk(arg0);
            return new Stream<T1>
            {
                Value = tmp.Item1,
                Next = tmp.Item2.Select(x => Unfold(thunk, x)),
            };
        }

        public static Tuple<Stream<T0>, Stream<T1>> Unzip<T0, T1>(this Stream<Tuple<T0, T1>> react)
        {
            var next = react.Next.Select(x => x.Unzip());
            return Tuple.Create(
                new Stream<T0> { Value = react.Value.Item1, Next = next.Select(x => x.Item1), },
                new Stream<T1> { Value = react.Value.Item2, Next = next.Select(x => x.Item2), });
        }
    }
}
