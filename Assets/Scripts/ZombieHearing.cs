using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class ZombieHearing : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.2f;
    public float forgetTime = 3f;   // how long they remember a sound / chase the player

    [Header("Patrol")]
    public bool enablePatrol = true;
    public float patrolRadius = 5f;      // how far from spawn point to wander
    public float waypointWaitTime = 2f;  // wait time at each patrol point
    public float patrolSpeed = 1f;       // slower than chase speed

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1f;
    public int avoidanceRays = 4;        // side rays to check around main direction
    public float sideAngleStep = 20f;    // degrees between side rays

    [Header("Chase Target")]
    public Transform playerTarget;       // the player to follow when sound is heard

    // >>> NEW: attack settings
    [Header("Attack")]
    public float attackRange = 0.6f;     // how close to attack
    public float attackDamage = 10f;     // damage per hit
    public float attackCooldown = 1.0f;  // seconds between hits
    public string attackTriggerName = "Attack"; // animator trigger (optional)

    [Header("Fog of War Echo on Attack")]
    public float attackEchoRadius = 4f;  // slightly bigger than walk echo
    public float attackEchoDuration = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showDebugGizmos = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private FogOfWar fogOfWar;

    // we still keep the sound origin for debug, but we chase player while this is set
    private Vector2? soundTarget;
    private float heardTime;

    // Patrol state
    private Vector2 spawnPoint;
    private Vector2? patrolTarget;
    private float patrolWaitTimer;
    private bool isWaiting;

    // >>> NEW: attack state
    private float lastAttackTime;

    // For gizmos
    private struct DebugRay
    {
        public Vector2 origin;
        public Vector2 dir;
        public float length;
        public Color color;
    }

    private DebugRay[] lastAvoidanceRays;

    private Vector2 currentTargetPos; // where we’re trying to go this frame

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        lastAvoidanceRays = new DebugRay[1 + avoidanceRays * 2]; // main + left/right
    }

    void Start()
    {
        spawnPoint = transform.position;
        if (enablePatrol)
        {
            PickNewPatrolTarget();
        }

        // Auto-find player by tag if not set
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTarget = p.transform;
            }
        }

        // Find FogOfWar in scene
        fogOfWar = FindFirstObjectByType<FogOfWar>();
    }

    /// <summary>
    /// Called when echo happens. We still get the sound position,
    /// but we will actually follow the PLAYER for forgetTime.
    /// </summary>
    public void HearSound(Vector2 pos)
    {
        soundTarget = pos;
        heardTime = Time.time;

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Heard sound at {pos}. Distance: {Vector2.Distance(transform.position, pos):F2}");
        }
    }

    void Update()
    {
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        float currentSpeed = 0f;
        bool hasTarget = false;

        // Priority 1: Chase player after sound
        if (soundTarget.HasValue)
        {
            // if forgetTime passed, stop chasing
            if (Time.time - heardTime > forgetTime)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[{gameObject.name}] Stopped chasing player (forgetTime reached)");
                }
                soundTarget = null;
            }
            else
            {
                // Follow player if we have a reference, otherwise fallback to sound origin
                if (playerTarget != null)
                {
                    currentTargetPos = playerTarget.position;
                }
                else
                {
                    currentTargetPos = soundTarget.Value;
                }

                currentSpeed = moveSpeed;
                hasTarget = true;

                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[{gameObject.name}] Chasing player at {currentTargetPos}");
                }
            }
        }

        // Priority 2: Patrol if not chasing
        if (!hasTarget && enablePatrol)
        {
            if (isWaiting)
            {
                patrolWaitTimer -= Time.fixedDeltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    isWaiting = false;
                    PickNewPatrolTarget();
                }
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (patrolTarget.HasValue)
            {
                currentTargetPos = patrolTarget.Value;
                currentSpeed = patrolSpeed;
                hasTarget = true;

                float distToPatrol = Vector2.Distance(transform.position, currentTargetPos);
                if (distToPatrol < stopDistance)
                {
                    isWaiting = true;
                    patrolWaitTimer = waypointWaitTime;
                    rb.linearVelocity = Vector2.zero;

                    if (showDebugLogs)
                    {
                        Debug.Log($"[{gameObject.name}] Reached patrol point, waiting...");
                    }
                    return;
                }
            }
        }

        // Move towards target (player or patrol)
        if (hasTarget)
        {
            Vector2 dir = currentTargetPos - (Vector2)transform.position;
            float dist = dir.magnitude;

            // >>> NEW: if target is the player and within attack range, stop and attack
            bool isChasingPlayer = (playerTarget != null && (Vector2)currentTargetPos == (Vector2)playerTarget.position);

            if (isChasingPlayer)
            {
                // without spamming, play every 0.4 seconds
                var timeSinceLastFootstep = Time.time % .4f;
                if (timeSinceLastFootstep < 0.02f)
                {
                    AudioManager.Instance.PlaySfxAtPosition("Footstep", transform.position);
                }
            }

            if (isChasingPlayer && playerTarget != null && dist <= attackRange)
            {
                rb.linearVelocity = Vector2.zero;
                TryAttackPlayer();
            }
            else
            {
                if (dist > stopDistance)
                {
                    dir.Normalize();

                    // Apply obstacle avoidance
                    Vector2 avoidedDir = AvoidObstacles(dir);

                    rb.linearVelocity = avoidedDir * currentSpeed;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // idle
        }
    }

    // >>> NEW: attempt to damage the player if cooldown is ready
    void TryAttackPlayer()
    {
        if (playerTarget == null) return;

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Animator attack trigger
        if (anim != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            anim.SetTrigger(attackTriggerName);
        }

        // Deal damage via LivingEntity / PlayerEntity
        LivingEntity playerEntity = playerTarget.GetComponent<LivingEntity>();
        if (playerEntity != null && !playerEntity.isDead)
        {
            playerEntity.TakeDamage(attackDamage, false);
            AudioManager.Instance.PlaySfxAtPosition("Zombie/Follow", transform.position);
        }

        // Trigger fog of war echo on attack
        if (fogOfWar != null)
        {
            fogOfWar.TriggerEcho(transform.position, attackEchoRadius, attackEchoDuration);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Attacked player for {attackDamage} damage.");
        }
    }

    void PickNewPatrolTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
        patrolTarget = spawnPoint + randomOffset;

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] New patrol target: {patrolTarget.Value}");
        }
    }

    /// <summary>
    /// Smarter obstacle avoidance:
    ///  1. Cast main ray in desired direction.
    ///  2. If blocked, try sliding along the obstacle using its normal.
    ///  3. If still blocked, scan side angles left/right.
    /// </summary>
    Vector2 AvoidObstacles(Vector2 desiredDir)
    {
        if (obstacleLayer == 0) return desiredDir; // No obstacle layer set

        Vector2 origin = transform.position;
        float checkDist = avoidanceDistance;

        int rayIndex = 0;

        // MAIN RAY
        RaycastHit2D mainHit = Physics2D.Raycast(origin, desiredDir, checkDist, obstacleLayer);

        lastAvoidanceRays[rayIndex++] = new DebugRay
        {
            origin = origin,
            dir = desiredDir,
            length = checkDist,
            color = mainHit.collider ? Color.red : Color.green
        };

        if (!mainHit.collider)
        {
            // Path is clear
            return desiredDir;
        }

        // Try sliding along obstacle using its normal
        Vector2 slideDir = Vector2.Perpendicular(mainHit.normal).normalized;
        if (Vector2.Dot(slideDir, desiredDir) < 0f)
            slideDir = -slideDir;

        RaycastHit2D slideHit = Physics2D.Raycast(origin, slideDir, checkDist, obstacleLayer);

        lastAvoidanceRays[rayIndex++] = new DebugRay
        {
            origin = origin,
            dir = slideDir,
            length = checkDist,
            color = slideHit.collider ? Color.red : Color.cyan
        };

        if (!slideHit.collider)
        {
            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{gameObject.name}] Sliding along obstacle");
            }
            return slideDir;
        }

        // Side rays around desired direction
        for (int i = 1; i <= avoidanceRays; i++)
        {
            float angleRight = sideAngleStep * i;
            float angleLeft = -sideAngleStep * i;

            Vector2 dirRight = Rotate(desiredDir, angleRight);
            Vector2 dirLeft = Rotate(desiredDir, angleLeft);

            RaycastHit2D hitRight = Physics2D.Raycast(origin, dirRight, checkDist, obstacleLayer);
            lastAvoidanceRays[rayIndex++] = new DebugRay
            {
                origin = origin,
                dir = dirRight,
                length = checkDist,
                color = hitRight.collider ? Color.red : Color.yellow
            };

            if (!hitRight.collider)
            {
                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[{gameObject.name}] Avoiding obstacle, turning right {angleRight}°");
                }
                return dirRight;
            }

            RaycastHit2D hitLeft = Physics2D.Raycast(origin, dirLeft, checkDist, obstacleLayer);
            lastAvoidanceRays[rayIndex++] = new DebugRay
            {
                origin = origin,
                dir = dirLeft,
                length = checkDist,
                color = hitLeft.collider ? Color.red : Color.yellow
            };

            if (!hitLeft.collider)
            {
                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[{gameObject.name}] Avoiding obstacle, turning left {angleLeft}°");
                }
                return dirLeft;
            }
        }

        // All paths blocked – stop
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[{gameObject.name}] Stuck! All paths blocked");
        }
        return Vector2.zero;
    }

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    void UpdateVisuals()
    {
        // Flip sprite based on velocity
        if (rb.linearVelocity.x > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0); // facing right
        }
        else if (rb.linearVelocity.x < -0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0); // facing left
        }

        if (anim != null)
        {
            bool isMoving = rb.linearVelocity.magnitude > 0.01f;
            anim.SetBool("isMoving", isMoving);
            anim.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Patrol radius
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
        }

        // If chasing via sound, draw towards player (if exists)
        if (soundTarget.HasValue)
        {
            Vector3 chasePos = soundTarget.Value;
            if (Application.isPlaying && playerTarget != null)
                chasePos = playerTarget.position;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, chasePos);
            Gizmos.DrawWireSphere(chasePos, 0.3f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(chasePos, stopDistance);
        }
        // Patrol target when not chasing
        else if (patrolTarget.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, patrolTarget.Value);
            Gizmos.DrawWireSphere(patrolTarget.Value, 0.2f);
        }

        // Velocity vector
        if (Application.isPlaying && rb != null && rb.linearVelocity.magnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Vector3 velocityEnd = transform.position + (Vector3)rb.linearVelocity.normalized * 0.5f;
            Gizmos.DrawLine(transform.position, velocityEnd);
        }

        // Obstacle avoidance rays
        if (Application.isPlaying && obstacleLayer != 0 && lastAvoidanceRays != null)
        {
            foreach (var ray in lastAvoidanceRays)
            {
                if (ray.length <= 0f) continue;
                Gizmos.color = ray.color;
                Gizmos.DrawRay(ray.origin, ray.dir * ray.length);
            }
        }

        // >>> Draw attack range
        if (Application.isPlaying && playerTarget != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
