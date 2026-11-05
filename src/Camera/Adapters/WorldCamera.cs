using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Show;

namespace Camera.Adapters;

/// <summary>
/// The world's camera, for code that runs outside the camera's own methods (key and HUD button hooks).
/// The game's singletons build themselves on first read, and a world built before the game makes it breaks the
/// session, so this only reads a world the game already made.
/// </summary>
internal static class WorldCamera
{
    public static CameraManager? Existing() => GameSingleton.Existing<BattleShowWorld>()?._CameraManager;
}
