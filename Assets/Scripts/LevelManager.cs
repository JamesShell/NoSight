using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    private GameState currentState = GameState.Menu;

    [System.Serializable]
    public class WeaponSettings
    {
        public string weaponName = "Pistol";

        [Header("Gun Stats")]
        public float fireRate = 4f;
        public float bulletSpeed = 10f;
        public float damage = 20f;

        [Header("Echo (Muzzle)")]
        public float muzzleEchoRadius = 8f;
        public float muzzleEchoDuration = 0.5f;

        [Header("Echo (Impact)")]
        public float impactEchoRadius = 10f;
        public float impactEchoDuration = 0.5f;

        [Header("Animator")]
        public int weaponIndex = 0; // for Animator int param, e.g. "WeaponIndex"
    }

    [System.Serializable]
    public class WaveSettings
    {
        public int zombieCount = 5;
        public float timeBetweenSpawns = 0.5f;
    }

    [System.Serializable]
    public class LevelSettings
    {
        public string levelName = "Level 1";
        public GameObject mapPrefab;
        public WeaponSettings weapon;
        public WaveSettings[] waves;

        [Header("Spawn Area (for random mode)")]
        public Vector2 spawnAreaMin = new Vector2(-10, -10);
        public Vector2 spawnAreaMax = new Vector2(10, 10);
    }

    [Header("Levels")]
    public LevelSettings[] levels;
    public int startLevelIndex = 0;

    [Header("References")]
    public GunShooter gunShooter;
    public ZombieSpawner zombieSpawner;

    [Header("UI References")]
    public CanvasGroup menuPanel;
    public CanvasGroup gameHUDPanel;
    public CanvasGroup gameOverPanel;
    public Button playButton;
    public Button playAgainButton;

    [Header("UI Animation")]
    public float uiFadeDuration = 0.5f;

    [Header("Flow")]
    public float levelEndDelay = 2f; // pause after last zombie before switching level
    public float gameOverDelay = 1f; // delay before showing game over screen

    private int currentLevelIndex = -1;
    private GameObject currentMapInstance;

    // 🔥 TRACK ALL ALIVE ZOMBIES HERE
    private readonly List<ZombieEntity> activeZombies = new List<ZombieEntity>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);
    }

    void Start()
    {
        // Start with menu instead of loading level
        ShowMenu();
    }

    public void LoadLevel(int levelIndex)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("[LevelManager] No levels configured!");
            return;
        }

        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError($"[LevelManager] Invalid level index {levelIndex}");
            return;
        }

        currentLevelIndex = levelIndex;
        LevelSettings level = levels[levelIndex];

        Debug.Log($"[LevelManager] Loading level {levelIndex}: {level.levelName}");

        // Clear zombie list for new level
        activeZombies.Clear();

        // Destroy previous map
        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
        }

        // Instantiate map
        if (level.mapPrefab != null)
        {
            currentMapInstance = Instantiate(level.mapPrefab, Vector3.zero, Quaternion.identity);
        }

        // Apply weapon settings
        if (gunShooter != null && level.weapon != null)
        {
            ApplyWeaponToGun(gunShooter, level.weapon);
        }

        // Configure spawner spawn area for this level (if using random area)
        if (zombieSpawner != null)
        {
            zombieSpawner.randomAreaMin = level.spawnAreaMin;
            zombieSpawner.randomAreaMax = level.spawnAreaMax;
        }

        // Start waves
        if (zombieSpawner != null && level.waves != null && level.waves.Length > 0)
        {
            StopAllCoroutines();
            StartCoroutine(RunWaves(level));
        }
    }

    private IEnumerator RunWaves(LevelSettings level)
    {
        // For each wave:
        //  - spawn all zombies for that wave
        //  - wait until activeZombies list is empty
        for (int i = 0; i < level.waves.Length; i++)
        {
            WaveSettings wave = level.waves[i];
            Debug.Log($"[LevelManager] Starting wave {i + 1}/{level.waves.Length} - {wave.zombieCount} zombies");

            // Spawn zombies over time
            yield return zombieSpawner.SpawnWaveCoroutine(wave.zombieCount, wave.timeBetweenSpawns);

            // Now wait until all zombies from this wave (and earlier) are dead
            Debug.Log("[LevelManager] Waiting for all zombies in wave to die...");
            yield return new WaitUntil(() => activeZombies.Count == 0);
        }

        // All waves done and no zombies left
        Debug.Log($"[LevelManager] Level '{level.levelName}' completed.");
        AudioManager.Instance.PlaySfx2D("Player/Level");

        yield return new WaitForSeconds(levelEndDelay);

        // Load next level, if any
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levels.Length)
        {
            LoadLevel(nextIndex);
        }
        else
        {
            Debug.Log("[LevelManager] No more levels. Game complete (for now).");
        }
    }

    private void ApplyWeaponToGun(GunShooter gun, WeaponSettings ws)
    {
        if (ws == null) return;

        gun.fireRate = ws.fireRate;
        gun.bulletSpeed = ws.bulletSpeed;

        gun.muzzleEchoRadius = ws.muzzleEchoRadius;
        gun.muzzleEchoDuration = ws.muzzleEchoDuration;

        gun.bulletDamage = ws.damage;

        Animator anim = gun.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetInteger("WeaponIndex", ws.weaponIndex);
        }

        Debug.Log($"[LevelManager] Equipped weapon: {ws.weaponName}");
    }

    // --------- CALLED BY ZOMBIES / SPAWNER ---------

    public void RegisterZombie(ZombieEntity zombie)
    {
        if (zombie == null) return;
        if (!activeZombies.Contains(zombie))
        {
            activeZombies.Add(zombie);
        }
    }

    public void UnregisterZombie(ZombieEntity zombie)
    {
        if (zombie == null) return;
        activeZombies.Remove(zombie);
    }

    // --------- MENU & GAME STATE ---------

    private void ShowMenu()
    {
        currentState = GameState.Menu;

        // Make sure all panels start in correct state
        if (menuPanel != null) menuPanel.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);

        // Fade in menu
        StartCoroutine(FadeInPanel(menuPanel, uiFadeDuration));

        // Disable gun shooter during menu
        if (gunShooter != null)
            gunShooter.enabled = false;

        // Play menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("Music/Main", 1f);
        }

        // Pause time (optional, comment out if you want background to keep animating)
        // Time.timeScale = 0f;
    }

    private void OnPlayButtonClicked()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        currentState = GameState.Playing;

        // Fade out menu, fade in game HUD
        yield return StartCoroutine(FadeOutThenIn(menuPanel, gameHUDPanel, uiFadeDuration));

        // Transition music from Main to Ambient
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("Music/Ambient", 2f);
        }

        // Enable gun shooter
        if (gunShooter != null)
            gunShooter.enabled = true;

        // Resume time if it was paused
        Time.timeScale = 1f;

        // Start the first level
        LoadLevel(startLevelIndex);
    }

    public void OnPlayerDeath()
    {
        if (currentState == GameState.GameOver) return;

        StartCoroutine(GameOverCoroutine());
    }

    private IEnumerator GameOverCoroutine()
    {
        currentState = GameState.GameOver;

        // Wait a moment before showing game over
        yield return new WaitForSeconds(gameOverDelay);

        // Disable gun shooter
        if (gunShooter != null)
            gunShooter.enabled = false;

        // Stop all spawning
        StopAllCoroutines();

        // Fade out game HUD, fade in game over screen
        yield return StartCoroutine(FadeOutThenIn(gameHUDPanel, gameOverPanel, uiFadeDuration));

        // Optionally play game over music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("Music/Main", 2f);
        }
    }

    private void OnPlayAgainButtonClicked()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // Fade out game over screen
        yield return StartCoroutine(FadeOutPanel(gameOverPanel, uiFadeDuration));

        // Ensure time scale is normal before reloading
        Time.timeScale = 1f;

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --------- UI FADE ANIMATIONS ---------

    private IEnumerator FadeInPanel(CanvasGroup panel, float duration)
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private IEnumerator FadeOutPanel(CanvasGroup panel, float duration)
    {
        if (panel == null) yield break;

        panel.alpha = 1f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        panel.alpha = 0f;
        panel.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutThenIn(CanvasGroup fadeOut, CanvasGroup fadeIn, float duration)
    {
        yield return StartCoroutine(FadeOutPanel(fadeOut, duration));
        yield return StartCoroutine(FadeInPanel(fadeIn, duration));
    }
}
