using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BulletProjectile : MonoBehaviour
{
    [Header("Lifetime")]
    public float maxLifeTime = 3f;

    [Header("Impact Echo")]
    public float impactEchoRadius = 10f;
    public float impactEchoDuration = 0.5f;

    [Header("Layers")]
    public LayerMask wallLayer;
    public LayerMask zombieLayer;
    public LayerMask enemyLayer;      // set this to your enemy layer

    [Header("Visuals")]
    public GameObject hitEffectPrefab;    // spritesheet-based animation prefab

    public float damage = 20f;

    private FogOfWar fog;
    private float spawnTime;

    void Start()
    {
        fog = FindObjectOfType<FogOfWar>();
        spawnTime = Time.time;

        // Ensure collider is a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        // Auto-destroy after some time
        if (Time.time - spawnTime >= maxLifeTime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        bool hitWall  = (wallLayer.value  & (1 << otherLayer)) != 0;
        bool hitEnemy = (enemyLayer.value & (1 << otherLayer)) != 0;

        if (!hitWall && !hitEnemy)
            return;

        Vector2 hitPos = transform.position;

        // If we hit enemy, apply damage and headshot logic
        if (hitEnemy)
        {
            bool isHeadshot = other.CompareTag("Head");

            // LivingEntity could be on the parent (zombie root)
            LivingEntity entity = other.GetComponentInParent<LivingEntity>();
            if (entity != null)
            {
                // Example: base damage 20
                entity.TakeDamage(damage, isHeadshot);
            }
        }

        // Spawn hit VFX, echo, alert zombies as before...
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
        }

        AudioManager.Instance.PlaySfxAtPosition("Impact/Wall", hitPos);

        if (fog != null)
        {
            fog.TriggerEcho(hitPos, impactEchoRadius, impactEchoDuration);
        }

        AlertZombies(hitPos, impactEchoRadius);

        Destroy(gameObject);
    }

    void AlertZombies(Vector2 origin, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, zombieLayer);
        foreach (var hit in hits)
        {
            Vector2 targetPos = hit.transform.position;
            Vector2 dir = (targetPos - origin).normalized;
            float dist = Vector2.Distance(origin, targetPos);

            RaycastHit2D wallHit = Physics2D.Raycast(origin, dir, dist, wallLayer);
            if (wallHit.collider != null)
                continue;

            ZombieHearing zh = hit.GetComponent<ZombieHearing>();
            if (zh != null)
            {
                zh.HearSound(origin);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, impactEchoRadius);
    }
}
