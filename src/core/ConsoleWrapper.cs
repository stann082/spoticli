namespace core;

public static class ConsoleWrapper
{

    #region Public Methods

    public static void WriteLine(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    #endregion
    
}
