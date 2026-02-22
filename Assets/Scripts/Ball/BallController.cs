using System;
using UnityEngine;

namespace BochaGame
{
    public class BallController : MonoBehaviour
    {
        [Header("Ball Settings")]
        public Team team = Team.Team1;
        public int ballIndex = 0;
        public bool isPallino = false;

        [Header("Settling Detection")]
        public float settleVelocityThreshold = 0.05f;
        public float settleAngularThreshold = 0.1f;
        public float settleTimeRequired = 0.5f;

        // Events
        public event Action<BallController> OnBallSettled;

        private Rigidbody rb;
        private float settleTimer = 0f;
        private bool hasSettled = false;
        private bool isTracking = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Call this when the ball is thrown to start tracking settling.
        /// </summary>
        public void StartTracking()
        {
            hasSettled = false;
            isTracking = true;
            settleTimer = 0f;
        }

        public void StopTracking()
        {
            isTracking = false;
        }

        private void FixedUpdate()
        {
            if (!isTracking || hasSettled || rb == null) return;

            // Check if ball is moving slowly enough to be considered settled
            bool isSlowEnough = rb.linearVelocity.magnitude < settleVelocityThreshold
                             && rb.angularVelocity.magnitude < settleAngularThreshold;

            if (isSlowEnough)
            {
                settleTimer += Time.fixedDeltaTime;
                if (settleTimer >= settleTimeRequired)
                {
                    Settle();
                }
            }
            else
            {
                settleTimer = 0f;
            }

            // Also settle if the ball has fallen off the court
            if (transform.position.y < -5f)
            {
                // Ball fell off — reset to a default position on the court edge
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, -1.5f, 1.5f),
                    0.5f,
                    Mathf.Clamp(transform.position.z, -12f, 12f)
                );
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Settle();
            }
        }

        private void Settle()
        {
            if (hasSettled) return;

            hasSettled = true;
            isTracking = false;

            // Fully stop the ball
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            OnBallSettled?.Invoke(this);
        }

        /// <summary>
        /// Returns the current velocity magnitude (useful for UI/camera).
        /// </summary>
        public float GetSpeed()
        {
            return rb != null ? rb.linearVelocity.magnitude : 0f;
        }

        public bool IsSettled()
        {
            return hasSettled;
        }
    }
}
