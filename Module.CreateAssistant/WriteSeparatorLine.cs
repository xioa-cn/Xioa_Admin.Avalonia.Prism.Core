using System;

namespace Module.CreateAssistant;

public partial class Program
{
    static void WriteSeparatorLine(ConsoleColor color = ConsoleColor.Magenta)
    {
        string separator = new string('=', SEPARATOR_LENGTH);
        WriteColorLine(Environment.NewLine + separator, color);
    }
}