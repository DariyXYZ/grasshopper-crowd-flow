using System;
using System.IO;

namespace GrasshopperComponents;

internal static class ComponentVisibility
{
    private static readonly object SyncRoot = new();
    private static bool? _isDeveloper;

    public static bool IsDeveloper
    {
        get
        {
            if (_isDeveloper.HasValue)
            {
                return _isDeveloper.Value;
            }

            lock (SyncRoot)
            {
                if (_isDeveloper.HasValue)
                {
                    return _isDeveloper.Value;
                }

                _isDeveloper = EvaluateDeveloperMode();
            }

            return _isDeveloper.Value;
        }
    }

    private static bool EvaluateDeveloperMode()
    {
        string? envValue = Environment.GetEnvironmentVariable("GHCROWDFLOW_DEV");
        if (!string.IsNullOrWhiteSpace(envValue) && IsTruthy(envValue))
        {
            return true;
        }

        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                string flagPath = Path.Combine(appData, "GhCrowdFlow", "dev.flag");
                if (File.Exists(flagPath))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore filesystem lookup failures and default to standard user visibility.
        }

        return false;
    }

    private static bool IsTruthy(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "yes" => true,
            "on" => true,
            _ => false,
        };
    }
}
