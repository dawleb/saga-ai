using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance;

    // =========================================================
    // BLOOD HIT
    // =========================================================

    [Header("Blood Hit")]

    [Tooltip("BloodFX prefab used for the hit burst.")]
    public GameObject bloodHitEffect;

    [Min(0f)]
    public float bloodHitLifetime = 1.5f;

    public bool rotateBloodToAttacker = false;

    public float bloodVerticalOffset = 0.30f;

    public float bloodForwardOffset = 0.03f;

    [Tooltip("Force all hit particle systems to play immediately.")]
    public bool forcePlayParticleSystems = true;

    [Tooltip("Clear particles already stored in the hit prefab.")]
    public bool clearParticlesBeforePlay = true;

    [Tooltip("Force the base particle color to white.")]
    public bool forceHitParticleWhite = true;


    // =========================================================
    // GROUND BLOOD
    // =========================================================

    [Header("Ground Blood")]

    [Tooltip("BloodFX BloodSplat prefab used for the ground stain.")]
    public GameObject bloodGroundEffect;

    [Min(0f)]
    [Tooltip("0 = never destroy the ground stain.")]
    public float bloodGroundLifetime = 0f;

    [Min(0.1f)]
    [Tooltip("How long the ground particle remains alive.")]
    public float groundParticleLifetime = 30f;

    [Min(0.01f)]
    [Tooltip("World scale of the spawned BloodSplat.")]
    public float groundScale = 1.5f;

    [Tooltip("Force horizontal billboard rendering.")]
    public bool forceGroundHorizontalBillboard = true;

    [Tooltip("Prevent ground particles from moving.")]
    public bool freezeGroundParticles = true;

    [Tooltip("Disable gravity on ground particles.")]
    public bool disableGroundGravity = true;

    [Tooltip("Disable all original particle behaviour before emitting.")]
    public bool clearGroundParticlesBeforePlay = true;

    [Tooltip("Random rotation around the floor normal.")]
    public bool randomGroundRotation = true;

    [Tooltip(
        "Disable Size over Lifetime. " +
        "This is important to prevent the stain from growing later."
    )]
    public bool freezeGroundParticleSize = true;

    [Tooltip(
        "Disable all particle modules that can create movement or delayed effects."
    )]
    public bool disableGroundParticleMotion = true;

    [Tooltip(
        "Disable sub emitters on the ground BloodSplat."
    )]
    public bool disableGroundSubEmitters = true;


    // =========================================================
    // GROUND SEARCH
    // =========================================================

    [Header("Ground Search")]

    [Min(0.01f)]
    [Tooltip("Small offset above the floor to prevent z-fighting.")]
    public float groundOffset = 0.015f;

    [Min(0.1f)]
    [Tooltip("Height above the search point where the ray begins.")]
    public float groundRayStartHeight = 1.0f;

    [Min(1f)]
    public float groundRayDistance = 100f;

    [Tooltip("Layers used for ground raycasts.")]
    public LayerMask groundLayers = ~0;

    [Tooltip("If selected layers fail, search all layers.")]
    public bool fallbackToAllLayers = true;


    // =========================================================
    // MELEE GROUND SEARCH
    // =========================================================

    [Header("Melee Ground Search")]

    [Tooltip(
        "Use the bottom of the victim bounds as the first ground search."
    )]
    public bool useVictimBottomRay = true;

    [Tooltip(
        "Use the center of the victim as a secondary search."
    )]
    public bool useVictimCenterRay = true;

    [Tooltip(
        "Search several positions around the victim."
    )]
    public bool useMultiPointGroundSearch = true;

    [Min(0.05f)]
    public float groundSearchRadius = 0.35f;

    [Min(1)]
    public int groundSearchPoints = 8;


    // =========================================================
    // FLOOR FILTER
    // =========================================================

    [Header("Floor Filter")]

    [Range(0f, 1f)]
    [Tooltip(
        "Minimum dot product between floor normal and Vector3.up."
    )]
    public float minimumFloorNormal = 0.65f;

    [Tooltip(
        "Reject walls and steep surfaces."
    )]
    public bool onlyHorizontalSurfaces = true;


    // =========================================================
    // HIT POINT
    // =========================================================

    [Header("Hit Point")]

    [Tooltip(
        "Vertical offset applied to the visual hit burst."
    )]
    public float hitHeightOffset = 0.25f;

    [Tooltip(
        "Fallback height if no collider can be found."
    )]
    public float fallbackHeight = 1.0f;

    [Tooltip(
        "Try a physical ray from attacker to victim."
    )]
    public bool useRaycastForMeshColliders = true;

    [Min(0.1f)]
    public float victimRaycastDistance = 50f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool debugHitPoint = false;

    public bool debugGroundRay = true;

    public bool debugLogs = true;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // MAIN PUBLIC ENTRY POINT
    //
    // IMPORTANT:
    //
    // Other scripts should call:
    //
    // HitEffectManager.Instance.PlayHitEffects(
    //     victim,
    //     attacker
    // );
    //
    // victim = Health
    // attacker = Transform
    // =========================================================

    public void PlayHitEffects(
        Health victim,
        Transform attacker
    )
    {
        if (victim == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[HIT FX] PlayHitEffects: victim is NULL."
                );
            }

            return;
        }

        Transform victimTransform =
            victim.transform;

        Vector3 hitPosition =
            GetHitPoint(
                victim,
                attacker
            );

        if (debugHitPoint)
        {
            Debug.DrawRay(
                hitPosition,
                Vector3.up * 0.5f,
                Color.red,
                3f
            );
        }

        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] DAMAGE FX\n" +
                "Victim: " +
                victim.name +
                "\nHit position: " +
                hitPosition
            );
        }

        // -----------------------------------------------------
        // HIT BURST
        // -----------------------------------------------------

        SpawnBloodHitEffect(
            hitPosition,
            attacker
        );

        // -----------------------------------------------------
        // GROUND BLOOD
        //
        // This is executed in THIS SAME FRAME.
        // No coroutine.
        // No Invoke.
        // No delay.
        // -----------------------------------------------------

        SpawnBloodGroundEffect(
            hitPosition,
            victimTransform
        );
    }


    // =========================================================
    // PUBLIC COMPATIBILITY METHOD
    //
    // Uses Transform intentionally.
    //
    // This avoids:
    //
    // CS1503:
    // cannot convert from 'Health'
    // to 'UnityEngine.Transform'
    //
    // when this method is called with victim.transform.
    // =========================================================

    public void PlayBloodHit(
        Vector3 hitPosition,
        Transform victim,
        Transform attacker = null
    )
    {
        SpawnBloodHitEffect(
            hitPosition,
            attacker
        );

        SpawnBloodGroundEffect(
            hitPosition,
            victim
        );
    }


    // =========================================================
    // BLOOD HIT BURST
    // =========================================================

    private void SpawnBloodHitEffect(
        Vector3 hitPosition,
        Transform attacker
    )
    {
        if (bloodHitEffect == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[HIT FX] bloodHitEffect is not assigned."
                );
            }

            return;
        }

        Vector3 spawnPosition =
            hitPosition +
            Vector3.up *
            bloodVerticalOffset;


        // -----------------------------------------------------
        // FORWARD OFFSET
        // -----------------------------------------------------

        if (attacker != null &&
            Mathf.Abs(bloodForwardOffset) > 0.0001f)
        {
            Vector3 direction =
                attacker.position -
                hitPosition;

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();

                spawnPosition +=
                    direction *
                    bloodForwardOffset;
            }
        }


        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        Quaternion rotation =
            Quaternion.identity;

        if (rotateBloodToAttacker &&
            attacker != null)
        {
            Vector3 direction =
                attacker.position -
                spawnPosition;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                rotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }


        // -----------------------------------------------------
        // INSTANTIATE
        // -----------------------------------------------------

        GameObject effect =
            Instantiate(
                bloodHitEffect,
                spawnPosition,
                rotation
            );

        if (effect == null)
        {
            return;
        }

        effect.SetActive(true);


        EnableAllRenderers(
            effect
        );


        ConfigureAndPlayHitParticles(
            effect
        );


        if (bloodHitLifetime > 0f)
        {
            Destroy(
                effect,
                bloodHitLifetime
            );
        }


        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] Blood hit spawned immediately at: " +
                spawnPosition
            );
        }
    }


    // =========================================================
    // GROUND BLOOD
    // =========================================================

    private void SpawnBloodGroundEffect(
        Vector3 hitPosition,
        Transform victim
    )
    {
        if (bloodGroundEffect == null)
        {
            Debug.LogError(
                "[HIT FX] bloodGroundEffect is NULL."
            );

            return;
        }

        if (victim == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[HIT FX] Ground spawn cancelled: victim is NULL."
                );
            }

            return;
        }


        RaycastHit groundHit;


        // =====================================================
        // 1. BELOW FEET
        //
        // THIS IS THE MOST IMPORTANT SEARCH FOR MELEE.
        //
        // We do NOT use the chest/head hit as the first choice.
        // =====================================================

        if (useVictimBottomRay)
        {
            Vector3 bottom =
                GetVictimBottomPoint(
                    victim
                );

            if (FindGround(
                bottom,
                victim,
                out groundHit
            ))
            {
                SpawnGroundBloodSplat(
                    groundHit.point,
                    groundHit.normal
                );

                return;
            }
        }


        // =====================================================
        // 2. VICTIM CENTER
        // =====================================================

        if (useVictimCenterRay)
        {
            Vector3 center =
                GetVictimCenterPoint(
                    victim
                );

            if (FindGround(
                center,
                victim,
                out groundHit
            ))
            {
                SpawnGroundBloodSplat(
                    groundHit.point,
                    groundHit.normal
                );

                return;
            }
        }


        // =====================================================
        // 3. HIT POINT
        // =====================================================

        if (FindGround(
            hitPosition,
            victim,
            out groundHit
        ))
        {
            SpawnGroundBloodSplat(
                groundHit.point,
                groundHit.normal
            );

            return;
        }


        // =====================================================
        // 4. MULTI POINT
        // =====================================================

        if (useMultiPointGroundSearch)
        {
            if (FindGroundAroundVictim(
                victim,
                out groundHit
            ))
            {
                SpawnGroundBloodSplat(
                    groundHit.point,
                    groundHit.normal
                );

                return;
            }
        }


        if (debugLogs)
        {
            Debug.LogWarning(
                "[HIT FX] Could not find valid ground."
            );
        }
    }


    // =========================================================
    // FIND GROUND
    // =========================================================

    private bool FindGround(
        Vector3 point,
        Transform victim,
        out RaycastHit selectedHit
    )
    {
        selectedHit = default;


        Vector3 origin =
            point +
            Vector3.up *
            groundRayStartHeight;


        if (debugGroundRay)
        {
            Debug.DrawRay(
                origin,
                Vector3.down * groundRayDistance,
                Color.green,
                2f
            );
        }


        // -----------------------------------------------------
        // USER LAYERS
        // -----------------------------------------------------

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                Vector3.down,
                groundRayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );


        if (TrySelectGroundHit(
            hits,
            victim,
            out selectedHit
        ))
        {
            return true;
        }


        // -----------------------------------------------------
        // ALL LAYERS FALLBACK
        // -----------------------------------------------------

        if (fallbackToAllLayers)
        {
            hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    groundRayDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );


            if (TrySelectGroundHit(
                hits,
                victim,
                out selectedHit
            ))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // SELECT GROUND HIT
    // =========================================================

    private bool TrySelectGroundHit(
        RaycastHit[] hits,
        Transform victim,
        out RaycastHit selectedHit
    )
    {
        selectedHit = default;


        if (hits == null ||
            hits.Length == 0)
        {
            return false;
        }


        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );


        foreach (RaycastHit hit in hits)
        {
            Collider collider =
                hit.collider;


            if (collider == null)
            {
                continue;
            }


            if (!IsValidCollider(
                collider
            ))
            {
                continue;
            }


            // -------------------------------------------------
            // NEVER ACCEPT ZOMBIE AS FLOOR
            // -------------------------------------------------

            if (victim != null &&
                IsColliderPartOfVictim(
                    collider,
                    victim
                ))
            {
                continue;
            }


            // -------------------------------------------------
            // FLOOR NORMAL
            // -------------------------------------------------

            Vector3 normal =
                hit.normal;


            if (normal.sqrMagnitude < 0.001f)
            {
                continue;
            }


            normal.Normalize();


            float upDot =
                Vector3.Dot(
                    normal,
                    Vector3.up
                );


            if (onlyHorizontalSurfaces &&
                upDot < minimumFloorNormal)
            {
                continue;
            }


            selectedHit =
                hit;


            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] GROUND FOUND\n" +
                    "Collider: " +
                    collider.name +
                    "\nPoint: " +
                    hit.point +
                    "\nNormal: " +
                    normal +
                    "\nUpDot: " +
                    upDot
                );
            }


            return true;
        }


        return false;
    }


    // =========================================================
    // MULTI POINT SEARCH
    // =========================================================

    private bool FindGroundAroundVictim(
        Transform victim,
        out RaycastHit selectedHit
    )
    {
        selectedHit = default;


        if (victim == null)
        {
            return false;
        }


        Vector3 center =
            GetVictimCenterPoint(
                victim
            );


        // CENTER

        if (FindGround(
            center,
            victim,
            out selectedHit
        ))
        {
            return true;
        }


        int count =
            Mathf.Max(
                1,
                groundSearchPoints
            );


        float radius =
            Mathf.Max(
                0.05f,
                groundSearchRadius
            );


        for (int i = 0; i < count; i++)
        {
            float angle =
                (360f / count) *
                i;


            float radians =
                angle *
                Mathf.Deg2Rad;


            Vector3 offset =
                new Vector3(
                    Mathf.Cos(radians),
                    0f,
                    Mathf.Sin(radians)
                ) *
                radius;


            Vector3 point =
                center +
                offset;


            if (FindGround(
                point,
                victim,
                out selectedHit
            ))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // SPAWN GROUND SPLAT
    // =========================================================

    private void SpawnGroundBloodSplat(
        Vector3 position,
        Vector3 normal
    )
    {
        if (bloodGroundEffect == null)
        {
            return;
        }


        // -----------------------------------------------------
        // NORMAL
        // -----------------------------------------------------

        if (normal.sqrMagnitude < 0.001f)
        {
            normal =
                Vector3.up;
        }


        normal.Normalize();


        // -----------------------------------------------------
        // FLOOR OFFSET
        // -----------------------------------------------------

        position +=
            normal *
            groundOffset;


        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        Quaternion rotation =
            Quaternion.FromToRotation(
                Vector3.up,
                normal
            );


        if (randomGroundRotation)
        {
            Quaternion randomRotation =
                Quaternion.AngleAxis(
                    Random.Range(
                        0f,
                        360f
                    ),
                    normal
                );


            rotation =
                randomRotation *
                rotation;
        }


        // =====================================================
        // CREATE NOW
        // =====================================================

        GameObject splat =
            Instantiate(
                bloodGroundEffect,
                position,
                rotation
            );


        if (splat == null)
        {
            return;
        }


        splat.name =
            bloodGroundEffect.name +
            "_GroundRuntime";


        splat.SetActive(true);


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Do NOT parent this to the zombie.
        //
        // The splat must stay at the world position.
        // -----------------------------------------------------

        splat.transform.SetParent(
            null,
            true
        );


        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        splat.transform.localScale =
            Vector3.one *
            groundScale;


        // -----------------------------------------------------
        // PARTICLES
        // -----------------------------------------------------

        ParticleSystem[] systems =
            splat.GetComponentsInChildren<ParticleSystem>(
                true
            );


        if (systems == null ||
            systems.Length == 0)
        {
            Debug.LogError(
                "[HIT FX] BloodSplat contains no ParticleSystem."
            );

            Destroy(
                splat
            );

            return;
        }


        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
            {
                continue;
            }


            ConfigureGroundParticle(
                ps
            );
        }


        EnableAllRenderers(
            splat
        );


        if (debugGroundRay)
        {
            Debug.DrawRay(
                position,
                normal * 0.5f,
                Color.magenta,
                5f
            );
        }


        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] GROUND SPLAT CREATED IMMEDIATELY\n" +
                "Prefab: " +
                bloodGroundEffect.name +
                "\nPosition: " +
                position +
                "\nNormal: " +
                normal
            );
        }


        if (bloodGroundLifetime > 0f)
        {
            Destroy(
                splat,
                bloodGroundLifetime
            );
        }
    }


    // =========================================================
    // CONFIGURE GROUND PARTICLE
    //
    // THIS IS THE IMPORTANT PART.
    //
    // We completely remove the systems responsible for:
    //
    // - delayed appearance
    // - growth
    // - movement
    // - acceleration
    // - noise
    // - gravity
    // - secondary particles
    // =========================================================

    private void ConfigureGroundParticle(
        ParticleSystem ps
    )
    {
        if (ps == null)
        {
            return;
        }


        ps.gameObject.SetActive(true);


        // -----------------------------------------------------
        // STOP PREFAB
        // -----------------------------------------------------

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );


        ps.Clear(
            true
        );


        // =====================================================
        // MAIN
        // =====================================================

        ParticleSystem.MainModule main =
            ps.main;


        // NO DELAY

        main.startDelay =
            0f;


        // ONE SHOT

        main.loop =
            false;


        // NO AUTO PLAY

        main.playOnAwake =
            false;


        // NO PREWARM

        main.prewarm =
            false;


        // PARTICLE LIFETIME

        main.startLifetime =
            Mathf.Max(
                0.1f,
                groundParticleLifetime
            );


        // -----------------------------------------------------
        // SPEED
        // -----------------------------------------------------

        if (freezeGroundParticles)
        {
            main.startSpeed =
                0f;
        }


        // -----------------------------------------------------
        // GRAVITY
        // -----------------------------------------------------

        if (disableGroundGravity)
        {
            main.gravityModifier =
                0f;
        }


        // -----------------------------------------------------
        // WORLD SPACE
        //
        // Particle will NOT follow the zombie.
        // -----------------------------------------------------

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;


        // =====================================================
        // EMISSION
        // =====================================================

        ParticleSystem.EmissionModule emission =
            ps.emission;


        emission.enabled =
            false;


        // =====================================================
        // SHAPE
        // =====================================================

        ParticleSystem.ShapeModule shape =
            ps.shape;


        shape.enabled =
            false;


        // =====================================================
        // SIZE OVER LIFETIME
        //
        // THIS PREVENTS:
        //
        // small stain -> grows -> huge stain
        //
        // after several seconds.
        // =====================================================

        if (freezeGroundParticleSize)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
                ps.sizeOverLifetime;


            sizeOverLifetime.enabled =
                false;
        }


        // =====================================================
        // VELOCITY OVER LIFETIME
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity =
                ps.velocityOverLifetime;


            velocity.enabled =
                false;
        }


        // =====================================================
        // FORCE OVER LIFETIME
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.ForceOverLifetimeModule force =
                ps.forceOverLifetime;


            force.enabled =
                false;
        }


        // =====================================================
        // NOISE
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.NoiseModule noise =
                ps.noise;


            noise.enabled =
                false;
        }


        // =====================================================
        // LIMIT VELOCITY
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity =
                ps.limitVelocityOverLifetime;


            limitVelocity.enabled =
                false;
        }


        // =====================================================
        // COLLISION
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.CollisionModule collision =
                ps.collision;


            collision.enabled =
                false;
        }


        // =====================================================
        // TRIGGER
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.TriggerModule trigger =
                ps.trigger;


            trigger.enabled =
                false;
        }


        // =====================================================
        // INHERIT VELOCITY
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.InheritVelocityModule inheritVelocity =
                ps.inheritVelocity;


            inheritVelocity.enabled =
                false;
        }


        // =====================================================
        // EXTERNAL FORCES
        // =====================================================

        if (disableGroundParticleMotion)
        {
            ParticleSystem.ExternalForcesModule externalForces =
                ps.externalForces;


            externalForces.enabled =
                false;
        }


        // =====================================================
        // TRAILS
        // =====================================================

        ParticleSystem.TrailModule trails =
            ps.trails;


        trails.enabled =
            false;


        // =====================================================
        // SUB EMITTERS
        //
        // This is extremely important if BloodFX has
        // additional particle systems attached to the
        // original BloodSplat.
        //
        // They can otherwise appear several seconds later.
        // =====================================================

        if (disableGroundSubEmitters)
        {
            ParticleSystem.SubEmittersModule subEmitters =
                ps.subEmitters;


            subEmitters.enabled =
                false;
        }


        // =====================================================
        // RENDERER
        // =====================================================

        ParticleSystemRenderer renderer =
            ps.GetComponent<ParticleSystemRenderer>();


        if (renderer != null)
        {
            renderer.enabled =
                true;


            if (forceGroundHorizontalBillboard)
            {
                renderer.renderMode =
                    ParticleSystemRenderMode.HorizontalBillboard;
            }


            renderer.sortMode =
                ParticleSystemSortMode.Distance;
        }


        // =====================================================
        // EMIT EXACTLY ONE PARTICLE
        //
        // EmitParams ensures the particle is created NOW.
        // =====================================================

        ParticleSystem.EmitParams emitParams =
            new ParticleSystem.EmitParams();


        emitParams.startLifetime =
            Mathf.Max(
                0.1f,
                groundParticleLifetime
            );


        if (freezeGroundParticles)
        {
            emitParams.velocity =
                Vector3.zero;
        }


        ps.Emit(
            emitParams,
            1
        );


        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] Ground particle emitted NOW: " +
                ps.name
            );
        }
    }


    // =========================================================
    // GET VICTIM CENTER
    // =========================================================

    private Vector3 GetVictimCenterPoint(
        Transform victim
    )
    {
        if (victim == null)
        {
            return Vector3.zero;
        }


        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );


        bool found =
            false;


        Bounds bounds =
            new Bounds(
                victim.position,
                Vector3.zero
            );


        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(
                collider
            ))
            {
                continue;
            }


            if (!found)
            {
                bounds =
                    collider.bounds;

                found =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    collider.bounds
                );
            }
        }


        if (found)
        {
            return bounds.center;
        }


        return victim.position;
    }


    // =========================================================
    // GET VICTIM BOTTOM
    // =========================================================

    private Vector3 GetVictimBottomPoint(
        Transform victim
    )
    {
        if (victim == null)
        {
            return Vector3.zero;
        }


        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );


        bool found =
            false;


        Bounds bounds =
            new Bounds(
                victim.position,
                Vector3.zero
            );


        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(
                collider
            ))
            {
                continue;
            }


            if (!found)
            {
                bounds =
                    collider.bounds;

                found =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    collider.bounds
                );
            }
        }


        if (found)
        {
            Vector3 bottom =
                bounds.center;


            bottom.y =
                bounds.min.y;


            return bottom;
        }


        return victim.position;
    }


    // =========================================================
    // GET HIT POINT
    // =========================================================

    public Vector3 GetHitPoint(
        Health victim,
        Transform attacker
    )
    {
        if (victim == null)
        {
            return attacker != null
                ? attacker.position
                : Vector3.zero;
        }


        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );


        if (colliders == null ||
            colliders.Length == 0)
        {
            return GetFallbackHitPoint(
                victim,
                attacker
            );
        }


        // =====================================================
        // RAYCAST FROM ATTACKER
        // =====================================================

        if (useRaycastForMeshColliders &&
            attacker != null)
        {
            Vector3 raycastPoint =
                TryGetVictimRaycastHitPoint(
                    victim,
                    attacker
                );


            if (raycastPoint != Vector3.zero)
            {
                return raycastPoint +
                       Vector3.up *
                       hitHeightOffset;
            }
        }


        // =====================================================
        // CLOSEST COLLIDER POINT
        // =====================================================

        Vector3 attackerPosition =
            attacker != null
                ? attacker.position
                : victim.transform.position;


        Vector3 closestPoint =
            victim.transform.position;


        float closestDistance =
            float.MaxValue;


        bool foundPoint =
            false;


        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(
                collider
            ))
            {
                continue;
            }


            if (!CanUseClosestPoint(
                collider
            ))
            {
                continue;
            }


            Vector3 point;


            try
            {
                point =
                    collider.ClosestPoint(
                        attackerPosition
                    );
            }
            catch
            {
                continue;
            }


            float distance =
                (
                    attackerPosition -
                    point
                ).sqrMagnitude;


            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;


                closestPoint =
                    point;


                foundPoint =
                    true;
            }
        }


        if (foundPoint)
        {
            return closestPoint +
                   Vector3.up *
                   hitHeightOffset;
        }


        return GetFallbackHitPoint(
            victim,
            attacker
        );
    }


    // =========================================================
    // RAYCAST VICTIM
    // =========================================================

    private Vector3 TryGetVictimRaycastHitPoint(
        Health victim,
        Transform attacker
    )
    {
        if (victim == null ||
            attacker == null)
        {
            return Vector3.zero;
        }


        Vector3 target =
            GetVictimCenterPoint(
                victim.transform
            );


        Vector3 direction =
            target -
            attacker.position;


        if (direction.sqrMagnitude < 0.001f)
        {
            direction =
                attacker.forward;
        }


        direction.Normalize();


        RaycastHit[] hits =
            Physics.RaycastAll(
                attacker.position,
                direction,
                victimRaycastDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );


        if (hits == null ||
            hits.Length == 0)
        {
            return Vector3.zero;
        }


        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );


        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }


            if (!IsValidCollider(
                hit.collider
            ))
            {
                continue;
            }


            if (IsColliderPartOfVictim(
                hit.collider,
                victim.transform
            ))
            {
                return hit.point;
            }
        }


        return Vector3.zero;
    }


    // =========================================================
    // VALID COLLIDER
    // =========================================================

    private bool IsValidCollider(
        Collider collider
    )
    {
        if (collider == null)
        {
            return false;
        }


        if (!collider.enabled)
        {
            return false;
        }


        if (!collider.gameObject.activeInHierarchy)
        {
            return false;
        }


        if (collider.isTrigger)
        {
            return false;
        }


        return true;
    }


    // =========================================================
    // CLOSEST POINT SUPPORT
    // =========================================================

    private bool CanUseClosestPoint(
        Collider collider
    )
    {
        if (collider == null)
        {
            return false;
        }


        if (collider is BoxCollider)
        {
            return true;
        }


        if (collider is SphereCollider)
        {
            return true;
        }


        if (collider is CapsuleCollider)
        {
            return true;
        }


        MeshCollider meshCollider =
            collider as MeshCollider;


        if (meshCollider != null)
        {
            return meshCollider.convex;
        }


        return false;
    }


    // =========================================================
    // COLLIDER PART OF VICTIM
    // =========================================================

    private bool IsColliderPartOfVictim(
        Collider collider,
        Transform victim
    )
    {
        if (collider == null ||
            victim == null)
        {
            return false;
        }


        Transform colliderTransform =
            collider.transform;


        if (colliderTransform == victim)
        {
            return true;
        }


        if (colliderTransform.IsChildOf(
            victim
        ))
        {
            return true;
        }


        if (victim.IsChildOf(
            colliderTransform
        ))
        {
            return true;
        }


        return false;
    }


    // =========================================================
    // FALLBACK HIT POINT
    // =========================================================

    private Vector3 GetFallbackHitPoint(
        Health victim,
        Transform attacker
    )
    {
        if (victim == null)
        {
            return attacker != null
                ? attacker.position
                : Vector3.zero;
        }


        Vector3 center =
            GetVictimCenterPoint(
                victim.transform
            );


        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );


        bool foundBounds =
            false;


        Bounds bounds =
            new Bounds(
                victim.transform.position,
                Vector3.zero
            );


        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(
                collider
            ))
            {
                continue;
            }


            if (!foundBounds)
            {
                bounds =
                    collider.bounds;

                foundBounds =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    collider.bounds
                );
            }
        }


        if (foundBounds)
        {
            Vector3 attackerPosition =
                attacker != null
                    ? attacker.position
                    : center;


            Vector3 direction =
                attackerPosition -
                center;


            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();


                Vector3 point =
                    center +
                    Vector3.Scale(
                        direction,
                        bounds.extents
                    );


                point +=
                    Vector3.up *
                    hitHeightOffset;


                return point;
            }


            center.y +=
                hitHeightOffset;


            return center;
        }


        Vector3 fallback =
            victim.transform.position;


        fallback.y +=
            fallbackHeight;


        return fallback;
    }


    // =========================================================
    // CONFIGURE HIT PARTICLES
    //
    // Here we also remove delayed behaviour from the HIT BURST.
    // =========================================================

    private void ConfigureAndPlayHitParticles(
        GameObject effect
    )
    {
        if (effect == null)
        {
            return;
        }


        ParticleSystem[] systems =
            effect.GetComponentsInChildren<ParticleSystem>(
                true
            );


        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
            {
                continue;
            }


            ps.gameObject.SetActive(true);


            ParticleSystem.MainModule main =
                ps.main;


            // -------------------------------------------------
            // NO DELAY
            // -------------------------------------------------

            main.startDelay =
                0f;


            // -------------------------------------------------
            // NO LOOP
            // -------------------------------------------------

            main.loop =
                false;


            // -------------------------------------------------
            // NO AUTO PLAY
            // -------------------------------------------------

            main.playOnAwake =
                false;


            // -------------------------------------------------
            // NO PREWARM
            // -------------------------------------------------

            main.prewarm =
                false;


            // -------------------------------------------------
            // FORCE WHITE
            //
            // Prevents a black Start Color from the prefab.
            // -------------------------------------------------

            if (forceHitParticleWhite)
            {
                main.startColor =
                    Color.white;
            }


            // -------------------------------------------------
            // CLEAR OLD PARTICLES
            // -------------------------------------------------

            if (clearParticlesBeforePlay)
            {
                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );


                ps.Clear(
                    true
                );
            }


            // -------------------------------------------------
            // PLAY NOW
            // -------------------------------------------------

            if (forcePlayParticleSystems)
            {
                ps.Play(
                    true
                );
            }


            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Hit particle played NOW: " +
                    ps.name
                );
            }
        }
    }


    // =========================================================
    // ENABLE ALL RENDERERS
    // =========================================================

    private void EnableAllRenderers(
        GameObject effect
    )
    {
        if (effect == null)
        {
            return;
        }


        Renderer[] renderers =
            effect.GetComponentsInChildren<Renderer>(
                true
            );


        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }


            renderer.gameObject.SetActive(true);


            renderer.enabled =
                true;
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}