using UnityEngine;
using System.Collections;

namespace BochaGame
{
    /// <summary>
    /// A procedural humanoid player character built from Unity primitives.
    /// Stands at the throw position, holds and throws the ball.
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Character Settings")]
        public Color teamColor = Color.red;
        public string playerName = "Player";

        // Body parts
        private GameObject head;
        private GameObject body;
        private GameObject leftArm;
        private GameObject rightArm;
        private GameObject leftLeg;
        private GameObject rightLeg;
        private GameObject throwingHand; // tip of right arm, holds the ball

        // Materials
        private Material skinMat;
        private Material shirtMat;
        private Material pantsMat;

        // Animation
        private bool isAnimating = false;

        private void Awake()
        {
            BuildCharacter();
        }

        private void BuildCharacter()
        {
            // Create materials
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            skinMat = new Material(shader);
            SetMatColor(skinMat, new Color(0.87f, 0.72f, 0.58f)); // skin tone

            shirtMat = new Material(shader);
            SetMatColor(shirtMat, teamColor);

            pantsMat = new Material(shader);
            SetMatColor(pantsMat, new Color(0.25f, 0.25f, 0.3f)); // dark pants

            // === HEAD (sphere) ===
            head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.25f, 0.28f, 0.25f);
            RemoveCollider(head);
            head.GetComponent<Renderer>().material = skinMat;

            // === BODY (capsule) ===
            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0, 1.2f, 0);
            body.transform.localScale = new Vector3(0.35f, 0.35f, 0.22f);
            RemoveCollider(body);
            body.GetComponent<Renderer>().material = shirtMat;

            // === LEFT ARM (capsule) ===
            leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(transform);
            leftArm.transform.localPosition = new Vector3(-0.28f, 1.2f, 0);
            leftArm.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
            leftArm.transform.localRotation = Quaternion.Euler(0, 0, 10f);
            RemoveCollider(leftArm);
            leftArm.GetComponent<Renderer>().material = shirtMat;

            // === RIGHT ARM (capsule) — throwing arm ===
            rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(transform);
            rightArm.transform.localPosition = new Vector3(0.28f, 1.2f, 0);
            rightArm.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
            rightArm.transform.localRotation = Quaternion.Euler(0, 0, -10f);
            RemoveCollider(rightArm);
            rightArm.GetComponent<Renderer>().material = shirtMat;

            // Throwing hand (small sphere at end of right arm)
            throwingHand = new GameObject("ThrowingHand");
            throwingHand.transform.SetParent(rightArm.transform);
            throwingHand.transform.localPosition = new Vector3(0, -0.9f, 0);

            // === LEFT LEG (capsule) ===
            leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(transform);
            leftLeg.transform.localPosition = new Vector3(-0.1f, 0.45f, 0);
            leftLeg.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
            RemoveCollider(leftLeg);
            leftLeg.GetComponent<Renderer>().material = pantsMat;

            // === RIGHT LEG (capsule) ===
            rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(transform);
            rightLeg.transform.localPosition = new Vector3(0.1f, 0.45f, 0);
            rightLeg.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
            RemoveCollider(rightLeg);
            rightLeg.GetComponent<Renderer>().material = pantsMat;

            // === SHOES (small cubes at bottom of legs) ===
            CreateShoe(leftLeg.transform, "LeftShoe");
            CreateShoe(rightLeg.transform, "RightShoe");
        }

        private void CreateShoe(Transform legParent, string name)
        {
            GameObject shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoe.name = name;
            shoe.transform.SetParent(legParent);
            shoe.transform.localPosition = new Vector3(0, -0.8f, 0.2f);
            shoe.transform.localScale = new Vector3(0.8f, 0.3f, 1.5f);
            RemoveCollider(shoe);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            Material shoeMat = new Material(shader);
            SetMatColor(shoeMat, new Color(0.2f, 0.15f, 0.1f)); // dark brown
            shoe.GetComponent<Renderer>().material = shoeMat;
        }

        /// <summary>
        /// Set the team color for the character's shirt.
        /// </summary>
        public void SetTeamColor(Color color)
        {
            teamColor = color;
            if (shirtMat != null)
                SetMatColor(shirtMat, color);
        }

        /// <summary>
        /// Position the character at the specified location, facing forward (+Z).
        /// </summary>
        public void SetPosition(Vector3 position, float facingAngle = 0f)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0, facingAngle, 0);
        }

        /// <summary>
        /// Show or hide the character.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Play a throw animation — right arm swings forward.
        /// </summary>
        public void PlayThrowAnimation()
        {
            if (!isAnimating)
                StartCoroutine(ThrowAnimationCoroutine());
        }

        private IEnumerator ThrowAnimationCoroutine()
        {
            isAnimating = true;

            // Phase 1: Wind up — arm goes back
            float windUpTime = 0.25f;
            float elapsed = 0f;
            Quaternion startRot = rightArm.transform.localRotation;
            Quaternion windUpRot = Quaternion.Euler(-60f, 0, -10f); // arm back

            while (elapsed < windUpTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windUpTime;
                rightArm.transform.localRotation = Quaternion.Slerp(startRot, windUpRot, t);
                // Lean body back slightly
                body.transform.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-10f, 0, 0), t);
                yield return null;
            }

            // Phase 2: Throw forward — arm swings forward quickly
            float throwTime = 0.15f;
            elapsed = 0f;
            Quaternion throwRot = Quaternion.Euler(80f, 0, -10f); // arm forward/up

            while (elapsed < throwTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / throwTime;
                rightArm.transform.localRotation = Quaternion.Slerp(windUpRot, throwRot, t);
                body.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(-10f, 0, 0), Quaternion.Euler(5f, 0, 0), t);
                yield return null;
            }

            // Phase 3: Follow through — arm comes back to rest
            float followTime = 0.5f;
            elapsed = 0f;
            Quaternion restRot = Quaternion.Euler(0, 0, -10f);

            while (elapsed < followTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / followTime;
                t = t * t * (3f - 2f * t); // smoothstep easing
                rightArm.transform.localRotation = Quaternion.Slerp(throwRot, restRot, t);
                body.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(5f, 0, 0), Quaternion.identity, t);
                yield return null;
            }

            rightArm.transform.localRotation = restRot;
            body.transform.localRotation = Quaternion.identity;
            isAnimating = false;
        }

        /// <summary>
        /// Get the world position of the throwing hand (for ball positioning).
        /// </summary>
        public Vector3 GetThrowingHandPosition()
        {
            return throwingHand != null ? throwingHand.transform.position : transform.position + Vector3.up;
        }

        private void RemoveCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void SetMatColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }
    }
}
