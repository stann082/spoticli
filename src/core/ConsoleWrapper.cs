namespace core;

public static class ConsoleWrapper
{

    private static readonly object lockObject = new object();
    
    #region Public Methods

    public static void WriteLine(string message, ConsoleColor color)
    {
        lock (lockObject)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    #endregion
    
}
