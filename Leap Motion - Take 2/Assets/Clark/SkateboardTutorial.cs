using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Trick = Fingerboard.SkateboardController.Trick;

namespace Fingerboard
{
    public class SkateboardTutorial : MonoBehaviour
    {
        private enum Step { Default, Steering, Ollie, PopShoveIt, Kickflip, Grinding, Complete }

        private static readonly string[] Titles =
        {
            "DEFAULT", "STEERING", "OLLIE", "POP SHOVE-IT", "KICKFLIP", "GRINDING"
        };

        private static readonly string[] Instructions =
        {
            "Hold your right hand flat, palm down.",
            "Keep your palm down and move your hand left and right.",
            "Start in Default, then lift your hand quickly. Land the ollie.",
            "Start in Default, then spread your index and middle fingers. Land the trick.",
            "Start in Default, then turn your open palm up. Land the trick.",
            "Make a fist when your board is next to the rail."
        };

        [Header("Player")]
        [SerializeField] private SkateboardLeapInput leapInput;
        [SerializeField] private Transform railCheckpoint;
        [SerializeField] private string nextSceneName;

        [Header("Intro")]
        [SerializeField] private Image introImage;

        [Header("Right Panel")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Image gestureImage;
        [SerializeField] private Sprite[] gestureSprites = new Sprite[6];

        [Header("Left Preview")]
        [SerializeField] private Camera demoCamera;

        [Header("Success Feedback")]
        [SerializeField] private Image successOverlay;

        private const float IntroHoldDuration = 5f;
        private const float IntroFadeDuration = 0.6f;
        private const float StepDelay = 1f;
        private const float FlashDuration = 0.35f;
        private const float FlashOpacity = 0.45f;

        private SkateboardController board;
        private Step step;
        private float neutralTime;
        private float leftTime;
        private float rightTime;
        private bool turnedLeft;
        private bool turnedRight;
        private float successStartedAt = -1f;
        private float steeringCenterX;
        private Vector3 demoCameraOffset;
        private Quaternion demoCameraRotation;
        private bool introActive;
        private bool resumeLeapInput;
        private float introStartedAt;

        private Trick ExpectedTrick => step switch
        {
            Step.Ollie => Trick.Ollie,
            Step.PopShoveIt => Trick.PopShoveIt,
            Step.Kickflip => Trick.Kickflip,
            Step.Grinding => Trick.Grinding,
            _ => Trick.None
        };

        private void Start()
        {
            if (leapInput == null)
            {
                Debug.LogError("Assign the player's SkateboardLeapInput to SkateboardTutorial.", this);
                enabled = false;
                return;
            }

            board = leapInput.GetComponent<SkateboardController>();
            board.TrickCompleted += HandleTrickCompleted;
            board.SetTutorialSteering(0f);
            board.SetTutorialAllowedTrick(ExpectedTrick);
            board.SetRoundRunning(false);

            if (successOverlay != null) successOverlay.raycastTarget = false;
            SetSuccessFlashAlpha(0f);

            if (demoCamera != null)
            {
                demoCameraOffset = demoCamera.transform.position - board.transform.position;
                demoCameraRotation = demoCamera.transform.rotation;
            }

            ShowStep();
            if (introImage != null) BeginIntro();
        }

        private void OnDisable()
        {
            if (introActive) EndIntro();
        }

        private void OnDestroy()
        {
            if (board == null) return;

            board.TrickCompleted -= HandleTrickCompleted;
            board.SetTutorialSteering(null);
            board.SetTutorialAllowedTrick(null);
        }

        private void Update()
        {
            if (introActive)
            {
                UpdateIntro();
                return;
            }

            if (successStartedAt >= 0f)
            {
                float elapsed = Time.unscaledTime - successStartedAt;
                SetSuccessFlashAlpha(FlashOpacity * (1f - Mathf.Clamp01(elapsed / FlashDuration)));
                if (elapsed >= StepDelay) Advance();
                return;
            }

            if (step == Step.Default)
            {
                neutralTime = leapInput.IsNeutralPose ? neutralTime + Time.unscaledDeltaTime : 0f;
                if (neutralTime >= 0.35f) BeginStepSuccess();
            }
            else if (step == Step.Steering)
            {
                CheckSteering();
            }
        }

        private void LateUpdate()
        {
            if (demoCamera != null && board != null)
                demoCamera.transform.SetPositionAndRotation(
                    board.transform.position + demoCameraOffset, demoCameraRotation);
        }

        private void BeginIntro()
        {
            introActive = true;
            introStartedAt = Time.unscaledTime;
            resumeLeapInput = leapInput.enabled;
            leapInput.enabled = false;

            introImage.enabled = true;
            SetIntroAlpha(1f);
        }

        private void UpdateIntro()
        {
            float fadeElapsed = Time.unscaledTime - introStartedAt - IntroHoldDuration;
            if (fadeElapsed < 0f) return;

            float fade = Mathf.Clamp01(fadeElapsed / IntroFadeDuration);
            SetIntroAlpha(1f - fade);
            if (fade >= 1f) EndIntro();
        }

        private void SetIntroAlpha(float alpha)
        {
            if (introImage == null) return;

            Color color = introImage.color;
            color.a = alpha;
            introImage.color = color;
        }

        private void EndIntro()
        {
            if (introImage != null) introImage.enabled = false;

            introActive = false;
            if (leapInput != null) leapInput.enabled = resumeLeapInput;
        }

        private void CheckSteering()
        {
            float axis = ReadHandSteering();
            board.SetTutorialSteering(axis);
            leftTime = axis <= -0.35f ? leftTime + Time.unscaledDeltaTime : 0f;
            rightTime = axis >= 0.35f ? rightTime + Time.unscaledDeltaTime : 0f;
            turnedLeft |= leftTime >= 0.15f;
            turnedRight |= rightTime >= 0.15f;

            if (turnedLeft && turnedRight) BeginStepSuccess();
        }

        private float ReadHandSteering()
        {
            return board.IsCruising && leapInput.IsNeutralPose
                ? leapInput.ReadSteeringFrom(steeringCenterX)
                : 0f;
        }

        private void HandleTrickCompleted(Trick trick)
        {
            if (successStartedAt < 0f && trick == ExpectedTrick)
                BeginStepSuccess();
        }

        private void BeginStepSuccess()
        {
            if (step == Step.Default) steeringCenterX = leapInput.PalmX;

            successStartedAt = Time.unscaledTime;
            board.SetTutorialAllowedTrick(Trick.None);
            board.SetTutorialSteering(0f);
            SetSuccessFlashAlpha(FlashOpacity);
        }

        private void SetSuccessFlashAlpha(float alpha)
        {
            if (successOverlay == null) return;

            successOverlay.color = new Color(0.2f, 1f, 0.35f, alpha);
            successOverlay.enabled = alpha > 0f;
        }

        private void Advance()
        {
            successStartedAt = -1f;
            step++;
            board.SetTutorialAllowedTrick(ExpectedTrick);

            if (step == Step.Steering) board.SetRoundRunning(true);
            if (step == Step.Grinding && railCheckpoint != null) board.MoveToCheckpoint(railCheckpoint);

            ShowStep();
            if (step == Step.Complete) StartCoroutine(FinishTutorial());
        }

        private void ShowStep()
        {
            bool finished = step == Step.Complete;
            int index = (int)step;
            if (titleText != null) titleText.text = finished ? "TUTORIAL COMPLETE" : Titles[index];
            if (instructionText != null) instructionText.text = finished ? "" : Instructions[index];

            if (gestureImage != null)
            {
                gestureImage.sprite = !finished && index < gestureSprites.Length ? gestureSprites[index] : null;
                gestureImage.preserveAspect = true;
                gestureImage.enabled = gestureImage.sprite != null;
            }
        }

        private IEnumerator FinishTutorial()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            board.SetRoundRunning(false);

            if (string.IsNullOrWhiteSpace(nextSceneName)) yield break;
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError($"Add '{nextSceneName}' to the game's scene list.", this);
        }
    }
}
