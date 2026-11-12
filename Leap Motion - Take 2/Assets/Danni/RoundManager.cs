using Fingerboard;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RoundManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int countdownSeconds = 3;
    [SerializeField, Min(1)] private int roundSeconds = 60;
    [SerializeField] private bool enableKeyboardTest = true;

    private float roundStartsAt;
    private float roundEndsAt;
    private int score;
    private string landingFeedback = "";

    private float previousTime = 0f;
    public TrickPoints basePoints;

    private void Start()
    {
        roundStartsAt = Time.time + countdownSeconds;
        roundEndsAt = roundStartsAt + roundSeconds;
    }

    private void Update()
    {
        if (previousTime < roundStartsAt && Time.time >= roundStartsAt)
        {
            ActivateSkateboard();
        }

        if(previousTime < roundEndsAt && Time.time >= roundEndsAt)
        {
            Time.timeScale = 0f; // pause game
        }

        previousTime = Time.time;

        if (!enableKeyboardTest) return;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame) RecordLanding(50, 0.5f, 1f);
        if (keyboard.fKey.wasPressedThisFrame) RecordLanding(50, 0.5f, 0f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space)) RecordLanding(50, 0.5f, 1f);
        if (Input.GetKeyDown(KeyCode.F)) RecordLanding(50, 0.5f, 0f);
#endif
    }

    public void Land(SkateboardController.Trick trick, float airSeconds, bool landSuccess)
    {
        Debug.Log("LANDED TRICK");
        int bp = basePoints.GetBasePoints(trick);
        RecordLanding(bp, airSeconds, landSuccess ? 1f : 0.5f);
    }

    public void RecordLanding(int basePoints, float airSeconds, float landingSuccess,
        float environmentMultiplier = 1f, float comboMultiplier = 1f)
    {
        if (Time.time < roundStartsAt || Time.time >= roundEndsAt) return;

        float points = basePoints * environmentMultiplier * (airSeconds * 100f)
            * comboMultiplier * Mathf.Clamp01(landingSuccess);

        int earnedPoints = Mathf.RoundToInt(points);
        score += earnedPoints;
        landingFeedback = landingSuccess <= 0f ? "Bail! +0" : $"+{earnedPoints}";
    }

    private void OnGUI()
    {
        int secondsLeft = Mathf.Clamp(
            Mathf.CeilToInt(roundEndsAt - Time.time), 0, roundSeconds);
        string timer = $"{secondsLeft / 60}:{secondsLeft % 60:00}";

        GUI.Box(new Rect((Screen.width - 200 - 12), 12, 200, 90),
            $"Time  {timer}\nScore  {score}\n{landingFeedback}");

        string message = "";
        if (Time.time < roundStartsAt)
            message = Mathf.CeilToInt(roundStartsAt - Time.time).ToString();
        else if (Time.time >= roundEndsAt)
            message = $"Time Up!  Final Score: {score}";
        else if (Time.time < roundStartsAt + 0.75f)
            message = "GO!";

        if (message.Length > 0)
            GUI.Box(new Rect((Screen.width - 260) / 2f, (Screen.height - 45) / 2f, 260, 45), message); 
    }

    private void ActivateSkateboard()
    {
        FindAnyObjectByType<HandToSkateboardHookup>().isActive = true;
    }
}