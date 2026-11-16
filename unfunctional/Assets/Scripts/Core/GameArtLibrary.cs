using UnityEngine;

/// <summary>References to authored project art; the editor baker creates this resource once.</summary>
public class GameArtLibrary : ScriptableObject
{
    public Material tile, concrete, wood, steel;
    public GameObject shovel, door;
    public AudioClip achievementSound;
    public AudioClip[] achievementVariants;
    public AudioClip shovelSnap;
    public Mesh shovelLowerShaft, shovelUpperShaft, soilClod;
    public static GameArtLibrary Load() => Resources.Load<GameArtLibrary>("GameArt/Library");
}
