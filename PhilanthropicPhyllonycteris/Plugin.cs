using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace PhilanthropicPhyllonycteris;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "nianeyna.PhilanthropicPhyllonycteris";
    public const string PluginName = "PhilanthropicPhyllonycteris";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
