using System.Collections;
using UnityEngine;

namespace BochaGame
{
    public class AIPlayer : MonoBehaviour
    {
        [Header("AI Settings")]
        public AIDifficulty difficulty = AIDifficulty.Medium;
        public float thinkingDelay = 1.5f;

        [Header("Difficulty Tuning")]
        [Tooltip("Angle variance in degrees (lower = more accurate)")]
        public float easyAngleVariance = 25f;
        public float mediumAngleVariance = 12f;
        public float hardAngleVariance = 4f;

        [Tooltip("Power variance as a fraction (lower = more accurate)")]
        public float easyPowerVariance = 0.35f;
        public float mediumPowerVariance = 0.15f;
        public float hardPowerVariance = 0.05f;

        /// <summary>
        /// Called by GameManager when it's AI's turn.
        /// </summary>
        public void TakeTurn()
        {
            StartCoroutine(ThinkAndThrow());
        }

        private IEnumerator ThinkAndThrow()
        {
            // Wait for "thinking" time
            yield return new WaitForSeconds(thinkingDelay);

            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Pallino == null || gm.ballLauncher == null) yield break;

            // Calculate ideal throw parameters
            Vector3 pallinoPos = gm.Pallino.transform.position;
            Vector3 throwPos = gm.courtSetup != null ? gm.courtSetup.GetThrowPosition() : new Vector3(0, 0.5f, -10f);

            // Direction to pallino
            Vector3 toPallino = pallinoPos - throwPos;
            toPallino.y = 0;
            float distance = toPallino.magnitude;

            // Calculate ideal angle (relative to forward)
            float idealAngle = Mathf.Atan2(toPallino.x, toPallino.z) * Mathf.Rad2Deg;

            // Calculate ideal power based on distance
            // This is approximate — the ball launcher maps power to impulse force
            BallLauncher launcher = gm.ballLauncher;
            float idealPower = Mathf.Lerp(launcher.minPower, launcher.maxPower,
                Mathf.InverseLerp(2f, 25f, distance));

            // Apply variance based on difficulty
            float angleVar = GetAngleVariance();
            float powerVar = GetPowerVariance();

            float finalAngle = idealAngle + Random.Range(-angleVar, angleVar);
            float finalPower = idealPower * (1f + Random.Range(-powerVar, powerVar));
            finalPower = Mathf.Clamp(finalPower, launcher.minPower, launcher.maxPower);

            // Execute the throw
            launcher.AIThrow(finalPower, finalAngle);
        }

        private float GetAngleVariance()
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return easyAngleVariance;
                case AIDifficulty.Medium: return mediumAngleVariance;
                case AIDifficulty.Hard: return hardAngleVariance;
                default: return mediumAngleVariance;
            }
        }

        private float GetPowerVariance()
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return easyPowerVariance;
                case AIDifficulty.Medium: return mediumPowerVariance;
                case AIDifficulty.Hard: return hardPowerVariance;
                default: return mediumPowerVariance;
            }
        }
    }
}
