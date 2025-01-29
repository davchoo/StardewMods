using HarmonyLib;
using StardewModdingAPI;

namespace AnalogMovement;
public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var harmony = new Harmony(ModManifest.UniqueID);
        FarmerMovementPatches.Initialize(Monitor, harmony);
    }
}
