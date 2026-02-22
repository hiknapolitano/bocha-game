using System.Collections.Generic;
using UnityEngine;

namespace BochaGame
{
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// Returns the XZ-plane distance between two objects (ignoring vertical height).
        /// </summary>
        public float GetXZDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            diff.y = 0;
            return diff.magnitude;
        }

        /// <summary>
        /// Returns the distance of the closest ball in the list to the pallino.
        /// </summary>
        public float GetClosestDistance(List<GameObject> balls, GameObject pallino)
        {
            float closest = float.MaxValue;
            Vector3 pallinoPos = pallino.transform.position;

            foreach (GameObject ball in balls)
            {
                if (ball == null || !ball.activeInHierarchy) continue;
                float dist = GetXZDistance(ball.transform.position, pallinoPos);
                if (dist < closest)
                    closest = dist;
            }
            return closest;
        }

        /// <summary>
        /// Calculates the round score. The scoring team gets 1 point for each of their balls
        /// that is closer to the pallino than the opponent's closest ball.
        /// </summary>
        public void CalculateRoundScore(
            List<GameObject> team1Balls,
            List<GameObject> team2Balls,
            GameObject pallino,
            out Team scoringTeam,
            out int points)
        {
            points = 0;
            scoringTeam = Team.Team1;

            if (pallino == null)
            {
                return;
            }

            Vector3 pallinoPos = pallino.transform.position;

            // Find closest ball for each team
            float team1Closest = GetClosestDistance(team1Balls, pallino);
            float team2Closest = GetClosestDistance(team2Balls, pallino);

            // Determine scoring team
            if (team1Closest <= team2Closest)
            {
                scoringTeam = Team.Team1;
                // Count Team1 balls closer than Team2's closest
                foreach (GameObject ball in team1Balls)
                {
                    if (ball == null || !ball.activeInHierarchy) continue;
                    float dist = GetXZDistance(ball.transform.position, pallinoPos);
                    if (dist < team2Closest)
                        points++;
                }
            }
            else
            {
                scoringTeam = Team.Team2;
                // Count Team2 balls closer than Team1's closest
                foreach (GameObject ball in team2Balls)
                {
                    if (ball == null || !ball.activeInHierarchy) continue;
                    float dist = GetXZDistance(ball.transform.position, pallinoPos);
                    if (dist < team1Closest)
                        points++;
                }
            }

            // Ensure at least 1 point is scored
            if (points == 0) points = 1;
        }

        /// <summary>
        /// Returns a sorted list of (ball, distance) pairs from closest to farthest.
        /// </summary>
        public List<(GameObject ball, float distance)> GetBallDistances(List<GameObject> balls, GameObject pallino)
        {
            var result = new List<(GameObject ball, float distance)>();
            if (pallino == null) return result;

            Vector3 pallinoPos = pallino.transform.position;
            foreach (GameObject ball in balls)
            {
                if (ball == null || !ball.activeInHierarchy) continue;
                float dist = GetXZDistance(ball.transform.position, pallinoPos);
                result.Add((ball, dist));
            }

            result.Sort((a, b) => a.distance.CompareTo(b.distance));
            return result;
        }
    }
}
