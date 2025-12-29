using System;

namespace GestionTime.Desktop.Diagnostics;

public static class LogHub
{
    public static event Action<string>? Line;

    public static void Publish(string message)
        => Line?.Invoke(message);
}

