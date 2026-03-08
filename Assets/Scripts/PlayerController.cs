using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Fog of War Echo")]
    public float walkEchoRadius = 3f;
    public float walkEchoDuration = 0.3f;
    public float walkEchoInterval = 0.3f;

    [Header("Enemy Detection")]
    public float enemyDetectionRadius = 5f;
    public LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Vector2 input;
    private SpriteRenderer sr;
    private Animator anim;
    private AudioEchoFilter echoFilter;
    private FogOfWar fogOfWar;
    private float lastWalkEchoTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // Get or add AudioEchoFilter for walking echo effect
        echoFilter = GetComponent<AudioEchoFilter>();
        if (echoFilter == null)
        {
            echoFilter = gameObject.AddComponent<AudioEchoFilter>();
        }

        // Configure echo with moderate settings (not too big)
        echoFilter.delay = 150f;        // 150ms delay
        echoFilter.decayRatio = 0.3f;   // 30% decay (subtle)
        echoFilter.wetMix = 0.2f;       // 20% echo mix (not overpowering)
        echoFilter.dryMix = 1.0f;       // 100% original sound

        // Find FogOfWar in scene
        fogOfWar = FindFirstObjectByType<FogOfWar>();
    }

    void Update()
    {
        // --- INPUT ---
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized;

        // --- FLIP SPRITE LEFT / RIGHT ---
        if (input.x > 0.01f)
        {
            // Flip using transform rotation y
            transform.rotation = Quaternion.Euler(0, 180, 0); // facing right
        }
        else if (input.x < -0.01f)
        {
            // Unlip using transform rotation y
            transform.rotation = Quaternion.Euler(0, 0, 0); // facing right
        }

        if (input.x != 0 || input.y != 0)
        {
            anim.SetBool("isMoving", true);
        } else
        {
            anim.SetBool("isMoving", false);
        }

        // Play footstep sounds
        if (input.magnitude > 0.1f)
        {
            // without spamming, play every 0.4 seconds
            var timeSinceLastFootstep = Time.time % .2f;
            if (timeSinceLastFootstep < 0.02f)
            {
                AudioManager.Instance.PlaySfxAtPosition("Footstep", transform.position);
            }
        }

        // Trigger fog of war echo when walking
        if (input.magnitude > 0.1f && fogOfWar != null)
        {
            if (Time.time - lastWalkEchoTime >= walkEchoInterval)
            {
                fogOfWar.TriggerEcho(transform.position, walkEchoRadius, walkEchoDuration);
                lastWalkEchoTime = Time.time;

                // Notify nearby enemies
                NotifyNearbyEnemies();
            }
        }

        // --- ANIMATION PARAMETERS ---
        if (anim != null)
        {
            // 0 when idle, >0 when moving
            float speed = input.sqrMagnitude; 

            anim.SetFloat("Speed", speed);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

    void NotifyNearbyEnemies()
    {
        // Find all enemies within detection radius
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, enemyDetectionRadius, enemyLayer);

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            ZombieHearing zombie = enemyCollider.GetComponent<ZombieHearing>();
            if (zombie != null)
            {
                zombie.HearSound(transform.position);
            }
        }
    }
}
