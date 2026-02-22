using UnityEngine;

namespace BochaGame
{
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        public float followSmoothTime = 0.3f;
        public Vector3 followOffset = new Vector3(0, 5f, -6f);

        [Header("Overview Settings")]
        public Vector3 overviewPosition = new Vector3(0, 18f, 0);
        public Vector3 overviewRotation = new Vector3(80f, 0, 0);

        [Header("Aim View Settings")]
        public Vector3 aimOffset = new Vector3(0, 3f, -4f);

        [Header("Transition")]
        public float transitionSpeed = 3f;

        private CameraMode currentMode = CameraMode.Overview;
        private Transform followTarget;
        private Vector3 velocity = Vector3.zero;
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private void Start()
        {
            // Start in overview mode
            float courtLength = 27.5f;
            overviewPosition = new Vector3(0, courtLength * 0.6f, 0);
            SetMode(CameraMode.Overview);

            // Position camera initially
            transform.position = overviewPosition;
            transform.rotation = Quaternion.Euler(overviewRotation);
        }

        private void LateUpdate()
        {
            switch (currentMode)
            {
                case CameraMode.Follow:
                    UpdateFollow();
                    break;
                case CameraMode.Overview:
                    UpdateOverview();
                    break;
                case CameraMode.Transitioning:
                    UpdateTransition();
                    break;
            }
        }

        [Header("Zoom Settings")]
        public Vector3 zoomOffset = new Vector3(0, 2.5f, -2.5f); // close-up when ball is slow
        public float zoomSpeedThreshold = 3f; // below this speed, fully zoomed in

        private void UpdateFollow()
        {
            if (followTarget == null)
            {
                SetMode(CameraMode.Overview);
                return;
            }

            GameManager gm = GameManager.Instance;
            bool isAiming = gm != null && (gm.CurrentState == GameState.Aiming || gm.CurrentState == GameState.ThrowingPallino);
            bool isBallMoving = gm != null && gm.CurrentState == GameState.BallInMotion;

            Vector3 desiredOffset;
            if (isAiming)
            {
                desiredOffset = aimOffset;
            }
            else if (isBallMoving)
            {
                // Dynamic zoom: closer as ball slows down
                Rigidbody rb = followTarget.GetComponent<Rigidbody>();
                float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
                float zoomT = 1f - Mathf.Clamp01(speed / zoomSpeedThreshold);
                zoomT = zoomT * zoomT; // ease-in for smooth zoom
                desiredOffset = Vector3.Lerp(followOffset, zoomOffset, zoomT);
            }
            else
            {
                desiredOffset = followOffset;
            }

            Vector3 desiredPosition = followTarget.position + desiredOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);

            Vector3 lookTarget = followTarget.position;
            if (isAiming)
                lookTarget += Vector3.forward * 5f;

            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * 5f);
        }

        private void UpdateOverview()
        {
            transform.position = Vector3.Lerp(transform.position, overviewPosition, Time.deltaTime * transitionSpeed);
            Quaternion target = Quaternion.Euler(overviewRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * transitionSpeed);
        }

        private void UpdateTransition()
        {
            float step = transitionSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPosition, step);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, step);

            // Check if close enough to snap
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;

                // Determine final mode
                if (followTarget != null)
                    currentMode = CameraMode.Follow;
                else
                    currentMode = CameraMode.Overview;
            }
        }

        /// <summary>
        /// Start following a ball.
        /// </summary>
        public void FollowBall(Transform ball)
        {
            followTarget = ball;
            currentMode = CameraMode.Follow;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Set the camera mode.
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            if (mode == CameraMode.Overview)
            {
                followTarget = null;
                targetPosition = overviewPosition;
                targetRotation = Quaternion.Euler(overviewRotation);
                currentMode = CameraMode.Transitioning;
            }
            else if (mode == CameraMode.Follow && followTarget != null)
            {
                currentMode = CameraMode.Follow;
            }
        }

        /// <summary>
        /// Set temporary focus on a position (for scoring view).
        /// </summary>
        public void FocusOn(Vector3 position, float height = 8f)
        {
            targetPosition = position + new Vector3(0, height, -height * 0.5f);
            targetRotation = Quaternion.LookRotation(position - targetPosition);
            followTarget = null;
            currentMode = CameraMode.Transitioning;
        }
    }
}
