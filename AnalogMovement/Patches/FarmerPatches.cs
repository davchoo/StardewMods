using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace AnalogMovement.Patches;
internal class FarmerPatches
{
    internal static void Initialize(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), "MovePositionImpl"),
            prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(MovePositionImpl_Prefix)) { priority = Priority.Low }
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.nextPosition)),
            prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(NextPosition_Prefix)) { priority = Priority.Low }
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.nextPositionHalf)),
            prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(NextPositionHalf_Prefix)) { priority = Priority.Low }
        );
    }

    /// <returns>True if the gamepad is enabled and the left is moved out of the Deadzone</returns>
    private static bool UsingLeftThumbStick()
    {
        if (!Game1.options.gamepadControls)
        {
            return false;
        }
        GamePadState state = Game1.input.GetGamePadState();
        return Math.Abs(state.ThumbSticks.Left.X) > ModEntry.Config.Deadzone || Math.Abs(state.ThumbSticks.Left.Y) > ModEntry.Config.Deadzone;
    }

    /// <summary>
    /// Adjust movementSpeedX and movementSpeedY to match the angle of the left thumbstick
    /// </summary>
    private static void ApplyThumbstickAdjustment(ref float movementSpeedX, ref float movementSpeedY)
    {
        if (!UsingLeftThumbStick())
        {
            return;
        }
        GamePadState state = Game1.input.GetGamePadState();
        float x = state.ThumbSticks.Left.X;
        float y = state.ThumbSticks.Left.Y;
        // Apply deadband
        if (Math.Abs(x) < ModEntry.Config.Deadzone)
        {
            x = 0;
        }
        if (Math.Abs(y) < ModEntry.Config.Deadzone)
        {
            y = 0;
        }
        if (ModEntry.Config.SquareInput)
        {
            // Convert thumbstick input into a square and restrict to [-1, 1]
            float maxComp = Math.Max(Math.Abs(x), Math.Abs(y));
            if (maxComp > 0.0)
            {
                x /= maxComp;
                y /= maxComp;
            }
        }
        else
        {
            // Normalize thumbstick input to keep it circular
            float len = (float)Math.Sqrt(x * x + y * y);
            x /= len;
            y /= len;
        }
        movementSpeedX *= Math.Abs(x);
        movementSpeedY *= Math.Abs(y);
    }

    private static void MovePositionImpl_Prefix(Farmer __instance, int direction, ref float movementSpeedX, ref float movementSpeedY)
    {
        if (__instance != Game1.player)
        {
            return;
        }
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);
    }

    private static bool NextPosition_Prefix(Farmer __instance, int direction, ref Rectangle __result)
    {
        if (__instance != Game1.player)
        {
            return true;
        }
        Rectangle nextPosition = __instance.GetBoundingBox();

        float movementSpeedX = __instance.getMovementSpeed();
        float movementSpeedY = movementSpeedX;
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);

        switch (direction)
        {
            case Direction.Up:
                nextPosition.Y -= (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Right:
                nextPosition.X += (int)Math.Ceiling(movementSpeedX);
                break;
            case Direction.Down:
                nextPosition.Y += (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Left:
                nextPosition.X -= (int)Math.Ceiling(movementSpeedX);
                break;
        }
        __result = nextPosition;
        return false;
    }

    private static bool NextPositionHalf_Prefix(Farmer __instance, int direction, ref Rectangle __result)
    {
        if (__instance != Game1.player)
        {
            return true;
        }
        Rectangle nextPosition = __instance.GetBoundingBox();
        float movementSpeedX = __instance.getMovementSpeed() / 2.0f;
        float movementSpeedY = movementSpeedX;
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);

        switch (direction)
        {
            case Direction.Up:
                nextPosition.Y -= (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Right:
                nextPosition.X += (int)Math.Ceiling(movementSpeedX);
                break;
            case Direction.Down:
                nextPosition.Y += (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Left:
                nextPosition.X -= (int)Math.Ceiling(movementSpeedX);
                break;
        }
        __result = nextPosition;
        return false;
    }

    private class Direction
    {
        internal const int Up = 0;
        internal const int Right = 1;
        internal const int Down = 2;
        internal const int Left = 3;
    }
}