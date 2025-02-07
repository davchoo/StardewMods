using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace OverflowingBin;
internal class ShippingBinTextures
{
    private readonly Dictionary<string, string> Assets;

    public Lazy<Texture2D>? ShippingBinFront { get; private set; }

    public Lazy<Texture2D>? IslandWestBinFront { get; private set; }

    public ShippingBinTextures(IModHelper helper)
    {
        Assets = DefineAssets();
        helper.Events.Content.AssetRequested += OnAssetRequested;

        ResetTextures(helper);
    }

    public void ResetTextures(IModHelper helper)
    {
        ShippingBinFront = new Lazy<Texture2D>(
            () => ApplyMask(
                "Buildings/Shipping Bin",
                $"{ModEntry.AssetBasePath}/ShippingBinMask",
                helper
            )
        );

        IslandWestBinFront = new Lazy<Texture2D>(
            () =>
            {
                var IslandWestTileSheet = helper.GameContent.Load<Texture2D>(
                    "Maps/island_tilesheet_1"
                );
                var IslandWestBase = ExtractRectangle(
                    IslandWestTileSheet,
                    new Rectangle(192, 720, 32, 32)
                );
                var IslandWestBinMask = helper.GameContent.Load<Texture2D>(
                    $"{ModEntry.AssetBasePath}/IslandWestBinMask"
                );
                var result = ApplyMask(IslandWestBase, IslandWestBinMask);
                IslandWestBase.Dispose();
                return result;
            }
        );
    }

    private static Texture2D ApplyMask(string baseTexturePath, string maskTexturePath, IModHelper helper)
    {
        return ApplyMask(
            helper.GameContent.Load<Texture2D>(baseTexturePath),
            helper.GameContent.Load<Texture2D>(maskTexturePath)
        );
    }

    private static Texture2D ApplyMask(Texture2D baseTexture, Texture2D maskTexture)
    {
        if (baseTexture.Width != maskTexture.Width)
        {
            throw new ArgumentException(
                "Mask texture width does not match base texture width."
                + $"Expected: {baseTexture.Width} Actual: {maskTexture.Width}"
            );
        }
        if (baseTexture.Height != maskTexture.Height)
        {
            throw new ArgumentException(
                "Mask texture height does not match base texture height."
                + $"Expected: {baseTexture.Height} Actual: {maskTexture.Height}"
            );
        }

        Color[] colors = new Color[baseTexture.Width * baseTexture.Height];
        baseTexture.GetData(colors);

        Color[] maskColors = new Color[maskTexture.Width * maskTexture.Height];
        maskTexture.GetData(maskColors);

        for (int i = 0; i < Math.Min(colors.Length, maskColors.Length); i++)
        {
            if (maskColors[i] != Color.Lime) // Lime is rgb(0, 255, 0)
            {
                colors[i] = Color.Transparent;
            }
        }

        Texture2D result = new(Game1.graphics.GraphicsDevice, baseTexture.Width, baseTexture.Height);
        result.SetData(colors);
        return result;
    }

    private static Texture2D ExtractRectangle(Texture2D baseTexture, Rectangle rectangle)
    {
        Color[] colors = new Color[rectangle.Width * rectangle.Height];
        baseTexture.GetData(0, 0, rectangle, colors, 0, colors.Length);

        Texture2D result = new(Game1.graphics.GraphicsDevice, rectangle.Width, rectangle.Height);
        result.SetData(colors);
        return result;
    }

    private static Dictionary<string, string> DefineAssets()
    {
        return new Dictionary<string, string>
        {
            { $"{ModEntry.AssetBasePath}/ShippingBinMask", "assets/ShippingBinMask.png" },
            { $"{ModEntry.AssetBasePath}/IslandWestBinMask", "assets/IslandWestBinMask.png" },
        };
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        foreach (var pair in Assets)
        {
            if (e.Name.IsEquivalentTo(pair.Key))
            {
                e.LoadFromModFile<Texture2D>(pair.Value, AssetLoadPriority.Medium);
            }
        }
    }
}