using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Broccoli.Controller
{
    /// <summary>
	/// Der er mange Vector4-felter for vindværdier.
    /// der sætter jeg dem i en struct, såsom WindShaderValues.
    /// for at gøre koden lettere at administrere og reducere rod.
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

    ///<summary>
	///	Kontrollerer et træbroccoli-forekomster i terræner.
	///</summary>
    public class BroccoTerrainController_FJG_1_10_3 : MonoBehaviour {
        #region Vars
        /// <resume>
        /// Terrænkomponent.
        /// </resume>
        Terrain terrain = null;
        /// <resume>
        /// Holder styr på de væsentlige instanser, der skal opdateres.
        /// </resume>
        List<Material> _mats = new List<Material>();
        /// <resume>
        /// Holder styr på parametrene for materialeinstanser.
        /// for BroccoTreeController:
        /// x: sproutTurbulance.
        /// y: sproutSway.
        /// z: localWindAmplitude.
        /// for BroccoTreeController_FJG_1_10_3:
        /// x: trunkBending.
        /// </resume>
        List<Vector3> _matParams = new List<Vector3>();
        /// <resume>
        /// Materiale række
        /// Materialerne er taget fra BroccoTreeController_FJG_1_10_3.
        /// </resume>
        Material[] broccoMaterials;
        /// <resume>
        /// Materialeparameterrække.
        /// Parametre er taget fra BroccoTreeControllers2.
        /// </resume>
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

        #region Shader values
        float valueTime = 0f;
		float valueTimeWindMain = 0f;
		float windTimeScale = 1f;
        #endregion

        ///<summary>
        ///Her laver jeg et nyt objekt af WindShaderValues som kan indeholde alle Vector4 værdierne for SpeedTree8 vindshaderen.
        ///</summary>
        WindShaderValues value2STWind = new WindShaderValues();

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
        /// <resume>
        /// Statisk kontroller til denne klasse
        /// </resume>
        static BroccoTerrainController_FJG_1_10_3() {
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
        #region Events

        ///<summer>
        ///Starter denne instans.
        ///</summer>
        public void Start()
		{
			// henter terrainet.
			terrain = GetComponent<Terrain>();
			if (terrain != null)
			{
				requiresUpdateWindZoneValues = true;
				SetupWind();
			}
		}

        ///<summary>
		///Opdater denne instans.
		///</summary>
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
			if (_frameCount != Time.frameCount) {
				valueTime = Time.time;
				valueTime *= windTimeScale;
				valueTimeWindMain = valueTime * 0.66f;
				for (int i = 0; i < broccoMaterials.Length; i++) {
					UpdateBroccoTreeWind (broccoMaterials [i], broccoMaterialParams [i]);
				}
				_frameCount = Time.frameCount;
			}
		#endif
		}
        #endregion
        #region Wind
        /// <resume>
        /// udatere vindparametrene for systemet, inklusive vindstyrke, turbulens og retning.
        /// </resume>
		/// denne metode opdaterer de interne vindparametre og anvender ændringerne på alle tilknyttede materialer.
		/// <remarks>Dette er nyttigt i scenarier, hvor vinden skal ændres i realtid,
		/// det skulle kaldes, når vindforholdene skal justeres dynamisk.</remarks>
        /// <param name="windMain">primert vindstyrke. Denne værdi bestemmer den samlede intensitet af vindeffekten.</param>
        /// <param name="windTurbulence"> turbulence af vinden. Højere værdier resulterer i mere uregelmæssig og kaotisk vindadfærd.</param>
        /// <param name="windDirection"> directionen af vinden, repræsenteret som en 3D-vektor.</param>
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
		///<summary>
        /// Opsæt vinden på træprototypematerialer fundet på dette terræn 
        /// og tilføj deres materialer til et array for at opdatere vinden på hver ramme.
		///</summary>
        //private void SetupWind(BroccoTreeController treeController)
        private void SetupWind()
		{
			// Henter alle BroccoTreeController marterialerne
			GameObject treePrefab;
			BroccoTreeController_FJG_1_10_3[] brocco2TreeControllers;

            // henter alle BroccoTreeController materialer for terræntræprototyper.
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
            // henter alle BroccoTreeController_FJG_1_10_3 materialer for terrændetaljeprototyper.
            for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
			{
				treePrefab = terrain.terrainData.detailPrototypes[i].prototype;
				if (treePrefab != null)
				{
					brocco2TreeControllers = treePrefab.GetComponentsInChildren<BroccoTreeController_FJG_1_10_3>();
					foreach (BroccoTreeController_FJG_1_10_3 treeController in brocco2TreeControllers)
					{
                        // setup instanser af træcontroller i henhold til controlleren.
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
		/// Opsæt materialer i instanser med BroccoTreeController
		///	</summary>
        /// <param name="treeController"></param>
        private void SetupBrocco2TreeController(BroccoTreeController_FJG_1_10_3 treeController,BroccoTreeController_FJG_1_10_3.WindQuality windQuality = BroccoTreeController_FJG_1_10_3.WindQuality.Best)
		{
            const float STW_Leaf1Tumble_X = 0.15f;
            const float STW_Leaf1Tumble_Y = 0.15f;
            const float STW_Turbulences_X = 0.7f;
            const float STW_Turbulences_Y = 0.3f;
            const float FROND_RIPPLE_TIME_SCALE = 1f;
            const float FROND_RIPPLE_SECOND = 0.01f;
            const float FROND_RIPPLE_THIRD = 2f;
            const float FROND_RIPPLE_FOURTH = 10f;
            const float DEFAULT_VEC_ZERO = 0f;
            const float DEFAULT_VEC_ONE = 1f;
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
						material.DisableKeyword("_WINDQUALITY_NONE");
						material.DisableKeyword("_WINDQUALITY_FASTEST");
						material.DisableKeyword("_WINDQUALITY_FAST");
						material.DisableKeyword("_WINDQUALITY_BETTER");
						material.DisableKeyword("_WINDQUALITY_BEST");
						material.DisableKeyword("_WINDQUALITY_PALM");
						if (isWindEnabled)
						{
							switch (windQuality)
							{
								case BroccoTreeController_FJG_1_10_3.WindQuality.None:
									material.EnableKeyword("_WINDQUALITY_NONE");
									break;
								case BroccoTreeController_FJG_1_10_3.WindQuality.Fastest:
									material.EnableKeyword("_WINDQUALITY_FASTEST");
									break;
								case BroccoTreeController_FJG_1_10_3.WindQuality.Fast:
									material.EnableKeyword("_WINDQUALITY_FAST");
									break;
								case BroccoTreeController_FJG_1_10_3.WindQuality.Better:
									material.EnableKeyword("_WINDQUALITY_BETTER");
									break;
								case BroccoTreeController_FJG_1_10_3.WindQuality.Best:
									material.EnableKeyword("_WINDQUALITY_BEST");
									break;
								case BroccoTreeController_FJG_1_10_3.WindQuality.Palm:
									material.EnableKeyword("_WINDQUALITY_PALM");
									break;
							}
						}
					}
					else if (isWindEnabled)
					{
						if (windQuality != BroccoTreeController_FJG_1_10_3.WindQuality.None)
						{
							material.EnableKeyword("ENABLE_WIND");
						}
						else
						{
							material.DisableKeyword("ENABLE_WIND");
						}
					}
                    // setter vindaktiveringsflaget og vindkvaliteten
                    material.SetFloat(propWindEnabled, (isWindEnabled ? 1f : 0f));
                    material.SetFloat(propWindQuality, (float)windQuality);

                    // sætter de indledende ST-vektorstandarder ved hjælp af navngivne konstanter
                    value2STWind.STWindBranch = new Vector4(DEFAULT_VEC_ZERO, DEFAULT_VEC_ZERO, DEFAULT_VEC_ZERO, DEFAULT_VEC_ZERO);
                    material.SetVector(propSTWindBranchWhip, value2STWind.STWindBranch);

                    value2STWind.STWindLeaf1Tumble = new Vector4(STW_Leaf1Tumble_X, STW_Leaf1Tumble_Y, DEFAULT_VEC_ZERO, DEFAULT_VEC_ZERO);
                    material.SetVector(propSTWindBranchAdherences, value2STWind.STWindLeaf1Tumble);

                    value2STWind.STWindTurbulences = new Vector4(STW_Turbulences_X, STW_Turbulences_Y, DEFAULT_VEC_ZERO, DEFAULT_VEC_ZERO);
                    material.SetVector(propSTWindTurbulences, value2STWind.STWindTurbulences);

                    value2STWind.STWindFrondRipple = new Vector4(Time.time * FROND_RIPPLE_TIME_SCALE, FROND_RIPPLE_SECOND, FROND_RIPPLE_THIRD, FROND_RIPPLE_FOURTH);
                    material.SetVector(propSTWindFrondRipple, value2STWind.STWindFrondRipple);

                    // registrer materiale til opdateringer pr. ramme og anvend indledende vindværdier
                    if (!_mats.Contains(material) && isWindEnabled)
					{
						_mats.Add(material);
						Vector3 matParams = new Vector3(treeController.trunkBending, 0f, 0f);
						_matParams.Add(matParams);
						ApplyBroccoTreeWind(material, matParams); // sætter initiale værdier
                    }
				}
			}
		}
        /// <summary>
        /// Sætter en enkelt vind-relateret shader-egenskab (Vector4) på et materiale.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="propertyId"></param>
        /// <param name="value"></param>
        private void SetWindShaderProperty(Material mat, int propertyId, Vector4 value)
		{
			mat.SetVector(propertyId, value);
		}
		/// <summary>
		/// Sætter en vindshader-egenskab på et materiale.
		/// </summary>
        /// <param name="mat"></param>
        /// <param name="propertyId"></param>
        /// <param name="value"></param>
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
        /// Beregner og opdaterer alle SpeedTree8-vindværdier for et materiale
        /// ud fra vindstyrke, turbulens, vindretning og træets trunkBending.
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="matParams"></param>
        private void ApplyBroccoTreeWind(Material mat, Vector3 matParams)
		{
			const float TrunkBendingScale = 0.1f;
			const float TrunkBendingOffset = 0.001f;
			const float WindGlobalWScale = 1.125f;
			const float BranchWindMainScale = 0.35f;
			const float BranchAnchorScale = 2f;
			const float BranchTwitchBase = 0.65f;
			const float BranchTwitchMaxReduction = 0.35f;
			const float BranchTwitchTurbulenceNorm = 3f;
			const float LeafTumbleTurbulenceScale = 0.1f;
			const float LeafTumbleMainMax = 0.5f;
			const float LeafTumbleMainMin = 0.1f;
			const float LeafTumbleMainNorm = 4f;
			const float LeafTumbleMainScale = 0.085f;
			const float LeafTwitchMainScale = 0.165f;
			const float LeafTwitchTurbulenceScale = 0.165f;
			const float LeafRippleTurbulenceScale = 0.01f;


            // kalkulerer vindværdier
            // 1: det sætter X-komponenten i vektoren til 1.
            // 0f: det sætter Y, Z og W komponenterne i vektoren til 0.
            value2STWind.STWindGlobal = new Vector4(0f,baseWindAmplitude * valueWindMain,matParams.x * TrunkBendingScale + TrunkBendingOffset, windGlobalW * WindGlobalWScale);

            value2STWind.STWindBranch = new Vector4(0f, valueWindMain * BranchWindMainScale, 0f, 0f);
            value2STWind.STWindVector = valueWindDirection;
            value2STWind.STWindBranchAnchor = new Vector4(
                valueWindDirection.x,
                valueWindDirection.y,
                valueWindDirection.z,
                valueWindMain * BranchAnchorScale);

            float branchTwitchAmount = BranchTwitchBase - Mathf.Lerp(0f, BranchTwitchMaxReduction, valueWindTurbulence / BranchTwitchTurbulenceNorm);
            value2STWind.STWindBranchTwitch = new Vector4(branchTwitchAmount * branchTwitchAmount, 1f, 0f, 0f);

            value2STWind.STWindLeaf1Tumble = new Vector4(0f,
                valueWindTurbulence * LeafTumbleTurbulenceScale,
                valueWindMain * Mathf.Lerp(LeafTumbleMainMax, LeafTumbleMainMin, valueWindMain / LeafTumbleMainNorm),
                valueWindMain * LeafTumbleMainScale);

            value2STWind.STWindLeaf1Twitch = new Vector4(
                valueWindMain * LeafTwitchMainScale,
                valueWindTurbulence * LeafTwitchTurbulenceScale, 0f, 0f);

            value2STWind.STWindLeaf1Ripple = new Vector4(0f, valueWindTurbulence * LeafRippleTurbulenceScale, 0f, 0f);

            value2STWind.STWindLeaf2Tumble = new Vector4(0f,
                valueWindTurbulence * LeafTumbleTurbulenceScale,
                valueWindMain * Mathf.Lerp(LeafTumbleMainMax, LeafTumbleMainMin, valueWindMain / LeafTumbleMainNorm),
                valueWindMain * LeafTumbleMainScale);

            value2STWind.STWindLeaf2Twitch = new Vector4(
                valueWindMain * LeafTwitchMainScale,
                valueWindTurbulence * LeafTwitchTurbulenceScale, 0f, 0f);

            value2STWind.STWindLeaf2Ripple = new Vector4(0f, valueWindTurbulence * LeafRippleTurbulenceScale, 0f, 0f);



			// sætter alle egenskaber ved hjælp af hjælperen
            SetAllWindShaderProperties(mat, value2STWind);
		}
        /// <summary>
        /// Opdaterer værdierne for materialer.
        /// </summary>
        /// <param name="material">Material</param>
        /// <param name="windParams">Wind parameters, x: sproutTurbulance, y: sproutSway, z: localWindAmplitude.</param>
        private void UpdateBroccoTreeWind(Material material, Vector3 windParams)
		{
			const float WindGlobalTimeScale = 0.5f;
			const float localWindAmplitudeScale = 0.1f;
			const float localWindAmplitudeOffset = 0.001f;
			const float leafTwitchTimeScale = 0.5f;


            // updaterer vindværdier
            value2STWind.STWindGlobal.x = valueTime * WindGlobalTimeScale;
			value2STWind.STWindGlobal.z = windParams.x * localWindAmplitudeScale + localWindAmplitudeOffset;

			value2STWind.STWindBranch.x = valueTimeWindMain;

			value2STWind.STWindLeaf1Tumble.x = valueTimeWindMain;
			value2STWind.STWindLeaf1Twitch.z = valueTime * WindGlobalTimeScale;
			value2STWind.STWindLeaf1Ripple.x = valueTime;

			value2STWind.STWindLeaf2Tumble.x = valueTimeWindMain;
			value2STWind.STWindLeaf2Twitch.z = valueTime * WindGlobalTimeScale;
			value2STWind.STWindLeaf2Ripple.x = valueTime;

            // sætter alle egenskaber ved hjælp af hjælperen
            SetAllWindShaderProperties(material, value2STWind);
		}
        /// <summary>
		/// Opdater parametre relateret til den første detekterede retningsbestemte vindzone.
        /// </summary>
        /// <param name="treeController">Tree controller.</param>
        public void GetWindZoneValues()
		{
			Vector4 DefaultWindDirection = new Vector4(1f, 0f, 0f, 0f);// direction: ny Vector4(1f, 0f, 0f, 0f) betyder, at vinden blæser langs X-aksen som standard.//0f: sætter Y, Z og W komponenterne til 0.
            const float SpeedTree8WindScale = 0.4f;
			const float DefaultWindScale = 1f;

			bool isST8 = true;
			valueWindDirection = DefaultWindDirection;
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
	}
}
#endregion
