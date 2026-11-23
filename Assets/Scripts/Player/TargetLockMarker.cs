using UnityEngine;

/// <summary>
/// The subtle "lock-on" marker: a small camera-facing sprite on the critter the
/// stick would hit right now (PlayerCapture decides that every frame). Pops in
/// when a critter comes into reach, fades out when it leaves it, keeps the same
/// size on screen at any distance.
///
/// Built in code (SpriteRenderer + Sprites/Default) - no prefab or scene setup.
/// With no sprite assigned it draws a small soft dot.
///
/// Call SetTarget every frame it should stay up; a frame without one starts the fade-out,
/// so it can never get stuck on screen.
/// </summary>
public class TargetLockMarker : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Material material;
    private Sprite generatedSprite;

    private Color color = Color.white;
    private float screenSize = 0.025f;
    private float popDuration = 0.12f;
    private float popScale = 1.6f;
    private float fadeDuration = 0.12f;

    private CreatureAI target;
    private float targetRadius;
    private int lastSetFrame = -1;
    private float popTimer;      // counts up from 0 when a new critter is locked
    private float visibility;    // 0-1, alpha
    private Vector3 lastPosition;

    public static TargetLockMarker Create(Sprite sprite, Color color, float screenSize, float popDuration, float popScale, float fadeDuration)
    {
        var go = new GameObject("TargetLockMarker");
        var marker = go.AddComponent<TargetLockMarker>();
        marker.Configure(sprite, color, screenSize, popDuration, popScale, fadeDuration);
        return marker;
    }

    private void Configure(Sprite sprite, Color color, float screenSize, float popDuration, float popScale, float fadeDuration)
    {
        this.color = color;
        this.screenSize = screenSize;
        this.popDuration = popDuration;
        this.popScale = popScale;
        this.fadeDuration = fadeDuration;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("TargetLockMarker: 'Sprites/Default' shader not found - no target marker.");
            enabled = false;
            return;
        }

        material = new Material(shader);
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sharedMaterial = material;
        spriteRenderer.sprite = sprite != null ? sprite : (generatedSprite = MakeDot());
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        spriteRenderer.sortingOrder = 100;
        spriteRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (generatedSprite != null)
        {
            Destroy(generatedSprite.texture);
            Destroy(generatedSprite);
        }
    }

    /// <summary>Keeps the marker on this critter for the current frame. An invalid target = hide.</summary>
    public void SetTarget(CreatureTarget t)
    {
        if (!t.IsValid) return;

        if (t.creature != target || visibility <= 0f) popTimer = 0f; // new lock (or re-lock after fading out): pop again
        target = t.creature;
        targetRadius = t.bodyRadius;
        lastSetFrame = Time.frameCount;
    }

    /// <summary>Starts fading out now (e.g. the player got frozen).</summary>
    public void Clear() => lastSetFrame = -1;

    // After the camera and the critter have moved this frame, so it doesn't trail.
    private void LateUpdate()
    {
        if (spriteRenderer == null) return;

        Camera cam = Camera.main;
        bool wanted = lastSetFrame == Time.frameCount && target != null && target.BodyCollider != null && cam != null;

        float fadeStep = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
        visibility = Mathf.Clamp01(visibility + (wanted ? fadeStep : -fadeStep));
        if (visibility <= 0f || cam == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        // Pulled toward the camera by the critter's radius, so it sits in front of the critter's sprite instead of inside it.
        if (wanted)
        {
            Vector3 center = target.BodyCollider.bounds.center;
            Vector3 toCam = cam.transform.position - center;
            lastPosition = center + toCam.normalized * Mathf.Min(targetRadius, toCam.magnitude * 0.5f);
        }

        transform.position = lastPosition;
        transform.rotation = cam.transform.rotation;

        // Constant size on screen: a fraction of the view height at this distance.
        float distance = Vector3.Distance(cam.transform.position, lastPosition);
        float worldHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * screenSize;
        float spriteHeight = Mathf.Max(0.0001f, spriteRenderer.sprite.bounds.size.y);

        popTimer += Time.deltaTime;
        float pop = popDuration > 0f ? Mathf.Clamp01(popTimer / popDuration) : 1f;
        float popFactor = Mathf.Lerp(popScale, 1f, 1f - (1f - pop) * (1f - pop)); // ease-out shrink onto the critter

        transform.localScale = Vector3.one * (worldHeight / spriteHeight * popFactor);

        Color c = color;
        c.a *= visibility * pop;
        spriteRenderer.color = c;
        spriteRenderer.enabled = true;
    }

    /// <summary>Fallback when no sprite is assigned: a small soft white dot.</summary>
    private static Sprite MakeDot()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TargetLockDot" };
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
            float a = Mathf.Clamp01((1f - d) * 3f); // solid centre, soft edge
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
