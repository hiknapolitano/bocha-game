namespace BochaGame
{
    public enum GameState
    {
        WaitingToStart,
        ThrowingPallino,
        Aiming,
        BallInMotion,
        Scoring,
        RoundOver,
        GameOver
    }

    public enum Team
    {
        Team1,
        Team2
    }

    public enum CameraMode
    {
        Follow,
        Overview,
        Transitioning
    }

    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard
    }
}
