using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Broccoli.Controller
{
    /// <resume>
    /// Styrer en Broccoli træ-instance.
    /// </resume>
    [ExecuteInEditMode]
    public class BroccoTreeController_FJG_1_10_3 : MonoBehaviour
    {
        #region WindParams Class
        [System.Serializable]
        /// <resume>
        /// Indeholder vind-beregningerne for en Broccoli træ-instance.
        /// </resume>
        public struct WindParams
        {
            public WindQuality windQuality;
            public WindSource windSource;
            public float windMain;
            public float windTurbulence;
            public Vector3 windDirection;
            public float customWindMain;
            public float customWindTurbulence;
            public Vector3 customWindDirection;
            public float windTimeScale;
            public float valueTime;
            public float valueTimeWindMain;
            public Vector4 valueSTWindVector;
            public Vector4 valueSTWindGlobal;
            public Vector4 valueSTWindBranch;
            public Vector4 valueSTWindBranchTwitch;
            public Vector4 valueSTWindBranchWhip;
            public Vector4 valueSTWindBranchAnchor;
            public Vector4 valueSTWindBranchAdherences;
            public Vector4 valueSTWindTurbulences;
            public Vector4 valueSTWindLeaf1Ripple;
            public Vector4 valueSTWindLeaf1Tumble;
            public Vector4 valueSTWindLeaf1Twitch;
            public Vector4 valueSTWindLeaf2Ripple;
            public Vector4 valueSTWindLeaf2Tumble;
            public Vector4 valueSTWindLeaf2Twitch;
            public Vector4 valueSTWindFrondRipple;
            public float branchWindMainFactor;
            public float sproutWindMainFactor;
            public WindParams(float windMain, float windTurbulence, Vector3 windDirection)
            {
                this.windQuality = WindQuality.Best;
                this.windSource = WindSource.WindZone;
                this.windMain = windMain;
                this.windTurbulence = windTurbulence;
                this.windDirection = windDirection;
                this.customWindMain = windMain;
                this.customWindTurbulence = windTurbulence;
                this.customWindDirection = windDirection;
                this.windTimeScale = 1f;
                this.valueTime = 0f;
                this.valueTimeWindMain = 0f;
                this.valueSTWindVector = Vector4.zero;
                this.valueSTWindGlobal = Vector4.zero;
                this.valueSTWindBranch = Vector4.zero;
                this.valueSTWindBranchTwitch = Vector4.zero;
                this.valueSTWindBranchWhip = Vector4.zero;
                this.valueSTWindBranchAnchor = Vector4.zero;
                this.valueSTWindBranchAdherences = Vector4.zero;
                this.valueSTWindTurbulences = Vector4.zero;
                this.valueSTWindLeaf1Ripple = Vector4.zero;
                this.valueSTWindLeaf1Tumble = Vector4.zero;
                this.valueSTWindLeaf1Twitch = Vector4.zero;
                this.valueSTWindLeaf2Ripple = Vector4.zero;
                this.valueSTWindLeaf2Tumble = Vector4.zero;
                this.valueSTWindLeaf2Twitch = Vector4.zero;
                this.valueSTWindFrondRipple = Vector4.zero;
                this.branchWindMainFactor = 1f;
                this.sproutWindMainFactor = 1f;
            }
        }
        #endregion

        #region Vars
        /// <resume>
        /// Version af Broccoli Tree Creator som har oprettet denne controller.
        /// </resume>
        public string version = "";
        /// <resume>
        /// Flag der angiver om rendereren er synlig (styrer om vinden opdateres).
        /// Startværdien er true for at få fat i rendereren i Start().
        /// </resume>
        bool isVisible = true;
        /// <resume>
        /// Typer af shaders der er tilgængelige.
        /// </resume>
        public enum ShaderType
        {
            Standard,
            TreeCreatorOrCompatible,
            SpeedTree7OrCompatible,
            SpeedTree8OrCompatible,
            Billboard
        }
        /// <resume>
        /// Shader-type der bruges til at behandle denne instance.
        /// </resume>
        public ShaderType localShaderType = ShaderType.SpeedTree8OrCompatible;
        /// <resume>
        /// True hvis denne instance har vinddata baseret på SpeedTree.
        /// </resume>
        private bool hasLocalSpeedTreeWind
        {
            get { return localShaderType == ShaderType.SpeedTree8OrCompatible; }
        }
        /// <resume>
        /// Global shader-type der bruges til at behandle instancer.
        /// </resume>
        public ShaderType globalShaderType = ShaderType.SpeedTree8OrCompatible;
        /// <resume>
        /// True hvis globale vinddata er baseret på SpeedTree.
        /// </resume>
        private bool hasGlobalSpeedTreeWind
        {
            get { return globalShaderType == ShaderType.SpeedTree8OrCompatible; }
        }
        /// <resume>
        /// Tilstande for hvordan vinden beregnes og sendes til shaders.
        /// Local: vinden beregnes pr. GameObject-instance pr. frame (mere performance-krævende,
        ///        men giver individuel kontrol).
        /// Global: vinden beregnes én gang pr. frame og deles mellem alle Alfalfa-instancer i scenen
        ///        (anbefalet og mere effektiv).
        /// </resume>
        public enum WindInstance
        {
            Local,
            Global,
        }
        /// <resume>
        /// Kilde til de værdier der bruges til at beregne vind.
        /// </resume>
        public enum WindSource
        {
            Self,
            WindZone
        }
        /// <resume>
        /// Vind-instancing tilstand for dette GameObject.
        /// </resume>
        [SerializeField, HideInInspector]
        private WindInstance _windInstance = WindInstance.Local;
        /// <resume>
        /// Vind-instancing tilstand for dette GameObject.
        /// </resume>
        public WindInstance windInstance
        {
            get { return _windInstance; }
            set
            {
                _windInstance = value;
                if (_windInstance == WindInstance.Local)
                {
                    DeregisterGlobalWindInstance();
                    InitLocalWind();
                }
                else
                {
                    RegisterGlobalWindInstance();
                    InitGlobalWind();
                }
            }
        }
        public enum WindType
        {
            None,
            TreeCreator,
            ST7,
            ST8
        }
        public WindType windType = WindType.None;
        public enum WindQuality
        {
            None,
            Fastest,
            Fast,
            Better,
            Best,
            Palm
        }
        /// <resume>
        /// Hvis true bliver vind for sprout type 1 beregnet.
        /// </resume>
        public bool hasSprout1 = true;
        /// <resume>
        /// Hvis true bliver vind for sprout type 2 beregnet.
        /// </resume>
        public bool hasSprout2 = true;
        public float trunkBending = 1f;
        private float baseWindAmplitude = 0.2752f;
        private float windGlobalW = 1.728f;
        #endregion

        #region Local Wind Vars
        public WindParams localWindParams = new WindParams(0f, 0f, Vector3.zero);
        public void SetLocalCustomWind(float windMain, float windTurbulence, Vector3 windDirection)
        {
            localWindParams.customWindMain = windMain;
            localWindParams.customWindTurbulence = windTurbulence;
            localWindParams.customWindDirection = windDirection;
            SetupWindInternal(ref localWindParams);
        }
        public WindQuality localWindQuality
        {
            get { return localWindParams.windQuality; }
            set
            {
                localWindParams.windQuality = value;
                SetupWindInternal(ref localWindParams);
            }
        }
        public Vector3 localWindDirection
        {
            get { return localWindParams.customWindDirection; }
            set
            {
                localWindParams.customWindDirection = value;
                SetupWindInternal(ref localWindParams);
            }
        }
        public float localWindMain
        {
            get { return localWindParams.customWindMain; }
            set
            {
                localWindParams.customWindMain = value;
                SetupWindInternal(ref localWindParams);
            }
        }
        public float localWindTurbulence
        {
            get { return localWindParams.customWindTurbulence; }
            set
            {
                localWindParams.customWindTurbulence = value;
                SetupWindInternal(ref localWindParams);
            }
        }
        public WindSource localWindSource
        {
            get { return localWindParams.windSource; }
            set
            {
                localWindParams.windSource = value;
                SetupWindInternal(ref localWindParams);
            }
        }
        public bool customPreviewMode = false;
        /// <resume>
        /// Hvis true bliver de beregnede vind-properties skrevet direkte til GameObjectets renderer.
        /// Hvis false sættes værdierne kun på MaterialPropertyBlock.
        /// </resume>
        public bool windAppliesToRenderer = true;
        /// <resume>
        /// True når vind skal forhåndsvises i editoren.
        /// </resume>
        [SerializeField]
        private bool _editorWindEnabled = false;
        public bool editorWindEnabled
        {
            get { return _editorWindEnabled; }
            set
            {
                _editorWindEnabled = value;
#if UNITY_EDITOR
                EditorApplication.update -= EditorUpdate;
                if (_editorWindEnabled)
                {
                    EditorApplication.update += EditorUpdate;
                }
                SetupWindInternal(ref localWindParams, _editorWindEnabled);
#endif
            }
        }
        /// <resume>
        /// Renderer-komponenten for denne instance.
        /// </resume>
        private Renderer _localRenderer = null;
        /// <resume>
        /// MaterialPropertyBlock der bruges til at sætte shader-værdier.
        /// </resume>
        private MaterialPropertyBlock _propBlock = null;
        private static bool _isPropsInit = false;
        private int _localFrameCount = -1;
        private bool _restartWind = false;
        #endregion

        #region Global Wind
        public struct WindSettings
        {
            public bool enabled;
            public float trunkBending;
            public WindSettings(float trunkBending)
            {
                this.enabled = true;
                this.trunkBending = trunkBending;
            }
        }
        private static Dictionary<int, Renderer> _globalRenderers = new Dictionary<int, Renderer>();
        private static Dictionary<int, WindSettings> _globalWindSettings = new Dictionary<int, WindSettings>();
        public static WindParams globalWindParams = new WindParams(0f, 0f, Vector3.zero);
        public void SetGlobalCustomWind(float windMain, float windTurbulence, Vector3 windDirection)
        {
            globalWindParams.customWindMain = windMain;
            globalWindParams.customWindTurbulence = windTurbulence;
            globalWindParams.customWindDirection = windDirection;
            SetupWindInternal(ref globalWindParams);
        }
        public WindQuality globalWindQuality
        {
            get { return globalWindParams.windQuality; }
            set
            {
                globalWindParams.windQuality = value;
                SetupWindInternal(ref globalWindParams);
            }
        }
        public Vector3 globalWindDirection
        {
            get { return globalWindParams.customWindDirection; }
            set
            {
                globalWindParams.customWindDirection = value;
                SetupWindInternal(ref globalWindParams);
            }
        }
        public float globalWindMain
        {
            get { return globalWindParams.customWindMain; }
            set
            {
                globalWindParams.customWindMain = value;
                SetupWindInternal(ref globalWindParams);
            }
        }
        public float globalWindTurbulence
        {
            get { return globalWindParams.customWindTurbulence; }
            set
            {
                globalWindParams.customWindTurbulence = value;
                SetupWindInternal(ref globalWindParams);
            }
        }
        public WindSource globalWindSource
        {
            get { return globalWindParams.windSource; }
            set
            {
                globalWindParams.windSource = value;
                SetupWindInternal(ref globalWindParams);
            }
        }
        private static int _globalFrameCount = -1;
        #endregion

        #region Shader Property Ids
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
        #endregion

        #region Static Constructor
        static BroccoTreeController_FJG_1_10_3()
        {
            InitializeShaderPropIds();
        }
        #endregion

        #region Events
        public void Awake()
        {
            InitializeShaderPropIds();
            _localRenderer = GetComponent<Renderer>();
        }
        /// <resume>
        /// Startkaldes når instancen bliver aktiveret første gang.
        /// </resume>
        public void Start()
        {
            _restartWind = true;
            _localFrameCount = -1;
            _globalFrameCount = -1;
        }
        /// <resume>
        /// Kører når komponenten bliver slået til (OnEnable).
        /// </resume>
        void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= StateChange;
            EditorApplication.update -= EditorUpdate;
            if (_editorWindEnabled)
            {
                EditorApplication.playModeStateChanged += StateChange;
                EditorApplication.update += EditorUpdate;
            }
#endif
            // Init LOCAL Wind.
            if (_windInstance == WindInstance.Local)
            {
                InitLocalWind();
                DeregisterGlobalWindInstance();
            }
            // Init GLOBAL Wind.
            else
            {
                RegisterGlobalWindInstance();
                InitGlobalWind();
            }
        }
        /// <resume>
        /// Kører når komponenten bliver slået fra (OnDisable).
        /// </resume>
        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= StateChange;
            EditorApplication.update -= EditorUpdate;
#endif
            DeregisterGlobalWindInstance();
        }
        void OnBecameVisible()
        {
            isVisible = true;
        }
        void OnBecameInvisible()
        {
            isVisible = false;
        }
        /// <resume>
        /// Opdaterer vind-beregningerne hver frame.
        /// </resume>
        void Update()
        {
            if (isVisible && hasLocalSpeedTreeWind)
            {
                bool shouldUpdate = true;
#if UNITY_EDITOR
                shouldUpdate = _editorWindEnabled ||
                    (EditorApplication.isPlaying && ((_windInstance == WindInstance.Local && _localFrameCount != Time.frameCount) ||
                    (_windInstance == WindInstance.Global && _globalFrameCount != Time.frameCount)));
#else
				if (windInstance == WindInstance.Local) {
					shouldUpdate = _localFrameCount != Time.frameCount;
				} else {
					shouldUpdate = _globalFrameCount != Time.frameCount;
				}
#endif
                if (_restartWind)
                {
                    if (_windInstance == WindInstance.Local)
                    {
                        SetupWindInternal(ref localWindParams);
                    }
                    else
                    {
                        SetupWindInternal(ref globalWindParams);
                    }
                    _restartWind = false;
                }

                if (shouldUpdate && _windInstance == WindInstance.Local)
                {
                    UpdateWindTime(ref localWindParams);
                    if (_propBlock == null) SetMaterialPropertyBlock();
                    ApplyWind(ref localWindParams);
                    _localFrameCount = Time.frameCount;
                }
                else if (shouldUpdate && _windInstance == WindInstance.Global)
                {
                    if (_propBlock == null) SetMaterialPropertyBlock();
                    UpdateWindTime(ref globalWindParams);
                    ApplyWind(ref globalWindParams);
                    _globalFrameCount = Time.frameCount;
                }
            }
        }
        void EditorUpdate()
        {
#if UNITY_EDITOR
            Update();
#endif
        }
#if UNITY_EDITOR
        void StateChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                editorWindEnabled = windType != WindType.None;
                if (customPreviewMode && localWindSource == WindSource.Self) localWindSource = WindSource.WindZone;
            }
            else
            {
                editorWindEnabled = false;
            }
        }
#endif
        #endregion

        #region Wind
        public void UpdateWind(float windMain, float windTurbulence, Vector3 windDirection)
        {
            if (_windInstance == WindInstance.Local)
            {
                localWindParams.windMain = windMain;
                localWindParams.windTurbulence = windTurbulence;
                localWindParams.windDirection = windDirection;
                windAppliesToRenderer = false;
                UpdateWindValues(ref localWindParams);
                UpdateWindTime(ref localWindParams);
                windAppliesToRenderer = true;
                ApplyWind(ref localWindParams);
            }
            else
            {
                globalWindParams.customWindMain = windMain;
                globalWindParams.windMain = windMain;
                globalWindParams.customWindTurbulence = windTurbulence;
                globalWindParams.windTurbulence = windTurbulence;
                globalWindParams.customWindDirection = windDirection;
                globalWindParams.windDirection = windDirection;
                //windAppliesToRenderer = false;
                UpdateWindValues(ref globalWindParams);
                UpdateWindTime(ref globalWindParams);
                //windAppliesToRenderer = true;
                ApplyWind(ref globalWindParams);
            }
        }
        private void InitLocalWind()
        {
            SetMaterialPropertyBlock();
            SetupWindInternal(ref localWindParams);
        }
        private void InitGlobalWind()
        {
            SetMaterialPropertyBlock();
            SetupWindInternal(ref globalWindParams);
        }
        void SetMaterialPropertyBlock()
        {
            _propBlock = new MaterialPropertyBlock();
            if (windAppliesToRenderer && _localRenderer != null && localShaderType != ShaderType.Standard)
            {
                _localRenderer.GetPropertyBlock(_propBlock);
            }
        }
        private void SetupWindInternal(ref WindParams windParams, bool enabled = true)
        {
            bool isEnabled;
            isEnabled = windParams.windQuality != WindQuality.None && enabled;

            UpdateWindValues(ref windParams, windParams.windSource, windParams.customWindDirection, windParams.customWindMain, windParams.customWindTurbulence);

            SetWindQuality(ref windParams, isEnabled);

            if (_localRenderer == null)
            {
                _localRenderer = GetComponent<Renderer>();
                if (_localRenderer == null) return;
            }
            if (windAppliesToRenderer)
                _localRenderer.GetPropertyBlock(_propBlock);

            if (!isEnabled)
            {
                _propBlock.SetFloat(propWindEnabled, 0f);
                if (windAppliesToRenderer)
                    ApplyToRenderer();
                return;
            }

            // WindEnabled
            _propBlock.SetFloat(propWindEnabled, (isEnabled ? 1f : 0f));
            // WindQuality
            _propBlock.SetFloat(propWindQuality, (float)windParams.windQuality);

            UpdateWindValues(ref windParams);
        }
        private void UpdateWindValues(ref WindParams windParams)
        {
            // STWindGlobal (time / 2, 0.3, 0.1, 1.7)
            windParams.valueSTWindGlobal = new Vector4(0f,
                baseWindAmplitude * windParams.windMain,
                trunkBending * 0.1f + 0.001f,
                windGlobalW * 1.125f);

            // STWindBranch (time / 1.5, 0.4f, time * 1.5, 0f)
            windParams.valueSTWindBranch = new Vector4(0f, windParams.windMain * 0.35f, 0f, 0f);

            // WIND DIRECTION.
            // STWindVector
            windParams.valueSTWindVector = windParams.windDirection;
            _propBlock.SetVector(propSTWindVector, windParams.valueSTWindVector);
            // STWindBranchAnchor
            windParams.valueSTWindBranchAnchor = new Vector4(
                windParams.windDirection.x,
                windParams.windDirection.y,
                windParams.windDirection.z,
                windParams.windMain * 2f);
            _propBlock.SetVector(propSTWindBranchAnchor, windParams.valueSTWindBranchAnchor);

            // STWindBranchTwitch (AMOUNT, SCALE, 0, 0)
            float branchTwitchAmount = 0.65f - Mathf.Lerp(0f, 0.35f, windParams.windTurbulence / 3f);
            windParams.valueSTWindBranchTwitch = new Vector4(branchTwitchAmount * branchTwitchAmount, 1, 0f, 0f);
            _propBlock.SetVector(propSTWindBranchTwitch, windParams.valueSTWindBranchTwitch);

            // STWindBranchWhip
            windParams.valueSTWindBranchWhip = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            _propBlock.SetVector(propSTWindBranchWhip, windParams.valueSTWindBranchWhip);
            // STWindBranchAdherences
            windParams.valueSTWindBranchAdherences = new Vector4(0.15f, 0.15f, 0f, 0f);
            _propBlock.SetVector(propSTWindBranchAdherences, windParams.valueSTWindBranchAdherences);
            // STWindTurbulences
            windParams.valueSTWindTurbulences = new Vector4(0.7f, 0.3f, 0f, 0f);
            _propBlock.SetVector(propSTWindTurbulences, windParams.valueSTWindTurbulences);
            // STWindFrondRipple
            windParams.valueSTWindFrondRipple = new Vector4(Time.time * 1f, 0.01f, 2f, 10f);
            _propBlock.SetVector(propSTWindFrondRipple, windParams.valueSTWindFrondRipple);

            // CALM BREEZE 	Main: 0.5f, Turbulence: 0.5f, Vector3.right);
            // BREEZE 		Main: 1.2f, Turbulence: 0.75f
            // WINDY 		Main: 2f, Turbulence: 1.4f
            // STRONG WIND	Main: 3f, Turbulence 2f
            // STORMY		Main: 4f, Turbulence: 3f
            //windParams.sproutWindMainFactor = Mathf.Lerp (1f, 4f, windParams.windMain / 4f);

            windParams.sproutWindMainFactor = EaseInCubic(1f, 5f, windParams.windMain / 4f);
            windParams.branchWindMainFactor = EaseInCubic(0.5f, 1.25f, windParams.windMain / 4f);
            if (hasSprout1)
            {
                // STWindLeaf1Tumble (TIME, FLIP, TWIST, ADHERENCE)
                windParams.valueSTWindLeaf1Tumble = new Vector4(
                    windParams.valueSTWindLeaf1Tumble.x,
                    windParams.windTurbulence * 0.12f,
                    windParams.sproutWindMainFactor * 0.1f,
                    windParams.sproutWindMainFactor * 0.13f);
                // STWindLeaf1Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                windParams.valueSTWindLeaf1Twitch = new Vector4(
                    windParams.windMain * 0.185f,
                    windParams.windTurbulence * 0.165f * windParams.sproutWindMainFactor,
                    windParams.valueSTWindLeaf1Twitch.z,
                    0f);
                // STWindLeaf1Ripple (TIME, AMOUNT, 0, 0)
                windParams.valueSTWindLeaf1Ripple = new Vector4(
                    windParams.valueSTWindLeaf1Ripple.x,
                    windParams.windTurbulence * 0.01f,
                    0f,
                    0f);
            }

            if (hasSprout2)
            {
                // STWindLeaf2Tumble (TIME, FLIP, TWIST, ADHERENCE)
                windParams.valueSTWindLeaf2Tumble = new Vector4(
                    windParams.valueSTWindLeaf2Tumble.x,
                    windParams.windTurbulence * 0.12f,
                    windParams.sproutWindMainFactor * 0.1f,
                    windParams.sproutWindMainFactor * 0.13f);
                // STWindLeaf2Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                windParams.valueSTWindLeaf1Twitch = new Vector4(
                    windParams.windMain * 0.185f,
                    windParams.windTurbulence * 0.165f * windParams.sproutWindMainFactor,
                    windParams.valueSTWindLeaf2Twitch.z,
                    0f);
                // STWindLeaf2Ripple (TIME, AMOUNT, 0, 0)
                windParams.valueSTWindLeaf2Ripple = new Vector4(
                    windParams.valueSTWindLeaf2Ripple.x,
                    windParams.windTurbulence * 0.01f,
                    0f,
                    0f);
            }

            if (windAppliesToRenderer)
                ApplyToRenderer();
        }
        private void UpdateWindTime(ref WindParams windParams)
        {
#if UNITY_EDITOR
            windParams.valueTime = (EditorApplication.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
#else
			windParams.valueTime = Time.time;
#endif
            windParams.valueTime *= windParams.windTimeScale;

            windParams.valueTimeWindMain = windParams.valueTime * 0.66f;

            //_localRenderer.GetPropertyBlock (_propBlock);
            // STWindGlobal
            windParams.valueSTWindGlobal.x = windParams.valueTime * 0.5f;

            // STWindBranch (TIME, DISTANCE, 0, 0)
            windParams.valueSTWindBranch.x = windParams.valueTimeWindMain * windParams.branchWindMainFactor;

            if (hasSprout1)
            {
                // STWindLeaf1Tumble (TIME, FLIP, TWIST, ADHERENCE)
                windParams.valueSTWindLeaf1Tumble.x = windParams.valueTimeWindMain * windParams.sproutWindMainFactor;
                // STWindLeaf1Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                windParams.valueSTWindLeaf1Twitch.z = windParams.valueTime * 0.5f;
                // STWindLeaf1Ripple (TIME, AMOUNT, 0, 0)
                windParams.valueSTWindLeaf1Ripple.x = windParams.valueTime;
            }

            if (hasSprout2)
            {
                // STWindLeaf2Tumble (TIME, FLIP, TWIST, ADHERENCE)
                windParams.valueSTWindLeaf2Tumble.x = windParams.valueTimeWindMain * windParams.sproutWindMainFactor;
                // STWindLeaf2Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                windParams.valueSTWindLeaf2Twitch.z = windParams.valueTime * 0.5f;
                // STWindLeaf2Ripple (TIME, AMOUNT, 0, 0)
                windParams.valueSTWindLeaf2Ripple.x = windParams.valueTime;
            }
        }
        public void ApplyWind(ref WindParams windParams)
        {
            /*
			if (windAppliesToRenderer && _localRenderer != null)
				_localRenderer.GetPropertyBlock (_propBlock);
				*/

            // STWindGlobal
            _propBlock.SetVector(propSTWindGlobal, windParams.valueSTWindGlobal);
            // STWindBranch
            _propBlock.SetVector(propSTWindBranch, windParams.valueSTWindBranch);

            if (hasSprout1)
            {
                // STWindLeaf1Tumble (TIME, FLIP, TWIST, ADHERENCE)
                _propBlock.SetVector(propSTWindLeaf1Tumble, windParams.valueSTWindLeaf1Tumble);
                // STWindLeaf1Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                _propBlock.SetVector(propSTWindLeaf1Twitch, windParams.valueSTWindLeaf1Twitch);
                // STWindLeaf1Ripple (TIME, AMOUNT, 0, 0)
                _propBlock.SetVector(propSTWindLeaf1Ripple, windParams.valueSTWindLeaf1Ripple);
            }

            if (hasSprout2)
            {
                // STWindLeaf2Tumble (TIME, FLIP, TWIST, ADHERENCE)
                _propBlock.SetVector(propSTWindLeaf2Tumble, windParams.valueSTWindLeaf2Tumble);
                // STWindLeaf2Twitch (AMOUNT, SHARPNESS, TIME, 0.0)
                _propBlock.SetVector(propSTWindLeaf2Twitch, windParams.valueSTWindLeaf2Twitch);
                // STWindLeaf2Ripple (TIME, AMOUNT, 0, 0)
                _propBlock.SetVector(propSTWindLeaf2Ripple, windParams.valueSTWindLeaf1Ripple);
            }

            if (windAppliesToRenderer && _localRenderer != null)
                ApplyToRenderer();
        }
        private static void UpdateWindValues(ref WindParams windParams, WindSource windSource, Vector3 windDirection, float windMain, float windTurbulence)
        {
            windParams.windDirection = new Vector4(1f, 0f, 0f, 0f);
            if (windSource == WindSource.WindZone)
            {
                WindZone[] windZones = FindObjectsOfType<WindZone>();
                for (int i = 0; i < windZones.Length; i++)
                {
                    if (windZones[i].gameObject.activeSelf && windZones[i].mode == WindZoneMode.Directional)
                    {
                        windParams.windMain = windZones[i].windMain;
                        windParams.windDirection = new Vector4(windZones[i].transform.forward.x, windZones[i].transform.forward.y, windZones[i].transform.forward.z, 1f);
                        windParams.windTurbulence = windZones[i].windTurbulence;
                        break;
                    }
                }
            }
            else
            {
                windParams.windMain = windMain;
                windParams.windTurbulence = windTurbulence;
                windParams.windDirection = new Vector4(windDirection.x, windDirection.y, windDirection.z, 1f);
            }

        }
        void SetWindQuality(ref WindParams windParams, bool enable = true)
        {
            if (_windInstance == WindInstance.Local)
            {
                SetWindQualityPerRenderer(ref windParams, _localRenderer, enable);
            }
            else
            {
                var rendEnum = _globalRenderers.GetEnumerator();
                Renderer renderer;
                while (rendEnum.MoveNext())
                {
                    renderer = rendEnum.Current.Value;
                    SetWindQualityPerRenderer(ref windParams, renderer, enable);
                }
            }
            if (windAppliesToRenderer && _localRenderer != null)
                _localRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(propWindEnabled, (enable ? 1f : 0f));
            _propBlock.SetFloat(propWindQuality, (float)windParams.windQuality);
            if (windAppliesToRenderer && _localRenderer != null)
                ApplyToRenderer();
        }
        void SetWindQualityPerRenderer(ref WindParams windParams, Renderer renderer, bool enable = true)
        {
            if (renderer != null)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.DisableKeyword("_WINDQUALITY_NONE");
                        material.DisableKeyword("_WINDQUALITY_FASTEST");
                        material.DisableKeyword("_WINDQUALITY_FAST");
                        material.DisableKeyword("_WINDQUALITY_BETTER");
                        material.DisableKeyword("_WINDQUALITY_BEST");
                        material.DisableKeyword("_WINDQUALITY_PALM");
                        if (enable)
                        {
                            switch (windParams.windQuality)
                            {
                                case WindQuality.None:
                                    material.EnableKeyword("_WINDQUALITY_NONE");
                                    break;
                                case WindQuality.Fastest:
                                    material.EnableKeyword("_WINDQUALITY_FASTEST");
                                    break;
                                case WindQuality.Fast:
                                    material.EnableKeyword("_WINDQUALITY_FAST");
                                    break;
                                case WindQuality.Better:
                                    material.EnableKeyword("_WINDQUALITY_BETTER");
                                    break;
                                case WindQuality.Best:
                                    material.EnableKeyword("_WINDQUALITY_BEST");
                                    break;
                                case WindQuality.Palm:
                                    material.EnableKeyword("_WINDQUALITY_PALM");
                                    break;
                            }
                        }
                    }
                }
            }
        }
        void ApplyToRenderer()
        {
            // LOCAL
            if (_windInstance == WindInstance.Local)
            {
                _localRenderer.SetPropertyBlock(_propBlock);
            }
            // GLOBAL 
            else
            {
                //_localRenderer.SetPropertyBlock (_propBlock);
                var rendEnum = _globalRenderers.GetEnumerator();
                Renderer renderer;
                WindSettings windSettings;
                while (rendEnum.MoveNext())
                {
                    renderer = rendEnum.Current.Value;
                    windSettings = _globalWindSettings[rendEnum.Current.Key];
                    if (renderer != null && windSettings.enabled && renderer.isVisible)
                    {
                        globalWindParams.valueSTWindGlobal.z = windSettings.trunkBending * 0.1f + 0.001f;
                        _propBlock.SetVector(propSTWindGlobal, globalWindParams.valueSTWindGlobal);
                        renderer.SetPropertyBlock(_propBlock);
                    }
                }
            }
        }
        /// <resume>
        /// Initialiserer shader-property ID-felterne én gang.
        /// </resume>
        private static void InitializeShaderPropIds()
        {
            if (!_isPropsInit)
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
                _isPropsInit = true;
            }
        }
        /// <resume>
        /// EaseInCubic-funktion.
        /// </resume>
        /// <returns>Det beregnede eased resultat.</returns>
        /// <param name="start">Startværdi.</param>
        /// <param name="end">Slutværdi.</param>
        /// <param name="value">Tidsværdi mellem 0 og 1.</param>
        public static float EaseInCubic(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value + start;
        }
        #endregion

        #region Global Wind
        public void RegisterGlobalWindInstance()
        {
            int rendererId;
            if (_localRenderer == null) _localRenderer = GetComponent<Renderer>();
            rendererId = _localRenderer.GetInstanceID();
            if (!_globalRenderers.ContainsKey(rendererId))
            {
                _globalRenderers.Add(rendererId, _localRenderer);
                _globalWindSettings.Add(rendererId, new WindSettings(trunkBending));
            }
        }
        public void DeregisterGlobalWindInstance()
        {
            if (_localRenderer == null) return;
            int rendererId = _localRenderer.GetInstanceID();
            if (_globalRenderers.ContainsKey(rendererId))
            {
                _globalRenderers.Remove(rendererId);
                _globalWindSettings.Remove(rendererId);
            }
        }
        #endregion

        #region Debug
        private string _windInfo;
        public string GetLocalWindValues()
        {
            _windInfo = string.Format("Wind Direction: {0}\n", localWindParams.windDirection);
            _windInfo += string.Format("Wind Main: {0}\n", localWindParams.windMain);
            _windInfo += string.Format("Wind Turbulence: {0}", localWindParams.windTurbulence);
            return _windInfo;
        }
        public string GetGlobalWindValues()
        {
            _windInfo = string.Format("Wind Direction: {0}\n", globalWindParams.windDirection);
            _windInfo += string.Format("Wind Main: {0}\n", globalWindParams.windMain);
            _windInfo += string.Format("Wind Turbulence: {0}", globalWindParams.windTurbulence);
            return _windInfo;
        }
        public string GetDebugInfo()
        {
            _windInfo = string.Format("Wind Instance: {0}\n", _windInstance);
            // LOCAL
            if (_windInstance == WindInstance.Local)
            {
                _windInfo += string.Format("Local Wind Source: {0}\n", localWindParams.windSource);
            }
            // GLOBAL
            else
            {
                _windInfo += string.Format("Global Wind Source: {0}\n", globalWindParams.windSource);
            }
            return _windInfo;
        }
        #endregion

        /*
		#ifdef ENABLE_WIND

		#define WIND_QUALITY_NONE       0
		#define WIND_QUALITY_FASTEST    1
		#define WIND_QUALITY_FAST       2
		#define WIND_QUALITY_BETTER     3
		#define WIND_QUALITY_BEST       4
		#define WIND_QUALITY_PALM       5

		uniform half _WindQuality;
		uniform half _WindEnabled;

		#include "SpeedTreeWind.cginc"

		#endif
		*/

        /*
		https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
		https://forum.unity.com/threads/description-of-packed-speedtree-wind-shader-inputs.575182/
		CBUFFER_START(SpeedTreeWind)
		float4 _ST_WindVector;
		float4 _ST_WindGlobal;  // (_Time.y, ?, ?, ?)
		float4 _ST_WindBranch; // (_Time.y, ?, ?, ?)
		float4 _ST_WindBranchTwitch;
		float4 _ST_WindBranchWhip;
		float4 _ST_WindBranchAnchor;
		float4 _ST_WindBranchAdherences;
		float4 _ST_WindTurbulences;
		float4 _ST_WindLeaf1Ripple; // (_Time.y, ?, ?, ?)
		float4 _ST_WindLeaf1Tumble; // (_Time.z, ?, ?, ?)
		float4 _ST_WindLeaf1Twitch; // (?, ?, _Time.y, ?)
		float4 _ST_WindLeaf2Ripple; // (_Time.y, ?, ?, ?)
		float4 _ST_WindLeaf2Tumble; // (_Time.z, ?, ?, ?)
		float4 _ST_WindLeaf2Twitch; // (?, ?, _Time.y, ?)
		float4 _ST_WindFrondRipple; // (_Time.y, ?, ?, ?)
		float4 _ST_WindAnimation;
		CBUFFER_END
		*/
    }
}
