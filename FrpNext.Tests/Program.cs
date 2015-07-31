using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace FrpNext.Tests
{
    class Program
    {
        static Stream<int> Ints(int n)
        {
            return Streams.Generate(i => Tuple.Create(i, Next.Delay(() => i + 1)), n);
        }
        static void Run(int k, Stream<int> xs)
        {
            while (k-- > 0)
            {
                Debug.Assert(15 + 11 - k - 1 == xs.Value);
                Console.WriteLine(xs.Value);
                Runtime.Tick();
                xs = xs.Next.Force();
            }
        }
        static void Main(string[] args)
        {
            var stream = Ints(15);
            Run(11, stream);
            Console.ReadLine();
        }
    }
}
