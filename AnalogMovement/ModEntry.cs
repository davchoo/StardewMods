using AnalogMovement.Patches;
using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AnalogMovement;
public class ModEntry : Mod
{
    internal static ModConfig Config = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        var harmony = new Harmony(ModManifest.UniqueID);
        FarmerPatches.Initialize(harmony);
        ControllerInputPatch.Initialize(Monitor, harmony);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
        {
            return;
        }
        configMenu.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Deadzone",
            tooltip: () => "Vanilla Default: 20%",
            getValue: () => Config.Deadzone,
            setValue: value => Config.Deadzone = value,
            formatValue: value => value.ToString("P0"),
            min: 0.01f,
            max: 0.25f,
            interval: 0.01f
        );
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Square Controller Input",
            tooltip: () => "When enabled, your character will move at Vanilla speed when moving diagonally.\n"
                            + "When disabled, your character will move at a constant speed in all directions.",
            getValue: () => Config.SquareInput,
            setValue: value => Config.SquareInput = value
        );
    }
}
