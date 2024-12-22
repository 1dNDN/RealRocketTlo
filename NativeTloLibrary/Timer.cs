using System.Diagnostics;

namespace NativeTloLibrary;

public class Speedometer
{
    private static readonly Stopwatch _stopwatch = new();

    /// <summary>
    ///     Сбрасывает и перезапускает таймер
    /// </summary>
    public static void Start()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    /// <summary>
    ///     Останавливает таймер и пишет в консоль буквы
    /// </summary>
    /// <param name="text">Буквы которые писать</param>
    public static void Stop(string text)
    {
        _stopwatch.Stop();
        Console.WriteLine($"{text} {_stopwatch.Elapsed}");
    }
}
