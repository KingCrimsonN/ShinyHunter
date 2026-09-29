using UnityEngine;

[CreateAssetMenu(fileName = "CritterFrames", menuName = "ShinyHunt/Critter Frames")]
public class CritterFrames : ScriptableObject
{
    public Sprite[] rarityFrames = new Sprite[4];
    public Sprite[] selectedFrames = new Sprite[4];

    public Sprite[] familyFrames = new Sprite[5];
}
