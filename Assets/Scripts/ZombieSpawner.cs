using System.Collections;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ZombieType
    {
        public GameObject prefab;
        [Tooltip("Weight/probability for this zombie type (higher = more common)")]
        [Range(0f, 100f)]
        public float weight = 1f;
    }

    [Header("Zombie Types")]
    [Tooltip("Leave empty to use single zombie prefab below")]
    public ZombieType[] zombieTypes;

    [Header("Zombie (Legacy - used if zombie types is empty)")]
    public GameObject zombiePrefab;

    [Header("Spawn Mode")]
    public bool useSpawnPoints = true;
    public Transform[] spawnPoints;

    [Header("Random Spawn Area (used if not using spawn points)")]
    public Vector2 randomAreaMin = new Vector2(-10, -10);
    public Vector2 randomAreaMax = new Vector2(10, 10);

    [Header("Fog of War")]
    public FogOfWar fogOfWar;
    [Tooltip("How many attempts per spawn before giving up")]
    public int maxSpawnAttempts = 20;

    [Header("Collision Check")]
    [Tooltip("Layers that should block spawning (Walls, Obstacles, maybe Zombies)")]
    public LayerMask blockedLayers;
    public float spawnCheckRadius = 0.4f;

    public IEnumerator SpawnWaveCoroutine(int zombieCount, float timeBetweenSpawns)
    {
        int spawned = 0;

        while (spawned < zombieCount)
        {
            Vector2 spawnPos;
            bool gotPos = TryGetValidSpawnPosition(out spawnPos);

            if (gotPos)
            {
                // Select zombie prefab based on distribution
                GameObject selectedPrefab = GetRandomZombiePrefab();

                if (selectedPrefab == null)
                {
                    Debug.LogWarning("[ZombieSpawner] No valid zombie prefab to spawn!");
                    yield break;
                }

                GameObject z = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

                // 🔥 Register with LevelManager
                ZombieEntity entity = z.GetComponent<ZombieEntity>();
                if (entity != null && LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegisterZombie(entity);
                }

                spawned++;
            }
            else
            {
                Debug.LogWarning("[ZombieSpawner] Failed to find valid spawn position (walls/fog).");
            }

            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    GameObject GetRandomZombiePrefab()
    {
        // If using new zombie types system
        if (zombieTypes != null && zombieTypes.Length > 0)
        {
            // Calculate total weight
            float totalWeight = 0f;
            foreach (var zombieType in zombieTypes)
            {
                if (zombieType.prefab != null)
                    totalWeight += zombieType.weight;
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning("[ZombieSpawner] Total weight is 0. Using first valid prefab.");
                foreach (var zombieType in zombieTypes)
                {
                    if (zombieType.prefab != null)
                        return zombieType.prefab;
                }
                return null;
            }

            // Select random value within total weight
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            // Find which zombie type this value corresponds to
            foreach (var zombieType in zombieTypes)
            {
                if (zombieType.prefab == null) continue;

                currentWeight += zombieType.weight;
                if (randomValue <= currentWeight)
                {
                    return zombieType.prefab;
                }
            }

            // Fallback (shouldn't happen, but just in case)
            foreach (var zombieType in zombieTypes)
            {
                if (zombieType.prefab != null)
                    return zombieType.prefab;
            }
        }

        // Fallback to legacy single prefab
        return zombiePrefab;
    }

    bool TryGetValidSpawnPosition(out Vector2 result)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 candidate;

            if (useSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (sp == null) continue;
                candidate = sp.position;
            }
            else
            {
                float x = Random.Range(randomAreaMin.x, randomAreaMax.x);
                float y = Random.Range(randomAreaMin.y, randomAreaMax.y);
                candidate = new Vector3(x, y, 0f);
            }

            // 1) Fog check: only spawn where it is still dark (no echo)
            bool visible = false;
            if (fogOfWar != null)
            {
                visible = fogOfWar.IsWorldPositionVisible(candidate);
            }
            if (visible) continue;

            // 2) Collision check
            bool blocked = Physics2D.OverlapCircle(candidate, spawnCheckRadius, blockedLayers) != null;
            if (blocked) continue;

            result = candidate;
            return true;
        }

        result = Vector2.zero;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (!useSpawnPoints)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.2f);
            Gizmos.DrawCube(
                new Vector3(
                    (randomAreaMin.x + randomAreaMax.x) * 0.5f,
                    (randomAreaMin.y + randomAreaMax.y) * 0.5f,
                    0f
                ),
                new Vector3(
                    Mathf.Abs(randomAreaMax.x - randomAreaMin.x),
                    Mathf.Abs(randomAreaMax.y - randomAreaMin.y),
                    0.1f
                )
            );
        }

        if (spawnPoints != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            foreach (var sp in spawnPoints)
            {
                if (sp == null) continue;
                Gizmos.DrawWireSphere(sp.position, spawnCheckRadius);
            }
        }
    }
}
