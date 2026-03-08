using UnityEngine;

public class EchoPulse : MonoBehaviour
{
    [Header("Echo Settings")]
    public float echoRadius = 6f;           // how far the echo reaches
    public float cooldown = 3f;
    public float fogRevealDuration = 1.5f;  // how long the reveal fades out

    [Header("Layers")]
    public LayerMask zombieLayer;
    public LayerMask wallLayer;             // used for LOS to zombies

    [Header("Visual")]
    public GameObject echoWavePrefab;       // circle prefab with EchoWave

    [Header("Debug")]
    public bool showDebugLogs = true;

    private float nextEchoTime = 0f;
    private FogOfWar fog;

    void Start()
    {
        fog = FindObjectOfType<FogOfWar>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= nextEchoTime)
        {
            DoEcho();
            nextEchoTime = Time.time + cooldown;
        }
    }

    void DoEcho()
    {
        Vector2 origin = transform.position;

        if (showDebugLogs)
        {
            Debug.Log($"[EchoPulse] Echo triggered at {origin}");
        }

        // 1) Tell FogOfWar to start a fading reveal
        if (fog != null)
        {
            fog.TriggerEcho(origin, echoRadius, fogRevealDuration);
        }

        // 2) Alert zombies in range IF not behind a wall
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, echoRadius, zombieLayer);

        if (showDebugLogs)
        {
            Debug.Log($"[EchoPulse] Found {hits.Length} zombie(s) in radius");
        }

        int alertedCount = 0;
        int blockedByWallCount = 0;

        foreach (var hit in hits)
        {
            Vector2 targetPos = hit.transform.position;
            Vector2 dir = (targetPos - origin).normalized;
            float dist = Vector2.Distance(origin, targetPos);

            // If a wall is between player and zombie, skip
            RaycastHit2D wallHit = Physics2D.Raycast(origin, dir, dist, wallLayer);
            if (wallHit.collider != null)
            {
                blockedByWallCount++;
                if (showDebugLogs)
                {
                    Debug.Log($"[EchoPulse] {hit.gameObject.name} blocked by wall: {wallHit.collider.gameObject.name}");
                }
                continue;
            }

            ZombieHearing zh = hit.GetComponent<ZombieHearing>();
            if (zh != null)
            {
                zh.HearSound(origin);
                alertedCount++;

                if (showDebugLogs)
                {
                    Debug.Log($"[EchoPulse] Alerted {hit.gameObject.name} at distance {dist:F2}");
                }
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"[EchoPulse] {hit.gameObject.name} has no ZombieHearing component!");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[EchoPulse] Summary: {alertedCount} alerted, {blockedByWallCount} blocked by walls");
        }

        // 3) Visual wave
        if (echoWavePrefab != null)
        {
            GameObject waveObj = Instantiate(echoWavePrefab, origin, Quaternion.identity);
            EchoWave ew = waveObj.GetComponent<EchoWave>();
            if (ew != null)
            {
                ew.maxRadius = echoRadius;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, echoRadius);
    }
}
