using UnityEngine;
using TMPro;

public class CombatFeedbackManager : MonoBehaviour
{
    public static CombatFeedbackManager Instance { get; private set; }

    [Header("Damage Text")]
    public Transform worldSpaceCanvas;
    public GameObject damageTextPrefab;   // prefab with TMP_Text + FloatingDamageText

    [Header("Streak Text")]
    public GameObject streakTextPrefab;   // prefab with TMP_Text + FloatingStreakText
    [Tooltip("Target for streak texts to follow (usually player). If null, auto-find by tag.")]
    public Transform streakFollowTarget;

    [Header("Score UI")]
    public TMP_Text scoreText;

    [Header("Scoring")]
    public int baseKillScore = 100;
    public int headshotBonus = 50;
    public int doubleKillBonus = 100;
    public int streakBonusPerKill = 25;
    public float streakResetTime = 5f;
    public float doubleKillWindow = 1.5f;

    private int currentScore = 0;
    private int currentStreak = 0;
    private float lastKillTime = -999f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (streakFollowTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) streakFollowTarget = p.transform;
        }
    }

    // ------------ DAMAGE TEXT ------------

    public void ShowDamage(Vector3 worldPos, float amount, bool isHeadshot)
    {
        if (damageTextPrefab == null || worldSpaceCanvas == null)
            return;

        GameObject go = Instantiate(damageTextPrefab, worldSpaceCanvas);
        Vector3 offset = new Vector3(0f, 0.75f, 0f);
        go.transform.position = worldPos + offset;

        string textString = Mathf.RoundToInt(amount).ToString();

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = textString;
            tmp.color = isHeadshot ? Color.yellow : Color.white;
        }
    }

    // ------------ KILL / SCORE ------------

    public void RegisterKill(bool headshot)
    {
        float now = Time.time;
        float dt = now - lastKillTime;

        bool continuesStreak = dt <= streakResetTime;
        if (continuesStreak)
            currentStreak++;
        else
            currentStreak = 1;

        int addScore = baseKillScore;

        if (headshot)
            addScore += headshotBonus;

        // Double kill
        if (currentStreak == 2 && dt <= doubleKillWindow)
        {
            addScore += doubleKillBonus;
            ShowSpecialText("DOUBLE KILL!", Color.cyan);
        }

        // Streak 3+
        if (currentStreak >= 3)
        {
            int streakBonus = streakBonusPerKill * (currentStreak - 1);
            addScore += streakBonus;

            ShowSpecialText($"STREAK x{currentStreak}", Color.magenta);
        }

        lastKillTime = now;
        AddScore(addScore);
    }

    void AddScore(int amount)
    {
        currentScore += amount;
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    // ------------ SPECIAL TEXT (FOLLOW PLAYER) ------------

    void ShowSpecialText(string message, Color color)
    {
        if (worldSpaceCanvas == null || (streakTextPrefab == null && damageTextPrefab == null))
            return;

        GameObject prefabToUse = streakTextPrefab != null ? streakTextPrefab : damageTextPrefab;
        GameObject go = Instantiate(prefabToUse, worldSpaceCanvas);

        // Find player if needed
        if (streakFollowTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) streakFollowTarget = p.transform;
        }

        if (streakFollowTarget != null)
        {
            FloatingStreakText follow = go.GetComponent<FloatingStreakText>();
            if (follow != null)
            {
                follow.target = streakFollowTarget;
            }

            go.transform.position = streakFollowTarget.position + new Vector3(0f, 2f, 0f);
        }
        else
        {
            // fallback: center
            go.transform.position = Vector3.zero;
        }

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = color;
        }
    }
}
