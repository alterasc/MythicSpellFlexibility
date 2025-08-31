using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using System.Reflection;
using UnityModManagerNet;

namespace MythicSpellFlexibility;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        log = modEntry.Logger;
#if DEBUG
        modEntry.OnUnload = OnUnload;
#endif
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }

#if DEBUG
    static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
#endif

    internal class SettingsStarter
    {
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        internal static class BlueprintsCache_Init_Patch
        {
            private static bool _initialized;

            [HarmonyPostfix]
            static void Postfix()
            {
                if (_initialized) return;
                _initialized = true;
                var mythicIgnoreAlignmentRestrictions = Utils.GetBlueprint<BlueprintFeature>("24e78475f0a243e1a810452d14d0a1bd");
                mythicIgnoreAlignmentRestrictions.AddComponent(new MythicSpellFlexibility());
                log.Log("Added Mythic Spell Flexibility");
            }
        }
    }

    public static void LogTrace(string message)
    {
#if DEBUG
        log.Log(message);
#endif
    }
}