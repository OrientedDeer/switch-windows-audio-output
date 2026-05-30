using System;
using System.IO;

const string ConfigPath = @"C:\Program Files\EqualizerAPO\config\config.txt";
const string SwapLine = "Copy: L=R R=L";

if (!File.Exists(ConfigPath))
{
    Console.WriteLine("Equalizer APO not found. Install it first from https://sourceforge.net/projects/equalizerapo/");
    return 1;
}

try
{
    var lines = File.ReadAllLines(ConfigPath);
    var swapIndex = Array.FindIndex(lines, line => line.Trim() == SwapLine);

    if (swapIndex >= 0)
    {
        // Remove the swap line
        var updated = new string[lines.Length - 1];
        Array.Copy(lines, 0, updated, 0, swapIndex);
        Array.Copy(lines, swapIndex + 1, updated, swapIndex, lines.Length - swapIndex - 1);
        File.WriteAllLines(ConfigPath, updated);
        Console.WriteLine("Channels set to: Normal");
    }
    else
    {
        // Prepend the swap line
        var updated = new string[lines.Length + 1];
        updated[0] = SwapLine;
        Array.Copy(lines, 0, updated, 1, lines.Length);
        File.WriteAllLines(ConfigPath, updated);
        Console.WriteLine("Channels set to: Swapped (L\u2194R)");
    }

    return 0;
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Run this tool as Administrator.");
    return 1;
}
