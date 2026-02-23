using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BochaGame
{
    /// <summary>
    /// A 2D minimap showing the court, all balls, and the pallino.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [Header("Minimap Settings")]
        public float mapWidth = 160f;
        public float mapHeight = 260f;
        public float margin = 12f;
        public float dotSize = 12f;
        public float pallinoDotSize = 8f;

        private float courtLength = 27.5f;
        private float courtWidth = 4f;

        private RectTransform mapPanel;
        private RectTransform courtArea;
        private List<Image> team1Dots = new List<Image>();
        private List<Image> team2Dots = new List<Image>();
        private Image pallinoDot;
        private Sprite circleSprite;
        private bool initialized = false;

        private Color team1Color = new Color(0.95f, 0.2f, 0.2f, 1f);
        private Color team2Color = new Color(0.2f, 0.45f, 0.95f, 1f);
        private Color pallinoColor = new Color(0.95f, 0.95f, 0.2f, 1f);
        private Color courtBgColor = new Color(0.55f, 0.42f, 0.28f, 0.85f);
        private Color borderColor = new Color(0.3f, 0.22f, 0.12f, 0.95f);

        private void LateUpdate()
        {
            if (!initialized)
            {
                TryInitialize();
                return;
            }

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            // Update pallino
            if (gm.Pallino != null)
            {
                // Pallino is "in play" if it has been thrown (not at hidden position)
                bool inPlay = gm.Pallino.transform.position.y > -5f;
                if (inPlay)
                {
                    UpdateDot(pallinoDot, gm.Pallino.transform.position);
                    pallinoDot.gameObject.SetActive(true);
                    
                    // Match pallino color
                    Renderer r = gm.Pallino.GetComponent<Renderer>();
                    if (r != null) pallinoDot.color = r.material.color;
                }
                else
                {
                    pallinoDot.gameObject.SetActive(false);
                }
            }

            // Update team balls
            UpdateTeamDots(gm.Team1Balls, team1Dots);
            UpdateTeamDots(gm.Team2Balls, team2Dots);
        }

        private void TryInitialize()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.courtSetup == null) return;

            courtLength = gm.courtSetup.courtLength;
            courtWidth = gm.courtSetup.courtWidth;

            CreateCircleSprite();
            CreateMinimapUI();
            initialized = true;
        }

        private void CreateCircleSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            float center = size / 2f;
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist < radius)
                    {
                        // Antialiasing
                        float alpha = Mathf.Clamp01(radius - dist);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void CreateMinimapUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Border/container
            GameObject panelObj = new GameObject("MinimapPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            mapPanel = panelObj.AddComponent<RectTransform>();
            mapPanel.anchorMin = new Vector2(0, 1);
            mapPanel.anchorMax = new Vector2(0, 1);
            mapPanel.pivot = new Vector2(0, 1);
            mapPanel.anchoredPosition = new Vector2(margin, -margin);
            mapPanel.sizeDelta = new Vector2(mapWidth + 6, mapHeight + 6);

            Image borderImg = panelObj.AddComponent<Image>();
            borderImg.color = borderColor;

            // Court area
            GameObject courtObj = new GameObject("CourtArea");
            courtObj.transform.SetParent(panelObj.transform, false);
            Image courtImg = courtObj.AddComponent<Image>();
            courtImg.color = courtBgColor;
            courtArea = courtObj.GetComponent<RectTransform>();
            courtArea.anchorMin = Vector2.zero;
            courtArea.anchorMax = Vector2.one;
            courtArea.offsetMin = new Vector2(3, 3);
            courtArea.offsetMax = new Vector2(-3, -3);

            // Center line
            GameObject cl = new GameObject("CenterLine");
            cl.transform.SetParent(courtObj.transform, false);
            Image clImg = cl.AddComponent<Image>();
            clImg.color = new Color(1f, 1f, 1f, 0.15f);
            RectTransform clRect = cl.GetComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.05f, 0.5f);
            clRect.anchorMax = new Vector2(0.95f, 0.5f);
            clRect.sizeDelta = new Vector2(0, 1);

            // Foul line
            GameObject fl = new GameObject("FoulLine");
            fl.transform.SetParent(courtObj.transform, false);
            Image flImg = fl.AddComponent<Image>();
            flImg.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform flRect = fl.GetComponent<RectTransform>();
            float foulNorm = (3f) / courtLength; // 3m from near end
            flRect.anchorMin = new Vector2(0.05f, foulNorm);
            flRect.anchorMax = new Vector2(0.95f, foulNorm);
            flRect.sizeDelta = new Vector2(0, 1);

            // Create dots (4 per team)
            for (int i = 0; i < 4; i++)
            {
                team1Dots.Add(CreateDot(courtObj.transform, $"T1_{i}", team1Color, dotSize));
                team2Dots.Add(CreateDot(courtObj.transform, $"T2_{i}", team2Color, dotSize));
            }
            pallinoDot = CreateDot(courtObj.transform, "Pallino", pallinoColor, pallinoDotSize);
        }

        private Image CreateDot(Transform parent, string name, Color color, float size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = color;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            obj.SetActive(false);
            return img;
        }

        private void UpdateTeamDots(List<GameObject> balls, List<Image> dots)
        {
            if (balls == null) return;

            for (int i = 0; i < dots.Count; i++)
            {
                if (i < balls.Count && balls[i] != null)
                {
                    // Ball is "in play" if it's not at the hidden position
                    bool inPlay = balls[i].transform.position.y > -5f;
                    if (inPlay)
                    {
                        UpdateDot(dots[i], balls[i].transform.position);
                        dots[i].gameObject.SetActive(true);

                        // Match ball color
                        Renderer r = balls[i].GetComponent<Renderer>();
                        if (r != null) dots[i].color = r.material.color;
                    }
                    else
                    {
                        dots[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    dots[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateDot(Image dot, Vector3 worldPos)
        {
            float nx = (worldPos.x + courtWidth / 2f) / courtWidth;
            float nz = (worldPos.z + courtLength / 2f) / courtLength;
            nx = Mathf.Clamp01(nx);
            nz = Mathf.Clamp01(nz);

            RectTransform rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(nx, nz);
            rect.anchorMax = new Vector2(nx, nz);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
