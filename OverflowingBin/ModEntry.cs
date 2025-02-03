using HarmonyLib;
using OverflowingBin.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;

namespace OverflowingBin;
public class ModEntry : Mod
{
    public const string AssetBasePath = "Mods/Amberichu.OverflowingBin";

    private ShippingBinTextures? ShippingBinTextures;

    private List<Item> AllItems = new List<Item>();

    public override void Entry(IModHelper helper)
    {
        ShippingBinTextures = new ShippingBinTextures(helper);

        var harmony = new Harmony(ModManifest.UniqueID);
        ShippingBinPatches.Initialize(Monitor, harmony, ShippingBinTextures);

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        // TODO debug remove later
        // helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        // helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.K) {
            var bin = Game1.getFarm().getShippingBin(Game1.player);
            bin.Clear();
            for (int i = 0; i < Game1.random.Next(5, 30); i++) {
                var item = AllItems[Game1.random.Next(0, AllItems.Count)].getOne();
                item.Stack = Game1.random.Next(0, item.maximumStackSize());
                bin.Add(item);
            }
            Monitor.Log($"{string.Join(',', bin.Select(i => i.DisplayName))}", LogLevel.Debug);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        bool a = true;
        foreach (var result in ItemQueryResolver.TryResolve("ALL_ITEMS @requirePrice", null))
        {
            if (result.Item is Item item)
            {
                if (item.canBeShipped())
                {
                    var itemData = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
                    var rect = itemData.GetSourceRect(0, null);
                    Monitor.Log($"{item.Name} {item.sellToStorePrice()} {rect}", LogLevel.Debug);
                    AllItems.Add(item);

                    if (a && item.Category == StardewValley.Object.VegetableCategory) {
                        a = false;
                        var def = ItemRegistry.GetObjectTypeDefinition();
                        foreach (var preserveType in Enum.GetValues<StardewValley.Object.PreserveType>()) {
                            var flavoredItem = def.CreateFlavoredItem(preserveType, item as StardewValley.Object);
                            var iD = ItemRegistry.GetDataOrErrorItem(flavoredItem.QualifiedItemId);
                            var fR = iD.GetSourceRect(0, null);
                            Monitor.Log($"{flavoredItem.Name} {flavoredItem.sellToStorePrice()} {fR}", LogLevel.Debug);
                            AllItems.Add(flavoredItem);
                        }
                    }
                }
            }
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        ShippingBinTextures!.ResetTextures(Helper);
    }
}
