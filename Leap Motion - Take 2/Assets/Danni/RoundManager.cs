using Fingerboard;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    private enum Phase { Countdown, Playing, Finished }

    [SerializeField, Min(0)] private int countdownSeconds = 3;
    [SerializeField, Min(1)] private int roundSeconds = 60;
    [SerializeField] private SkateboardController board;

    private Phase phase;
    private float roundStartsAt;
    private float roundEndsAt;
    private float timeScaleBeforeFinish;

    private GUIStyle roundTextStyle;

    private void Start()
    {
        if (board == null) board = FindAnyObjectByType<SkateboardController>();

        roundStartsAt = Time.time + countdownSeconds;
        roundEndsAt = roundStartsAt + roundSeconds;

        if (countdownSeconds == 0)
            StartRound();
        else if (board != null)
            board.SetRoundRunning(false);
    }

    private void Update()
    {
        if (phase == Phase.Countdown && Time.time >= roundStartsAt)
            StartRound();

        if (phase == Phase.Playing && Time.time >= roundEndsAt)
            FinishRound();
    }

    private void StartRound()
    {
        phase = Phase.Playing;
        if (board != null) board.SetRoundRunning(true);
    }

    private void FinishRound()
    {
        phase = Phase.Finished;
        if (board != null) board.SetRoundRunning(false);
        timeScaleBeforeFinish = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (phase == Phase.Finished && Time.timeScale == 0f)
            Time.timeScale = timeScaleBeforeFinish;
    }

    private void OnGUI()
    {
        int score = board != null ? board.Score : 0;
        int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(roundEndsAt - Time.time), 0, roundSeconds);
        string timer = $"Time  {secondsLeft / 60}:{secondsLeft % 60:00}";

        bool showScoreHere = board == null || !board.ShowsScoreOnHud;
        string hud = showScoreHere ? $"{timer}\nScore  {score}" : timer;
        roundTextStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter
        };

        roundTextStyle.fontSize = 24;
        GUI.Box(new Rect(12, 12, 220, showScoreHere ? 76 : 42), hud, roundTextStyle);

        string message = "";
        if (phase == Phase.Countdown)
            message = Mathf.CeilToInt(roundStartsAt - Time.time).ToString();
        else if (phase == Phase.Finished)
            message = $"Time Up!  Final Score: {score}";
        else if (Time.time < roundStartsAt + 0.75f)
            message = "GO!";

        if (message.Length > 0)
        {
            roundTextStyle.fontSize = 36;
            GUI.Box(
                new Rect((Screen.width - 540) / 2f, (Screen.height - 80) / 2f, 540, 80),
                message,
                roundTextStyle
            );
        }
    }
}
