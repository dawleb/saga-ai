using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance;

    // =========================================================
    // BLOOD HIT
    // =========================================================

    [Header("Blood Hit")]
    [Tooltip("Prefab BloodSplat / efekt krwi na trafionym przeciwniku.")]
    public GameObject bloodHitEffect;

    [Min(0f)]
    public float bloodHitLifetime = 1.5f;

    public bool rotateBloodToAttacker = false;

    public float bloodVerticalOffset = 0.30f;
    public float bloodForwardOffset = 0.03f;

    public bool forcePlayParticleSystems = true;
    public bool clearParticlesBeforePlay = true;

    // =========================================================
    // BLOOD GROUND
    // =========================================================

    [Header("Blood Ground")]
    [Tooltip(
        "TU PRZYPISZ PRAWDZIWY BloodSplat.prefab z BloodFX/Prefabs. " +
        "Nie Decal Projector."
    )]
    public GameObject bloodGroundEffect;

    [Min(0f)]
    [Tooltip(
        "0 = plama nie jest niszczona przez ten skrypt. " +
        "Dla prawdziwego BloodSplat prefab jest to zalecane."
    )]
    public float bloodGroundLifetime = 0f;

    [Tooltip(
        "Jak długo cząstki BloodSplat mają być widoczne na ziemi."
    )]
    [Min(0.1f)]
    public float groundParticleLifetime = 30f;

    [Tooltip(
        "Skala plamy na ziemi."
    )]
    [Min(0.01f)]
    public float groundScale = 1.5f;

    [Tooltip(
        "Wymusza poziomy billboard, dzięki czemu BloodSplat leży na podłodze."
    )]
    public bool forceGroundHorizontalBillboard = true;

    [Tooltip(
        "Wyłącza prędkość cząstek dla wersji używanej jako plama na ziemi."
    )]
    public bool freezeGroundParticles = true;

    [Tooltip(
        "Wymusza brak grawitacji dla plamy."
    )]
    public bool disableGroundGravity = true;

    [Tooltip(
        "Jeżeli prefab ma istniejące cząstki, usuwa je i tworzy je ponownie."
    )]
    public bool clearGroundParticlesBeforePlay = true;

    [Tooltip(
        "Losowo obraca plamę wokół normalnej podłoża."
    )]
    public bool randomGroundRotation = true;

    // =========================================================
    // GROUND RAYCAST
    // =========================================================

    [Header("Ground Raycast")]

    [Min(0f)]
    public float groundRayStartHeight = 1.5f;

    [Min(0f)]
    public float groundOffset = 0.02f;

    [Min(0.1f)]
    public float groundRayDistance = 50f;

    public LayerMask groundLayers = ~0;

    public bool fallbackToAllLayers = true;

    public bool useHitPointGroundRay = true;
    public bool useVictimBottomRay = true;
    public bool useVictimCenterRay = true;

    // =========================================================
    // FLOOR FILTER
    // =========================================================

    [Header("Floor Filter")]

    [Range(0f, 1f)]
    public float minimumFloorNormal = 0.7f;

    public bool onlyHorizontalSurfaces = true;

    // =========================================================
    // HIT POINT
    // =========================================================

    [Header("Hit Point")]

    public float hitHeightOffset = 0.25f;

    public float fallbackHeight = 1.0f;

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
    // PUBLIC
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
    // BLOOD HIT
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
                    "[HIT FX] bloodHitEffect == NULL."
                );
            }

            return;
        }

        Vector3 spawnPosition =
            hitPosition +
            Vector3.up *
            bloodVerticalOffset;

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

        GameObject effect =
            Instantiate(
                bloodHitEffect,
                spawnPosition,
                rotation
            );

        if (effect == null)
        {
            Debug.LogError(
                "[HIT FX] Nie udało się utworzyć Blood HIT."
            );

            return;
        }

        effect.SetActive(true);

        EnableAllRenderers(effect);

        PlayAllParticleSystems(
            effect,
            false
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
                "[HIT FX] Blood HIT spawned: " +
                spawnPosition
            );
        }
    }

    // =========================================================
    // BLOOD GROUND
    // =========================================================

    private void SpawnBloodGroundEffect(
        Vector3 hitPosition,
        Transform victim
    )
    {
        if (bloodGroundEffect == null)
        {
            Debug.LogError(
                "[HIT FX] bloodGroundEffect == NULL. " +
                "Przypisz tutaj BloodSplat.prefab."
            );

            return;
        }

        RaycastHit groundHit;

        // -----------------------------------------------------
        // 1. HIT POINT
        // -----------------------------------------------------

        if (useHitPointGroundRay)
        {
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
        }

        // -----------------------------------------------------
        // 2. VICTIM BOTTOM
        // -----------------------------------------------------

        if (useVictimBottomRay &&
            victim != null)
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

        // -----------------------------------------------------
        // 3. VICTIM CENTER
        // -----------------------------------------------------

        if (useVictimCenterRay &&
            victim != null)
        {
            if (FindGround(
                victim.position,
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

        Debug.LogWarning(
            "[HIT FX] Nie znaleziono podłoża dla BloodSplat."
        );
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
                Vector3.down *
                groundRayDistance,
                Color.green,
                3f
            );
        }

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
    // SELECT GROUND
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
            if (hit.collider == null)
                continue;

            if (!hit.collider.enabled)
                continue;

            if (!hit.collider.gameObject.activeInHierarchy)
                continue;

            if (hit.collider.isTrigger)
                continue;

            if (victim != null &&
                IsColliderPartOfVictim(
                    hit.collider,
                    victim
                ))
            {
                continue;
            }

            Vector3 normal =
                hit.normal;

            if (normal.sqrMagnitude < 0.001f)
                continue;

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
                    "[HIT FX] GROUND FOUND: " +
                    hit.collider.name +
                    " | point=" +
                    hit.point +
                    " | normal=" +
                    hit.normal +
                    " | upDot=" +
                    upDot
                );
            }

            return true;
        }

        return false;
    }

    // =========================================================
    // REAL BLOODSPLAT PREFAB
    // =========================================================

    private void SpawnGroundBloodSplat(
        Vector3 position,
        Vector3 normal
    )
    {
        if (bloodGroundEffect == null)
        {
            Debug.LogError(
                "[HIT FX] BloodSplat prefab jest NULL."
            );

            return;
        }

        if (normal.sqrMagnitude <
            0.001f)
        {
            normal =
                Vector3.up;
        }

        normal.Normalize();

        // -----------------------------------------------------
        // LEKKIE ODSUNIĘCIE OD PODŁOGI
        // -----------------------------------------------------

        position +=
            normal *
            groundOffset;

        // -----------------------------------------------------
        // ROTACJA
        //
        // BloodSplat z repo jest Particle Systemem.
        // Nie potrzebujemy Decal Projectora.
        //
        // Renderer jest później ustawiany jako
        // HorizontalBillboard.
        // -----------------------------------------------------

        Quaternion rotation =
            Quaternion.FromToRotation(
                Vector3.up,
                normal
            );

        if (randomGroundRotation)
        {
            Quaternion random =
                Quaternion.AngleAxis(
                    Random.Range(
                        0f,
                        360f
                    ),
                    normal
                );

            rotation =
                random *
                rotation;
        }

        // -----------------------------------------------------
        // INSTANTIATE
        // -----------------------------------------------------

        GameObject splat =
            Instantiate(
                bloodGroundEffect,
                position,
                rotation
            );

        if (splat == null)
        {
            Debug.LogError(
                "[HIT FX] BloodSplat Instantiate FAILED."
            );

            return;
        }

        splat.name =
            bloodGroundEffect.name +
            "_GroundRuntime";

        splat.SetActive(true);

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        splat.transform.localScale =
            Vector3.one *
            groundScale;

        // -----------------------------------------------------
        // PARTICLE SYSTEMS
        // -----------------------------------------------------

        ParticleSystem[] systems =
            splat.GetComponentsInChildren<ParticleSystem>(
                true
            );

        if (systems == null ||
            systems.Length == 0)
        {
            Debug.LogError(
                "[HIT FX] BloodSplat prefab NIE MA ParticleSystem."
            );

            Destroy(splat);

            return;
        }

        // -----------------------------------------------------
        // CONFIGURE PARTICLES
        // -----------------------------------------------------

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
                continue;

            ps.gameObject.SetActive(true);

            ParticleSystem.MainModule main =
                ps.main;

            // ---------------------------------------------
            // BLOODSPLAT Z REPO MA OKOŁO 0.4-0.5s
            //
            // Tutaj celowo wydłużamy życie cząstek,
            // żeby ground splat nie znikał po chwili.
            // ---------------------------------------------

            main.startLifetime =
                groundParticleLifetime;

            // ---------------------------------------------
            // BRAK GRAWITACJI
            // ---------------------------------------------

            if (disableGroundGravity)
            {
                main.gravityModifier =
                    0f;
            }

            // ---------------------------------------------
            // BRAK RUCHU
            // ---------------------------------------------

            if (freezeGroundParticles)
            {
                main.startSpeed =
                    0f;
            }

            // ---------------------------------------------
            // EMISSION
            //
            // Nie zmieniamy tekstury ani materiału.
            // Używamy konfiguracji prawdziwego BloodSplat.
            // ---------------------------------------------

            if (clearGroundParticlesBeforePlay)
            {
                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                ps.Clear(true);
            }

            // ---------------------------------------------
            // RENDERER
            // ---------------------------------------------

            ParticleSystemRenderer renderer =
                ps.GetComponent<ParticleSystemRenderer>();

            if (renderer != null)
            {
                if (forceGroundHorizontalBillboard)
                {
                    renderer.renderMode =
                        ParticleSystemRenderMode.HorizontalBillboard;
                }

                renderer.enabled = true;

                renderer.gameObject.SetActive(true);

                renderer.sortMode =
                    ParticleSystemSortMode.Distance;
            }

            // ---------------------------------------------
            // PLAY
            // ---------------------------------------------

            ps.Play(true);

            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] GROUND BLOODSPLAT PLAY: " +
                    ps.name +
                    " | lifetime=" +
                    groundParticleLifetime +
                    " | startSpeed=0"
                );
            }
        }

        // -----------------------------------------------------
        // RENDERERS
        // -----------------------------------------------------

        EnableAllRenderers(
            splat
        );

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (debugGroundRay)
        {
            Debug.DrawRay(
                position,
                normal * 0.5f,
                Color.magenta,
                10f
            );
        }

        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] BLOODSPLAT GROUND CREATED\n" +
                "Prefab: " +
                bloodGroundEffect.name +
                "\nObject: " +
                splat.name +
                "\nPosition: " +
                position +
                "\nNormal: " +
                normal +
                "\nScale: " +
                groundScale +
                "\nParticle lifetime: " +
                groundParticleLifetime
            );
        }

        // -----------------------------------------------------
        // OPTIONAL DESTROY
        // -----------------------------------------------------

        if (bloodGroundLifetime > 0f)
        {
            Destroy(
                splat,
                bloodGroundLifetime
            );
        }
    }

    // =========================================================
    // VICTIM BOTTOM
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

        bool foundBounds =
            false;

        Bounds bounds =
            new Bounds(
                victim.position,
                Vector3.zero
            );

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
                continue;

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
            Vector3 bottom =
                bounds.center;

            bottom.y =
                bounds.min.y;

            return bottom;
        }

        return victim.position;
    }

    // =========================================================
    // HIT POINT
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
                if (debugHitPoint)
                {
                    Debug.DrawRay(
                        raycastPoint,
                        Vector3.up * 0.5f,
                        Color.red,
                        3f
                    );
                }

                return raycastPoint +
                       Vector3.up *
                       hitHeightOffset;
            }
        }

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
            if (!IsValidCollider(collider))
                continue;

            if (!CanUseClosestPoint(collider))
                continue;

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
            if (debugHitPoint)
            {
                Debug.DrawRay(
                    closestPoint,
                    Vector3.up * 0.5f,
                    Color.red,
                    3f
                );
            }

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
    // VALID COLLIDER
    // =========================================================

    private bool IsValidCollider(
        Collider collider
    )
    {
        if (collider == null)
            return false;

        if (!collider.enabled)
            return false;

        if (!collider.gameObject.activeInHierarchy)
            return false;

        if (collider.isTrigger)
            return false;

        return true;
    }

    // =========================================================
    // CLOSEST POINT
    // =========================================================

    private bool CanUseClosestPoint(
        Collider collider
    )
    {
        if (collider == null)
            return false;

        if (collider is BoxCollider)
            return true;

        if (collider is SphereCollider)
            return true;

        if (collider is CapsuleCollider)
            return true;

        MeshCollider meshCollider =
            collider as MeshCollider;

        if (meshCollider != null)
        {
            return meshCollider.convex;
        }

        return false;
    }

    // =========================================================
    // VICTIM RAYCAST
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
            victim.transform.position;

        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );

        bool foundBounds =
            false;

        Bounds bounds =
            new Bounds(
                target,
                Vector3.zero
            );

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
                continue;

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
            target =
                bounds.center;
        }

        Vector3 direction =
            target -
            attacker.position;

        if (direction.sqrMagnitude <
            0.001f)
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
                continue;

            if (!IsValidCollider(
                hit.collider
            ))
            {
                continue;
            }

            if (IsColliderPartOfVictim(
                hit.collider,
                victim
            ))
            {
                return hit.point;
            }
        }

        return Vector3.zero;
    }

    // =========================================================
    // VICTIM COLLIDER
    // =========================================================

    private bool IsColliderPartOfVictim(
        Collider collider,
        Health victim
    )
    {
        if (victim == null)
            return false;

        return IsColliderPartOfVictim(
            collider,
            victim.transform
        );
    }

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
            return true;

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
            if (!IsValidCollider(collider))
                continue;

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
            Vector3 center =
                bounds.center;

            Vector3 attackerPosition =
                attacker != null
                    ? attacker.position
                    : center;

            Vector3 direction =
                attackerPosition -
                center;

            if (direction.sqrMagnitude >
                0.001f)
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
    // PUBLIC HIT EFFECT
    // =========================================================

    public void PlayHitEffects(
        Health victim,
        Transform attacker
    )
    {
        if (victim == null)
        {
            Debug.LogWarning(
                "[HIT FX] Victim is null."
            );

            return;
        }

        Vector3 hitPosition =
            GetHitPoint(
                victim,
                attacker
            );

        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] Calculated hit position: " +
                hitPosition
            );
        }

        PlayBloodHit(
            hitPosition,
            victim.transform,
            attacker
        );
    }

    // =========================================================
    // PLAY PARTICLES
    // =========================================================

    private void PlayAllParticleSystems(
        GameObject effect,
        bool ground
    )
    {
        if (effect == null)
            return;

        ParticleSystem[] systems =
            effect.GetComponentsInChildren<ParticleSystem>(
                true
            );

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
                continue;

            ps.gameObject.SetActive(true);

            if (clearParticlesBeforePlay)
            {
                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                ps.Clear(true);
            }

            if (forcePlayParticleSystems)
            {
                ps.Play(true);
            }

            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Playing particle: " +
                    ps.name +
                    " | ground=" +
                    ground
                );
            }
        }
    }

    // =========================================================
    // RENDERERS
    // =========================================================

    private void EnableAllRenderers(
        GameObject effect
    )
    {
        if (effect == null)
            return;

        Renderer[] renderers =
            effect.GetComponentsInChildren<Renderer>(
                true
            );

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.gameObject.SetActive(true);
            renderer.enabled = true;
        }

        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] Enabled renderers: " +
                renderers.Length
            );
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