using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using Microsoft.Win32;

const string ApoClsid = "{1B5C2483-B741-4C18-9B0E-8B07FF3CA0F2}";
const string DllDir = @"C:\ProgramData\SwapAudio";
const string DllPath = DllDir + @"\SwapAPO.dll";
const string ApoRegPath = @"SOFTWARE\Classes\AudioEngine\AudioProcessingObjects\" + ApoClsid;
const string ComRegPath = @"SOFTWARE\Classes\CLSID\" + ApoClsid + @"\InprocServer32";
const string StateRegPath = @"SOFTWARE\SwapAudio";
const string SfxClsidKey = "{d04e05a6-594b-4fb6-a80d-01af5eed7d1d},5";

const RegistryRights ValueAccess =
    RegistryRights.QueryValues | RegistryRights.SetValue | RegistryRights.EnumerateSubKeys;

int exitCode;
try
{
    if (args.Length > 0 && args[0].Equals("uninstall", StringComparison.OrdinalIgnoreCase))
        exitCode = Uninstall();
    else
        exitCode = InstallAndToggle();
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Run this tool as Administrator.");
    exitCode = 1;
}
catch (System.Security.SecurityException)
{
    Console.WriteLine("Run this tool as Administrator.");
    exitCode = 1;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    exitCode = 1;
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
return exitCode;

// ---- Core logic ----

int InstallAndToggle()
{
    EnsureInstalled();

    var endpoints = GetAllRenderEndpoints();
    if (endpoints.Count == 0)
    {
        Console.WriteLine("No audio endpoints found.");
        return 1;
    }

    bool currentlySwapped = false;
    foreach (var ep in endpoints)
    {
        using var fxKey = Registry.LocalMachine.OpenSubKey(
            ep + @"\FxProperties", RegistryKeyPermissionCheck.ReadSubTree, ValueAccess);
        if (fxKey == null) continue;

        var sfx = fxKey.GetValue(SfxClsidKey) as string;
        if (sfx != null)
        {
            currentlySwapped = string.Equals(sfx, ApoClsid, StringComparison.OrdinalIgnoreCase);
            break;
        }
    }

    int modified = 0;
    foreach (var ep in endpoints)
    {
        string fxPropsPath = ep + @"\FxProperties";
        using var fxKey = Registry.LocalMachine.OpenSubKey(
            fxPropsPath, RegistryKeyPermissionCheck.ReadWriteSubTree, ValueAccess);
        if (fxKey == null) continue;

        string deviceId = ep.Split('\\')[^1];
        var sfx = fxKey.GetValue(SfxClsidKey) as string;
        if (sfx == null) continue;

        if (currentlySwapped)
        {
            var orig = GetSavedOriginal(deviceId);
            if (orig != null && orig.Length > 0)
                fxKey.SetValue(SfxClsidKey, orig[0]);
            Console.WriteLine($"  {deviceId}: restored");
            modified++;
        }
        else
        {
            SaveOriginal(deviceId, [sfx]);
            fxKey.SetValue(SfxClsidKey, ApoClsid);
            Console.WriteLine($"  {deviceId}: swapped");
            modified++;
        }
    }

    if (modified == 0)
    {
        Console.WriteLine("No endpoints found.");
        return 1;
    }

    RestartAudioService();
    Console.WriteLine(currentlySwapped
        ? "Channels set to: Normal"
        : "Channels set to: Swapped (L↔R)");
    Console.WriteLine($"({modified} endpoint(s) updated)");
    return 0;
}

int Uninstall()
{
    foreach (var ep in GetAllRenderEndpoints())
    {
        string fxPropsPath = ep + @"\FxProperties";
        string deviceId = ep.Split('\\')[^1];
        using var fxKey = Registry.LocalMachine.OpenSubKey(
            fxPropsPath, RegistryKeyPermissionCheck.ReadWriteSubTree, ValueAccess);
        if (fxKey == null) continue;

        var legacy = fxKey.GetValue(SfxClsidKey) as string;
        if (string.Equals(legacy, ApoClsid, StringComparison.OrdinalIgnoreCase))
        {
            var orig = GetSavedOriginal(deviceId);
            if (orig != null && orig.Length > 0)
                fxKey.SetValue(SfxClsidKey, orig[0]);
        }
    }

    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Classes\CLSID\" + ApoClsid, throwOnMissingSubKey: false);
    Registry.LocalMachine.DeleteSubKeyTree(ApoRegPath, throwOnMissingSubKey: false);
    Registry.LocalMachine.DeleteSubKeyTree(StateRegPath, throwOnMissingSubKey: false);

    if (File.Exists(DllPath))
        File.Delete(DllPath);
    if (Directory.Exists(DllDir))
    {
        foreach (var f in Directory.GetFiles(DllDir))
            try { File.Delete(f); } catch { }
        try { Directory.Delete(DllDir); } catch { }
    }

    RestartAudioService();
    Console.WriteLine("SwapAudio uninstalled.");
    return 0;
}

// ---- Installation ----

void EnsureInstalled()
{
    Directory.CreateDirectory(DllDir);
    try
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SwapAPO.dll")
            ?? throw new Exception("Embedded SwapAPO.dll not found in exe resources.");
        using var fs = File.Create(DllPath);
        stream.CopyTo(fs);
    }
    catch (IOException) when (File.Exists(DllPath))
    {
    }

    using (var key = Registry.LocalMachine.OpenSubKey(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
        RegistryKeyPermissionCheck.ReadWriteSubTree, ValueAccess)
        ?? Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio"))
    {
        key.SetValue("DisableProtectedAudioDG", 1, RegistryValueKind.DWord);
    }

    using (var key = Registry.LocalMachine.CreateSubKey(ComRegPath))
    {
        key.SetValue(null, DllPath);
        key.SetValue("ThreadingModel", "Both");
    }

    using (var key = Registry.LocalMachine.CreateSubKey(ApoRegPath))
    {
        key.SetValue("FriendlyName", "SwapAPO");
        key.SetValue("Copyright", "");
        key.SetValue("MajorVersion", 1, RegistryValueKind.DWord);
        key.SetValue("MinorVersion", 0, RegistryValueKind.DWord);
        key.SetValue("Flags", 0x0000000F, RegistryValueKind.DWord);
        key.SetValue("MinInputConnections", 1, RegistryValueKind.DWord);
        key.SetValue("MaxInputConnections", 1, RegistryValueKind.DWord);
        key.SetValue("MinOutputConnections", 1, RegistryValueKind.DWord);
        key.SetValue("MaxOutputConnections", 1, RegistryValueKind.DWord);
        key.SetValue("MaxInstances", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
        key.SetValue("NumAPOInterfaces", 1, RegistryValueKind.DWord);
        key.SetValue("APOInterface0", "{fd7f2b29-24d0-4b5c-b177-592c39f9ca10}");
    }
}

// ---- Endpoint discovery ----

List<string> GetAllRenderEndpoints()
{
    var results = new List<string>();
    const string renderDevices = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
    using var renderKey = Registry.LocalMachine.OpenSubKey(renderDevices);
    if (renderKey == null) return results;

    foreach (var deviceId in renderKey.GetSubKeyNames())
    {
        string devicePath = renderDevices + @"\" + deviceId;
        using var deviceKey = Registry.LocalMachine.OpenSubKey(devicePath);
        if (deviceKey == null) continue;

        var state = deviceKey.GetValue("DeviceState");
        if (state is int stateInt && stateInt != 1) continue;

        using var fxKey = Registry.LocalMachine.OpenSubKey(devicePath + @"\FxProperties");
        if (fxKey != null)
            results.Add(devicePath);
    }

    return results;
}

// ---- State persistence (per-device) ----

void SaveOriginal(string deviceId, string[] clsids)
{
    using var key = Registry.LocalMachine.CreateSubKey(StateRegPath + @"\" + deviceId);
    key.SetValue("OriginalAPOs", clsids, RegistryValueKind.MultiString);
}

string[]? GetSavedOriginal(string deviceId)
{
    using var key = Registry.LocalMachine.OpenSubKey(StateRegPath + @"\" + deviceId);
    return key?.GetValue("OriginalAPOs") as string[];
}

// ---- Audio service restart ----

void RestartAudioService()
{
    Console.WriteLine("Restarting audio service...");
    try
    {
        var psi = new ProcessStartInfo("net", "stop Audiosrv /y")
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        var p = Process.Start(psi)!;
        p.WaitForExit(10000);

        psi = new ProcessStartInfo("net", "start Audiosrv")
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        p = Process.Start(psi)!;
        p.WaitForExit(10000);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not restart audio service: {ex.Message}");
        Console.WriteLine("A reboot may be needed for changes to take effect.");
    }
}
