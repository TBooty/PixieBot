using System;

namespace PixieBot
{
    class Program
    {
        public static void Main(string[] args)
            => Bootstrap.RunAsync(args).GetAwaiter().GetResult();
    }
}
