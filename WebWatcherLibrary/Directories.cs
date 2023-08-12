using System;
using System.IO;

namespace DiegoG.WebWatcher;

public static class Directories
{
    public static readonly string EnvDataDir = Environment.GetEnvironmentVariable("DataDirectory") ?? Environment.CurrentDirectory;
    public static readonly string Configuration = Path.Combine(EnvDataDir, ".config");
    public static readonly string Data = Path.Combine(EnvDataDir, ".data");
    public static readonly string Logs = Path.Combine(EnvDataDir, ".logs");
    public static readonly string Extensions = Path.Combine(EnvDataDir, "extensions");

    public static string InConfiguration(params string[] path)
        => Path.Combine(Configuration, Path.Combine(path));

    public static string InData(params string[] path)
        => Path.Combine(Data, Path.Combine(path));

    public static string InLogs(params string[] path)
        => Path.Combine(Logs, Path.Combine(path));

    public static string InExtenstions(params string[] path)
        => Path.Combine(Extensions, Path.Combine(path));
}
