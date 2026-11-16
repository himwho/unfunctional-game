/// <summary>Compatibility hook: department art is now serialized in the level scenes.</summary>
public static class CampaignRoomArt
{
    // Intentionally never creates or moves scene geometry. Authoring recipes live in Editor.
    public static void Dress(LevelManager level) { }
}
