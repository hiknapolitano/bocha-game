using UnityEngine;

namespace BochaGame
{
    public enum LaunchStep
    {
        Idle,
        Position,
        Aim,
        Power,
        Throwing
    }

    public class BallLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        public float minPower = 3f;
        public float maxPower = 18f;
        public float pallinoMinPower = 2f;
        public float pallinoMaxPower = 6f;

        [Header("Position Step")]
        public float positionSpeed = 3f;

        [Header("Aim Step")]
        public float aimSpeed = 60f;
        public float maxAimAngle = 60f;

        [Header("Power Step")]
        public float powerOscillateSpeed = 1.5f; // full cycles per second

        [Header("Trajectory Preview")]
        public int trajectoryPoints = 30;
        public float trajectoryTimeStep = 0.05f;

        // State
        private GameObject currentBall;
        private Rigidbody currentRb;
        private BallController currentController;
        private bool isPallinoThrow;
        private LaunchStep currentStep = LaunchStep.Idle;
        private float currentPower = 0f;
        private float aimAngle = 0f;
        private float positionX = 0f;
        private float powerT = 0f; // 0..1 oscillating value
        private float activePowerMin;
        private float activePowerMax;

        // Court bounds for positioning
        private float courtHalfWidth = 1.8f; // slightly inside the walls

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
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Unlit/Color");
            trajectoryLine.material = new Material(lineShader);
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
            Shader arrowShader = Shader.Find("Universal Render Pipeline/Lit");
            if (arrowShader == null) arrowShader = Shader.Find("Standard");
            if (arrowShader == null) arrowShader = Shader.Find("Diffuse");
            Material arrowMat = new Material(arrowShader);
            Color arrowColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            if (arrowMat.HasProperty("_BaseColor"))
                arrowMat.SetColor("_BaseColor", arrowColor);
            else
                arrowMat.color = arrowColor;
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

            // Reset state
            aimAngle = 0f;
            positionX = 0f;
            currentPower = 0f;
            powerT = 0f;
            throwDirection = Vector3.forward;

            // Use lower power for pallino (it's very light)
            activePowerMin = pallinoThrow ? pallinoMinPower : minPower;
            activePowerMax = pallinoThrow ? pallinoMaxPower : maxPower;

            // Get the base throw position from court setup
            GameManager gm = GameManager.Instance;
            throwPosition = (gm != null && gm.courtSetup != null)
                ? gm.courtSetup.GetThrowPosition()
                : new Vector3(0, 0.5f, -10f);

            // Get court width for position bounds
            if (gm != null && gm.courtSetup != null)
                courtHalfWidth = gm.courtSetup.courtWidth / 2f - 0.2f;

            // Position ball at the throw position
            currentBall.transform.position = throwPosition;
            currentBall.SetActive(true);

            // Make the ball kinematic while aiming
            if (currentRb != null)
            {
                currentRb.isKinematic = true;
                currentRb.linearVelocity = Vector3.zero;
                currentRb.angularVelocity = Vector3.zero;
            }

            // Start at Position step
            currentStep = LaunchStep.Position;

            // Show aim arrow
            aimArrow.SetActive(true);
            UpdateBallPosition();
            UpdateAimVisuals();

            if (trajectoryLine != null)
                trajectoryLine.enabled = true;
        }

        private void Update()
        {
            if (currentStep == LaunchStep.Idle || currentStep == LaunchStep.Throwing)
                return;

            // Don't process input if it's AI's turn
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.CurrentTeam == Team.Team2)
            {
                // Hide visuals during AI turn
                aimArrow.SetActive(false);
                if (trajectoryLine != null)
                    trajectoryLine.positionCount = 0;
                return;
            }

            switch (currentStep)
            {
                case LaunchStep.Position:
                    HandlePositionStep();
                    break;
                case LaunchStep.Aim:
                    HandleAimStep();
                    break;
                case LaunchStep.Power:
                    HandlePowerStep();
                    break;
            }

            UpdateAimVisuals();
        }

        // ===== STEP 1: POSITION =====
        private void HandlePositionStep()
        {
            // A/D or Left/Right arrows to slide ball horizontally
            float input = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input = 1f;

            positionX += input * positionSpeed * Time.deltaTime;
            positionX = Mathf.Clamp(positionX, -courtHalfWidth, courtHalfWidth);

            UpdateBallPosition();

            // Confirm position
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                currentStep = LaunchStep.Aim;
                Debug.Log("[BallLauncher] Position confirmed. Moving to Aim step.");
            }
        }

        // ===== STEP 2: AIM =====
        private void HandleAimStep()
        {
            // A/D or Left/Right arrows to rotate aim direction
            float input = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input = 1f;

            aimAngle += input * aimSpeed * Time.deltaTime;
            aimAngle = Mathf.Clamp(aimAngle, -maxAimAngle, maxAimAngle);
            throwDirection = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

            // Confirm aim
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                currentStep = LaunchStep.Power;
                powerT = 0f;
                Debug.Log("[BallLauncher] Aim confirmed. Moving to Power step.");
            }
        }

        // ===== STEP 3: POWER =====
        private void HandlePowerStep()
        {
            // Auto-oscillate power (ping-pong from 0 to 1)
            powerT += Time.deltaTime * powerOscillateSpeed;
            float oscillation = Mathf.PingPong(powerT, 1f);
            currentPower = Mathf.Lerp(activePowerMin, activePowerMax, oscillation);

            // Show trajectory preview
            ShowTrajectory();

            // Lock power and throw
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[BallLauncher] Power locked at {currentPower:F1}. Throwing!");
                ThrowBall();
            }
        }

        private void UpdateBallPosition()
        {
            if (currentBall == null) return;

            Vector3 pos = throwPosition;
            pos.x = positionX;
            currentBall.transform.position = pos;
        }

        private void UpdateAimVisuals()
        {
            if (currentBall == null) return;

            Vector3 ballPos = currentBall.transform.position;

            // Show aim arrow during Position and Aim steps
            if (aimArrow != null && (currentStep == LaunchStep.Position || currentStep == LaunchStep.Aim || currentStep == LaunchStep.Power))
            {
                aimArrow.SetActive(true);
                aimArrow.transform.position = new Vector3(ballPos.x, 0.05f, ballPos.z);
                aimArrow.transform.rotation = Quaternion.LookRotation(throwDirection);
            }

            // Show trajectory during Power step
            if (trajectoryLine != null)
            {
                if (currentStep == LaunchStep.Power)
                {
                    ShowTrajectory();
                }
                else
                {
                    trajectoryLine.positionCount = 0;
                }
            }
        }

        private void ShowTrajectory()
        {
            if (currentBall == null) return;

            trajectoryLine.positionCount = trajectoryPoints;
            Vector3 startPos = currentBall.transform.position;
            Vector3 velocity = throwDirection * currentPower;

            float friction = 0.95f;
            for (int i = 0; i < trajectoryPoints; i++)
            {
                trajectoryLine.SetPosition(i, startPos);
                startPos += velocity * trajectoryTimeStep;
                velocity *= friction;
                startPos.y = Mathf.Max(startPos.y, 0.15f);
            }
        }

        private void ThrowBall()
        {
            if (currentBall == null || currentRb == null) return;

            currentStep = LaunchStep.Throwing;

            // Make ball physics-driven
            currentRb.isKinematic = false;

            // Apply force
            Vector3 force = throwDirection * currentPower;
            force.y = currentPower * 0.08f; // slight upward arc
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

            // Reset step after throw completes
            currentStep = LaunchStep.Idle;
        }

        /// <summary>
        /// Called by AI to execute a throw with specific parameters.
        /// Bypasses the multi-step flow.
        /// </summary>
        public void AIThrow(float power, float angle)
        {
            if (currentBall == null || currentRb == null)
            {
                Debug.LogWarning("[BallLauncher] AIThrow called but no ball is set up!");
                return;
            }

            aimAngle = angle;
            throwDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            currentPower = Mathf.Clamp(power, minPower, maxPower);

            ThrowBall();
        }

        // ===== PUBLIC GETTERS FOR UI =====

        public LaunchStep GetCurrentStep()
        {
            return currentStep;
        }

        public float GetPowerNormalized()
        {
            return Mathf.InverseLerp(activePowerMin, activePowerMax, currentPower);
        }

        public bool IsAiming()
        {
            return currentStep != LaunchStep.Idle && currentStep != LaunchStep.Throwing;
        }

        public bool IsCharging()
        {
            return currentStep == LaunchStep.Power;
        }

        public Vector3 GetThrowDirection()
        {
            return throwDirection;
        }
    }
}
