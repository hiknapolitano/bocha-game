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
        public float maxPower = 12f;
        public float pallinoMinPower = 1f;
        public float pallinoMaxPower = 4f;

        [Header("Position Step")]
        public float positionSpeed = 3f;

        [Header("Aim Step — Auto Oscillation")]
        public float aimOscillateSpeed = 1.2f; // full swings per second
        public float maxAimAngle = 45f;

        [Header("Power Step — Sweet Spot")]
        public float powerOscillateSpeed = 1.5f;
        public float sweetSpotMin = 0.68f; // normalized 0..1
        public float sweetSpotMax = 0.76f;
        public float overpowerSpreadMax = 15f; // max random angle deviation in degrees

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
        private float powerT = 0f;
        private float aimT = 0f;
        private float activePowerMin;
        private float activePowerMax;

        // Sweet spot result
        private bool wasOverpowered = false;
        private float overpowerAmount = 0f;

        // Court bounds for positioning
        private float courtHalfWidth = 1.8f;

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
            aimT = 0f;
            throwDirection = Vector3.forward;
            wasOverpowered = false;
            overpowerAmount = 0f;

            // Use lower power for pallino
            activePowerMin = pallinoThrow ? pallinoMinPower : minPower;
            activePowerMax = pallinoThrow ? pallinoMaxPower : maxPower;

            // Get throw position
            GameManager gm = GameManager.Instance;
            throwPosition = (gm != null && gm.courtSetup != null)
                ? gm.courtSetup.GetThrowPosition()
                : new Vector3(0, 0.5f, -10f);

            if (gm != null && gm.courtSetup != null)
                courtHalfWidth = gm.courtSetup.courtWidth / 2f - 0.2f;

            currentBall.transform.position = throwPosition;
            currentBall.SetActive(true);

            if (currentRb != null)
            {
                currentRb.isKinematic = true;
                currentRb.linearVelocity = Vector3.zero;
                currentRb.angularVelocity = Vector3.zero;
            }

            currentStep = LaunchStep.Position;
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
            UpdatePlayerCharacterPosition();
        }

        /// <summary>
        /// Keep the player character following the ball during positioning/aiming.
        /// </summary>
        private void UpdatePlayerCharacterPosition()
        {
            if (currentBall == null) return;
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            Vector3 ballPos = currentBall.transform.position;
            Vector3 charPos = new Vector3(ballPos.x - 0.5f, 0f, ballPos.z - 1.2f);

            PlayerCharacter activeChar = gm.CurrentTeam == Team.Team1
                ? gm.player1Character
                : gm.player2Character;

            if (activeChar != null)
                activeChar.SetPosition(charPos, 0f);
        }

        // ===== STEP 1: POSITION — Free A/D control =====
        private void HandlePositionStep()
        {
            float input = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input = 1f;

            positionX += input * positionSpeed * Time.deltaTime;
            positionX = Mathf.Clamp(positionX, -courtHalfWidth, courtHalfWidth);
            UpdateBallPosition();

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                currentStep = LaunchStep.Aim;
                aimT = 0f;
                Debug.Log("[BallLauncher] Position confirmed. Moving to Aim step.");
            }
        }

        // ===== STEP 2: AIM — Auto-oscillating, press to lock =====
        private void HandleAimStep()
        {
            // Auto-oscillate the aim angle left and right
            aimT += Time.deltaTime * aimOscillateSpeed;
            // Sine wave: smoothly oscillates between -maxAimAngle and +maxAimAngle
            aimAngle = Mathf.Sin(aimT * Mathf.PI * 2f) * maxAimAngle;
            throwDirection = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

            // Lock direction
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                currentStep = LaunchStep.Power;
                powerT = 0f;
                Debug.Log($"[BallLauncher] Aim locked at {aimAngle:F1}°. Moving to Power step.");
            }
        }

        // ===== STEP 3: POWER — Sweet spot bar =====
        private void HandlePowerStep()
        {
            // Auto-oscillate power (ping-pong 0..1)
            powerT += Time.deltaTime * powerOscillateSpeed;
            float normalized = Mathf.PingPong(powerT, 1f);
            currentPower = Mathf.Lerp(activePowerMin, activePowerMax, normalized);

            // Show trajectory preview
            ShowTrajectory();

            // Lock power
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                // Determine sweet spot result
                if (normalized >= sweetSpotMin && normalized <= sweetSpotMax)
                {
                    // PERFECT — sweet spot hit!
                    wasOverpowered = false;
                    overpowerAmount = 0f;
                    Debug.Log($"[BallLauncher] SWEET SPOT! Power={currentPower:F1}");
                }
                else if (normalized > sweetSpotMax)
                {
                    // OVERPOWERED — add random angle deviation
                    wasOverpowered = true;
                    float excessRatio = (normalized - sweetSpotMax) / (1f - sweetSpotMax);
                    overpowerAmount = excessRatio * overpowerSpreadMax;
                    Debug.Log($"[BallLauncher] OVERPOWERED! Power={currentPower:F1}, spread=±{overpowerAmount:F1}°");
                }
                else
                {
                    // UNDERPOWERED — just less power, no penalty
                    wasOverpowered = false;
                    overpowerAmount = 0f;
                    Debug.Log($"[BallLauncher] Underpowered. Power={currentPower:F1}");
                }

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

            if (aimArrow != null && currentStep != LaunchStep.Idle && currentStep != LaunchStep.Throwing)
            {
                aimArrow.SetActive(true);
                aimArrow.transform.position = new Vector3(ballPos.x, 0.05f, ballPos.z);
                aimArrow.transform.rotation = Quaternion.LookRotation(throwDirection);
            }

            if (trajectoryLine != null)
            {
                if (currentStep == LaunchStep.Power)
                    ShowTrajectory();
                else
                    trajectoryLine.positionCount = 0;
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

            // Apply overpower inaccuracy — random angle deviation
            Vector3 finalDirection = throwDirection;
            if (wasOverpowered && overpowerAmount > 0f)
            {
                float randomDeviation = Random.Range(-overpowerAmount, overpowerAmount);
                finalDirection = Quaternion.Euler(0, randomDeviation, 0) * finalDirection;
                Debug.Log($"[BallLauncher] Overpower deviation: {randomDeviation:F1}°");
            }

            // Make ball physics-driven
            currentRb.isKinematic = false;

            // Apply force
            Vector3 force = finalDirection * currentPower;
            if (!isPallinoThrow)
                force.y = currentPower * 0.08f;
            currentRb.AddForce(force, ForceMode.Impulse);

            // Start settling detection
            if (currentController != null)
                currentController.StartTracking();

            // Notify game manager
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnBallThrown(currentBall);

                // Trigger player throw animation
                if (gm.CurrentTeam == Team.Team1 && gm.player1Character != null)
                    gm.player1Character.PlayThrowAnimation();
                else if (gm.CurrentTeam == Team.Team2 && gm.player2Character != null)
                    gm.player2Character.PlayThrowAnimation();
            }

            // Follow with camera
            CameraController cam = gm?.cameraController;
            if (cam != null)
                cam.FollowBall(currentBall.transform);

            // Hide aim visuals
            aimArrow.SetActive(false);
            if (trajectoryLine != null)
                trajectoryLine.enabled = false;

            currentStep = LaunchStep.Idle;
        }

        /// <summary>
        /// Called by AI — bypasses multi-step flow.
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
            currentPower = Mathf.Clamp(power, activePowerMin, activePowerMax);
            wasOverpowered = false;
            overpowerAmount = 0f;

            ThrowBall();
        }

        // ===== PUBLIC GETTERS FOR UI =====

        public LaunchStep GetCurrentStep() => currentStep;

        public float GetPowerNormalized()
        {
            return Mathf.InverseLerp(activePowerMin, activePowerMax, currentPower);
        }

        public float GetSweetSpotMin() => sweetSpotMin;
        public float GetSweetSpotMax() => sweetSpotMax;

        public bool IsAiming() => currentStep != LaunchStep.Idle && currentStep != LaunchStep.Throwing;
        public bool IsCharging() => currentStep == LaunchStep.Power;
        public Vector3 GetThrowDirection() => throwDirection;
    }
}
