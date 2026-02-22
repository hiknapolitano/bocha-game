using UnityEngine;

namespace BochaGame
{
    public class BallLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        public float minPower = 3f;
        public float maxPower = 18f;
        public float chargeSpeed = 8f;
        public float aimSensitivity = 2f;

        [Header("Trajectory Preview")]
        public int trajectoryPoints = 30;
        public float trajectoryTimeStep = 0.05f;

        // State
        private GameObject currentBall;
        private Rigidbody currentRb;
        private BallController currentController;
        private bool isPallinoThrow;
        private bool isAiming = false;
        private bool isCharging = false;
        private float currentPower = 0f;
        private float aimAngle = 0f;

        // Trajectory visualization
        private LineRenderer trajectoryLine;
        private GameObject aimArrow;

        // Direction
        private Vector3 throwDirection = Vector3.forward;
        private Vector3 throwPosition;

        private void Awake()
        {
            CreateTrajectoryLine();
            CreateAimArrow();
        }

        private void CreateTrajectoryLine()
        {
            GameObject lineObj = new GameObject("TrajectoryLine");
            lineObj.transform.SetParent(transform);
            trajectoryLine = lineObj.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.05f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = new Color(1f, 1f, 1f, 0.6f);
            trajectoryLine.endColor = new Color(1f, 1f, 1f, 0.1f);
            trajectoryLine.positionCount = 0;
            trajectoryLine.enabled = false;
        }

        private void CreateAimArrow()
        {
            aimArrow = new GameObject("AimArrow");
            aimArrow.transform.SetParent(transform);

            // Create a simple arrow using a stretched cube
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.transform.SetParent(aimArrow.transform);
            shaft.transform.localScale = new Vector3(0.08f, 0.02f, 1.5f);
            shaft.transform.localPosition = new Vector3(0, 0.01f, 0.75f);

            // Remove collider from visual-only object
            Collider shaftCol = shaft.GetComponent<Collider>();
            if (shaftCol != null) Object.Destroy(shaftCol);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(aimArrow.transform);
            head.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            head.transform.localPosition = new Vector3(0, 0.01f, 1.6f);
            head.transform.localRotation = Quaternion.Euler(0, 45, 0);

            Collider headCol = head.GetComponent<Collider>();
            if (headCol != null) Object.Destroy(headCol);

            // Color the arrow
            Material arrowMat = new Material(Shader.Find("Standard"));
            arrowMat.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            arrowMat.SetFloat("_Mode", 3); // Transparent
            arrowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            arrowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            arrowMat.SetInt("_ZWrite", 0);
            arrowMat.DisableKeyword("_ALPHATEST_ON");
            arrowMat.EnableKeyword("_ALPHABLEND_ON");
            arrowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            arrowMat.renderQueue = 3000;
            shaft.GetComponent<Renderer>().material = arrowMat;
            head.GetComponent<Renderer>().material = arrowMat;

            aimArrow.SetActive(false);
        }

        /// <summary>
        /// Called by GameManager to set up a throw.
        /// </summary>
        public void SetupThrow(GameObject ball, bool pallinoThrow)
        {
            currentBall = ball;
            currentRb = ball.GetComponent<Rigidbody>();
            currentController = ball.GetComponent<BallController>();
            isPallinoThrow = pallinoThrow;

            isAiming = true;
            isCharging = false;
            currentPower = 0f;
            aimAngle = 0f;
            throwDirection = Vector3.forward;
            throwPosition = ball.transform.position;

            // Make sure the ball is kinematic while aiming
            if (currentRb != null)
            {
                currentRb.isKinematic = true;
                currentRb.velocity = Vector3.zero;
                currentRb.angularVelocity = Vector3.zero;
            }

            aimArrow.SetActive(true);
            aimArrow.transform.position = new Vector3(throwPosition.x, 0.05f, throwPosition.z);

            if (trajectoryLine != null)
                trajectoryLine.enabled = true;
        }

        private void Update()
        {
            if (!isAiming) return;

            // Don't process input if it's AI's turn
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.CurrentTeam == Team.Team2)
                return;

            HandleAiming();
            HandleCharging();
            UpdateVisuals();
        }

        private void HandleAiming()
        {
            // Mouse horizontal movement rotates aim direction
            float mouseX = Input.GetAxis("Mouse X") * aimSensitivity;
            aimAngle += mouseX;

            // Clamp aiming angle to prevent shooting backwards
            aimAngle = Mathf.Clamp(aimAngle, -60f, 60f);

            throwDirection = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;
        }

        private void HandleCharging()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isCharging = true;
                currentPower = minPower;
            }

            if (Input.GetMouseButton(0) && isCharging)
            {
                currentPower += chargeSpeed * Time.deltaTime;
                currentPower = Mathf.Clamp(currentPower, minPower, maxPower);
            }

            if (Input.GetMouseButtonUp(0) && isCharging)
            {
                ThrowBall();
            }
        }

        private void UpdateVisuals()
        {
            // Update aim arrow
            if (aimArrow != null)
            {
                aimArrow.transform.position = new Vector3(throwPosition.x, 0.05f, throwPosition.z);
                aimArrow.transform.rotation = Quaternion.LookRotation(throwDirection);
            }

            // Update trajectory preview
            if (trajectoryLine != null && isCharging)
            {
                ShowTrajectory();
            }
            else if (trajectoryLine != null)
            {
                trajectoryLine.positionCount = 0;
            }
        }

        private void ShowTrajectory()
        {
            trajectoryLine.positionCount = trajectoryPoints;
            Vector3 startPos = throwPosition;
            Vector3 velocity = throwDirection * currentPower;

            // Simple ground-roll prediction (approximate)
            float friction = 0.95f; // per-step friction
            for (int i = 0; i < trajectoryPoints; i++)
            {
                trajectoryLine.SetPosition(i, startPos);
                startPos += velocity * trajectoryTimeStep;
                velocity *= friction;
                startPos.y = Mathf.Max(startPos.y, 0.15f); // Keep on ground
            }
        }

        private void ThrowBall()
        {
            if (currentBall == null || currentRb == null) return;

            isAiming = false;
            isCharging = false;

            // Make ball physics-driven
            currentRb.isKinematic = false;

            // Apply force
            Vector3 force = throwDirection * currentPower;
            // Add a slight upward arc for a nice throw feel
            force.y = currentPower * 0.08f;
            currentRb.AddForce(force, ForceMode.Impulse);

            // Start settling detection
            if (currentController != null)
            {
                currentController.StartTracking();
            }

            // Notify game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBallThrown(currentBall);
            }

            // Follow with camera
            CameraController cam = GameManager.Instance?.cameraController;
            if (cam != null)
            {
                cam.FollowBall(currentBall.transform);
            }

            // Hide aim visuals
            aimArrow.SetActive(false);
            if (trajectoryLine != null)
                trajectoryLine.enabled = false;
        }

        /// <summary>
        /// Called by AI to execute a throw with specific parameters.
        /// </summary>
        public void AIThrow(float power, float angle)
        {
            if (currentBall == null || currentRb == null) return;

            aimAngle = angle;
            throwDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            currentPower = Mathf.Clamp(power, minPower, maxPower);

            ThrowBall();
        }

        /// <summary>
        /// Returns the normalized power (0-1) for UI display.
        /// </summary>
        public float GetPowerNormalized()
        {
            return Mathf.InverseLerp(minPower, maxPower, currentPower);
        }

        public bool IsAiming()
        {
            return isAiming;
        }

        public bool IsCharging()
        {
            return isCharging;
        }

        public Vector3 GetThrowDirection()
        {
            return throwDirection;
        }
    }
}
