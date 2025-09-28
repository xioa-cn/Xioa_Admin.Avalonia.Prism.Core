using System;

namespace Module.CreateAssistant;

public partial class Program
{
    static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        ResetColor();
    }
    
    static void WriteColorLine(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        ResetColor();
    }
    
    static void ResetColor()
    {
        Console.ForegroundColor = COLOR_DEFAULT;
    }


}