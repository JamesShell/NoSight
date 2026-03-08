using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GunShooter : MonoBehaviour
{
    [Header("Gun Stats")]
    public string gun = "Pistol"; // audio ID
    public float fireRate = 4f;          // shots per second
    public float bulletSpeed = 10f;
    public float bulletDamage = 20f;

    [Header("Muzzle Echo")]
    public float muzzleEchoRadius = 8f;
    public float muzzleEchoDuration = 0.5f;

    [Header("References")]
    public Transform muzzle;             // where bullets spawn
    public GameObject bulletPrefab;      // prefab with BulletProjectile
    public Animator gunAnimator;
    public LayerMask zombieLayer;
    public LayerMask wallLayer;

    private float nextFireTime = 0f;
    private FogOfWar fog;
    private AudioSource audioSource;
    private Camera cam;

    void Start()
    {
        fog = FindObjectOfType<FogOfWar>();
        audioSource = GetComponent<AudioSource>();
        cam = Camera.main;
        gunAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void Shoot()
    {
        if (muzzle == null || bulletPrefab == null) return;
        
        // Play shooting animation
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Fire");
        }

        // Aim towards mouse
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - muzzle.position).normalized;

        // 1) Spawn bullet
        GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        Rigidbody2D brb = bulletObj.GetComponent<Rigidbody2D>();
        if (brb != null)
        {
            brb.linearVelocity = dir * bulletSpeed;
        }

        // Pass data to bullet (for impact echo + layers)
        BulletProjectile bp = bulletObj.GetComponent<BulletProjectile>();
        if (bp != null)
        {
            bp.wallLayer = wallLayer;
            bp.zombieLayer = zombieLayer;
            bp.damage = bulletDamage;
        }

        AudioManager.Instance.PlaySfxAtPosition(gun+"/Shoot", muzzle.position);

        // 3) Muzzle echo in fog
        if (fog != null)
        {
            fog.TriggerEcho(muzzle.position, muzzleEchoRadius, muzzleEchoDuration);
        }

        // 4) Alert zombies from muzzle blast
        AlertZombies(muzzle.position, muzzleEchoRadius);
    }

    void AlertZombies(Vector2 origin, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, zombieLayer);
        foreach (var hit in hits)
        {
            Vector2 targetPos = hit.transform.position;
            Vector2 dir = (targetPos - origin).normalized;
            float dist = Vector2.Distance(origin, targetPos);

            // Blocked by wall?
            RaycastHit2D wallHit = Physics2D.Raycast(origin, dir, dist, wallLayer);
            if (wallHit.collider != null)
                continue;

            ZombieHearing zh = hit.GetComponent<ZombieHearing>();
            if (zh != null)
            {
                zh.HearSound(origin);     // they chase player for forgetTime
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(muzzle.position, muzzleEchoRadius);
    }
}
