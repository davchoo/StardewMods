using HarmonyLib;
using OverflowingBin.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace OverflowingBin;
public class ModEntry : Mod
{
    public const string AssetBasePath = "Mods/Amberichu.OverflowingBin";

    private ShippingBinTextures? ShippingBinTextures;

    public override void Entry(IModHelper helper)
    {
        ShippingBinTextures = new ShippingBinTextures(helper);

        var harmony = new Harmony(ModManifest.UniqueID);
        ShippingBinPatches.Initialize(Monitor, harmony, ShippingBinTextures);
        IslandWestPatches.Initialize(Monitor, harmony, ShippingBinTextures);

        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        ShippingBinTextures!.ResetTextures(Helper);
    }
}
