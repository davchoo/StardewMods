using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;

namespace OverflowingBin.Patches;
internal class ShippingBinPatches
{
    private static IMonitor? Monitor;

    private static ShippingBinTextures? ShippingBinTextures;

    internal static void Initialize(IMonitor monitor, Harmony harmony, ShippingBinTextures shippingBinTextures)
    {
        Monitor = monitor;
        ShippingBinTextures = shippingBinTextures;

        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), nameof(ShippingBin.draw)),
            postfix: new HarmonyMethod(typeof(ShippingBinPatches), nameof(Draw_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), "closeShippingBinLid"),
            prefix: new HarmonyMethod(typeof(ShippingBinPatches), nameof(CloseShippingBinLid_Prefix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), nameof(ShippingBin.showShipment)),
            transpiler: new HarmonyMethod(typeof(ShippingBinPatches), nameof(ShowShipment_Transpiler))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(ShippingBin), "openShippingBinLid"),
            standin: new HarmonyMethod(typeof(ShippingBinPatches), nameof(OpenShippingBinLid))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(ShippingBin), "isShippingBinLidOpen"),
            standin: new HarmonyMethod(typeof(ShippingBinPatches), nameof(IsShippingBinLidOpen))
        );
    }
    private static void UpdateLidDepth(ShippingBin instance, TemporaryAnimatedSprite? shippingBinLid)
    {
        if (shippingBinLid == null)
        {
            return;
        }
        shippingBinLid.layerDepth = (instance.tileY.Value + 1) * 64 / 10000f;
        if (IsShippingBinLidOpen(instance, true))
        {
            shippingBinLid.layerDepth += 0.0001f;
        }
        else
        {
            shippingBinLid.layerDepth += 0.0025f;
        }
    }

    private static void DrawItems(ShippingBin instance, ShippingBinState state, IInventory inventory, SpriteBatch b)
    {
        // Constants TODO move
        // int StackSize = 50;
        // float ItemScale = 0.5f;

        // int Columns = 5;

        // float Width = instance.tilesWide.Value * 64f;
        // float Padding = 16f / 0.8f * ItemScale;

        // float ColumnSpacing = (Width - Padding * 2) / Columns;
        // float RowSpacing = 24f * ItemScale;

        // float OddColumnOffset = 4f;

        // float LidThreshold = 60f;
        int StackSize = 50;
        float ItemScale = 0.8f;

        int Columns = 3;

        float Width = instance.tilesWide.Value * 64f;
        float Padding = 16f / 0.8f * ItemScale;

        float ColumnSpacing = (Width - Padding * 2) / Columns;
        float RowSpacing = 24f * ItemScale;

        float OddColumnOffset = 4f;

        float LidThreshold = 60f;

        // Global position of bottom left corner of the shipping bin
        var basePosition = new Vector2(instance.tileX.Value, instance.tileY.Value) * 64f;
        var random = new Random(Game1.Date.TotalDays + inventory.Count);
        var itemTint = Color.White * instance.alpha;
        int spriteIdx = 0;
        foreach (var item in inventory)
        {
            if (item == null)
            {
                continue;
            }
            int lastRow = -1;
            Vector2 rowOffset = Vector2.Zero;
            float columnSpacing = 0;
            for (int j = 0; j < item.Stack; j += StackSize)
            {
                var column = spriteIdx % Columns;
                var row = spriteIdx / Columns;

                if (lastRow != row)
                {
                    lastRow = row;

                    rowOffset = new Vector2(random.Next(-8, 8), random.Next(-4, 0));
                    columnSpacing = ColumnSpacing;
                    if (row * RowSpacing < 64f)
                    {
                        rowOffset.X *= 0.5f;
                        columnSpacing += random.Next(-2, 2);
                    } else {
                        columnSpacing *= 1 - (random.Next(-8, 8) / 64f);
                    }
                }

                var position = basePosition;
                position.X += Width * 0.5f; // Move item to the horizontal center of the shipping box
                position.X -= 32f;          // Center item horizontally
                position.Y -= 20f;          // Move items up so the first row is partially visible

                position.X += columnSpacing * (column - (Columns - 1) * 0.5f); // Column horizontal offset
                position.Y += -row * RowSpacing;                               // Row vertical offset

                position.Y += column % 2 * OddColumnOffset; // Move odd columns down vertically

                position += rowOffset;

                if (state.CanCloseLid && (basePosition.Y - position.Y) > LidThreshold)
                {
                    state.CanCloseLid = false;
                    if (!IsShippingBinLidOpen(instance, true))
                    {
                        // Don't render items that can clip with the lid while it is openning
                        break;
                    }
                }

                var location = Game1.GlobalToLocal(Game1.viewport, position);
                var layerDepth = (basePosition.Y + 64) / 10000f + 0.00012f + (column + row * 3) * 0.00011f;
                item.drawInMenu(b, location, ItemScale, 1.0f, layerDepth, StackDrawType.Hide, itemTint, true);
                spriteIdx++;
            }
        }
    }

    private static void DrawFront(ShippingBin instance, ShippingBinState state, SpriteBatch b)
    {
        if (IsShipmentInProgress(instance, state))
        {
            return;
        }
        var position = new Vector2(instance.tileX.Value, instance.tileY.Value) * 64f;
        b.Draw(
            ShippingBinTextures!.ShippingBinFront!.Value,
            Game1.GlobalToLocal(Game1.viewport, position - new Vector2(0, 64)),
            instance.getSourceRect(),
            instance.color * instance.alpha,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            (position.Y + 64) / 10000f + 0.0016f
        );
    }

    private static void Draw_Postfix(ShippingBin __instance, Farm? ___farm, TemporaryAnimatedSprite? ___shippingBinLid, SpriteBatch b)
    {
        if (___farm == null)
        {
            return;
        }
        if (__instance.isMoving)
        {
            return;
        }
        if (__instance.daysOfConstructionLeft.Value > 0)
        {
            return;
        }

        var state = ShippingBinState.GetState(__instance);
        state.CanCloseLid = true; // Default to true. DrawItems will update it later.

        UpdateLidDepth(__instance, ___shippingBinLid);

        var inventory = ___farm.getShippingBin(Game1.player);
        if (inventory.Count == 0)
        {
            return;
        }

        DrawItems(__instance, state, inventory, b);
        DrawFront(__instance, state, b);
    }

    private static bool CloseShippingBinLid_Prefix(ShippingBin __instance, TemporaryAnimatedSprite? ___shippingBinLid)
    {
        if (___shippingBinLid == null)
        {
            return true;
        }
        var state = ShippingBinState.GetState(__instance);
        if (state.CanCloseLid)
        {
            return true;
        }
        if (!IsShippingBinLidOpen(__instance, false))
        {
            OpenShippingBinLid(__instance);
        }
        // Prevent the lid from closing
        return false;
    }

    private static void PatchSpriteLayerDepths(CodeMatcher codeMatcher)
    {
        // Adjust the layer depth for the two ShippingBin TemporaryAnimatedSprites
        var i = 0;
        codeMatcher.MatchStartForward(new CodeMatch[] {
            new(inst => inst.LoadsConstant(10000f)),
            new(OpCodes.Div),
            new(OpCodes.Ldc_R4),
            new(OpCodes.Add),
            new(OpCodes.Stfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.layerDepth)))
        })
        .Repeat(cm =>
        {
            cm.Advance(2); // Advance to ldc.r4 <layer depth offset>
            if (i == 0)
            {
                // Full sprite, background layer
                cm.SetOperandAndAdvance(0.00011f); // Move it back more. Original: 0.0002f
            }
            else if (i == 1)
            {
                // Bottom half of sprite, foreground layer
                cm.SetOperandAndAdvance(0.001601f); // Move it forward more. Original: 0.0003f
                cm.End(); // Stop since the next sprite is the item being shipped
            }
            i++;
        });
        if (i != 2)
        {
            throw new InvalidOperationException("Unable to patch layer depths for ShippingBin's TemporaryAnimatedSprite's");
        }
    }

    private static void CaptureSpriteId(CodeMatcher codeMatcher)
    {
        // Store the id of the TemporaryAnimatedSprites to ShippingBinState.ShippmentSpriteId
        codeMatcher.Start()
        .MatchStartForward(new CodeMatch[] {
            new(inst => inst.IsLdloc()),
            new(OpCodes.Stfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.extraInfoForEndBehavior))),
            new(OpCodes.Dup),
            new(inst => inst.IsLdloc()),
            new(OpCodes.Ldftn, AccessTools.Method(typeof(GameLocation), nameof(GameLocation.removeTemporarySpritesWithID))),
        })
        .ThrowIfInvalid("Failed to find code for ShippingBin's TemporaryAnimatedSprite id");

        var ldlocExtraInfo = codeMatcher.Instruction;
        codeMatcher.Insert(new CodeInstruction[] {
            new(OpCodes.Ldarg_0), // Load reference to the current ShippingBin
            new(OpCodes.Call, AccessTools.Method(typeof(ShippingBinState), nameof(ShippingBinState.GetState))),
            ldlocExtraInfo, // Load sprite id
            new(OpCodes.Call, AccessTools.PropertySetter(typeof(ShippingBinState), nameof(ShippingBinState.ShippmentSpriteId))),
        });
    }

    private static IEnumerable<CodeInstruction> ShowShipment_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);
        PatchSpriteLayerDepths(codeMatcher);
        CaptureSpriteId(codeMatcher);
        return codeMatcher.Instructions();
    }

    private static void OpenShippingBinLid(ShippingBin instance)
    {
        throw new NotImplementedException("Stub method. This is reverse patched in Initialize");
    }

    private static bool IsShippingBinLidOpen(ShippingBin instance, bool requiredToBeFullyOpen)
    {
        throw new NotImplementedException("Stub method. This is reverse patched in Initialize");
    }

    private static bool IsShipmentInProgress(ShippingBin instance, ShippingBinState state)
    {
        if (!state.HasShippmentSpriteId)
        {
            return false;
        }
        var sprite = instance.GetParentLocation().getTemporarySpriteByID(state.ShippmentSpriteId);
        if (sprite == null)
        {
            state.HasShippmentSpriteId = false;
            return false;
        }
        return true;
    }
}