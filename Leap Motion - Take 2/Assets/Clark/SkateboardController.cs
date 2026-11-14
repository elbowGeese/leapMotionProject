using UnityEngine;


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

        private enum State { Grounded, Airborne, RampAir, Crashed }
        public enum Trick { None, Kickflip, PopShoveIt }

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

        private const float MinGroundY = 0.25f;
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

            SetAirCamera(false);
        }

        public void SetSideInput(float amount) => turnInput = Mathf.Clamp(amount, -1f, 1f);
        public void RequestOllie() => ollieRequested = true;
        public void RequestKickflip() => queuedTrick = Trick.Kickflip;
        public void RequestPopShoveIt() => queuedTrick = Trick.PopShoveIt;
        public void RequestReset() => resetRequested = true;

        private void FixedUpdate()
        {
            if (resetRequested)
            {
                ResetBoard();
                return;
            }

            bool ollie = ollieRequested;
            Trick requested = queuedTrick;
            ollieRequested = false;
            queuedTrick = Trick.None;

            if (state == State.Crashed) return;

            if (state == State.RampAir)
            {
                UpdateRampAir(requested);
                return;
            }

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
                    Velocity = heading * maxSpeed + Vector3.up * jumpSpeed;
                    body.angularVelocity = Vector3.zero;
                    state = State.Airborne;
                    isOllie = true;
                    airTime = 0f;
                }
                else
                {
                    body.angularVelocity = Vector3.up * (turnInput * turnSpeed * Mathf.Deg2Rad);
                    Vector3 heading = body.rotation * Vector3.forward;
                    Velocity = Vector3.ProjectOnPlane(heading, groundNormal).normalized * maxSpeed;
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
            if (state == State.RampAir || state == State.Crashed) return;

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

        private void CheckRampLip(Collider other)
        {
            if (state != State.Grounded || rampSides == null || incomingVelocity.y <= 0.15f)
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
            }

            if (activeTrick == Trick.None) return;

            trickTime += step;
            float duration = activeTrick == Trick.Kickflip ? kickflipReadyTime : shoveItTime;
            if (trickTime < duration) return;

            if (activeTrick == Trick.PopShoveIt)
                deckYaw = Mathf.Repeat(deckYaw + 180f, 360f);

            activeTrick = Trick.None;
        }

        private void Land()
        {
            if (activeTrick != Trick.None)
            {
                Crash("trick unfinished on landing");
                return;
            }

            state = State.Grounded;
        }

        private void Crash(string reason)
        {
            state = State.Crashed;
            crashReason = reason;
            activeTrick = Trick.None;
            body.angularVelocity = Vector3.zero;
            Velocity = new Vector3(0f, Mathf.Min(Velocity.y, 0f), 0f);
        }

        private void ResetBoard()
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
            groundNormal = Vector3.up;
            hasGroundContact = false;
            visualUp = Vector3.up;
            visualOffset = Vector3.zero;
            activeTrick = Trick.None;
            queuedTrick = Trick.None;
            isOllie = false;
            deckYaw = 0f;
            ollieRequested = false;
            resetRequested = false;
            SetAirCamera(false);
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

            visualUp = Vector3.Slerp(visualUp, localUp, blend);
            visualOffset = Vector3.Lerp(visualOffset, offset, blend);

            float pitch = 0f;
            if (state == State.Airborne && isOllie)
                pitch = -25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(airTime / ollieReadyTime));

            float yaw = deckYaw;
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