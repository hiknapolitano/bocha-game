using UnityEngine;

namespace BochaGame
{
    /// <summary>
    /// Adds a pulsing ground marker ring around the pallino so it's always easy to spot.
    /// Attach this component to the pallino GameObject.
    /// </summary>
    public class PallinoMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        public float markerRadius = 0.25f;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.15f;
        public Color markerColor = new Color(0.68f, 1f, 0.18f, 0.5f);

        private GameObject markerRing;
        private Vector3 baseScale;

        private void Start()
        {
            CreateMarkerRing();
        }

        private void CreateMarkerRing()
        {
            // Create a flat cylinder as the ring marker
            markerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerRing.name = "PallinoMarker";
            markerRing.transform.SetParent(transform);
            markerRing.transform.localPosition = new Vector3(0, -transform.localScale.y / 2f + 0.005f, 0);

            float diameter = markerRadius * 2f;
            baseScale = new Vector3(diameter, 0.005f, diameter);
            markerRing.transform.localScale = baseScale;

            // Remove collider — visual only
            Collider col = markerRing.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Create material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            Material mat = new Material(shader);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", markerColor);
            else
                mat.color = markerColor;

            // Make transparent
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            if (mat.HasProperty("_Emission"))
                mat.SetColor("_EmissionColor", markerColor * 0.5f);

            markerRing.GetComponent<Renderer>().material = mat;
        }

        private void Update()
        {
            if (markerRing == null) return;

            // Pulse the marker scale
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            markerRing.transform.localScale = baseScale * pulse;

            // Keep the marker at ground level even if the ball bounces
            Vector3 localPos = markerRing.transform.localPosition;
            localPos.y = -transform.localScale.y / 2f + 0.005f;
            markerRing.transform.localPosition = localPos;
        }
    }
}
