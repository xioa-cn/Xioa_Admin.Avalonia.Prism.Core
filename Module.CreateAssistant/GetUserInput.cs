using System;

namespace Module.CreateAssistant;

public partial class Program
{
    static string GetUserInput(string prompt, ConsoleColor promptColor)
    {
        WriteColor(prompt, promptColor);
        string input = Console.ReadLine()?.Trim();
        ResetColor();
        return input;
    }

}