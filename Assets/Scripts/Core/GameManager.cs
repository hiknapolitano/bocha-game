using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BochaGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public int pointsToWin = 12;
        public int ballsPerTeam = 4;

        [Header("References (auto-assigned at runtime)")]
        public BallLauncher ballLauncher;
        public CameraController cameraController;
        public ScoreManager scoreManager;
        public UIManager uiManager;
        public AIPlayer aiPlayer;
        public CourtSetup courtSetup;

        // Game state
        public GameState CurrentState { get; private set; } = GameState.WaitingToStart;
        public Team CurrentTeam { get; private set; } = Team.Team1;
        public int Team1Score { get; private set; } = 0;
        public int Team2Score { get; private set; } = 0;
        public int Team1BallsThrown { get; private set; } = 0;
        public int Team2BallsThrown { get; private set; } = 0;
        public int CurrentRound { get; private set; } = 1;

        // Ball tracking
        public GameObject Pallino { get; private set; }
        public List<GameObject> Team1Balls { get; private set; } = new List<GameObject>();
        public List<GameObject> Team2Balls { get; private set; } = new List<GameObject>();
        public List<GameObject> ThrownTeam1Balls { get; private set; } = new List<GameObject>();
        public List<GameObject> ThrownTeam2Balls { get; private set; } = new List<GameObject>();

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<Team> OnTurnChanged;
        public event Action<int, int> OnScoreUpdated;
        public event Action<Team, int> OnRoundEnded;
        public event Action<Team> OnGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitializeGameDelayed());
        }

        private IEnumerator InitializeGameDelayed()
        {
            // Wait a frame for CourtSetup to finish
            yield return null;
            yield return null;

            // Gather references
            if (courtSetup == null) courtSetup = FindFirstObjectByType<CourtSetup>();
            if (ballLauncher == null) ballLauncher = FindFirstObjectByType<BallLauncher>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (aiPlayer == null) aiPlayer = FindFirstObjectByType<AIPlayer>();

            // Gather ball references from CourtSetup
            if (courtSetup != null)
            {
                Pallino = courtSetup.pallinoInstance;
                Team1Balls = new List<GameObject>(courtSetup.team1BallInstances);
                Team2Balls = new List<GameObject>(courtSetup.team2BallInstances);
            }

            StartGame();
        }

        public void StartGame()
        {
            Team1Score = 0;
            Team2Score = 0;
            CurrentRound = 1;
            OnScoreUpdated?.Invoke(Team1Score, Team2Score);
            StartNewRound();
        }

        public void StartNewRound()
        {
            Team1BallsThrown = 0;
            Team2BallsThrown = 0;
            ThrownTeam1Balls.Clear();
            ThrownTeam2Balls.Clear();

            // Reset ball positions (hide them off-screen until thrown)
            ResetBalls();

            // Team1 always throws pallino first
            CurrentTeam = Team.Team1;
            SetState(GameState.ThrowingPallino);
        }

        private void ResetBalls()
        {
            Vector3 hiddenPos = new Vector3(0, -10, 0);

            if (Pallino != null)
            {
                ResetBallPhysics(Pallino, hiddenPos);
            }

            for (int i = 0; i < Team1Balls.Count; i++)
            {
                ResetBallPhysics(Team1Balls[i], hiddenPos);
            }
            for (int i = 0; i < Team2Balls.Count; i++)
            {
                ResetBallPhysics(Team2Balls[i], hiddenPos);
            }
        }

        private void ResetBallPhysics(GameObject ball, Vector3 position)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            ball.transform.position = position;
            ball.SetActive(true);
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.ThrowingPallino:
                    SetupPallinoThrow();
                    break;
                case GameState.Aiming:
                    SetupAiming();
                    break;
                case GameState.BallInMotion:
                    // Wait for ball to settle
                    break;
                case GameState.Scoring:
                    HandleScoring();
                    break;
                case GameState.RoundOver:
                    HandleRoundOver();
                    break;
                case GameState.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        private void SetupPallinoThrow()
        {
            if (Pallino == null) return;

            // Position pallino at the throw position
            Vector3 throwPos = courtSetup != null ? courtSetup.GetThrowPosition() : new Vector3(0, 0.5f, -10f);
            Pallino.transform.position = throwPos;
            Pallino.SetActive(true);

            if (ballLauncher != null)
            {
                ballLauncher.SetupThrow(Pallino, true);
            }

            OnTurnChanged?.Invoke(CurrentTeam);

            // If AI's turn to throw pallino (currently Team1 = player)
            // Team1 always throws pallino, so no AI needed here
        }

        private void SetupAiming()
        {
            GameObject ballToThrow = GetNextBall();
            if (ballToThrow == null)
            {
                SetState(GameState.Scoring);
                return;
            }

            Vector3 throwPos = courtSetup != null ? courtSetup.GetThrowPosition() : new Vector3(0, 0.5f, -10f);
            ballToThrow.transform.position = throwPos;
            ballToThrow.SetActive(true);

            if (ballLauncher != null)
            {
                ballLauncher.SetupThrow(ballToThrow, false);
            }

            OnTurnChanged?.Invoke(CurrentTeam);

            // If it's AI's turn, trigger AI
            if (CurrentTeam == Team.Team2 && aiPlayer != null)
            {
                aiPlayer.TakeTurn();
            }
        }

        private GameObject GetNextBall()
        {
            if (CurrentTeam == Team.Team1 && Team1BallsThrown < ballsPerTeam)
            {
                return Team1Balls[Team1BallsThrown];
            }
            else if (CurrentTeam == Team.Team2 && Team2BallsThrown < ballsPerTeam)
            {
                return Team2Balls[Team2BallsThrown];
            }
            return null;
        }

        public void OnBallThrown(GameObject ball)
        {
            SetState(GameState.BallInMotion);

            BallController bc = ball.GetComponent<BallController>();
            if (bc != null)
            {
                bc.OnBallSettled += OnBallSettled;
            }
        }

        private void OnBallSettled(BallController ball)
        {
            ball.OnBallSettled -= OnBallSettled;

            if (CurrentState == GameState.BallInMotion)
            {
                if (ball.isPallino)
                {
                    // Pallino has been thrown, now start bocce throws
                    // First throw goes to team that threw pallino (Team1), then opponent
                    CurrentTeam = Team.Team2;
                    SetState(GameState.Aiming);
                }
                else
                {
                    // Register the thrown ball
                    RegisterThrownBall(ball.gameObject);

                    // Check if all balls have been thrown
                    if (Team1BallsThrown >= ballsPerTeam && Team2BallsThrown >= ballsPerTeam)
                    {
                        SetState(GameState.Scoring);
                    }
                    else
                    {
                        // Determine next team
                        DetermineNextTeam();
                        SetState(GameState.Aiming);
                    }
                }
            }
        }

        private void RegisterThrownBall(GameObject ball)
        {
            if (CurrentTeam == Team.Team1)
            {
                ThrownTeam1Balls.Add(ball);
                Team1BallsThrown++;
            }
            else
            {
                ThrownTeam2Balls.Add(ball);
                Team2BallsThrown++;
            }
        }

        private void DetermineNextTeam()
        {
            // If one team is out of balls, the other team throws
            if (Team1BallsThrown >= ballsPerTeam)
            {
                CurrentTeam = Team.Team2;
                return;
            }
            if (Team2BallsThrown >= ballsPerTeam)
            {
                CurrentTeam = Team.Team1;
                return;
            }

            // The team farthest from pallino throws next
            if (scoreManager != null && Pallino != null && ThrownTeam1Balls.Count > 0 && ThrownTeam2Balls.Count > 0)
            {
                float team1Closest = scoreManager.GetClosestDistance(ThrownTeam1Balls, Pallino);
                float team2Closest = scoreManager.GetClosestDistance(ThrownTeam2Balls, Pallino);

                CurrentTeam = team1Closest <= team2Closest ? Team.Team2 : Team.Team1;
            }
            else
            {
                // If one team hasn't thrown yet, they go next
                if (ThrownTeam2Balls.Count == 0)
                    CurrentTeam = Team.Team2;
                else if (ThrownTeam1Balls.Count == 0)
                    CurrentTeam = Team.Team1;
            }
        }

        private void HandleScoring()
        {
            if (scoreManager == null || Pallino == null) return;

            int roundPoints = 0;
            Team scoringTeam = Team.Team1;
            scoreManager.CalculateRoundScore(ThrownTeam1Balls, ThrownTeam2Balls, Pallino, out scoringTeam, out roundPoints);

            if (scoringTeam == Team.Team1)
                Team1Score += roundPoints;
            else
                Team2Score += roundPoints;

            OnScoreUpdated?.Invoke(Team1Score, Team2Score);
            OnRoundEnded?.Invoke(scoringTeam, roundPoints);

            // Switch camera to overview
            if (cameraController != null)
                cameraController.SetMode(CameraMode.Overview);

            // Check for game over
            if (Team1Score >= pointsToWin || Team2Score >= pointsToWin)
            {
                StartCoroutine(DelayedStateChange(GameState.GameOver, 3f));
            }
            else
            {
                StartCoroutine(DelayedStateChange(GameState.RoundOver, 3f));
            }
        }

        private void HandleRoundOver()
        {
            CurrentRound++;
            StartCoroutine(DelayedNewRound(2f));
        }

        private IEnumerator DelayedNewRound(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNewRound();
        }

        private IEnumerator DelayedStateChange(GameState state, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetState(state);
        }

        private void HandleGameOver()
        {
            Team winner = Team1Score >= pointsToWin ? Team.Team1 : Team.Team2;
            OnGameOver?.Invoke(winner);
        }

        public void RestartGame()
        {
            StartGame();
        }
    }
}
