using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Fingerboard
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class SkateboardController : MonoBehaviour
    {
        [System.Serializable]
        private class RampSide
        {
            public BoxCollider takeoffTrigger;
            public Transform landing;
        }

        private enum State { Grounded, Airborne, RampAir, Grinding, Crashed }
        public enum Trick { None, Kickflip, PopShoveIt, Ollie, Grinding }

        [Header("Board")]
        [SerializeField] private Transform boardVisual;
        [SerializeField, Min(0.1f)] private float maxSpeed = 4f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float jumpSpeed = 3.8f;
        [SerializeField, Min(0.05f)] private float ollieReadyTime = 0.32f;
        [SerializeField, Min(1f)] private float slopeFollowSpeed = 16f;
        [SerializeField, Min(0.05f)] private float kickflipReadyTime = 0.58f;
        [SerializeField, Min(0.05f)] private float shoveItTime = 0.4f;

        [Header("Half-pipe")]
        [SerializeField] private RampSide[] rampSides;
        [SerializeField, Min(0.1f)] private float rampAirHeight = 1.2f;
        [SerializeField, Min(0.2f)] private float rampAirDuration = 1.1f;

        [Header("Optional Cameras")]
        [SerializeField] private Camera followCamera;
        [SerializeField] private Camera airCamera;
        [SerializeField] private Vector3 airCameraOffset = new Vector3(3f, 1.3f, -3f);

        [Header("Scoring")]
        [SerializeField, Min(0)] private int pointsPerSecond = 10;
        [SerializeField, Min(0)] private int olliePoints = 40;
        [SerializeField, Min(0)] private int flipPoints = 100;
        [SerializeField, Min(0)] private int grindPointsPerSecond = 40;
        [SerializeField] private TMP_Text scoreText;

        [Header("Trick Feed")]
        [SerializeField] private SkateboardTrickFeed trickFeed;

        [Header("Skate Audio")]
        [SerializeField] private AudioSource cruisingSource;
        [SerializeField] private AudioClip cruisingClip;
        [SerializeField] private AudioClip grindingClip;
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private AudioClip landingClip;
        [SerializeField] private AudioClip hurtClip;

        [Header("Scene Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;

        [Header("Crash and Respawn")]
        [SerializeField] private Image crashOverlay;
        [SerializeField, Range(0f, 1f)] private float crashFlashOpacity = 0.55f;
        [SerializeField, Min(0.01f)] private float crashFlashDuration = 0.45f;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        public int Score { get; private set; }
        public bool ShowsScoreOnHud => scoreText != null && scoreText.isActiveAndEnabled;
        public bool IsCruising => roundRunning && state == State.Grounded;
        public event System.Action<Trick> TrickCompleted;

        private bool roundRunning = true;
        private bool roundHoldingBody;
        private double scoreTime;
        private int completedAirTricks;
        private Trick lastCompletedFlip;
        private float grindTime;

        private const float MinGroundY = 0.1f;
        private const float GroundGap = 0.045f;

        private Rigidbody body;
        private SphereCollider contactSphere;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 visualRestPosition;
        private Quaternion visualRestRotation;
        private Vector3 groundNormal = Vector3.up;
        private Vector3 contactNormal;
        private bool hasGroundContact;
        private Vector3 incomingVelocity;
        private Vector3 visualUp = Vector3.up;
        private Vector3 visualOffset;
        private float turnInput;
        private float? tutorialSteeringInput;
        private Trick? tutorialAllowedTrick;

        private State state;
        private bool ollieRequested;
        private bool isOllie;
        private bool resetRequested;
        private Trick queuedTrick;
        private Trick activeTrick;
        private float airTime;
        private float trickTime;
        private float deckYaw;
        private string crashReason;
        private float crashedAt;

        private Vector3 rampStart;
        private Vector3 rampEnd;
        private Vector3 rampOut;
        private Vector3 rampLandingNormal;
        private Quaternion rampStartRotation;
        private Quaternion rampEndRotation;
        private Quaternion rampCameraRotation;
        private float rampPeakY;
        private float rampTime;
        private CollisionDetectionMode groundCollisionMode;

        public string Status
        {
            get
            {
                if (state == State.Crashed) return "Crashed: " + crashReason;
                if (activeTrick != Trick.None) return activeTrick.ToString();
                if (state == State.RampAir) return "Half-pipe air";
                if (state == State.Grinding) return "Boardslide";
                if (state == State.Airborne) return "Airborne";
                return "Cruising";
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
            body = GetComponent<Rigidbody>();
            contactSphere = GetComponent<SphereCollider>();
            spawnPosition = body.position;
            spawnRotation = body.rotation;
            groundCollisionMode = body.collisionDetectionMode;
            body.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            body.constraints |= RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;

            if (boardVisual != null)
            {
                visualRestPosition = boardVisual.localPosition;
                visualRestRotation = boardVisual.localRotation;
            }

            if (cruisingSource != null)
            {
                cruisingSource.Stop();
                cruisingSource.clip = null;
                cruisingSource.playOnAwake = false;
                cruisingSource.loop = true;
            }

            SetCrashOverlayAlpha(0f);
            SetScore(0);
            SetAirCamera(false);
        }

        private void Start()
        {
            if (musicSource == null || musicClip == null) return;

            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        private void OnDisable()
        {
            if (cruisingSource != null)
                cruisingSource.Stop();

            SetCrashOverlayAlpha(0f);
        }

        private void Update()
        {
            if (!roundRunning || state != State.Crashed) return;

            float elapsed = Time.unscaledTime - crashedAt;
            if (elapsed >= respawnDelay)
            {
                RespawnBoard();
                return;
            }

            float fade = Mathf.Clamp01(elapsed / crashFlashDuration);
            SetCrashOverlayAlpha(crashFlashOpacity * (1f - fade));
        }

        public void SetRoundRunning(bool running)
        {
            if (roundRunning == running) return;

            roundRunning = running;
            turnInput = 0f;
            ollieRequested = false;
            grindRequested = false;
            queuedTrick = Trick.None;
            resetRequested = false;

            if (running)
            {
                if (roundHoldingBody)
                {
                    body.isKinematic = false;
                    body.collisionDetectionMode = groundCollisionMode;
                    roundHoldingBody = false;
                }

                return;
            }

            if (!body.isKinematic)
            {
                Velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.isKinematic = true;
                roundHoldingBody = true;
            }

            SetCrashOverlayAlpha(0f);
        }

        public void SetSideInput(float amount) => turnInput = Mathf.Clamp(amount, -1f, 1f);
        public void SetTutorialSteering(float? amount) => tutorialSteeringInput = amount;

        public void SetTutorialAllowedTrick(Trick? trick)
        {
            tutorialAllowedTrick = trick;
            ollieRequested = false;
            grindRequested = false;
            queuedTrick = Trick.None;
        }

        public void RequestOllie() => ollieRequested = true;
        public void RequestKickflip() => queuedTrick = Trick.Kickflip;
        public void RequestPopShoveIt() => queuedTrick = Trick.PopShoveIt;
        public void RequestReset() => resetRequested = true;

        public void MoveToCheckpoint(Transform checkpoint)
        {
            spawnPosition = checkpoint.position;
            spawnRotation = checkpoint.rotation;
            RespawnBoard();
        }

        private void FixedUpdate()
        {
            if (!roundRunning) return;

            if (resetRequested)
            {
                ResetBoard();
                return;
            }

            bool ollie = ollieRequested;
            bool grind = grindRequested;
            grindRequested = false;
            Trick requested = queuedTrick;
            ollieRequested = false;
            queuedTrick = Trick.None;

            if (tutorialAllowedTrick is Trick allowed)
            {
                bool allowedFlip = requested != Trick.None && requested == allowed;
                // A flip's jump request must be allowed or rejected together with the flip.
                ollie &= allowedFlip || (allowed == Trick.Ollie && requested == Trick.None);
                grind &= allowed == Trick.Grinding;
                if (!allowedFlip) requested = Trick.None;
            }

            if (state == State.Crashed) return;

            TickScore();

            if (state == State.RampAir)
            {
                UpdateRampAir(requested);
                return;
            }

            if (state == State.Grinding)
            {
                UpdateGrind(ollie, requested);
                return;
            }

            if (grind && TryStartGrind()) return;



            if (state == State.Grounded)
            {
                if (!ReadGround())
                {
                    state = State.Airborne;
                    isOllie = false;
                    airTime = 0f;
                    body.angularVelocity = Vector3.zero;
                }
                else if (ollie)
                {
                    Vector3 heading = body.rotation * Vector3.forward;
                    float forwardSpeed = tutorialSteeringInput.HasValue ? 0f : maxSpeed;
                    Velocity = heading * forwardSpeed + Vector3.up * jumpSpeed;
                    body.angularVelocity = Vector3.zero;
                    state = State.Airborne;
                    isOllie = true;
                    airTime = 0f;

                    if (requested == Trick.None && trickFeed != null)
                        trickFeed.ShowOllie();
                }
                else
                {
                    float steering = tutorialSteeringInput ?? turnInput;
                    if (tutorialSteeringInput.HasValue && Mathf.Abs(steering) < 0.12f)
                        steering = 0f;

                    float speed = tutorialSteeringInput.HasValue && steering == 0f ? 0f : maxSpeed;
                    body.angularVelocity = Vector3.up * (steering * turnSpeed * Mathf.Deg2Rad);
                    Vector3 heading = body.rotation * Vector3.forward;
                    Velocity = Vector3.ProjectOnPlane(heading, groundNormal).normalized * speed;
                }
            }

            hasGroundContact = false;
            if (state == State.Airborne)
            {
                airTime += Time.fixedDeltaTime;
                UpdateTrick(requested, Time.fixedDeltaTime);
            }

            incomingVelocity = Velocity;
            if (body.position.y < spawnPosition.y - 5f)
                Crash("fell off the track");
        }

        private bool ReadGround()
        {
            if (hasGroundContact)
            {
                groundNormal = contactNormal;
                return true;
            }

            Vector3 center = body.position + body.rotation * contactSphere.center;
            float reach = contactSphere.radius + GroundGap;

            if (!Physics.Raycast(center, Vector3.down, out RaycastHit hit,
                    reach / MinGroundY, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            if (hit.rigidbody == body || hit.normal.y < MinGroundY ||
                hit.distance * hit.normal.y > reach)
                return false;

            groundNormal = hit.normal;
            return true;
        }

        private void OnCollisionEnter(Collision collision) => ReadContacts(collision);
        private void OnCollisionStay(Collision collision) => ReadContacts(collision);

        private void ReadContacts(Collision collision)
        {
            if (!roundRunning || state == State.RampAir ||
                state == State.Grinding || state == State.Crashed)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (contact.thisCollider != contactSphere) continue;

                Vector3 normal = contact.normal;
                float impactSpeed = -Vector3.Dot(incomingVelocity, normal);

                if (normal.y < MinGroundY)
                {
                    if (impactSpeed > 1.5f)
                    {
                        Crash("hit a wall");
                        return;
                    }

                    continue;
                }

                if (!hasGroundContact || normal.y > contactNormal.y)
                    contactNormal = normal;

                hasGroundContact = true;

                if (state == State.Airborne && airTime >= 0.06f && impactSpeed >= -0.05f)
                {
                    groundNormal = normal;
                    Land();
                }
            }
        }

        private void OnTriggerEnter(Collider other) => CheckRampLip(other);
        private void OnTriggerStay(Collider other) => CheckRampLip(other);



        [System.Serializable]
        private class GrindEdge
        {
            public Transform start;
            public Transform end;
        }

        [Header("Boardslide")]
        [SerializeField] private GrindEdge[] grindEdges;
        [SerializeField, Min(0.1f)] private float grindSpeed = 4f;
        [SerializeField, Min(0.05f)] private float grindSnapRange = 0.55f;
        [SerializeField, Min(0.05f)] private float grindSnapHeight = 0.55f;
        [SerializeField, Range(0f, 90f)] private float grindBoardAngle = 60f;
        [SerializeField, Range(0f, 30f)] private float grindNoseUpAngle = 12f;
        [SerializeField, Min(0f)] private float grindDeckHeight = 0.14f;

        private bool grindRequested;
        private Vector3 grindEnd;
        private Vector3 grindDirection;

        public void RequestGrind() => grindRequested = true;

        private bool TryStartGrind()
        {
            if (grindEdges == null || activeTrick != Trick.None) return false;

            Vector3 deckPosition = body.position + Vector3.up * grindDeckHeight;
            Vector3 forward = body.rotation * Vector3.forward;
            float snapRangeSquared = grindSnapRange * grindSnapRange;
            float bestDistanceSquared = float.PositiveInfinity;
            Vector3 snapPoint = Vector3.zero;
            bool found = false;

            foreach (GrindEdge edge in grindEdges)
            {
                if (edge == null || edge.start == null || edge.end == null)
                    continue;

                Vector3 start = edge.start.position;
                Vector3 end = edge.end.position;
                Vector3 line = end - start;
                Vector3 flatLine = Vector3.ProjectOnPlane(line, Vector3.up);
                if (flatLine.sqrMagnitude < 0.0025f) continue;

                float fraction = Mathf.Clamp01(
                    Vector3.Dot(deckPosition - start, flatLine) / flatLine.sqrMagnitude);
                Vector3 closest = start + line * fraction;
                Vector3 offset = closest - deckPosition;
                float horizontalDistanceSquared = offset.x * offset.x + offset.z * offset.z;

                if (horizontalDistanceSquared > snapRangeSquared ||
                    Mathf.Abs(offset.y) > grindSnapHeight ||
                    offset.sqrMagnitude >= bestDistanceSquared)
                    continue;

                float alignment = Vector3.Dot(forward, flatLine.normalized);
                bool towardEnd = Mathf.Abs(alignment) >= 0.15f
                    ? alignment > 0f
                    : fraction <= 0.5f;

                Vector3 exit = towardEnd ? end : start;
                if ((exit - closest).sqrMagnitude < 0.01f)
                    exit = towardEnd ? start : end;

                Vector3 travel = exit - closest;
                if (travel.sqrMagnitude < 0.01f) continue;

                bestDistanceSquared = offset.sqrMagnitude;
                snapPoint = closest;
                grindEnd = exit;
                grindDirection = travel.normalized;
                found = true;
            }

            if (!found) return false;

            BankAirScore();
            grindTime = 0f;

            Quaternion heading = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(grindDirection, Vector3.up), Vector3.up);

            Velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            state = State.Grinding;
            hasGroundContact = false;
            body.MovePosition(snapPoint);
            body.MoveRotation(heading);

            if (trickFeed != null)
                trickFeed.ShowGrinding();

            TrickCompleted?.Invoke(Trick.Grinding);
            return true;
        }

        private void UpdateGrind(bool ollie, Trick requested)
        {
            float distanceLeft = Vector3.Distance(body.position, grindEnd);
            bool reachedEnd = distanceLeft < 0.01f;

            if (ollie || reachedEnd)
            {
                AddScore(Mathf.RoundToInt(grindTime * grindPointsPerSecond));
                grindTime = 0f;
                body.isKinematic = false;
                body.collisionDetectionMode = groundCollisionMode;
                Velocity = grindDirection * grindSpeed + Vector3.up * (ollie ? jumpSpeed : 0.4f);
                incomingVelocity = Velocity;
                state = State.Airborne;
                isOllie = ollie;
                airTime = 0f;

                if (ollie && requested == Trick.None && trickFeed != null)
                    trickFeed.ShowOllie();

                UpdateTrick(requested, 0f);
                return;
            }

            grindTime += Mathf.Min(Time.fixedDeltaTime, distanceLeft / grindSpeed);
            body.MovePosition(Vector3.MoveTowards(
                body.position, grindEnd, grindSpeed * Time.fixedDeltaTime));
        }




        private void CheckRampLip(Collider other)
        {
            if (!roundRunning || state != State.Grounded || rampSides == null || incomingVelocity.y <= 0.15f)
                return;

            foreach (RampSide side in rampSides)
            {
                if (side == null || side.takeoffTrigger != other || side.landing == null)
                    continue;

                Transform lip = side.takeoffTrigger.transform;
                Vector3 uphill = Vector3.ProjectOnPlane(lip.forward, Vector3.up).normalized;
                if (Vector3.Dot(incomingVelocity, uphill) <= 0.25f) return;

                Vector3 widthAxis = Vector3.Cross(Vector3.up, uphill);
                Vector3 lipCenter = lip.TransformPoint(side.takeoffTrigger.center);
                float lane = Vector3.Dot(body.position - lipCenter, widthAxis);
                float halfWidth = lip.TransformVector(
                    Vector3.right * side.takeoffTrigger.size.x * 0.5f).magnitude;

                if (Mathf.Abs(lane) > halfWidth) return;

                Vector3 surfacePoint = side.landing.position + widthAxis * lane;

                rampOut = -uphill;
                rampLandingNormal = side.landing.up;
                rampEndRotation = Quaternion.LookRotation(rampOut, Vector3.up);
                rampStartRotation = body.rotation;
                rampCameraRotation = Quaternion.LookRotation(uphill, Vector3.up);

                rampStart = body.position;
                rampEnd = surfacePoint + rampLandingNormal * (contactSphere.radius + 0.02f)
                          - rampEndRotation * contactSphere.center;

                rampPeakY = Mathf.Max(
                    lipCenter.y + rampAirHeight, rampStart.y + 0.05f, rampEnd.y + 0.05f);
                rampTime = 0f;

                Velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.isKinematic = true;
                state = State.RampAir;
                SetAirCamera(true);
                return;
            }
        }

        private void UpdateRampAir(Trick requested)
        {
            // Let the final MovePosition finish before restoring physics.
            if (rampTime >= rampAirDuration)
            {
                body.isKinematic = false;
                body.collisionDetectionMode = groundCollisionMode;
                groundNormal = rampLandingNormal;
                contactNormal = rampLandingNormal;
                hasGroundContact = true;
                SetAirCamera(false);
                Land();

                if (state == State.Grounded)
                    Velocity = Vector3.ProjectOnPlane(rampOut, groundNormal).normalized * maxSpeed;

                return;
            }

            float step = Mathf.Min(Time.fixedDeltaTime, rampAirDuration - rampTime);
            rampTime += step;
            UpdateTrick(requested, step);

            float t = rampTime / rampAirDuration;
            Vector3 point = Vector3.Lerp(rampStart, rampEnd, t * t);

            if (t < 0.5f)
            {
                float rise = 1f - 2f * t;
                point.y = Mathf.Lerp(rampPeakY, rampStart.y, rise * rise);
            }
            else
            {
                float fall = 2f * t - 1f;
                point.y = Mathf.Lerp(rampPeakY, rampEnd.y, fall * fall);
            }

            float turn = Mathf.SmoothStep(0f, 1f, t);
            body.MovePosition(point);
            body.MoveRotation(Quaternion.Slerp(rampStartRotation, rampEndRotation, turn));
        }

        private void UpdateTrick(Trick requested, float step)
        {
            if (activeTrick == Trick.None && requested != Trick.None)
            {
                activeTrick = requested;
                trickTime = 0f;

                if (trickFeed != null)
                {
                    if (requested == Trick.Kickflip)
                        trickFeed.ShowKickflip();
                    else if (requested == Trick.PopShoveIt)
                        trickFeed.ShowPopShoveIt();
                }
            }

            if (activeTrick == Trick.None) return;

            trickTime += step;
            float duration = activeTrick == Trick.Kickflip ? kickflipReadyTime : shoveItTime;
            if (trickTime < duration) return;

            if (activeTrick == Trick.PopShoveIt)
                deckYaw = Mathf.Repeat(deckYaw + 180f, 360f);

            completedAirTricks++;
            lastCompletedFlip = activeTrick;
            activeTrick = Trick.None;
        }

        private void Land()
        {
            if (activeTrick != Trick.None)
            {
                Crash("trick unfinished on landing");
                return;
            }

            BankAirScore();
            state = State.Grounded;
            PlayImpact(landingClip);
        }

        private void TickScore()
        {
            scoreTime += Time.fixedDeltaTime;

            // Treat values within a microsecond of the next tick as a whole second.
            int wholeSeconds = (int)(scoreTime + 0.000001);
            if (wholeSeconds == 0) return;

            scoreTime -= wholeSeconds;
            AddScore(wholeSeconds * pointsPerSecond);
        }

        private void BankAirScore()
        {
            Trick completed = Trick.None;
            if (completedAirTricks > 0)
            {
                AddScore(completedAirTricks * flipPoints);
                completed = lastCompletedFlip;
            }
            else if (isOllie)
            {
                AddScore(olliePoints);
                completed = Trick.Ollie;
            }

            completedAirTricks = 0;
            isOllie = false;

            if (completed != Trick.None)
                TrickCompleted?.Invoke(completed);
        }

        private void AddScore(int points)
        {
            if (points > 0)
                SetScore(Score + points);
        }

        private void SetScore(int total)
        {
            Score = total;

            if (scoreText != null)
                scoreText.text = "Score " + Score;
        }

        private void Crash(string reason)
        {
            if (state == State.Crashed) return;

            state = State.Crashed;
            crashReason = reason;
            crashedAt = Time.unscaledTime;
            activeTrick = Trick.None;
            completedAirTricks = 0;
            isOllie = false;
            grindTime = 0f;
            body.angularVelocity = Vector3.zero;
            Velocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            SetCrashOverlayAlpha(crashFlashOpacity);
            PlayImpact(hurtClip);
        }

        private void ResetBoard()
        {
            RespawnBoard();
            scoreTime = 0.0;
            SetScore(0);

            if (trickFeed != null)
                trickFeed.Clear();

            resetRequested = false;
        }

        private void RespawnBoard()
        {
            if (body.isKinematic)
            {
                body.isKinematic = false;
                body.collisionDetectionMode = groundCollisionMode;
            }

            body.position = spawnPosition;
            body.rotation = spawnRotation;
            Velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            incomingVelocity = Vector3.zero;
            state = State.Grounded;
            crashReason = null;
            groundNormal = Vector3.up;
            hasGroundContact = false;
            visualUp = Vector3.up;
            visualOffset = Vector3.zero;
            activeTrick = Trick.None;
            queuedTrick = Trick.None;
            isOllie = false;
            deckYaw = 0f;
            grindRequested = false;
            ollieRequested = false;
            completedAirTricks = 0;
            grindTime = 0f;
            SetCrashOverlayAlpha(0f);
            SetAirCamera(false);
        }

        private void SetCrashOverlayAlpha(float alpha)
        {
            if (crashOverlay == null) return;

            Color color = crashOverlay.color;
            color.a = alpha;
            crashOverlay.color = color;
            crashOverlay.enabled = alpha > 0f;
        }

        private void PlayImpact(AudioClip clip)
        {
            if (impactSource != null && clip != null)
                impactSource.PlayOneShot(clip);
        }

        private void UpdateMovementAudio()
        {
            if (cruisingSource == null) return;

            AudioClip loopClip = null;
            if (roundRunning && Time.timeScale > 0f)
            {
                if (state == State.Grinding)
                    loopClip = grindingClip;
                else if (state == State.Grounded && Velocity.sqrMagnitude > 0.01f)
                    loopClip = cruisingClip;
            }

            if (cruisingSource.clip != loopClip)
            {
                cruisingSource.Stop();
                cruisingSource.clip = loopClip;
            }

            if (loopClip != null && !cruisingSource.isPlaying)
                cruisingSource.Play();
        }

        private void SetAirCamera(bool useAirCamera)
        {
            if (followCamera == null || airCamera == null || airCamera == followCamera) return;

            if (useAirCamera) PlaceAirCamera();
            followCamera.enabled = !useAirCamera;
            airCamera.enabled = useAirCamera;
        }

        private void PlaceAirCamera()
        {
            if (airCamera == null) return;

            Vector3 target = transform.position + Vector3.up * 0.2f;
            Vector3 offset = rampCameraRotation * airCameraOffset;

            if (offset.sqrMagnitude > 0.001f)
                airCamera.transform.SetPositionAndRotation(
                    target + offset, Quaternion.LookRotation(-offset, Vector3.up));
        }

        private void LateUpdate()
        {
            UpdateMovementAudio();
            if (state == State.RampAir) PlaceAirCamera();
            if (boardVisual == null) return;

            float blend = 1f - Mathf.Exp(-slopeFollowSpeed * Time.deltaTime);
            Vector3 localUp = Vector3.up;
            Vector3 offset = Vector3.zero;

            if (state == State.Grounded)
            {
                localUp = transform.InverseTransformDirection(groundNormal);
                offset = contactSphere.center - localUp * contactSphere.radius;
            }
            else if (state == State.Grinding)
            {
                offset = Vector3.down * grindDeckHeight;
            }

            visualUp = Vector3.Slerp(visualUp, localUp, blend);
            visualOffset = Vector3.Lerp(visualOffset, offset, blend);

            float pitch = 0f;
            if (state == State.Airborne && isOllie)
                pitch = -25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(airTime / ollieReadyTime));

            float yaw = deckYaw;
            if (state == State.Grinding)
            {
                yaw += grindBoardAngle;
                pitch = -grindNoseUpAngle;
            }
            float roll = 0f;
            float lift = 0f;

            if (activeTrick == Trick.Kickflip)
                roll = 360f * Mathf.Clamp01(trickTime / kickflipReadyTime);
            else if (activeTrick == Trick.PopShoveIt)
                yaw += 180f * Mathf.Clamp01(trickTime / shoveItTime);
            else if (state == State.Crashed)
            {
                roll = 65f;
                lift = 0.08f;
            }

            Quaternion slope = Quaternion.FromToRotation(Vector3.up, visualUp);
            Quaternion trick = Quaternion.Euler(pitch, yaw, roll);
            Vector3 pivot = Vector3.up * 0.14f;

            boardVisual.localPosition = visualOffset + Vector3.up * lift +
                                        slope * (pivot + trick * (visualRestPosition - pivot));
            boardVisual.localRotation = slope * trick * visualRestRotation;
        }
    }
}
