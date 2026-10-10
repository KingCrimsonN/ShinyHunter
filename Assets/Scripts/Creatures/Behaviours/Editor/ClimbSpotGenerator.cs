using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tools > ShinyHunt > Generate Climb Spots: creates a ClimbSpot for terrain
/// trees (which can't carry components themselves). Each spot sits at the
/// trunk's base; its radius and height come from the tree prototype's own
/// Capsule/Box collider, scaled per tree instance, so the Climber follows the
/// same collider the player bumps into.
///
/// Everything goes under one "ClimbSpots (Generated)" object per terrain, which
/// Generate replaces wholesale - re-run it after repainting trees. Undo works.
/// </summary>
public class ClimbSpotGenerator : EditorWindow
{
    private const string RootName = "ClimbSpots (Generated)";

    [SerializeField] private Terrain terrain;
    [SerializeField] private List<bool> prototypeEnabled = new List<bool>();

    [SerializeField, Range(0f, 1f)] private float chance = 0.5f;
    [SerializeField] private float minSpacing = 3f;
    [SerializeField] private Vector2 heightFraction = new Vector2(0.45f, 0.8f);
    [SerializeField] private Vector2 fallbackHeight = new Vector2(2f, 4f);
    [SerializeField] private float fallbackRadius = 0.4f;
    [SerializeField] private bool requireNavMesh = true;
    [SerializeField] private float navMeshDistance = 2f;
    [SerializeField] private int seed = 12345;

    private Vector2 scroll;

    [MenuItem("Tools/ShinyHunt/Generate Climb Spots")]
    public static void ShowWindow()
    {
        GetWindow<ClimbSpotGenerator>("Climb Spots");
    }

    private void OnEnable()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Adds a ClimbSpot to terrain trees, sized from each tree prototype's collider. " +
            "Generate replaces this terrain's previously generated spots.", MessageType.Info);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        if (terrain == null || terrain.terrainData == null)
        {
            EditorGUILayout.HelpBox("Pick a terrain.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        var prototypes = terrain.terrainData.treePrototypes;
        while (prototypeEnabled.Count < prototypes.Length) prototypeEnabled.Add(true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tree types", EditorStyles.boldLabel);
        for (int i = 0; i < prototypes.Length; i++)
        {
            var prefab = prototypes[i].prefab;
            string label = prefab != null ? prefab.name : $"Prototype {i}";
            if (prefab != null && prefab.GetComponentInChildren<Collider>() == null) label += "  (no collider - fallback size)";
            prototypeEnabled[i] = EditorGUILayout.ToggleLeft(label, prototypeEnabled[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        chance = EditorGUILayout.Slider(new GUIContent("Chance per tree", "Fraction of the eligible trees that get a spot."), chance, 0f, 1f);
        minSpacing = EditorGUILayout.FloatField(new GUIContent("Min spacing", "No two spots closer than this (world units)."), minSpacing);
        requireNavMesh = EditorGUILayout.Toggle(new GUIContent("Require NavMesh nearby", "Skip trees the critter couldn't walk to. Needs the NavMesh baked and loaded."), requireNavMesh);
        if (requireNavMesh)
            navMeshDistance = EditorGUILayout.FloatField(new GUIContent("  NavMesh distance", "How far beyond the trunk the NavMesh may be."), navMeshDistance);
        seed = EditorGUILayout.IntField(new GUIContent("Random seed", "Same seed = same result."), seed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        heightFraction = EditorGUILayout.Vector2Field(new GUIContent("Climb height (fraction of collider)", "Perch height as a random fraction (min, max) of the tree's collider height."), heightFraction);
        fallbackHeight = EditorGUILayout.Vector2Field(new GUIContent("Fallback height", "Perch height range (min, max) for trees without a collider."), fallbackHeight);
        fallbackRadius = EditorGUILayout.FloatField(new GUIContent("Fallback radius", "Trunk radius for trees without a collider."), fallbackRadius);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate", GUILayout.Height(28))) Generate();
            if (GUILayout.Button("Clear", GUILayout.Height(28))) Clear();
        }

        EditorGUILayout.EndScrollView();
    }

    private Transform FindRoot()
    {
        foreach (Transform child in terrain.transform)
            if (child.name == RootName) return child;
        return null;
    }

    private void Clear()
    {
        var root = FindRoot();
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root.gameObject);
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        }
    }

    private void Generate()
    {
        var data = terrain.terrainData;
        var prototypes = data.treePrototypes;
        var trees = data.treeInstances;

        Undo.SetCurrentGroupName("Generate Climb Spots");
        int undoGroup = Undo.GetCurrentGroup();

        Clear();

        // Under the terrain so it's obvious what they belong to; kept at world origin rotation/scale.
        var rootObject = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootObject, "Generate Climb Spots");
        rootObject.transform.SetParent(terrain.transform, false);
        rootObject.transform.position = Vector3.zero;
        rootObject.transform.rotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;

        var random = new System.Random(seed);
        var grid = new Dictionary<Vector2Int, List<Vector3>>();
        float cell = Mathf.Max(0.5f, minSpacing);

        int created = 0, skippedNavMesh = 0;

        for (int i = 0; i < trees.Length; i++)
        {
            var tree = trees[i];
            if (tree.prototypeIndex < 0 || tree.prototypeIndex >= prototypes.Length) continue;
            if (!prototypeEnabled[tree.prototypeIndex]) continue;
            if (random.NextDouble() > chance) continue;

            Vector3 position = Vector3.Scale(tree.position, data.size) + terrain.transform.position;
            if (minSpacing > 0f && TooClose(grid, cell, position)) continue;

            GetTreeSize(prototypes[tree.prototypeIndex].prefab, tree, out float radius, out float colliderTop);

            if (requireNavMesh && !NavMesh.SamplePosition(position, out _, radius + navMeshDistance, NavMesh.AllAreas))
            {
                skippedNavMesh++;
                continue;
            }

            float height = colliderTop > 0f
                ? colliderTop * Mathf.Lerp(heightFraction.x, heightFraction.y, (float)random.NextDouble())
                : Mathf.Lerp(fallbackHeight.x, fallbackHeight.y, (float)random.NextDouble());

            var spotObject = new GameObject($"ClimbSpot {created}");
            spotObject.transform.SetParent(rootObject.transform, false);
            spotObject.transform.position = position;
            spotObject.AddComponent<ClimbSpot>().Configure(radius, height);

            AddToGrid(grid, cell, position);
            created++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Selection.activeGameObject = rootObject;

        Debug.Log($"ClimbSpotGenerator: created {created} climb spots on '{terrain.name}' ({trees.Length} trees).");
        if (requireNavMesh && created == 0 && skippedNavMesh > 0)
            Debug.LogWarning("ClimbSpotGenerator: every tree was skipped for having no NavMesh nearby - is the NavMesh baked and loaded? Untick 'Require NavMesh nearby' to place them anyway.");
    }

    /// <summary>The trunk radius and collider top height (0 = no collider) of one placed tree, from its prototype's collider and the instance's scale.</summary>
    private void GetTreeSize(GameObject prefab, TreeInstance tree, out float radius, out float colliderTop)
    {
        radius = fallbackRadius * tree.widthScale;
        colliderTop = 0f;
        if (prefab == null) return;

        var collider = prefab.GetComponentInChildren<Collider>();
        Vector3 scale = collider != null ? collider.transform.lossyScale : Vector3.one;
        float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * tree.widthScale;
        float verticalScale = Mathf.Abs(scale.y) * tree.heightScale;

        if (collider is CapsuleCollider capsule)
        {
            radius = capsule.radius * horizontalScale;
            colliderTop = (capsule.center.y + capsule.height * 0.5f) * verticalScale;
        }
        else if (collider is BoxCollider box)
        {
            radius = Mathf.Max(box.size.x, box.size.z) * 0.5f * horizontalScale;
            colliderTop = (box.center.y + box.size.y * 0.5f) * verticalScale;
        }
        else if (collider is SphereCollider sphere)
        {
            radius = sphere.radius * horizontalScale;
            colliderTop = (sphere.center.y + sphere.radius) * verticalScale;
        }
    }

    private static bool TooClose(Dictionary<Vector2Int, List<Vector3>> grid, float cell, Vector3 position)
    {
        var key = Cell(position, cell);
        float minSqr = cell * cell;

        for (int x = -1; x <= 1; x++)
        for (int z = -1; z <= 1; z++)
        {
            if (!grid.TryGetValue(new Vector2Int(key.x + x, key.y + z), out var list)) continue;
            foreach (var other in list)
            {
                Vector3 d = other - position;
                d.y = 0f;
                if (d.sqrMagnitude < minSqr) return true;
            }
        }

        return false;
    }

    private static void AddToGrid(Dictionary<Vector2Int, List<Vector3>> grid, float cell, Vector3 position)
    {
        var key = Cell(position, cell);
        if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<Vector3>();
        list.Add(position);
    }

    private static Vector2Int Cell(Vector3 position, float cell) =>
        new Vector2Int(Mathf.FloorToInt(position.x / cell), Mathf.FloorToInt(position.z / cell));
}
