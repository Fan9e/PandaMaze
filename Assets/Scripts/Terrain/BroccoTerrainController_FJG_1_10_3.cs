using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Broccoli.Controller
{
    /// <summary>
    /// Der er mange Vector4-felter for vindværdier.
    /// Her samles de i en struct (WindShaderValues) for at gøre koden lettere at administrere og reducere rod.
    /// </summary>
    public struct WindShaderValues
    {
        public Vector4 STWindVector;
        public Vector4 STWindGlobal;
        public Vector4 STWindBranch;
        public Vector4 STWindBranchTwitch;
        public Vector4 STWindBranchWhip;
        public Vector4 STWindBranchAnchor;
        public Vector4 STWindBranchAdherences;
        public Vector4 STWindTurbulences;
        public Vector4 STWindLeaf1Ripple;
        public Vector4 STWindLeaf1Tumble;
        public Vector4 STWindLeaf1Twitch;
        public Vector4 STWindLeaf2Ripple;
        public Vector4 STWindLeaf2Tumble;
        public Vector4 STWindLeaf2Twitch;
        public Vector4 STWindFrondRipple;
    }

    /// <summary>
    /// Kontrollerer træbroccoli-forekomster i terræner og opdaterer vinddata til SpeedTree8-shadere.
    /// </summary>
    public class BroccoTerrainController_FJG_1_10_3 : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Terrænkomponent.
        /// </summary>
        Terrain terrain = null;

        /// <summary>
        /// Holder styr på de væsentlige materialer, der skal opdateres.
        /// </summary>
        List<Material> _mats = new List<Material>();

        /// <summary>
        /// Holder styr på parametrene for materialeinstanser.
        /// For BroccoTreeController:
        /// x: sproutTurbulance.
        /// y: sproutSway.
        /// z: localWindAmplitude.
        /// For BroccoTreeController_FJG_1_10_3:
        /// x: trunkBending.
        /// </summary>
        List<Vector3> _matParams = new List<Vector3>();

        /// <summary>
        /// Materialer taget fra BroccoTreeController_FJG_1_10_3.
        /// </summary>
        Material[] broccoMaterials;

        /// <summary>
        /// Materialeparameterrække.
        /// Parametre er taget fra BroccoTreeControllers2.
        /// </summary>
        Vector3[] broccoMaterialParams;

        bool requiresUpdateWindZoneValues = true;

        private float baseWindAmplitude = 0.2752f;
        private float windGlobalW = 1.728f;
        public static float globalWindAmplitude = 1f;

        public float valueWindMain = 0f;
        public float valueWindTurbulence = 0f;
        public Vector3 valueWindDirection = Vector3.zero;

        private float valueLeafSwayFactor = 1f;
        private float valueLeafTurbulenceFactor = 1f;
        private int _frameCount = -1;

        #endregion

        #region Shader State

        float valueTime = 0f;
        float valueTimeWindMain = 0f;
        float windTimeScale = 1f;

        /// <summary>
        /// Objekt der indeholder alle Vector4-vindværdierne til SpeedTree8-vindshaderen.
        /// </summary>
        WindShaderValues value2STWind = new WindShaderValues();

        #endregion

        #region Shader Property Ids & Keywords

        static int propWindEnabled = 0;
        static int propWindQuality = 0;
        static int propSTWindVector = 0;
        static int propSTWindGlobal = 0;
        static int propSTWindBranch = 0;
        static int propSTWindBranchTwitch = 0;
        static int propSTWindBranchWhip = 0;
        static int propSTWindBranchAnchor = 0;
        static int propSTWindBranchAdherences = 0;
        static int propSTWindTurbulences = 0;
        static int propSTWindLeaf1Ripple = 0;
        static int propSTWindLeaf1Tumble = 0;
        static int propSTWindLeaf1Twitch = 0;
        static int propSTWindLeaf2Ripple = 0;
        static int propSTWindLeaf2Tumble = 0;
        static int propSTWindLeaf2Twitch = 0;
        static int propSTWindFrondRipple = 0;

        // Shader keyword-navne som konstante variabler.
        private const string KW_WINDQUALITY_NONE = "_WINDQUALITY_NONE";
        private const string KW_WINDQUALITY_FASTEST = "_WINDQUALITY_FASTEST";
        private const string KW_WINDQUALITY_FAST = "_WINDQUALITY_FAST";
        private const string KW_WINDQUALITY_BETTER = "_WINDQUALITY_BETTER";
        private const string KW_WINDQUALITY_BEST = "_WINDQUALITY_BEST";
        private const string KW_WINDQUALITY_PALM = "_WINDQUALITY_PALM";

        private const string KW_ENABLE_WIND = "ENABLE_WIND";

        // Alle windquality-keywords samlet et sted, så de nemt kan disables.
        private static readonly string[] AllWindQualityKeywords =
        {
            KW_WINDQUALITY_NONE,
            KW_WINDQUALITY_FASTEST,
            KW_WINDQUALITY_FAST,
            KW_WINDQUALITY_BETTER,
            KW_WINDQUALITY_BEST,
            KW_WINDQUALITY_PALM
        };

        // Mapping fra WindQuality til shader-keyword.
        private static readonly Dictionary<BroccoTreeController_FJG_1_10_3.WindQuality, string> WindQualityKeywords =
            new Dictionary<BroccoTreeController_FJG_1_10_3.WindQuality, string>
            {
                { BroccoTreeController_FJG_1_10_3.WindQuality.None,    KW_WINDQUALITY_NONE },
                { BroccoTreeController_FJG_1_10_3.WindQuality.Fastest, KW_WINDQUALITY_FASTEST },
                { BroccoTreeController_FJG_1_10_3.WindQuality.Fast,    KW_WINDQUALITY_FAST },
                { BroccoTreeController_FJG_1_10_3.WindQuality.Better,  KW_WINDQUALITY_BETTER },
                { BroccoTreeController_FJG_1_10_3.WindQuality.Best,    KW_WINDQUALITY_BEST },
                { BroccoTreeController_FJG_1_10_3.WindQuality.Palm,    KW_WINDQUALITY_PALM },
            };

        #endregion

        #region Wind Constants

        /// <summary>
        /// Samler alle "magic number"-konstanter relateret til vindberegninger.
        /// </summary>
        private static class WindConstants
        {
            // ApplyBroccoTreeWind
            public const float TrunkBendingScale = 0.1f;
            public const float TrunkBendingOffset = 0.001f;
            public const float WindGlobalWScale = 1.125f;
            public const float BranchWindMainScale = 0.35f;
            public const float BranchAnchorScale = 2f;
            public const float BranchTwitchBase = 0.65f;
            public const float BranchTwitchMaxReduction = 0.35f;
            public const float BranchTwitchTurbulenceNorm = 3f;

            public const float LeafTumbleTurbulenceScale = 0.1f;
            public const float LeafTumbleMainMax = 0.5f;
            public const float LeafTumbleMainMin = 0.1f;
            public const float LeafTumbleMainNorm = 4f;
            public const float LeafTumbleMainScale = 0.085f;

            public const float LeafTwitchMainScale = 0.165f;
            public const float LeafTwitchTurbulenceScale = 0.165f;
            public const float LeafRippleTurbulenceScale = 0.01f;

            // UpdateBroccoTreeWind
            public const float WindGlobalTimeScale = 0.5f;
            public const float LocalWindAmplitudeScale = 0.1f;
            public const float LocalWindAmplitudeOffset = 0.001f;
            public const float LeafTwitchTimeScale = 0.5f;

            // Initial-værdier i SetupBrocco2TreeController
            public const float STW_Leaf1Tumble_X = 0.15f;
            public const float STW_Leaf1Tumble_Y = 0.15f;
            public const float STW_Turbulences_X = 0.7f;
            public const float STW_Turbulences_Y = 0.3f;

            public const float FrondRippleTimeScale = 1f;
            public const float FrondRippleSecond = 0.01f;
            public const float FrondRippleThird = 2f;
            public const float FrondRippleFourth = 10f;

            public const float DefaultVecZero = 0f;
        }

        #endregion

        #region Static Constructor

        /// <summary>
        /// Statisk konstruktør til denne klasse.
        /// Initialiserer shader-property ID-felterne en gang.
        /// </summary>
        static BroccoTerrainController_FJG_1_10_3()
        {
            propWindEnabled = Shader.PropertyToID("_WindEnabled");
            propWindQuality = Shader.PropertyToID("_WindQuality");
            propSTWindVector = Shader.PropertyToID("_ST_WindVector");
            propSTWindGlobal = Shader.PropertyToID("_ST_WindGlobal");
            propSTWindBranch = Shader.PropertyToID("_ST_WindBranch");
            propSTWindBranchTwitch = Shader.PropertyToID("_ST_WindBranchTwitch");
            propSTWindBranchWhip = Shader.PropertyToID("_ST_WindBranchWhip");
            propSTWindBranchAnchor = Shader.PropertyToID("_ST_WindBranchAnchor");
            propSTWindBranchAdherences = Shader.PropertyToID("_ST_WindBranchAdherences");
            propSTWindTurbulences = Shader.PropertyToID("_ST_WindTurbulences");
            propSTWindLeaf1Ripple = Shader.PropertyToID("_ST_WindLeaf1Ripple");
            propSTWindLeaf1Tumble = Shader.PropertyToID("_ST_WindLeaf1Tumble");
            propSTWindLeaf1Twitch = Shader.PropertyToID("_ST_WindLeaf1Twitch");
            propSTWindLeaf2Ripple = Shader.PropertyToID("_ST_WindLeaf2Ripple");
            propSTWindLeaf2Tumble = Shader.PropertyToID("_ST_WindLeaf2Tumble");
            propSTWindLeaf2Twitch = Shader.PropertyToID("_ST_WindLeaf2Twitch");
            propSTWindFrondRipple = Shader.PropertyToID("_ST_WindFrondRipple");
        }

        #endregion

        #region Unity Events

        /// <summary>
        /// Starter denne instans.
        /// </summary>
        public void Start()
        {
            // Henter terrænet.
            terrain = GetComponent<Terrain>();
            if (terrain != null)
            {
                requiresUpdateWindZoneValues = true;
                SetupWind();
            }
        }

        /// <summary>
        /// Opdater denne instans.
        /// </summary>
        private void Update()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying && _frameCount != Time.frameCount)
            {
                valueTime = (EditorApplication.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
                valueTime *= windTimeScale;
                valueTimeWindMain = valueTime * 0.66f;

                for (int i = 0; i < broccoMaterials.Length; i++)
                {
                    UpdateBroccoTreeWind(broccoMaterials[i], broccoMaterialParams[i]);
                }

                _frameCount = Time.frameCount;
            }
#else
            if (_frameCount != Time.frameCount)
            {
                valueTime = Time.time;
                valueTime *= windTimeScale;
                valueTimeWindMain = valueTime * 0.66f;
                for (int i = 0; i < broccoMaterials.Length; i++)
                {
                    UpdateBroccoTreeWind(broccoMaterials[i], broccoMaterialParams[i]);
                }
                _frameCount = Time.frameCount;
            }
#endif
        }

        #endregion

        #region Public Wind API

        /// <summary>
        /// Opdaterer vindparametrene for systemet, inklusive vindstyrke, turbulens og retning.
        /// Denne metode opdaterer de interne vindparametre og anvender ændringerne på alle tilknyttede materialer.
        /// </summary>
        /// <remarks>
        /// Dette er nyttigt i scenarier, hvor vinden skal ændres i realtid
        /// og bør kaldes, når vindforholdene skal justeres dynamisk.
        /// </remarks>
        /// <param name="windMain">Primær vindstyrke. Denne værdi bestemmer den samlede intensitet af vindeffekten.</param>
        /// <param name="windTurbulence">Turbulens af vinden. Højere værdier resulterer i mere uregelmæssig og kaotisk vindadfærd.</param>
        /// <param name="windDirection">Retningen af vinden, repræsenteret som en 3D-vektor.</param>
        public void UpdateWind(float windMain, float windTurbulence, Vector3 windDirection)
        {
            valueWindMain = windMain;
            valueWindTurbulence = windTurbulence;
            valueWindDirection = windDirection;

            for (int i = 0; i < broccoMaterials.Length; i++)
            {
                ApplyBroccoTreeWind(broccoMaterials[i], broccoMaterialParams[i]);
                UpdateBroccoTreeWind(broccoMaterials[i], broccoMaterialParams[i]);
            }
        }

        /// <summary>
        /// Opdater parametre relateret til den første detekterede retningsbestemte vindzone.
        /// </summary>
        public void GetWindZoneValues()
        {
            Vector4 defaultWindDirection = new Vector4(1f, 0f, 0f, 0f);
            const float SpeedTree8WindScale = 0.4f;
            const float DefaultWindScale = 1f;

            bool isST8 = true;
            valueWindDirection = defaultWindDirection;

            WindZone[] windZones = FindObjectsOfType<WindZone>();
            for (int i = 0; i < windZones.Length; i++)
            {
                if (windZones[i].gameObject.activeSelf && windZones[i].mode == WindZoneMode.Directional)
                {
                    valueWindMain = windZones[i].windMain;
                    valueWindTurbulence = windZones[i].windTurbulence;
                    valueWindDirection = new Vector4(
                        windZones[i].transform.forward.x,
                        windZones[i].transform.forward.y,
                        windZones[i].transform.forward.z,
                        1f);

                    float windScale = isST8 ? SpeedTree8WindScale : DefaultWindScale;
                    valueLeafSwayFactor = windScale * windZones[i].windMain;
                    valueLeafTurbulenceFactor = windScale * windZones[i].windTurbulence;
                    break;
                }
            }
        }

        #endregion

        #region Material Setup

        /// <summary>
        /// Opsæt vinden på træprototypematerialer fundet på dette terræn
        /// og tilføj deres materialer til et array for at opdatere vinden på hver frame.
        /// </summary>
        private void SetupWind()
        {
            // Henter alle BroccoTreeController materialerne.
            GameObject treePrefab;
            BroccoTreeController_FJG_1_10_3[] brocco2TreeControllers;

            // Træprototyper.
            for (int i = 0; i < terrain.terrainData.treePrototypes.Length; i++)
            {
                treePrefab = terrain.terrainData.treePrototypes[i].prefab;
                if (treePrefab != null)
                {
                    brocco2TreeControllers = treePrefab.GetComponentsInChildren<BroccoTreeController_FJG_1_10_3>();
                    foreach (BroccoTreeController_FJG_1_10_3 treeController in brocco2TreeControllers)
                    {
                        // Setup instanser af træcontroller i henhold til controlleren.
                        SetupBrocco2TreeController(treeController);
                    }
                }
            }

            // Detaljeprototyper.
            for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
            {
                treePrefab = terrain.terrainData.detailPrototypes[i].prototype;
                if (treePrefab != null)
                {
                    brocco2TreeControllers = treePrefab.GetComponentsInChildren<BroccoTreeController_FJG_1_10_3>();
                    foreach (BroccoTreeController_FJG_1_10_3 treeController in brocco2TreeControllers)
                    {
                        // Setup instanser af træcontroller i henhold til controlleren.
                        SetupBrocco2TreeController(treeController, BroccoTreeController_FJG_1_10_3.WindQuality.Better);
                    }
                }
            }

            broccoMaterials = _mats.ToArray();
            broccoMaterialParams = _matParams.ToArray();
            _mats.Clear();
            _matParams.Clear();
        }

        /// <summary>
        /// Opsæt materialer i instanser med BroccoTreeController.
        /// </summary>
        private void SetupBrocco2TreeController(
            BroccoTreeController_FJG_1_10_3 treeController,
            BroccoTreeController_FJG_1_10_3.WindQuality windQuality = BroccoTreeController_FJG_1_10_3.WindQuality.Best)
        {
            Renderer renderer = treeController.gameObject.GetComponent<Renderer>();
            Material material;

            if (renderer != null &&
                treeController.localShaderType == BroccoTreeController_FJG_1_10_3.ShaderType.SpeedTree8OrCompatible)
            {
                GetWindZoneValues();

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    material = renderer.sharedMaterials[i];
                    bool isWindEnabled = windQuality != BroccoTreeController_FJG_1_10_3.WindQuality.None;

                    if (treeController.localShaderType == BroccoTreeController_FJG_1_10_3.ShaderType.SpeedTree8OrCompatible)
                    {
                        // Slå alle windquality-keywords fra.
                        DisableAllWindKeywords(material);

                        // Aktiver kun det keyword der matcher den valgte kvalitet.
                        if (isWindEnabled &&
                            WindQualityKeywords.TryGetValue(windQuality, out string keyword))
                        {
                            material.EnableKeyword(keyword);
                        }
                    }
                    else if (isWindEnabled)
                    {
                        // Fallback til enkelt "ENABLE_WIND"-keyword.
                        if (windQuality != BroccoTreeController_FJG_1_10_3.WindQuality.None)
                        {
                            material.EnableKeyword(KW_ENABLE_WIND);
                        }
                        else
                        {
                            material.DisableKeyword(KW_ENABLE_WIND);
                        }
                    }

                    // Sæt vindaktiverings-flaget og vindkvaliteten.
                    material.SetFloat(propWindEnabled, (isWindEnabled ? 1f : 0f));
                    material.SetFloat(propWindQuality, (float)windQuality);

                    // Sæt de indledende ST-vektorstandarder ved hjælp af konstanter.
                    value2STWind.STWindBranch = new Vector4(
                        WindConstants.DefaultVecZero,
                        WindConstants.DefaultVecZero,
                        WindConstants.DefaultVecZero,
                        WindConstants.DefaultVecZero);
                    material.SetVector(propSTWindBranchWhip, value2STWind.STWindBranch);

                    value2STWind.STWindLeaf1Tumble = new Vector4(
                        WindConstants.STW_Leaf1Tumble_X,
                        WindConstants.STW_Leaf1Tumble_Y,
                        WindConstants.DefaultVecZero,
                        WindConstants.DefaultVecZero);
                    material.SetVector(propSTWindBranchAdherences, value2STWind.STWindLeaf1Tumble);

                    value2STWind.STWindTurbulences = new Vector4(
                        WindConstants.STW_Turbulences_X,
                        WindConstants.STW_Turbulences_Y,
                        WindConstants.DefaultVecZero,
                        WindConstants.DefaultVecZero);
                    material.SetVector(propSTWindTurbulences, value2STWind.STWindTurbulences);

                    value2STWind.STWindFrondRipple = new Vector4(
                        Time.time * WindConstants.FrondRippleTimeScale,
                        WindConstants.FrondRippleSecond,
                        WindConstants.FrondRippleThird,
                        WindConstants.FrondRippleFourth);
                    material.SetVector(propSTWindFrondRipple, value2STWind.STWindFrondRipple);

                    // Registrer materiale til opdateringer pr. frame og anvend indledende vindværdier.
                    if (!_mats.Contains(material) && isWindEnabled)
                    {
                        _mats.Add(material);
                        Vector3 matParams = new Vector3(treeController.trunkBending, 0f, 0f);
                        _matParams.Add(matParams);
                        ApplyBroccoTreeWind(material, matParams); // sætter initiale værdier.
                    }
                }
            }
        }

        #endregion

        #region Wind Application

        /// <summary>
        /// Beregner og opdaterer alle SpeedTree8-vindværdier for et materiale
        /// ud fra vindstyrke, turbulens, vindretning og træets trunkBending.
        /// </summary>
        private void ApplyBroccoTreeWind(Material mat, Vector3 matParams)
        {
            // STWindGlobal (tid, global amplitude, trunk bending, ekstra faktor).
            value2STWind.STWindGlobal = new Vector4(
                0f,
                baseWindAmplitude * valueWindMain,
                matParams.x * WindConstants.TrunkBendingScale + WindConstants.TrunkBendingOffset,
                windGlobalW * WindConstants.WindGlobalWScale);

            // STWindBranch.
            value2STWind.STWindBranch = new Vector4(
                0f,
                valueWindMain * WindConstants.BranchWindMainScale,
                0f,
                0f);

            // Vindretning.
            value2STWind.STWindVector = valueWindDirection;

            value2STWind.STWindBranchAnchor = new Vector4(
                valueWindDirection.x,
                valueWindDirection.y,
                valueWindDirection.z,
                valueWindMain * WindConstants.BranchAnchorScale);

            // Branch twitch.
            float branchTwitchAmount = WindConstants.BranchTwitchBase -
                Mathf.Lerp(0f, WindConstants.BranchTwitchMaxReduction,
                    valueWindTurbulence / WindConstants.BranchTwitchTurbulenceNorm);

            value2STWind.STWindBranchTwitch = new Vector4(
                branchTwitchAmount * branchTwitchAmount,
                1f,
                0f,
                0f);

            // Leaf 1.
            value2STWind.STWindLeaf1Tumble = new Vector4(
                0f,
                valueWindTurbulence * WindConstants.LeafTumbleTurbulenceScale,
                valueWindMain * Mathf.Lerp(
                    WindConstants.LeafTumbleMainMax,
                    WindConstants.LeafTumbleMainMin,
                    valueWindMain / WindConstants.LeafTumbleMainNorm),
                valueWindMain * WindConstants.LeafTumbleMainScale);

            value2STWind.STWindLeaf1Twitch = new Vector4(
                valueWindMain * WindConstants.LeafTwitchMainScale,
                valueWindTurbulence * WindConstants.LeafTwitchTurbulenceScale,
                0f,
                0f);

            value2STWind.STWindLeaf1Ripple = new Vector4(
                0f,
                valueWindTurbulence * WindConstants.LeafRippleTurbulenceScale,
                0f,
                0f);

            // Leaf 2.
            value2STWind.STWindLeaf2Tumble = new Vector4(
                0f,
                valueWindTurbulence * WindConstants.LeafTumbleTurbulenceScale,
                valueWindMain * Mathf.Lerp(
                    WindConstants.LeafTumbleMainMax,
                    WindConstants.LeafTumbleMainMin,
                    valueWindMain / WindConstants.LeafTumbleMainNorm),
                valueWindMain * WindConstants.LeafTumbleMainScale);

            value2STWind.STWindLeaf2Twitch = new Vector4(
                valueWindMain * WindConstants.LeafTwitchMainScale,
                valueWindTurbulence * WindConstants.LeafTwitchTurbulenceScale,
                0f,
                0f);

            value2STWind.STWindLeaf2Ripple = new Vector4(
                0f,
                valueWindTurbulence * WindConstants.LeafRippleTurbulenceScale,
                0f,
                0f);

            // Sæt alle egenskaber ved hjælp af helper.
            SetAllWindShaderProperties(mat, value2STWind);
        }

        /// <summary>
        /// Opdaterer vindværdierne for et materiale over tid.
        /// </summary>
        /// <param name="material">Materialet der skal opdateres.</param>
        /// <param name="windParams">Vindparametre, x: sproutTurbulance, y: sproutSway, z: localWindAmplitude.</param>
        private void UpdateBroccoTreeWind(Material material, Vector3 windParams)
        {
            // Opdaterer tidsafhængige vindværdier.
            value2STWind.STWindGlobal.x = valueTime * WindConstants.WindGlobalTimeScale;
            value2STWind.STWindGlobal.z = windParams.x * WindConstants.LocalWindAmplitudeScale +
                                          WindConstants.LocalWindAmplitudeOffset;

            value2STWind.STWindBranch.x = valueTimeWindMain;

            value2STWind.STWindLeaf1Tumble.x = valueTimeWindMain;
            value2STWind.STWindLeaf1Twitch.z = valueTime * WindConstants.LeafTwitchTimeScale;
            value2STWind.STWindLeaf1Ripple.x = valueTime;

            value2STWind.STWindLeaf2Tumble.x = valueTimeWindMain;
            value2STWind.STWindLeaf2Twitch.z = valueTime * WindConstants.LeafTwitchTimeScale;
            value2STWind.STWindLeaf2Ripple.x = valueTime;

            // Sæt alle egenskaber ved hjælp af helper.
            SetAllWindShaderProperties(material, value2STWind);
        }

        #endregion

        #region Shader Helpers

        /// <summary>
        /// Sætter en enkelt vind-relateret shader-egenskab (Vector4) på et materiale.
        /// </summary>
        private void SetWindShaderProperty(Material mat, int propertyId, Vector4 value)
        {
            mat.SetVector(propertyId, value);
        }

        /// <summary>
        /// Sætter alle relevante vindshader-egenskaber på et materiale ud fra de beregnede værdier.
        /// </summary>
        private void SetAllWindShaderProperties(Material mat, WindShaderValues windValues)
        {
            SetWindShaderProperty(mat, propSTWindGlobal, windValues.STWindGlobal);
            SetWindShaderProperty(mat, propSTWindBranch, windValues.STWindBranch);
            SetWindShaderProperty(mat, propSTWindBranchTwitch, windValues.STWindBranchTwitch);
            SetWindShaderProperty(mat, propSTWindBranchWhip, windValues.STWindBranchWhip);
            SetWindShaderProperty(mat, propSTWindBranchAnchor, windValues.STWindBranchAnchor);
            SetWindShaderProperty(mat, propSTWindBranchAdherences, windValues.STWindBranchAdherences);
            SetWindShaderProperty(mat, propSTWindTurbulences, windValues.STWindTurbulences);
            SetWindShaderProperty(mat, propSTWindLeaf1Ripple, windValues.STWindLeaf1Ripple);
            SetWindShaderProperty(mat, propSTWindLeaf1Tumble, windValues.STWindLeaf1Tumble);
            SetWindShaderProperty(mat, propSTWindLeaf1Twitch, windValues.STWindLeaf1Twitch);
            SetWindShaderProperty(mat, propSTWindLeaf2Ripple, windValues.STWindLeaf2Ripple);
            SetWindShaderProperty(mat, propSTWindLeaf2Tumble, windValues.STWindLeaf2Tumble);
            SetWindShaderProperty(mat, propSTWindLeaf2Twitch, windValues.STWindLeaf2Twitch);
            SetWindShaderProperty(mat, propSTWindFrondRipple, windValues.STWindFrondRipple);
            SetWindShaderProperty(mat, propSTWindVector, windValues.STWindVector);
        }

        /// <summary>
        /// Slår alle windquality-keywords fra på et materiale.
        /// </summary>
        private void DisableAllWindKeywords(Material mat)
        {
            foreach (string keyword in AllWindQualityKeywords)
            {
                mat.DisableKeyword(keyword);
            }
        }

        #endregion
    }
}
