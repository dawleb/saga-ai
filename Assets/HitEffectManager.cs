using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance;

    // =========================================================
    // BLOOD HIT
    // =========================================================

    [Header("Blood Hit")]
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
    public GameObject bloodGroundEffect;

    [Min(0f)]
    public float bloodGroundLifetime = 0f;

    [Min(0f)]
    public float groundRayStartHeight = 1.5f;

    [Min(0f)]
    public float groundOffset = 0.015f;

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
    // BLOOD SPLAT ROTATION
    // =========================================================

    [Header("Blood Splat Rotation")]

    [Tooltip(
        "Dla oryginalnego BloodFX nie ma większego znaczenia, " +
        "ponieważ BloodSplat jest Particle Systemem. " +
        "Zostaw TRUE."
    )]
    public bool particleUsesForwardAsNormal = true;

    public bool randomGroundRotation = true;

    // =========================================================
    // BLOOD GROUND SCALE
    // =========================================================

    [Header("Blood Ground Scale")]

    public bool overrideGroundScale = false;

    public Vector3 groundScale = Vector3.one;

    [Min(0.01f)]
    public float groundVisualMultiplier = 1.5f;

    // =========================================================
    // VISUAL FIX
    // =========================================================

    [Header("Visual Fix")]

    public bool preventZeroScale = true;

    [Min(0.0001f)]
    public float minimumVisualScale = 0.001f;

    public bool enableNonParticleRenderers = true;

    // =========================================================
    // BLOOD MATERIAL FIX
    // =========================================================

    [Header("Blood Material Fix")]

    [Tooltip(
        "Automatycznie zastępuje niekompatybilny shader BloodFX " +
        "prostym shaderem Particle/Unlit."
    )]
    public bool fixBloodMaterialAutomatically = true;

    [Tooltip(
        "Wymusza czerwony kolor krwi zamiast koloru z oryginalnego assetu."
    )]
    public bool forceBloodRedColor = true;

    public Color bloodColor =
        new Color(
            0.35f,
            0.005f,
            0.002f,
            1f
        );

    [Range(0.1f, 2f)]
    public float bloodBrightness = 1f;

    [Tooltip(
        "Dla plamy na ziemi ustawia Particle System jako Horizontal Billboard."
    )]
    public bool forceGroundHorizontalBillboard = true;

    [Tooltip(
        "Próbuje użyć tekstury BloodSplat z oryginalnego materiału."
    )]
    public bool copyOriginalBloodTexture = true;

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

    public bool debugRendererInfo = true;

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
            Debug.LogWarning(
                "[HIT FX] Blood Hit Effect is not assigned."
            );

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
                "[HIT FX] Failed to instantiate Blood Hit."
            );

            return;
        }

        effect.SetActive(true);

        if (fixBloodMaterialAutomatically)
        {
            FixBloodMaterials(
                effect,
                false
            );
        }

        EnableAllRenderers(effect);

        PlayAllParticleSystems(effect);

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
                "[HIT FX] Blood HIT spawned at " +
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
            Debug.LogWarning(
                "[HIT FX] Blood Ground Effect is not assigned!"
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
                SpawnGroundMark(
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
                SpawnGroundMark(
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
                SpawnGroundMark(
                    groundHit.point,
                    groundHit.normal
                );

                return;
            }
        }

        Debug.LogWarning(
            "[HIT FX] NIE ZNALEZIONO PODŁOGI - Blood Splat nie został utworzony."
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
                Vector3.down * groundRayDistance,
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
            {
                continue;
            }

            if (!hit.collider.enabled)
            {
                continue;
            }

            if (!hit.collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (hit.collider.isTrigger)
            {
                continue;
            }

            // Nie kładź krwi na przeciwniku.
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

            selectedHit = hit;

            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] FLOOR FOUND: " +
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

        bool foundBounds = false;

        Bounds bounds =
            new Bounds(
                victim.position,
                Vector3.zero
            );

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds =
                    collider.bounds;

                foundBounds = true;
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
    // SPAWN GROUND MARK
    // =========================================================

    private void SpawnGroundMark(
        Vector3 position,
        Vector3 normal
    )
    {
        if (bloodGroundEffect == null)
        {
            Debug.LogWarning(
                "[HIT FX] Blood Ground Effect is null."
            );

            return;
        }

        if (normal.sqrMagnitude < 0.001f)
        {
            normal = Vector3.up;
        }

        normal.Normalize();

        // -----------------------------------------------------
        // OFFSET
        // -----------------------------------------------------

        position +=
            normal *
            groundOffset;

        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        Quaternion rotation =
            Quaternion.identity;

        if (!forceGroundHorizontalBillboard)
        {
            if (particleUsesForwardAsNormal)
            {
                rotation =
                    Quaternion.FromToRotation(
                        Vector3.forward,
                        normal
                    );
            }
            else
            {
                rotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        normal
                    );
            }

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
        }

        // -----------------------------------------------------
        // INSTANTIATE
        // -----------------------------------------------------

        GameObject mark =
            Instantiate(
                bloodGroundEffect,
                position,
                rotation
            );

        if (mark == null)
        {
            Debug.LogError(
                "[HIT FX] Blood Ground Instantiate FAILED!"
            );

            return;
        }

        mark.name =
            bloodGroundEffect.name +
            "_Runtime";

        mark.SetActive(true);

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        if (overrideGroundScale)
        {
            mark.transform.localScale =
                groundScale;
        }
        else
        {
            mark.transform.localScale *=
                groundVisualMultiplier;
        }

        if (preventZeroScale)
        {
            EnsureMinimumScale(mark);
        }

        // -----------------------------------------------------
        // MATERIAL
        // -----------------------------------------------------

        if (fixBloodMaterialAutomatically)
        {
            FixBloodMaterials(
                mark,
                true
            );
        }

        // -----------------------------------------------------
        // PARTICLES
        // -----------------------------------------------------

        ParticleSystem[] systems =
            mark.GetComponentsInChildren<ParticleSystem>(
                true
            );

        // -----------------------------------------------------
        // FORCE GROUND PARTICLE MODE
        // -----------------------------------------------------

        if (forceGroundHorizontalBillboard)
        {
            foreach (ParticleSystem ps in systems)
            {
                if (ps == null)
                {
                    continue;
                }

                ParticleSystemRenderer psRenderer =
                    ps.GetComponent<ParticleSystemRenderer>();

                if (psRenderer != null)
                {
                    psRenderer.renderMode =
                        ParticleSystemRenderMode.HorizontalBillboard;

                    psRenderer.sortMode =
                        ParticleSystemSortMode.Distance;

                    psRenderer.alignment =
                        ParticleSystemRenderSpace.World;
                }
            }
        }

        // -----------------------------------------------------
        // RENDERERS
        // -----------------------------------------------------

        EnableAllRenderers(mark);

        Renderer[] renderers =
            mark.GetComponentsInChildren<Renderer>(
                true
            );

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (debugLogs)
        {
            Debug.Log(
                "[HIT FX] Blood Splat instantiated: " +
                mark.name +
                " | particles=" +
                systems.Length +
                " | renderers=" +
                renderers.Length +
                " | position=" +
                position +
                " | normal=" +
                normal
            );
        }

        if (systems.Length == 0 &&
            renderers.Length == 0)
        {
            Debug.LogWarning(
                "[HIT FX] BLOOD SPLAT NIE MA ParticleSystem ANI Renderer!"
            );
        }

        // -----------------------------------------------------
        // PLAY
        // -----------------------------------------------------

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
            {
                continue;
            }

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
                ParticleSystemRenderer psRenderer =
                    ps.GetComponent<ParticleSystemRenderer>();

                string materialName =
                    "NONE";

                if (psRenderer != null &&
                    psRenderer.sharedMaterial != null)
                {
                    materialName =
                        psRenderer.sharedMaterial.name;
                }

                Debug.Log(
                    "[HIT FX] Playing particle: " +
                    ps.name +
                    " | isPlaying=" +
                    ps.isPlaying +
                    " | material=" +
                    materialName
                );
            }
        }

        // -----------------------------------------------------
        // COLLIDERS OFF
        // -----------------------------------------------------

        Collider[] markColliders =
            mark.GetComponentsInChildren<Collider>(
                true
            );

        foreach (Collider collider in markColliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // -----------------------------------------------------
        // LIFETIME
        // -----------------------------------------------------

        if (bloodGroundLifetime > 0f)
        {
            Destroy(
                mark,
                bloodGroundLifetime
            );
        }

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (debugGroundRay)
        {
            Debug.DrawRay(
                position,
                normal * 0.5f,
                Color.magenta,
                5f
            );
        }
    }

    // =========================================================
    // MATERIAL FIX
    // =========================================================

    private void FixBloodMaterials(
        GameObject effect,
        bool ground
    )
    {
        if (effect == null)
        {
            return;
        }

        ParticleSystemRenderer[] particleRenderers =
            effect.GetComponentsInChildren<ParticleSystemRenderer>(
                true
            );

        foreach (ParticleSystemRenderer psRenderer in particleRenderers)
        {
            if (psRenderer == null)
            {
                continue;
            }

            Material original =
                psRenderer.sharedMaterial;

            Material fixedMaterial =
                CreateCompatibleBloodMaterial(
                    original
                );

            if (fixedMaterial != null)
            {
                psRenderer.material =
                    fixedMaterial;

                if (debugLogs)
                {
                    Debug.Log(
                        "[HIT FX] Blood material FIXED: " +
                        psRenderer.gameObject.name +
                        " | shader=" +
                        fixedMaterial.shader.name
                    );
                }
            }
        }

        if (enableNonParticleRenderers)
        {
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

                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                Material original =
                    renderer.sharedMaterial;

                Material fixedMaterial =
                    CreateCompatibleBloodMaterial(
                        original
                    );

                if (fixedMaterial != null)
                {
                    renderer.material =
                        fixedMaterial;
                }
            }
        }
    }

    // =========================================================
    // CREATE COMPATIBLE BLOOD MATERIAL
    // =========================================================

    private Material CreateCompatibleBloodMaterial(
        Material original
    )
    {
        Texture bloodTexture = null;

        if (original != null)
        {
            if (original.HasProperty("_bloodTex"))
            {
                bloodTexture =
                    original.GetTexture("_bloodTex");
            }

            if (bloodTexture == null &&
                original.HasProperty("_MainTex"))
            {
                bloodTexture =
                    original.GetTexture("_MainTex");
            }

            if (bloodTexture == null &&
                original.HasProperty("_BaseMap"))
            {
                bloodTexture =
                    original.GetTexture("_BaseMap");
            }
        }

        Shader shader =
            FindBestParticleShader();

        if (shader == null)
        {
            Debug.LogError(
                "[HIT FX] Nie znaleziono kompatybilnego shadera Particle."
            );

            return null;
        }

        Material material =
            new Material(shader);

        material.name =
            "BloodFX_Runtime_Material";

        // -----------------------------------------------------
        // TEXTURE
        // -----------------------------------------------------

        if (bloodTexture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture(
                    "_BaseMap",
                    bloodTexture
                );
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture(
                    "_MainTex",
                    bloodTexture
                );
            }

            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Blood texture copied: " +
                    bloodTexture.name
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[HIT FX] Nie znaleziono tekstury BloodSplat w materiale."
            );
        }

        // -----------------------------------------------------
        // COLOR
        // -----------------------------------------------------

        Color finalBloodColor =
            bloodColor *
            bloodBrightness;

        finalBloodColor.a = 1f;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                finalBloodColor
            );
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                finalBloodColor
            );
        }

        // -----------------------------------------------------
        // SURFACE SETTINGS
        // -----------------------------------------------------

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat(
                "_Blend",
                0f
            );
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat(
                "_ZWrite",
                0f
            );
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat(
                "_Cull",
                0f
            );
        }

        // -----------------------------------------------------
        // KEYWORDS
        // -----------------------------------------------------

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.EnableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );

        return material;
    }

    // =========================================================
    // FIND SHADER
    // =========================================================

    private Shader FindBestParticleShader()
    {
        // -----------------------------------------------------
        // URP
        // -----------------------------------------------------

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Particles/Unlit"
            );

        if (shader != null)
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Using URP Particle shader."
                );
            }

            return shader;
        }

        // -----------------------------------------------------
        // BUILT-IN UNITY
        // -----------------------------------------------------

        shader =
            Shader.Find(
                "Particles/Standard Unlit"
            );

        if (shader != null)
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Using Built-in Particle shader."
                );
            }

            return shader;
        }

        // -----------------------------------------------------
        // SPRITES FALLBACK
        // -----------------------------------------------------

        shader =
            Shader.Find(
                "Sprites/Default"
            );

        if (shader != null)
        {
            if (debugLogs)
            {
                Debug.Log(
                    "[HIT FX] Using Sprites/Default fallback shader."
                );
            }

            return shader;
        }

        return null;
    }

    // =========================================================
    // ENSURE MINIMUM SCALE
    // =========================================================

    private void EnsureMinimumScale(
        GameObject effect
    )
    {
        if (effect == null)
        {
            return;
        }

        Vector3 scale =
            effect.transform.localScale;

        if (Mathf.Abs(scale.x) <
            minimumVisualScale)
        {
            scale.x =
                scale.x < 0f
                    ? -minimumVisualScale
                    : minimumVisualScale;
        }

        if (Mathf.Abs(scale.y) <
            minimumVisualScale)
        {
            scale.y =
                scale.y < 0f
                    ? -minimumVisualScale
                    : minimumVisualScale;
        }

        if (Mathf.Abs(scale.z) <
            minimumVisualScale)
        {
            scale.z =
                scale.z < 0f
                    ? -minimumVisualScale
                    : minimumVisualScale;
        }

        effect.transform.localScale =
            scale;
    }

    // =========================================================
    // PLAY PARTICLES
    // =========================================================

    private void PlayAllParticleSystems(
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
                    "[HIT FX] Playing HIT particle: " +
                    ps.name
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
            renderer.enabled = true;

            if (debugRendererInfo &&
                debugLogs)
            {
                string materialName =
                    "NONE";

                if (renderer.sharedMaterial != null)
                {
                    materialName =
                        renderer.sharedMaterial.name;
                }

                Debug.Log(
                    "[HIT FX] Renderer: " +
                    renderer.GetType().Name +
                    " | object=" +
                    renderer.gameObject.name +
                    " | material=" +
                    materialName +
                    " | enabled=" +
                    renderer.enabled
                );
            }
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

        bool foundPoint = false;

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
            {
                continue;
            }

            if (!CanUseClosestPoint(collider))
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
    // CLOSEST POINT
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

        bool foundBounds = false;

        Bounds bounds =
            new Bounds(
                target,
                Vector3.zero
            );

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds =
                    collider.bounds;

                foundBounds = true;
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
        {
            return false;
        }

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

        Collider[] colliders =
            victim.GetComponentsInChildren<Collider>(
                true
            );

        bool foundBounds = false;

        Bounds bounds =
            new Bounds(
                victim.transform.position,
                Vector3.zero
            );

        foreach (Collider collider in colliders)
        {
            if (!IsValidCollider(collider))
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds =
                    collider.bounds;

                foundBounds = true;
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