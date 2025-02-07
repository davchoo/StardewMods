using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Inventories;

namespace OverflowingBin.ItemBin;
internal abstract class ItemBinLogic<Instance> where Instance : notnull
{
    protected abstract GameLocation GetGameLocation(Instance instance);

    protected abstract Vector2 GetTilePosition(Instance instance);

    protected abstract Vector2 GetTileSize(Instance instance);

    protected abstract Texture2D GetTexture();

    protected abstract Rectangle GetSourceRect(Instance instance);

    protected abstract Color GetColor(Instance instance);

    protected abstract float GetAlpha(Instance instance);

    protected abstract bool CanDraw(Instance instance);

    private void UpdateLidDepth(Instance instance, TemporaryAnimatedSprite? shippingBinLid)
    {
        if (shippingBinLid == null)
        {
            return;
        }
        shippingBinLid.layerDepth = (GetTilePosition(instance).Y + 1) * 64 / 10000f;
        if (IsShippingBinLidOpen(instance, true))
        {
            shippingBinLid.layerDepth += 0.0001f;
        }
        else
        {
            shippingBinLid.layerDepth += 0.0025f;
        }
    }

    private void DrawItems(Instance instance, ShippingBinState state, IInventory inventory, SpriteBatch b)
    {
        // Constants TODO move
        int StackSize = 15;
        float ItemScale = 0.8f;

        int Columns = 3;

        float Width = GetTileSize(instance).X * 64f;
        float Padding = 20f / 0.8f * ItemScale;

        float RowSpacing = 24f * ItemScale;

        float OddColumnOffset = 4f;

        float LidThreshold = 54f;

        float WobbleAmplitude = 16f;
        double WobbleFrequency = Math.Tau / 20.0;
        float WobbleFadeRows = 20f;

        int MaxSpriteCount = 250;
        float OffscreenThreshold = -64f * 1.5f * ItemScale;
        // Global position of bottom left corner of the shipping bin
        var basePosition = GetTilePosition(instance) * 64f;
        var random = new Random(Game1.Date.TotalDays);
        var itemTint = Color.White * GetAlpha(instance);
        int wobblePhase = random.Next(-20, 20);
        // State
        Vector2 rowOffset = Vector2.Zero;
        float columnSpacing = (Width - Padding * 2) / Columns;
        int columns = Columns;

        int row = 0;
        int column = 0;
        int spriteCount = 0;
        foreach (var item in inventory)
        {
            if (item == null)
            {
                continue;
            }
            for (int j = 0; j < item.Stack; j += StackSize)
            {
                var position = basePosition;
                position.X += Width * 0.5f; // Move item to the horizontal center of the shipping box
                position.X -= 32f;          // Center item horizontally
                position.Y -= 20f;          // Move items up so the first row is partially visible

                position.X += columnSpacing * (column - (columns - 1) * 0.5f); // Column horizontal offset
                position.Y += -row * RowSpacing;                               // Row vertical offset

                position.X += random.Next(-4, 4); // Per item randomness

                position.Y += column % 2 * OddColumnOffset; // Move odd columns down vertically

                position += rowOffset;

                if (state.CanCloseLid && (basePosition.Y - position.Y) > LidThreshold)
                {
                    state.CanCloseLid = false;
                    if (!IsShippingBinLidOpen(instance, true))
                    {
                        // Don't render items that can clip with the lid while it is opening
                        return;
                    }
                }

                var location = Game1.GlobalToLocal(Game1.viewport, position);
                var layerDepth = (basePosition.Y + 64) / 10000f + 0.00012f + (column + row * 3) * 0.00011f;
                item.drawInMenu(b, location, ItemScale, 1.0f, layerDepth, StackDrawType.Hide, itemTint, true);

                spriteCount++;
                if (spriteCount >= MaxSpriteCount)
                {
                    return;
                }
                if (!state.CanCloseLid && location.Y < OffscreenThreshold)
                {
                    return;
                }

                column++;
                if (column == columns)
                {
                    row++;
                    column = 0;

                    rowOffset = Vector2.Zero;
                    // Gradually make the stack wobble from side to side
                    float wobble = WobbleAmplitude * (float)Math.Sin((row + wobblePhase) * WobbleFrequency);
                    float wobbleFade = Math.Min(row / WobbleFadeRows, 1.0f);
                    rowOffset.X += wobble * wobbleFade;

                    if (row * RowSpacing < 64f)
                    {
                        columns = Columns;
                        columnSpacing = (Width - Padding * 2) / columns;
                        columnSpacing += random.Next(-2, 2);
                    }
                    else
                    {
                        columns = Columns + random.Next(0, 2);
                        columnSpacing = (Width - Padding * 2) / columns;
                        columnSpacing *= 1 - (random.Next(-24, 8) / 64f);
                    }
                }
            }
        }
    }

    private void DrawFront(Instance instance, ShippingBinState state, SpriteBatch b)
    {
        if (IsShipmentInProgress(instance, state))
        {
            return;
        }
        var position = GetTilePosition(instance) * 64f;
        b.Draw(
            GetTexture(),
            Game1.GlobalToLocal(Game1.viewport, position - new Vector2(0, 64)),
            GetSourceRect(instance),
            GetColor(instance) * GetAlpha(instance),
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            (position.Y + 64) / 10000f + 0.0016f
        );
    }

    internal void Draw_Postfix(Instance instance,
        TemporaryAnimatedSprite? ___shippingBinLid, SpriteBatch b)
    {
        if (!CanDraw(instance))
        {
            return;
        }

        var state = ShippingBinState.GetState(instance);
        state.CanCloseLid = true; // Default to true. DrawItems will update it later.

        UpdateLidDepth(instance, ___shippingBinLid);

        var inventory = Game1.getFarm().getShippingBin(Game1.player);
        if (inventory.Count == 0)
        {
            return;
        }

        DrawItems(instance, state, inventory, b);
        DrawFront(instance, state, b);
    }

    internal bool CloseShippingBinLid_Prefix(Instance instance,
        TemporaryAnimatedSprite? shippingBinLid)
    {
        if (shippingBinLid == null)
        {
            return true;
        }
        var state = ShippingBinState.GetState(instance);
        if (state.CanCloseLid)
        {
            return true;
        }
        if (!IsShippingBinLidOpen(instance, false))
        {
            OpenShippingBinLid(instance);
        }
        // Prevent the lid from closing
        return false;
    }

    internal abstract void OpenShippingBinLid(Instance instance);

    internal abstract bool IsShippingBinLidOpen(Instance instance, bool requiredToBeFullyOpen);

    private bool IsShipmentInProgress(Instance instance, ShippingBinState state)
    {
        if (!state.HasShippmentSpriteId)
        {
            return false;
        }
        var location = GetGameLocation(instance);
        var sprite = location.getTemporarySpriteByID(state.ShippmentSpriteId);
        if (sprite == null)
        {
            state.HasShippmentSpriteId = false;
            return false;
        }
        return true;
    }
}