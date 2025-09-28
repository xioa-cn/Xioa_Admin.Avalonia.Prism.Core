using System;
using System.Threading;

namespace Module.CreateAssistant;

public partial class Program
{
    static void ShowProgressBar(string tip)
    {
        const int PercentTextLength = 4;
        Console.Write($" [{tip}] [");
        int totalSteps = 20;
        int delayMs = 70;

        for (int i = 0; i <= totalSteps; i++)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("▓");
            Console.ForegroundColor = COLOR_DEFAULT;

            int currentPercent = (i * 100) / totalSteps;
            string percentText = $"{currentPercent,3}%";

            if (i > 0)
            {
                Console.Write(new string('\b', PercentTextLength + 1));
            }

            Console.Write(percentText);
            Thread.Sleep(delayMs);
        }

        Console.Write("]");
    }
}