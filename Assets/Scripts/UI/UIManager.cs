using UnityEngine;
using UnityEngine.UI;

namespace BochaGame
{
    public class UIManager : MonoBehaviour
    {
        // UI Elements
        private Canvas canvas;
        private Text scoreText;
        private Text turnText;
        private Text stateText;
        private Text roundResultText;
        private Slider powerBar;
        private GameObject powerBarContainer;
        private GameObject gameOverPanel;
        private Text gameOverText;
        private Button restartButton;

        // Colors
        private Color team1UIColor = new Color(0.9f, 0.25f, 0.25f);
        private Color team2UIColor = new Color(0.25f, 0.4f, 0.9f);

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            // Subscribe to events
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged += HandleStateChanged;
                gm.OnTurnChanged += HandleTurnChanged;
                gm.OnScoreUpdated += HandleScoreUpdated;
                gm.OnRoundEnded += HandleRoundEnded;
                gm.OnGameOver += HandleGameOver;
            }

            // Initial UI state
            if (powerBarContainer != null) powerBarContainer.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (roundResultText != null) roundResultText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged -= HandleStateChanged;
                gm.OnTurnChanged -= HandleTurnChanged;
                gm.OnScoreUpdated -= HandleScoreUpdated;
                gm.OnRoundEnded -= HandleRoundEnded;
                gm.OnGameOver -= HandleGameOver;
            }
        }

        private void Update()
        {
            // Update power bar
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.ballLauncher != null)
            {
                bool showPower = gm.ballLauncher.IsCharging();
                if (powerBarContainer != null)
                    powerBarContainer.SetActive(showPower);
                if (showPower && powerBar != null)
                    powerBar.value = gm.ballLauncher.GetPowerNormalized();
            }
        }

        private void CreateUI()
        {
            // --- Canvas ---
            GameObject canvasObj = new GameObject("GameCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // --- Score Display (top center) ---
            scoreText = CreateText(canvasObj.transform, "ScoreText",
                "Player 0  ×  0 AI",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -20f),
                36, TextAnchor.UpperCenter, FontStyle.Bold);
            AddTextShadow(scoreText);

            // --- Turn Indicator (top center, below score) ---
            turnText = CreateText(canvasObj.transform, "TurnText",
                "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -70f),
                28, TextAnchor.UpperCenter, FontStyle.Normal);

            // --- State Info (top left) ---
            stateText = CreateText(canvasObj.transform, "StateText",
                "",
                new Vector2(0, 1f), new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(20f, -20f),
                20, TextAnchor.UpperLeft, FontStyle.Italic);
            stateText.color = new Color(1f, 1f, 1f, 0.6f);

            // --- Power Bar (bottom center) ---
            powerBarContainer = new GameObject("PowerBarContainer");
            powerBarContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform pbcRect = powerBarContainer.AddComponent<RectTransform>();
            pbcRect.anchorMin = new Vector2(0.5f, 0f);
            pbcRect.anchorMax = new Vector2(0.5f, 0f);
            pbcRect.pivot = new Vector2(0.5f, 0f);
            pbcRect.anchoredPosition = new Vector2(0, 40f);
            pbcRect.sizeDelta = new Vector2(400, 30);

            // Power bar background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(powerBarContainer.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("PowerSlider");
            sliderObj.transform.SetParent(powerBarContainer.transform, false);
            powerBar = sliderObj.AddComponent<Slider>();
            powerBar.interactable = false;
            powerBar.minValue = 0f;
            powerBar.maxValue = 1f;
            powerBar.value = 0f;
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Fill area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.6f, 0.1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            powerBar.fillRect = fillRect;

            // Power label
            CreateText(powerBarContainer.transform, "PowerLabel",
                "POWER",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, 5f),
                18, TextAnchor.LowerCenter, FontStyle.Bold);

            powerBarContainer.SetActive(false);

            // --- Round Result (center screen, shown briefly) ---
            roundResultText = CreateText(canvasObj.transform, "RoundResult",
                "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero,
                48, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddTextShadow(roundResultText);
            roundResultText.gameObject.SetActive(false);

            // --- Game Over Panel ---
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform goPanelRect = gameOverPanel.AddComponent<RectTransform>();
            goPanelRect.anchorMin = Vector2.zero;
            goPanelRect.anchorMax = Vector2.one;
            goPanelRect.sizeDelta = Vector2.zero;
            Image goPanelImg = gameOverPanel.AddComponent<Image>();
            goPanelImg.color = new Color(0, 0, 0, 0.7f);

            gameOverText = CreateText(gameOverPanel.transform, "GameOverText",
                "GAME OVER",
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                Vector2.zero,
                64, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddTextShadow(gameOverText);

            // Restart button
            GameObject btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(gameOverPanel.transform, false);
            restartButton = btnObj.AddComponent<Button>();
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.7f, 0.3f);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.35f);
            btnRect.anchorMax = new Vector2(0.5f, 0.35f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(280, 60);

            Text btnText = CreateText(btnObj.transform, "ButtonText",
                "PLAY AGAIN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero,
                30, TextAnchor.MiddleCenter, FontStyle.Bold);
            btnText.color = Color.white;

            restartButton.onClick.AddListener(OnRestartClicked);
            gameOverPanel.SetActive(false);

            // --- Instructions (bottom right) ---
            Text instructions = CreateText(canvasObj.transform, "Instructions",
                "Mouse: Aim  |  Hold Click: Charge  |  Release: Throw",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 15f),
                16, TextAnchor.LowerRight, FontStyle.Normal);
            instructions.color = new Color(1f, 1f, 1f, 0.5f);
        }

        private Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, int fontSize, TextAnchor alignment, FontStyle style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 80);

            return text;
        }

        private void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        // --- Event Handlers ---

        private void HandleStateChanged(GameState state)
        {
            if (stateText == null) return;

            switch (state)
            {
                case GameState.ThrowingPallino:
                    stateText.text = "Throw the Pallino!";
                    break;
                case GameState.Aiming:
                    stateText.text = "Aim and throw!";
                    break;
                case GameState.BallInMotion:
                    stateText.text = "Ball rolling...";
                    break;
                case GameState.Scoring:
                    stateText.text = "Scoring...";
                    break;
                case GameState.RoundOver:
                    stateText.text = "Next round...";
                    break;
                case GameState.GameOver:
                    stateText.text = "";
                    break;
            }
        }

        private void HandleTurnChanged(Team team)
        {
            if (turnText == null) return;

            if (team == Team.Team1)
            {
                turnText.text = "Your Turn";
                turnText.color = team1UIColor;
            }
            else
            {
                turnText.text = "AI Thinking...";
                turnText.color = team2UIColor;
            }
        }

        private void HandleScoreUpdated(int team1Score, int team2Score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Player  {team1Score}  ×  {team2Score}  AI";
            }
        }

        private void HandleRoundEnded(Team scoringTeam, int points)
        {
            if (roundResultText == null) return;

            string teamName = scoringTeam == Team.Team1 ? "Player" : "AI";
            roundResultText.text = $"{teamName} scores {points} point{(points > 1 ? "s" : "")}!";
            roundResultText.color = scoringTeam == Team.Team1 ? team1UIColor : team2UIColor;
            roundResultText.gameObject.SetActive(true);

            // Hide after a delay
            StartCoroutine(HideRoundResult(2.5f));
        }

        private System.Collections.IEnumerator HideRoundResult(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (roundResultText != null)
                roundResultText.gameObject.SetActive(false);
        }

        private void HandleGameOver(Team winner)
        {
            if (gameOverPanel == null || gameOverText == null) return;

            string winnerName = winner == Team.Team1 ? "YOU WIN!" : "AI WINS!";
            gameOverText.text = winnerName;
            gameOverText.color = winner == Team.Team1 ? team1UIColor : team2UIColor;
            gameOverPanel.SetActive(true);

            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnRestartClicked()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (gm != null)
                gm.RestartGame();
        }
    }
}
