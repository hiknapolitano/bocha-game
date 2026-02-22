using System.Collections.Generic;
using UnityEngine;

namespace BochaGame
{
    public class CourtSetup : MonoBehaviour
    {
        [Header("Court Dimensions (meters)")]
        public float courtLength = 27.5f;
        public float courtWidth = 4f;
        public float wallHeight = 0.3f;
        public float wallThickness = 0.15f;

        [Header("Ball Settings")]
        public float bocceBallRadius = 0.11f;
        public float pallinoRadius = 0.06f;
        public float ballMass = 0.9f;    // ~900g for bocce ball
        public float pallinoMass = 0.06f; // ~60g for pallino

        [Header("Physics")]
        public float dynamicFriction = 0.5f;
        public float staticFriction = 0.6f;
        public float bounciness = 0.1f;

        [Header("Colors")]
        public Color team1Color = new Color(0.85f, 0.15f, 0.15f); // Red
        public Color team2Color = new Color(0.15f, 0.3f, 0.85f);  // Blue
        public Color pallinoColor = new Color(0.68f, 1f, 0.18f); // Bright yellow-green
        public Color courtColor = new Color(0.72f, 0.58f, 0.38f);  // Sandy brown
        public Color wallColor = new Color(0.45f, 0.3f, 0.15f);    // Dark wood
        public Color surroundColor = new Color(0.25f, 0.55f, 0.2f); // Grass green

        // Runtime references
        [HideInInspector] public GameObject pallinoInstance;
        [HideInInspector] public List<GameObject> team1BallInstances = new List<GameObject>();
        [HideInInspector] public List<GameObject> team2BallInstances = new List<GameObject>();

        private PhysicsMaterial courtPhysicsMat;
        private PhysicsMaterial ballPhysicsMat;

        private void Awake()
        {
            CreatePhysicsMaterials();
            CreateCourt();
            CreateBalls();
            CreateLighting();
            CreateSkybox();
            CreateGameComponents();
        }

        private void CreatePhysicsMaterials()
        {
            courtPhysicsMat = new PhysicsMaterial("CourtSurface");
            courtPhysicsMat.dynamicFriction = dynamicFriction;
            courtPhysicsMat.staticFriction = staticFriction;
            courtPhysicsMat.bounciness = bounciness;
            courtPhysicsMat.frictionCombine = PhysicsMaterialCombine.Average;
            courtPhysicsMat.bounceCombine = PhysicsMaterialCombine.Minimum;

            ballPhysicsMat = new PhysicsMaterial("BallSurface");
            ballPhysicsMat.dynamicFriction = 0.4f;
            ballPhysicsMat.staticFriction = 0.5f;
            ballPhysicsMat.bounciness = 0.15f;
            ballPhysicsMat.frictionCombine = PhysicsMaterialCombine.Average;
            ballPhysicsMat.bounceCombine = PhysicsMaterialCombine.Minimum;
        }

        private void CreateCourt()
        {
            // --- Parent container ---
            GameObject court = new GameObject("Court");

            // --- Ground ---
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "CourtGround";
            ground.transform.SetParent(court.transform);
            ground.transform.localScale = new Vector3(courtWidth, 0.1f, courtLength);
            ground.transform.localPosition = new Vector3(0, -0.05f, 0);
            ground.GetComponent<Renderer>().material = CreateMaterial(courtColor);
            ground.GetComponent<Collider>().material = courtPhysicsMat;
            ground.layer = 0;

            // --- Surrounding grass ---
            GameObject surround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surround.name = "Surround";
            surround.transform.SetParent(court.transform);
            surround.transform.localScale = new Vector3(courtWidth * 4, 0.08f, courtLength * 1.5f);
            surround.transform.localPosition = new Vector3(0, -0.1f, 0);
            surround.GetComponent<Renderer>().material = CreateMaterial(surroundColor);
            surround.GetComponent<Collider>().material = courtPhysicsMat;

            // --- Walls ---
            float halfLength = courtLength / 2f;
            float halfWidth = courtWidth / 2f;

            // Left wall
            CreateWall(court.transform, "LeftWall",
                new Vector3(-halfWidth - wallThickness / 2f, wallHeight / 2f, 0),
                new Vector3(wallThickness, wallHeight, courtLength));

            // Right wall
            CreateWall(court.transform, "RightWall",
                new Vector3(halfWidth + wallThickness / 2f, wallHeight / 2f, 0),
                new Vector3(wallThickness, wallHeight, courtLength));

            // Far end wall
            CreateWall(court.transform, "FarWall",
                new Vector3(0, wallHeight / 2f, halfLength + wallThickness / 2f),
                new Vector3(courtWidth + wallThickness * 2, wallHeight, wallThickness));

            // Near end wall
            CreateWall(court.transform, "NearWall",
                new Vector3(0, wallHeight / 2f, -halfLength - wallThickness / 2f),
                new Vector3(courtWidth + wallThickness * 2, wallHeight, wallThickness));

            // --- Court markings (foul lines) ---
            // Foul line at throwing end (about 3m from the end)
            CreateLine(court.transform, "FoulLine",
                new Vector3(0, 0.005f, -halfLength + 3f),
                new Vector3(courtWidth - 0.1f, 0.01f, 0.04f),
                Color.white);

            // Center line
            CreateLine(court.transform, "CenterLine",
                new Vector3(0, 0.005f, 0),
                new Vector3(courtWidth - 0.1f, 0.01f, 0.04f),
                new Color(1f, 1f, 1f, 0.3f));

            // Pallino minimum line (pallino must pass this)
            CreateLine(court.transform, "PallinoMinLine",
                new Vector3(0, 0.005f, halfLength * 0.3f),
                new Vector3(courtWidth - 0.1f, 0.01f, 0.03f),
                new Color(1f, 1f, 0f, 0.4f));
        }

        private void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = CreateMaterial(wallColor);
            wall.GetComponent<Collider>().material = ballPhysicsMat;
        }

        private void CreateLine(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = name;
            line.transform.SetParent(parent);
            line.transform.localPosition = position;
            line.transform.localScale = scale;

            Material mat = CreateMaterial(color);
            if (color.a < 1f)
            {
                SetMaterialTransparent(mat);
            }
            line.GetComponent<Renderer>().material = mat;

            // Lines don't need colliders
            Collider col = line.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        private void CreateBalls()
        {
            // Create pallino
            pallinoInstance = CreateBall("Pallino", pallinoRadius, pallinoMass, pallinoColor, true);

            // Create team balls
            for (int i = 0; i < 4; i++)
            {
                GameObject ball1 = CreateBall($"Team1_Ball_{i}", bocceBallRadius, ballMass, team1Color, false);
                BallController bc1 = ball1.GetComponent<BallController>();
                bc1.team = Team.Team1;
                bc1.ballIndex = i;
                team1BallInstances.Add(ball1);

                GameObject ball2 = CreateBall($"Team2_Ball_{i}", bocceBallRadius, ballMass, team2Color, false);
                BallController bc2 = ball2.GetComponent<BallController>();
                bc2.team = Team.Team2;
                bc2.ballIndex = i;
                team2BallInstances.Add(ball2);
            }
        }

        private GameObject CreateBall(string name, float radius, float mass, Color color, bool isPallino)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = name;
            ball.transform.localScale = Vector3.one * radius * 2f;
            ball.transform.position = new Vector3(0, -10f, 0); // Hidden initially

            // Material
            Material mat = CreateMaterial(color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.3f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.7f);
            else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.7f);
            ball.GetComponent<Renderer>().material = mat;

            // Physics
            SphereCollider col = ball.GetComponent<SphereCollider>();
            if (col == null) col = ball.AddComponent<SphereCollider>();
            col.material = ballPhysicsMat;

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.5f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Ball controller
            BallController bc = ball.AddComponent<BallController>();
            bc.isPallino = isPallino;

            // Add marker to pallino for visibility
            if (isPallino)
            {
                ball.AddComponent<PallinoMarker>();
            }

            return ball;
        }

        private void CreateLighting()
        {
            // Main directional light (sun)
            GameObject lightObj = new GameObject("DirectionalLight");
            Light sunLight = lightObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.88f); // Warm sunlight
            sunLight.intensity = 1.2f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.6f;
            lightObj.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

            // Fill light (subtle)
            GameObject fillObj = new GameObject("FillLight");
            Light fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.6f, 0.75f, 1f); // Cool sky fill
            fillLight.intensity = 0.3f;
            fillLight.shadows = LightShadows.None;
            fillObj.transform.rotation = Quaternion.Euler(150f, -60f, 0f);

            // Ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.65f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.65f, 0.6f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.2f);
        }

        private void CreateSkybox()
        {
            // Set a simple sky color as fallback
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.45f, 0.7f, 0.95f); // Light blue sky
            }
        }

        private void CreateGameComponents()
        {
            // Create BallLauncher
            GameObject launcherObj = new GameObject("BallLauncher");
            launcherObj.AddComponent<BallLauncher>();

            // Create ScoreManager
            GameObject scoreObj = new GameObject("ScoreManager");
            scoreObj.AddComponent<ScoreManager>();

            // Create CameraController (attach to main camera)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.GetComponent<CameraController>() == null)
                    mainCam.gameObject.AddComponent<CameraController>();
            }
            else
            {
                // Create a camera if none exists
                GameObject camObj = new GameObject("MainCamera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
                camObj.AddComponent<AudioListener>();
                camObj.AddComponent<CameraController>();
            }

            // Create UIManager
            GameObject uiObj = new GameObject("UIManager");
            uiObj.AddComponent<UIManager>();

            // Create AIPlayer
            GameObject aiObj = new GameObject("AIPlayer");
            aiObj.AddComponent<AIPlayer>();
        }

        /// <summary>
        /// Returns the position where balls should be placed for throwing.
        /// </summary>
        public Vector3 GetThrowPosition()
        {
            float halfLength = courtLength / 2f;
            // Throwing position: behind the foul line, centered
            return new Vector3(0, bocceBallRadius + 0.05f, -halfLength + 2f);
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            Material mat = new Material(shader);
            // Set color via _BaseColor (URP) or _Color (Built-in)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            return mat;
        }

        private void SetMaterialTransparent(Material mat)
        {
            // Try URP surface type first
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                mat.SetFloat("_Blend", 0);    // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                // Built-in RP fallback
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
    }
}
