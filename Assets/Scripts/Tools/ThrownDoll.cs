using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A thrown voodoo doll, added at runtime to the doll's visual prefab by
/// VoodooDoll. Two kinds of flight:
///   - GUIDED (thrown at a critter): a ballistic arc - real gravity, solved so
///     it lands on the critter after flightTime, nudged along if the critter
///     drifts. On arrival it asks the thrower whether it was caught (capture
///     starts, doll disappears). If not, it carries on as a free physics body.
///   - FREE (no target, or a guided one that missed): a Rigidbody with
///     gravity and bounces - it flies, hits the ground, rolls, lies there for
///     a moment, then shrinks away.
/// The guided part is computed, not simulated, so grass, branches and other
/// colliders can't knock a good throw off course.
///
/// The visual is a camera-facing sprite: while thrown, this replaces the
/// prefab's Billboard and "tumbles" by rolling the sprite around the view axis.
/// </summary>
public class ThrownDoll : MonoBehaviour
{
    private Rigidbody body;
    private Collider ownCollider;

    private bool guided;
    private Vector3 startPosition;
    private Vector3 launchVelocity;
    private Vector3 aimPoint;
    private float flightTime;
    private float elapsed;
    private CreatureAI homingTarget;
    private Func<ThrownDoll, bool> onArrive;

    private float rollAngle;
    private float rollSpeed;       // degrees per second
    private float landedLifetime;
    private float shrinkDuration = 0.3f;

    private static PhysicsMaterial bouncyMaterial;

    /// <summary>Current velocity (guided: along the arc; free: the Rigidbody's).</summary>
    public Vector3 Velocity => guided ? launchVelocity + Physics.gravity * elapsed : body.linearVelocity;

    // ---------------- Setup ----------------

    /// <param name="colliderRadius">World-space radius of the added sphere collider (0 = from the sprite's size).</param>
    public static ThrownDoll Spawn(GameObject prefab, Vector3 position, float colliderRadius, Collider[] ignoreColliders)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);
        var doll = go.AddComponent<ThrownDoll>();
        doll.Setup(colliderRadius, ignoreColliders);
        return doll;
    }

    private void Setup(float colliderRadius, Collider[] ignoreColliders)
    {
        // This drives the facing now (camera-facing + roll); a Billboard would overwrite it every LateUpdate.
        foreach (var billboard in GetComponents<Billboard>()) billboard.enabled = false;

        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.mass = 0.3f;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.5f;
        body.constraints = RigidbodyConstraints.FreezeRotation; // rotation is purely visual - see LateUpdate

        ownCollider = GetComponent<Collider>();
        if (ownCollider == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            float worldRadius = colliderRadius > 0f ? colliderRadius : SpriteRadius();
            float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
            sphere.radius = worldRadius / scale;
            ownCollider = sphere;
        }

        if (bouncyMaterial == null)
            bouncyMaterial = new PhysicsMaterial("ThrownDoll")
            {
                bounciness = 0.3f,
                dynamicFriction = 0.6f,
                staticFriction = 0.7f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
        ownCollider.sharedMaterial = bouncyMaterial;

        if (ignoreColliders != null)
            foreach (var other in ignoreColliders)
                if (other != null && other != ownCollider) Physics.IgnoreCollision(ownCollider, other);

        SetKinematic(true);
    }

    private float SpriteRadius()
    {
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return 0.1f;
        Vector3 extents = spriteRenderer.bounds.extents;
        return Mathf.Max(0.05f, Mathf.Min(extents.x, extents.y) * 0.8f);
    }

    // ---------------- Launching ----------------

    /// <summary>
    /// Ballistic arc onto a point: under normal gravity, reaching aimPoint after
    /// flightTime. If homingTarget moves meanwhile, the arc bends with it.
    /// onArrive returns true if the doll was "caught" (it then disappears).
    /// </summary>
    public void LaunchAt(Vector3 aimPoint, float flightTime, CreatureAI homingTarget, Func<ThrownDoll, bool> onArrive,
        float rollSpeed, float landedLifetime)
    {
        this.aimPoint = aimPoint;
        this.flightTime = Mathf.Max(0.05f, flightTime);
        this.homingTarget = homingTarget;
        this.onArrive = onArrive;
        this.rollSpeed = rollSpeed;
        this.landedLifetime = landedLifetime;

        startPosition = transform.position;
        // p(t) = p0 + v0 t + g t^2 / 2  ->  v0 = (target - p0 - g T^2 / 2) / T
        launchVelocity = (aimPoint - startPosition - 0.5f * Physics.gravity * this.flightTime * this.flightTime) / this.flightTime;
        elapsed = 0f;
        guided = true;
        SetKinematic(true);
    }

    /// <summary>Plain physics throw (nothing targeted).</summary>
    public void LaunchFree(Vector3 velocity, float rollSpeed, float landedLifetime)
    {
        this.rollSpeed = rollSpeed;
        this.landedLifetime = landedLifetime;
        Release(velocity);
    }

    // ---------------- Flight ----------------

    private void Update()
    {
        if (!guided) return;

        elapsed = Mathf.Min(elapsed + Time.deltaTime, flightTime);
        float progress = elapsed / flightTime;

        Vector3 position = startPosition + launchVelocity * elapsed + 0.5f * Physics.gravity * elapsed * elapsed;

        // Bend the arc toward wherever the critter is now, more the closer the doll is to landing.
        if (homingTarget != null && homingTarget.BodyCollider != null)
            position += (homingTarget.BodyCollider.bounds.center - aimPoint) * progress;

        transform.position = position;

        if (elapsed >= flightTime) Arrive();
    }

    private void Arrive()
    {
        guided = false;

        // The thrower may be gone (tool switched mid-flight) - then it's simply a miss.
        bool throwerAlive = onArrive != null && !(onArrive.Target is UnityEngine.Object o && o == null);
        bool caught = throwerAlive && onArrive(this);

        if (caught)
        {
            Destroy(gameObject);
            return;
        }

        Release(launchVelocity + Physics.gravity * flightTime); // keeps going the way it was flying, now with real collisions
    }

    private void Release(Vector3 velocity)
    {
        guided = false;
        SetKinematic(false);
        body.linearVelocity = velocity;
        StartCoroutine(ExpireRoutine());
    }

    private void SetKinematic(bool kinematic)
    {
        if (kinematic)
        {
            body.interpolation = RigidbodyInterpolation.None; // moved by hand in Update
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        else
        {
            body.isKinematic = false;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // small and fast - don't tunnel through the ground
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Each bounce takes some of the tumble out of it, so it settles instead of spinning on the ground.
        rollSpeed *= 0.5f;
    }

    private IEnumerator ExpireRoutine()
    {
        yield return new WaitForSeconds(landedLifetime);

        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            transform.localScale = startScale * (1f - Mathf.Clamp01(t / shrinkDuration));
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- Visual facing ----------------

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (!guided && body != null && !body.isKinematic && body.linearVelocity.sqrMagnitude < 0.04f)
            rollSpeed = Mathf.MoveTowards(rollSpeed, 0f, 720f * Time.deltaTime); // at rest: stop tumbling

        rollAngle += rollSpeed * Time.deltaTime;
        transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, -rollAngle);
    }
}
