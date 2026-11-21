using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > ShinyHunt > Build Shiny FX Prefab: (re)generates
/// Assets/FX/Shiny/ShinyFX.prefab - every shiny particle effect from the
/// particles README, for all three rarities, wired into a ShinyEffects
/// component. Drop the prefab under a critter and assign it to CreatureAI's
/// Shiny Particles.
///
/// Built by code rather than by hand so the numbers are readable and the
/// whole thing can be regenerated; after building, tweak it freely in the
/// prefab - just don't rebuild afterwards, it overwrites the prefab.
///
/// Units: the systems use Hierarchy scaling, so they scale with the critter
/// (CreatureData.size). Values are in the critter's LOCAL units, where a
/// critter sprite is ~10 units across. +Z faces the camera (Billboard).
///
/// Sprites: Sprites/Particles/shiny/shiny__stars.png, sliced 2 x 9:
/// 0-3 4-point, 4-7 5-point, 8-11 hollow, 12-15 thin (two colours per row,
/// two rows per type), 16 the white round mote. Picking a random sprite =
/// a random colour. Materials in FX/Shiny/Materials are duplicates of Epic
/// Toon FX ones (same particle shader as the old sparkles).
/// </summary>
public static class ShinyFXBuilder
{
    private const string PrefabPath = "Assets/FX/Shiny/ShinyFX.prefab";
    private const string StarsPath = "Assets/Sprites/Particles/shiny/shiny__stars.png";
    private const string MaterialFolder = "Assets/FX/Shiny/Materials/";

    private static Sprite[] stars;
    private static Material starsMat, bigStarMat, flashMat, glowMat, raysMat;

    [MenuItem("Tools/ShinyHunt/Build Shiny FX Prefab")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog("Shiny FX", "Overwrite " + PrefabPath + "? Any tweaks made to it by hand are lost.", "Overwrite", "Cancel"))
            return;

        stars = AssetDatabase.LoadAllAssetsAtPath(StarsPath).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        if (stars.Length < 17)
        {
            Debug.LogError($"ShinyFXBuilder: expected 17 sprites in {StarsPath}, found {stars.Length}.");
            return;
        }

        starsMat = LoadMat("ShinyStars_AB");
        bigStarMat = LoadMat("ShinyBigStar_AB");
        flashMat = LoadMat("ShinyFlash_ADD");
        glowMat = LoadMat("ShinyGlow_ADD");
        raysMat = LoadMat("ShinyRays_ADD");
        if (starsMat == null || bigStarMat == null || flashMat == null || glowMat == null || raysMat == null) return;

        var root = new GameObject("ShinyFX");
        var constants = new GameObject[4];
        var spawns = new GameObject[4];

        // ---- Sparkling: one 4-point star rises; white motes drift around.
        var sparkling = Child(root, "Sparkling");
        spawns[1] = Child(sparkling, "Spawn");
        RisingStar(spawns[1]);
        constants[1] = Child(sparkling, "Constant");
        Motes(constants[1]);

        // ---- Shining: star burst + white flash behind; motes + thin stars.
        var shining = Child(root, "Shining");
        spawns[2] = Child(shining, "Spawn");
        StarBurst(spawns[2]);
        Flash(spawns[2]);
        constants[2] = Child(shining, "Constant");
        Motes(constants[2]);
        ThinStars(constants[2]);

        // ---- Radiant: Shining's burst + flash + the big growing star; motes, thin stars, glow and rays.
        var radiant = Child(root, "Radiant");
        spawns[3] = Child(radiant, "Spawn");
        StarBurst(spawns[3]);
        Flash(spawns[3]);
        BigStar(spawns[3]);
        constants[3] = Child(radiant, "Constant");
        Motes(constants[3]);
        ThinStars(constants[3]);
        Glow(constants[3]);
        Rays(constants[3]);

        var effects = root.AddComponent<ShinyEffects>();
        var so = new SerializedObject(effects);
        for (int i = 0; i < 4; i++)
        {
            so.FindProperty("constantEffects").GetArrayElementAtIndex(i).objectReferenceValue = constants[i];
            so.FindProperty("spawnEffects").GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var s in spawns) if (s != null) s.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("ShinyFXBuilder: built " + PrefabPath);
    }

    // ---------------- the effects ----------------

    /// <summary>Sparkling spawn: one 4-point star of a random colour rises above the critter and fades.</summary>
    private static void RisingStar(GameObject parent)
    {
        var ps = System(parent, "RisingStar", starsMat, new Vector3(0f, 2f, 0.5f));
        var main = ps.main;
        main.duration = 1.5f;
        main.startLifetime = 1.2f;
        main.startSpeed = 0f;
        main.startSize = 3f;
        main.maxParticles = 1;
        Burst(ps, 1);
        NoShape(ps);
        Velocity(ps, new Vector3(0f, 5f, 0f), null);
        Fade(ps, 0.1f, 0.6f);
        Sprites(ps, 0, 4);
    }

    /// <summary>Shining/Radiant spawn: 4-point, 5-point and hollow stars fly out from the centre, spinning a little.</summary>
    private static void StarBurst(GameObject parent)
    {
        var ps = System(parent, "StarBurst", starsMat, new Vector3(0f, 0f, 0.5f));
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 38f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        main.maxParticles = 20;
        Burst(ps, 14);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle; // circle lies in the XY plane = facing the camera
        shape.radius = 0.5f;
        shape.radiusThickness = 1f;

        Velocity(ps, Vector3.zero, new AnimationCurve(new Keyframe(0f, 1f, 0f, -3f), new Keyframe(1f, 0.1f))); // shoot out, then slow
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f); // radians/s
        Fade(ps, 0f, 0.55f);
        Shrink(ps, 1f, 0.5f);
        Sprites(ps, 0, 12);
    }

    /// <summary>Shining/Radiant spawn: a quick white flash behind the critter.</summary>
    private static void Flash(GameObject parent)
    {
        var ps = System(parent, "Flash", flashMat, new Vector3(0f, 0f, -1.5f));
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.45f;
        main.startSpeed = 0f;
        main.startSize = 9f;
        main.maxParticles = 1;
        Burst(ps, 1);
        NoShape(ps);
        Fade(ps, 0f, 0.15f);
        Shrink(ps, 0.6f, 2.6f);
    }

    /// <summary>Radiant spawn: the big rainbow star grows from the centre and fades out, in front of the critter.</summary>
    private static void BigStar(GameObject parent)
    {
        var ps = System(parent, "BigStar", bigStarMat, new Vector3(0f, 0f, 1f));
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.9f;
        main.startSpeed = 0f;
        main.startSize = 5f;
        main.maxParticles = 1;
        Burst(ps, 1);
        NoShape(ps);
        Fade(ps, 0f, 0.3f);
        Shrink(ps, 0.6f, 3.2f);
    }

    /// <summary>Constant: small white round motes around (not only on) the critter, drifting away from its centre.</summary>
    private static void Motes(GameObject parent)
    {
        var ps = System(parent, "Motes", starsMat, new Vector3(0f, 0f, 0.3f));
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.rateOverTime = 7f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 6.5f;
        shape.radiusThickness = 1f;

        Fade(ps, 0.25f, 0.6f);
        Sprites(ps, 16, 1);
    }

    /// <summary>Constant: thin stars of random colours appear around the critter - no movement or spin, just scale in/out and fade.</summary>
    private static void ThinStars(GameObject parent)
    {
        var ps = System(parent, "ThinStars", starsMat, new Vector3(0f, 0f, 0.4f));
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        main.maxParticles = 12;
        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 6f;
        shape.radiusThickness = 1f;

        Fade(ps, 0f, 0.5f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f)));
        Sprites(ps, 12, 4);
    }

    /// <summary>Radiant constant: a soft warm glow behind and around the sprite (overlapping long particles = steady, gently pulsing).</summary>
    private static void Glow(GameObject parent)
    {
        var ps = System(parent, "Glow", glowMat, new Vector3(0f, 0f, -1.5f));
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 2f;
        main.startLifetime = 2.4f;
        main.startSpeed = 0f;
        main.startSize = 20f;
        main.startColor = new Color(1f, 0.92f, 0.7f, 0.8f);
        main.maxParticles = 4;
        var emission = ps.emission;
        emission.rateOverTime = 1.25f;
        NoShape(ps);
        Fade(ps, 0.4f, 0.6f);
    }

    /// <summary>Radiant constant: slow light rays turning behind the critter. Disable the Rays object if it's too much.</summary>
    private static void Rays(GameObject parent)
    {
        var ps = System(parent, "Rays", raysMat, new Vector3(0f, 0f, -1.6f));
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 3f;
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = 24f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 0.92f, 0.7f, 0.45f);
        main.maxParticles = 8;
        var emission = ps.emission;
        emission.rateOverTime = 2f;
        NoShape(ps);
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        Fade(ps, 0.4f, 0.6f);
    }

    // ---------------- helpers ----------------

    private static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static ParticleSystem System(GameObject parent, string name, Material material, Vector3 localPosition)
    {
        var go = Child(parent, name);
        go.transform.localPosition = localPosition;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true; // spawn roots are inactive until ShinyEffects plays them; constants run as soon as enabled
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.gravityModifier = 0f;
        main.startColor = Color.white;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private static void Burst(ParticleSystem ps, int count)
    {
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void NoShape(ParticleSystem ps)
    {
        var shape = ps.shape;
        shape.enabled = false;
    }

    /// <param name="speedModifier">Optional speed multiplier over lifetime (slows a burst down).</param>
    private static void Velocity(ParticleSystem ps, Vector3 velocity, AnimationCurve speedModifier)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(velocity.x);
        vel.y = new ParticleSystem.MinMaxCurve(velocity.y);
        vel.z = new ParticleSystem.MinMaxCurve(velocity.z);
        if (speedModifier != null) vel.speedModifier = new ParticleSystem.MinMaxCurve(1f, speedModifier);
    }

    /// <summary>Alpha over lifetime (0-1): fades in until fadeIn (0 = starts fully visible), stays until holdEnd, gone at the end.</summary>
    private static void Fade(ParticleSystem ps, float fadeIn, float holdEnd)
    {
        var alphas = fadeIn > 0f
            ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, holdEnd), new GradientAlphaKey(0f, 1f) }
            : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdEnd), new GradientAlphaKey(0f, 1f) };
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alphas);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void Shrink(ParticleSystem ps, float from, float to)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
    }

    /// <summary>Each particle shows one random sprite of stars[first .. first+count-1] (i.e. a random colour), not animated.</summary>
    private static void Sprites(ParticleSystem ps, int first, int count)
    {
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;
        while (tsa.spriteCount > 0) tsa.RemoveSprite(0);
        for (int i = 0; i < count; i++) tsa.AddSprite(stars[first + i]);
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        tsa.startFrame = count > 1 ? new ParticleSystem.MinMaxCurve(0f, count - 0.01f) : new ParticleSystem.MinMaxCurve(0f);
        tsa.cycleCount = 1;
    }

    private static Material LoadMat(string name)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + name + ".mat");
        if (mat == null) Debug.LogError("ShinyFXBuilder: missing material " + MaterialFolder + name + ".mat");
        return mat;
    }
}
