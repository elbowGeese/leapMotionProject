using Leap;
using UnityEngine;

namespace Fingerboard
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkateboardController))]
    public class SkateboardLeapInput : MonoBehaviour
    {
        [SerializeField] private LeapProvider provider;

        [Header("Steering")]
        [SerializeField] private float steeringCenterX = 0f;
        [SerializeField, Min(0f)] private float steeringDeadZone = 0.025f;
        [SerializeField, Min(0.01f)] private float fullTurnDistance = 0.12f;
        [SerializeField] private bool invertSteering;

        [Header("Hand Poses")]
        [SerializeField, Range(0f, 1f)] private float fistStrength = 0.80f;
        [SerializeField, Min(0f)] private float poseHoldTime = 0.03f;
        [SerializeField, Min(0f)] private float neutralHoldTime = 0.04f;

        [Header("Pop Shove-it")]
        [SerializeField, Range(5f, 45f)] private float popShoveItFingerAngle = 18f;
        [SerializeField, Min(0f)] private float popShoveItHoldTime = 0.05f;

        [Header("Ollie")]
        [SerializeField, Min(0f)] private float wristLiftSpeed = 0.12f;
        [SerializeField, Min(0f)] private float wristLeadSpeed = 0.07f;
        [SerializeField, Min(0f)] private float handLiftSpeed = 0.45f;
        [SerializeField, Min(0f)] private float ollieWindow = 0.28f;
        [SerializeField, Min(0.01f)] private float ollieMinRise = 0.055f;
        [SerializeField, Range(0f, 0.15f)] private float ollieConfirmTime = 0.06f;

        [SerializeField] private bool logGestures;

        private enum Gesture { None, Ollie, Grind, PopShoveIt, Kickflip }

        private const float TrackingTimeout = 0.15f;
        private const float PoseGapTolerance = 0.06f;
        private const float OllieCandidateTimeout = 0.22f;
        private SkateboardController board;
        private bool hasTracking;
        private bool hasPreviousSample;
        private int trackedHandId;
        private float lastSampleTime;
        private Vector3 previousPalmPosition;
        private float previousWristY;
        private float steering;
        private bool gestureArmed;
        private float neutralStart = -1f;
        private float wristLiftUntil = -1f;
        private float ollieCandidateSince = -1f;
        private float ollieStartHeight;
        private float olliePeakHeight;
        private float ollieStartPalmFacing;
        private Gesture pendingPose;
        private float poseLastSeen;
        private float poseHeldTime;
        private bool poseSeenLastSample;

        private void Awake()
        {
            board = GetComponent<SkateboardController>();
        }

        private void OnEnable()
        {
            if (provider == null)
            {
                Debug.LogWarning("Assign the Desktop Leap Provider on SkateboardLeapInput.", this);
                return;
            }

            provider.OnUpdateFrame += ReadHandFrame;
        }

        private void OnDisable()
        {
            if (provider != null) provider.OnUpdateFrame -= ReadHandFrame;
            ClearTracking();
        }

        private void Update()
        {
            if (hasTracking && Time.unscaledTime - lastSampleTime > TrackingTimeout)
                ClearTracking();
        }

        private void ReadHandFrame(Frame frame)
        {
            Hand hand = frame.GetHand(Chirality.Right);
            if (hand == null)
            {
                hasPreviousSample = false;
                pendingPose = Gesture.None;
                poseSeenLastSample = false;
                neutralStart = -1f;
                ClearOllieCandidate();
                return;
            }

            float now = Time.unscaledTime;
            if (!hasTracking || hand.Id != trackedHandId || now - lastSampleTime > TrackingTimeout)
            {
                ClearTracking();
                hasTracking = true;
                trackedHandId = hand.Id;
            }

            // Tracking data arrives in Unity world space. Convert it into sensor space.
            Transform sensor = provider.transform;
            Vector3 palm = sensor.InverseTransformPoint(hand.PalmPosition);
            Vector3 wrist = sensor.InverseTransformPoint(hand.WristPosition);
            float palmFacingY = sensor.InverseTransformDirection(hand.PalmNormal).y;

            float dt = now - lastSampleTime;
            bool hasVelocity = hasPreviousSample && dt > 0.001f;
            Vector3 palmVelocity = hasVelocity
                ? (palm - previousPalmPosition) / dt
                : Vector3.zero;
            float palmSpeed = palmVelocity.y;
            float wristSpeed = hasVelocity ? (wrist.y - previousWristY) / dt : 0f;

            UpdateSteering(palm.x, hasVelocity ? dt : 1f / 60f);
            lastSampleTime = now;
            float previousPalmHeight = previousPalmPosition.y;
            previousPalmPosition = palm;
            previousWristY = wrist.y;
            hasPreviousSample = true;

            int extendedFingers = 0;
            for (int i = 1; i <= 4; i++)
            {
                if (hand.fingers[i].IsExtended) extendedFingers++;
            }

            bool openHand = extendedFingers >= 3;
            bool palmDown = palmFacingY <= -0.55f;
            bool neutral = palmDown && openHand && Mathf.Abs(palmSpeed) < 0.22f;

            if (!gestureArmed)
            {
                if (!neutral)
                {
                    neutralStart = -1f;
                    return;
                }

                if (neutralStart < 0f) neutralStart = now;
                gestureArmed = now - neutralStart >= neutralHoldTime;
                return;
            }

            Gesture pose = Gesture.None;
            if (extendedFingers <= 1 && hand.GrabStrength >= fistStrength)
                pose = Gesture.Grind;
            else if (openHand && palmFacingY >= 0.65f)
                pose = Gesture.Kickflip;
            else if (palmDown && extendedFingers == 2 &&
                     hand.fingers[1].IsExtended && hand.fingers[2].IsExtended &&
                     Vector3.Angle(hand.fingers[1].Direction, hand.fingers[2].Direction)
                         >= popShoveItFingerAngle)
                pose = Gesture.PopShoveIt;

            if (pose != Gesture.None)
            {
                ClearOllieCandidate();
                bool continuingPose = pose == pendingPose &&
                                      now - poseLastSeen <= PoseGapTolerance;
                if (!continuingPose)
                {
                    pendingPose = pose;
                    poseHeldTime = 0f;
                }
                else if (poseSeenLastSample)
                {
                    poseHeldTime += now - poseLastSeen;
                }

                poseLastSeen = now;
                poseSeenLastSample = true;
                float holdTime = pose == Gesture.PopShoveIt ? popShoveItHoldTime : poseHoldTime;
                if (continuingPose && poseHeldTime >= holdTime)
                    FireGesture(pose);
                return;
            }

            poseSeenLastSample = false;
            if (now - poseLastSeen > PoseGapTolerance)
                pendingPose = Gesture.None;
            if (!hasVelocity || !openHand || palmFacingY > -0.35f)
            {
                ClearOllieCandidate();
                return;
            }

            CheckOllie(now, previousPalmHeight, palm.y,
                       palmVelocity, wristSpeed, palmFacingY);
        }

        private void CheckOllie(float now, float previousHeight, float height,
                                Vector3 palmVelocity, float wristSpeed, float palmFacingY)
        {
            if (ollieCandidateSince >= 0f)
            {
                float elapsed = now - ollieCandidateSince;
                float rise = height - ollieStartHeight;

                // Let a palm flip finish instead of consuming it as an Ollie.
                if (elapsed > OllieCandidateTimeout ||
                    palmFacingY - ollieStartPalmFacing > 0.28f || rise < -0.01f)
                {
                    ClearOllieCandidate();
                    return;
                }

                olliePeakHeight = Mathf.Max(olliePeakHeight, height);
                if (elapsed >= ollieConfirmTime && olliePeakHeight - ollieStartHeight >= ollieMinRise)
                    FireGesture(Gesture.Ollie);
                return;
            }

            float palmSpeed = palmVelocity.y;
            if (wristSpeed >= wristLiftSpeed && wristSpeed - palmSpeed >= wristLeadSpeed)
                wristLiftUntil = now + ollieWindow;

            float horizontalSpeedSquared = palmVelocity.x * palmVelocity.x +
                                           palmVelocity.z * palmVelocity.z;
            bool movingMostlyUp = palmSpeed * palmSpeed >= horizontalSpeedSquared;
            bool palmAndWristLifted = wristSpeed >= handLiftSpeed * 0.65f;

            if (palmSpeed < handLiftSpeed || !movingMostlyUp ||
                (now > wristLiftUntil && !palmAndWristLifted))
                return;

            ollieCandidateSince = now;
            ollieStartHeight = previousHeight;
            olliePeakHeight = height;
            ollieStartPalmFacing = palmFacingY;
        }

        private void ClearOllieCandidate()
        {
            wristLiftUntil = -1f;
            ollieCandidateSince = -1f;
        }

        private void UpdateSteering(float palmX, float dt)
        {
            float offset = palmX - steeringCenterX;
            float range = Mathf.Max(fullTurnDistance, steeringDeadZone + 0.01f);
            float target = Mathf.InverseLerp(steeringDeadZone, range, Mathf.Abs(offset));
            target *= Mathf.Sign(offset);
            if (invertSteering) target = -target;

            float blend = 1f - Mathf.Exp(-18f * dt);
            steering = Mathf.Lerp(steering, target, blend);
            if (target == 0f && Mathf.Abs(steering) < 0.01f) steering = 0f;
            board.SetSideInput(steering);
        }

        private void FireGesture(Gesture gesture)
        {
            gestureArmed = false;
            neutralStart = -1f;
            ClearOllieCandidate();
            pendingPose = Gesture.None;
            poseSeenLastSample = false;

            switch (gesture)
            {
                case Gesture.Ollie:
                    board.RequestOllie();
                    break;
                case Gesture.Grind:
                    board.RequestGrind();
                    break;
                case Gesture.PopShoveIt:
                    board.RequestOllie();
                    board.RequestPopShoveIt();
                    break;
                case Gesture.Kickflip:
                    board.RequestOllie();
                    board.RequestKickflip();
                    break;
            }

            if (logGestures) Debug.Log("Leap gesture: " + gesture, this);
        }

        private void ClearTracking()
        {
            hasTracking = false;
            hasPreviousSample = false;
            gestureArmed = false;
            neutralStart = -1f;
            ClearOllieCandidate();
            pendingPose = Gesture.None;
            poseSeenLastSample = false;
            steering = 0f;
            if (board != null) board.SetSideInput(0f);
        }
    }
}
