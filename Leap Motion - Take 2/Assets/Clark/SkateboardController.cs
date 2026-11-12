using UnityEngine;

namespace Fingerboard
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class SkateboardController : MonoBehaviour
    {
        // scoring
        private RoundManager rm;
        [HideInInspector] public bool isBraced = false;

        public enum State { Grounded, Airborne, Crashed }
        public enum Trick { None, Ollie, Kickflip }

        [Header("Board Visual")]
        [SerializeField] private Transform boardVisual;
        [SerializeField] private Vector3 rearAxle = new Vector3(0f, 0.065f, -0.26f);
        [SerializeField] private float manualAngle = 18f;
        [SerializeField, Min(1f)] private float slopeFollowSpeed = 16f;

        [Header("Movement")]
        [SerializeField] private float maxSpeed = 4f;
        [SerializeField, Min(0f)] private float turnSpeed = 120f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float coastDeceleration = 2f;
        [SerializeField] private float jumpSpeed = 3.8f;
        [SerializeField, Min(0.05f)] private float ollieReadyTime = 0.32f;
        [SerializeField, Min(0.05f)] private float kickflipReadyTime = 0.58f;

        private const float MinGroundNormalY = 0.65f;
        private const float GroundGap = 0.045f;
        private const float TakeoffGrace = 0.06f;
        private const float WallCrashSpeed = 1.5f;
        private const float LandingTime = 0.16f;

        private Rigidbody body;
        private SphereCollider wheelContact;
        private readonly RaycastHit[] edgeGroundHits = new RaycastHit[12];
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 visualRestPosition;
        private Quaternion visualRestRotation;
        private Vector3 incomingVelocity;
        private Vector3 groundNormal = Vector3.up;
        private float groundHeight;
        private Vector3 visualUp = Vector3.up;
        private float visualHeight;
        private float manualTilt;

        private State state;
        private Trick queuedTrick;
        private Trick activeTrick;
        private float airTime;
        private float landingTimer;
        private float pushAmount;
        private float turnInput;
        private bool manualHeld;
        private bool resetRequested;
        private string crashReason;

        private float TrickDuration =>
            activeTrick == Trick.Kickflip ? kickflipReadyTime : ollieReadyTime;

        public string Status
        {
            get
            {
                if (state == State.Crashed) return "Crashed: " + crashReason;
                if (state == State.Airborne)
                    return activeTrick == Trick.None ? "In the air" : activeTrick.ToString();
                if (landingTimer > 0f) return "Landed";
                return manualHeld ? "Manual" : "On ground";
            }
        }

        private Vector3 Velocity
        {
#if UNITY_6000_0_OR_NEWER
            get => body.linearVelocity;
            set => body.linearVelocity = value;
#else
            get => body.velocity;
            set => body.velocity = value;
#endif
        }

        private void Awake()
        {
            rm = FindAnyObjectByType<RoundManager>();

            body = GetComponent<Rigidbody>();
            wheelContact = GetComponent<SphereCollider>();
            spawnPosition = body.position;
            spawnRotation = body.rotation;

            body.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            body.constraints |= RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;

            if (boardVisual != null)
            {
                visualRestPosition = boardVisual.localPosition;
                visualRestRotation = boardVisual.localRotation;
            }
        }




        //Call these from your script!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//
        //below//
        public void SetPushInput(float amount) => pushAmount = Mathf.Clamp01(amount);
        public void SetSideInput(float amount) => turnInput = Mathf.Clamp(amount, -1f, 1f);
        public void SetManual(bool held) => manualHeld = held;
        public void RequestOllie() => queuedTrick = Trick.Ollie;
        public void RequestKickflip() => queuedTrick = Trick.Kickflip;
        public void RequestReset() => resetRequested = true;





        private void FixedUpdate()
        {
            if (resetRequested)
            {
                ResetBoard();
                return;
            }

            Trick requested = queuedTrick;
            queuedTrick = Trick.None;

            if (state == State.Crashed) return;

            landingTimer = Mathf.Max(0f, landingTimer - Time.fixedDeltaTime);

            bool hasGround = activeTrick == Trick.None && ReadGround();

            if (state == State.Airborne && hasGround && Velocity.y <= 0.05f)
            {
                state = State.Grounded;
            }

            if (state == State.Grounded)
            {
                if (!hasGround)
                {
                    BeginAir(Trick.None);
                }
                else if (requested != Trick.None)
                {
                    Vector3 alongGround = Vector3.ProjectOnPlane(Velocity, groundNormal);
                    Velocity = alongGround + Vector3.up * jumpSpeed;
                    BeginAir(requested);
                }
                else
                {
                    MoveOnGround();
                }
            }

            if (state == State.Airborne)
                airTime += Time.fixedDeltaTime;

            incomingVelocity = Velocity;

            if (body.position.y < spawnPosition.y - 4f)
                Crash("fell off the track");
        }

        private bool ReadGround()
        {
            Vector3 center = body.position + body.rotation * wheelContact.center;
            float radius = wheelContact.radius;
            float rayLength = (radius + GroundGap) / MinGroundNormalY;

            if (Physics.Raycast(center, Vector3.down, out RaycastHit hit, rayLength,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                hit.rigidbody != body &&
                hit.normal.y >= MinGroundNormalY &&
                hit.distance * hit.normal.y <= radius + GroundGap)
            {
                groundNormal = hit.normal;
                groundHeight = hit.point.y - body.position.y;
                return true;
            }

            const float minEdgeNormalY = 0.5f;
            const float probeLift = 0.04f;
            float probeRadius = radius * 0.95f;
            float inset = radius - probeRadius;
            float probeLength = probeLift + (inset + GroundGap) / minEdgeNormalY;

            int hitCount = Physics.SphereCastNonAlloc(
                center + Vector3.up * probeLift, probeRadius, Vector3.down,
                edgeGroundHits, probeLength, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit edgeHit = edgeGroundHits[i];

                if (edgeHit.rigidbody == body || edgeHit.distance <= 0f ||
                    edgeHit.normal.y < minEdgeNormalY)
                    continue;

                float gap = (edgeHit.distance - probeLift) * edgeHit.normal.y - inset;
                if (gap > GroundGap) continue;

                groundHeight = center.y - radius / groundNormal.y - body.position.y;
                return true;
            }

            return false;
        }

        private void MoveOnGround()
        {
            body.angularVelocity = Vector3.up * (turnInput * turnSpeed * Mathf.Deg2Rad);
            if (pushAmount <= 0f && groundNormal.y < 0.999f)
                return;

            Vector3 forward = body.rotation * Vector3.forward;
            Vector3 right = body.rotation * Vector3.right;

            float forwardSpeed = Vector3.Dot(Velocity, forward);
            float sidewaysSpeed = Vector3.Dot(Velocity, right);
            float forwardRate = pushAmount > 0f ? acceleration : coastDeceleration;

            float nextForward = Mathf.MoveTowards(
                forwardSpeed, maxSpeed * pushAmount, forwardRate * Time.fixedDeltaTime);

            float nextSideways = Mathf.MoveTowards(
                sidewaysSpeed, 0f, acceleration * Time.fixedDeltaTime);

            Vector3 change = forward * (nextForward - forwardSpeed) +
                             right * (nextSideways - sidewaysSpeed);

            body.AddForce(Vector3.ProjectOnPlane(change, groundNormal),
                ForceMode.VelocityChange);
        }

        private void BeginAir(Trick trick)
        {
            body.angularVelocity = Vector3.zero;
            state = State.Airborne;
            activeTrick = trick;
            airTime = 0f;
            landingTimer = 0f;
        }

        private void OnCollisionEnter(Collision collision) => CheckCollision(collision);
        private void OnCollisionStay(Collision collision) => CheckCollision(collision);

        private void CheckCollision(Collision collision)
        {
            if (state == State.Crashed) return;

            bool touchedGround = false;

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float speedIntoSurface = -Vector3.Dot(incomingVelocity, normal);

                if (Mathf.Abs(normal.y) < 0.2f && speedIntoSurface >= WallCrashSpeed)
                {
                    Crash("hit a wall");
                    return;
                }

                if (normal.y >= MinGroundNormalY && speedIntoSurface >= -0.05f)
                    touchedGround = true;
            }

            if (state != State.Airborne || !touchedGround) return;

            if (activeTrick != Trick.None && airTime < TakeoffGrace) return;

            if (activeTrick != Trick.None && airTime < TrickDuration)
            {
                Crash("landed before the trick finished");
                return;
            }

            Debug.Log("WAS AIRBORNE NOW GROUNDED");
            if (rm != null)
            {
                rm.Land(activeTrick, airTime, isBraced);
            }

            state = State.Grounded;
            activeTrick = Trick.None;
            landingTimer = LandingTime;
            ReadGround();
        }

        private void Crash(string reason)
        {
            body.angularVelocity = Vector3.zero;
            state = State.Crashed;
            crashReason = reason;
            activeTrick = Trick.None;
            landingTimer = 0f;
            Velocity = new Vector3(0f, Mathf.Min(Velocity.y, 0f), 0f);
        }

        private void ResetBoard()
        {
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.angularVelocity = Vector3.zero;
            Velocity = Vector3.zero;
            incomingVelocity = Vector3.zero;

            state = State.Grounded;
            activeTrick = Trick.None;
            queuedTrick = Trick.None;
            resetRequested = false;
            crashReason = null;

            airTime = 0f;
            landingTimer = 0f;
            groundNormal = Vector3.up;
            groundHeight = 0f;
            visualUp = Vector3.up;
            visualHeight = 0f;
            manualTilt = 0f;
        }

        private void LateUpdate()
        {
            if (boardVisual == null) return;

            bool grounded = state == State.Grounded;
            Vector3 targetUp = grounded
                ? transform.InverseTransformDirection(groundNormal)
                : Vector3.up;

            float blend = 1f - Mathf.Exp(-slopeFollowSpeed * Time.deltaTime);
            visualUp = Vector3.Slerp(visualUp, targetUp, blend);
            visualHeight = Mathf.Lerp(visualHeight, grounded ? groundHeight : 0f, blend);

            float targetManual = grounded && manualHeld ? manualAngle : 0f;
            manualTilt = Mathf.MoveTowards(manualTilt, targetManual, 150f * Time.deltaTime);

            Vector3 angles = Vector3.zero;
            Vector3 pivot = Vector3.zero;
            float lift = 0f;

            if (state == State.Crashed)
            {
                angles.z = 65f;
                pivot.y = 0.14f;
                lift = 0.08f;
            }
            else if (state == State.Airborne && activeTrick != Trick.None)
            {
                float timeSincePhysics = Mathf.Clamp(
                    Time.time - Time.fixedTime, 0f, Time.fixedDeltaTime);

                float visualAirTime = airTime - Time.fixedDeltaTime + timeSincePhysics;
                float progress = Mathf.Clamp01(visualAirTime / TrickDuration);

                angles.x = -25f * Mathf.Sin(progress * Mathf.PI);

                if (activeTrick == Trick.Kickflip)
                    angles.z = Mathf.SmoothStep(0f, 360f, progress);

                pivot.y = 0.14f;
            }
            else if (grounded && landingTimer > 0f)
            {
                float progress = 1f - landingTimer / LandingTime;
                angles.x = 7f * Mathf.Sin(progress * Mathf.PI);
            }
            else if (grounded)
            {
                angles.x = -manualTilt;
                pivot = rearAxle;
            }

            Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, visualUp);
            Quaternion trickRotation = Quaternion.Euler(angles);
            Vector3 posePosition = pivot + trickRotation * (visualRestPosition - pivot);

            boardVisual.localPosition =
                slopeRotation * posePosition + Vector3.up * (visualHeight + lift);

            boardVisual.localRotation =
                slopeRotation * trickRotation * visualRestRotation;
        }
    }
}