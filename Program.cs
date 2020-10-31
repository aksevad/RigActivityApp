using System;

namespace RigActivityApp
{
    class Program
    {
        static void Main(string[] args)
        {
            float Depth;
            DateTime Time;
            float HL, SPP, BD, RPM, Block;
            String k;
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("Enter Data:");
                Console.Write("Depth: ");
                Depth = float.Parse(Console.ReadLine());    
                Console.WriteLine("Time: [now]");
                Time = DateTime.Now;
                Console.Write("BitDepth: ");
                BD = float.Parse(Console.ReadLine());
                Console.Write("HookLoad: ");
                HL = float.Parse(Console.ReadLine());
                Console.Write("SPP: ");
                SPP = float.Parse(Console.ReadLine());
                Console.Write("RPM: ");
                RPM = float.Parse(Console.ReadLine());
                Console.Write("Block: ");
                Block = float.Parse(Console.ReadLine());
                RigActivity RA = new RigActivity("well1", Depth, Time, HL, SPP, BD, RPM, Block);
                Console.WriteLine("Current Activity is: " + RA.Activity);

                Console.WriteLine("[E]xit?");
                k = Console.ReadLine();
                if (k == "E" || k == "e") { i = 101; }
            }
        }
    }
}
