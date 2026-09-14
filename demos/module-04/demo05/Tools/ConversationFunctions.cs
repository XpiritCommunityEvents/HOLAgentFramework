using System.ComponentModel;

namespace Module04.Demo05.Tools;

public static class ConversationFunctions
{
    [Description("Asks the user a question and returns the user's answer. Use this instead of asking a question in text.")]
    public static string AskUser(
        [Description("The question to ask the user.")] string question)
    {
        Console.Write($"\n{question}\n> ");
        return Console.ReadLine() ?? string.Empty;
    }
}
