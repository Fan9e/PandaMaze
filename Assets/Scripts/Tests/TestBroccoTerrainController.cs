using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Broccoli.Controller;

/// <summary>
/// Indeholder PlayMode tests til BroccoTerrainController_FJG_1_10_3.
/// Testene sikrer at vindværdier, materialer og WindZone fungerer som forventet.
/// </summary>
public class BroccoTerrainControllerPlayModeTests
{
    GameObject go;
    BroccoTerrainController_FJG_1_10_3 controller;
    List<Object> createdObjects;
    BindingFlags allInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// Initialiserer et nyt GameObject og controller før hver test.
    /// Sørger også for at interne arrays er sat, så Update() ikke fejler.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        createdObjects = new List<Object>();
        go = new GameObject("TerrainControllerGO");
        createdObjects.Add(go);
        controller = go.AddComponent<BroccoTerrainController_FJG_1_10_3>();
        createdObjects.Add(controller);

        // Forhindrer at Update() fejler
        SetPrivateField(controller, "broccoMaterials", new Material[0]);
        SetPrivateField(controller, "broccoMaterialParams", new Vector3[0]);
    }

    /// <summary>
    /// Rydder alle objekter op efter hver test, så scenen holdes ren.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        foreach (var obj in createdObjects)
        {
            if (obj is GameObject g) Object.DestroyImmediate(g);
            else Object.DestroyImmediate(obj);
        }
    }

    // ---------------------------------------------
    // Reflection helpers
    // ---------------------------------------------

    /// <summary>
    /// Kalder en privat metode på controlleren via reflection.
    /// </summary>
    object InvokePrivate(object instance, string name, params object[] args)
    {
        var mi = instance.GetType().GetMethod(name, allInstance);
        Assert.IsNotNull(mi, $"Method {name} not found");
        return mi.Invoke(instance, args);
    }

    /// <summary>
    /// Henter værdien af et privat felt via reflection.
    /// </summary>
    T GetPrivateField<T>(object instance, string name)
    {
        var fi = instance.GetType().GetField(name, allInstance);
        Assert.IsNotNull(fi, $"Field {name} not found");
        return (T)fi.GetValue(instance);
    }

    /// <summary>
    /// Sætter værdien af et privat felt via reflection.
    /// </summary>
    void SetPrivateField(object instance, string name, object value)
    {
        var fi = instance.GetType().GetField(name, allInstance);
        Assert.IsNotNull(fi, $"Field {name} not found");
        fi.SetValue(instance, value);
    }

    // ---------------------------------------------
    // TESTS
    // ---------------------------------------------

    /// <summary>
    /// Tester at UpdateWind korrekt opdaterer controllerens interne felter
    /// og at materialets vindvektor (_ST_WindGlobal) bliver sat som forventet.
    /// </summary>
    [UnityTest]
    public IEnumerator UpdateWind_AppliesValuesToControllerAndMaterialProperties()
    {
        // Arrange
        var mat = new Material(Shader.Find("Standard"));
        createdObjects.Add(mat);
        SetPrivateField(controller, "broccoMaterials", new Material[] { mat });
        SetPrivateField(controller, "broccoMaterialParams", new Vector3[] { new Vector3(1.0f, 0f, 0f) });

        // Act
        Vector3 dir = new Vector3(0.1f, 0.2f, 0.3f);
        controller.UpdateWind(1.5f, 0.25f, dir);
        yield return null;

        // Assert controller felter
        Assert.AreEqual(1.5f, GetPrivateField<float>(controller, "valueWindMain"), 1e-6f);
        Assert.AreEqual(0.25f, GetPrivateField<float>(controller, "valueWindTurbulence"), 1e-6f);
        var dirField = GetPrivateField<Vector3>(controller, "valueWindDirection");
        Assert.AreEqual(dir.x, dirField.x, 1e-6f);
        Assert.AreEqual(dir.y, dirField.y, 1e-6f);
        Assert.AreEqual(dir.z, dirField.z, 1e-6f);

        // Materialets vektor skal være opdateret
        float baseWindAmplitude = GetPrivateField<float>(controller, "baseWindAmplitude");
        var stGlobal = mat.GetVector("_ST_WindGlobal");
        Assert.AreEqual(baseWindAmplitude * 1.5f, stGlobal.y, 1e-5f);

        yield return null;
    }

    /// <summary>
    /// Tester at GetWindZoneValues finder en Directional WindZone
    /// og at controllerens værdier opdateres korrekt ud fra dens data.
    /// </summary>
    [UnityTest]
    public IEnumerator GetWindZoneValues_PicksDirectionalWindZone()
    {
        // Arrange
        var wzGo = new GameObject("WindZoneGO");
        createdObjects.Add(wzGo);
        var wz = wzGo.AddComponent<WindZone>();
        wz.mode = WindZoneMode.Directional;
        wz.windMain = 3.0f;
        wz.windTurbulence = 0.7f;
        wzGo.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        // Act
        controller.GetWindZoneValues();
        yield return null;

        // Assert
        Assert.AreEqual(3.0f, GetPrivateField<float>(controller, "valueWindMain"), 1e-6f);
        Assert.AreEqual(0.7f, GetPrivateField<float>(controller, "valueWindTurbulence"), 1e-6f);

        var dir = GetPrivateField<Vector3>(controller, "valueWindDirection");
        var expected = wz.transform.forward;
        Assert.AreEqual(expected.x, dir.x, 1e-5f);
        Assert.AreEqual(expected.y, dir.y, 1e-5f);
        Assert.AreEqual(expected.z, dir.z, 1e-5f);

        yield return null;
    }

    /// <summary>
    /// Tester at SetupBrocco2TreeController registrerer materialer og parametre
    /// fra et Brocco-træ korrekt, samt at trunkBending gemmes i parametrene.
    /// </summary>
    [UnityTest]
    public IEnumerator SetupBrocco2TreeController_RegistersMaterialForUpdates()
    {
        // Arrange
        var prefab = new GameObject("TreePrefab");
        createdObjects.Add(prefab);
        var renderer = prefab.AddComponent<MeshRenderer>();
        createdObjects.Add(renderer);
        prefab.AddComponent<MeshFilter>();
        var mat = new Material(Shader.Find("Standard"));
        createdObjects.Add(mat);
        renderer.sharedMaterials = new Material[] { mat };

        var treeController = prefab.AddComponent<BroccoTreeController_FJG_1_10_3>();
        createdObjects.Add(treeController);

        // trunkBending hvis feltet findes
        var tcType = typeof(BroccoTreeController_FJG_1_10_3);
        var trunkField = tcType.GetField("trunkBending", BindingFlags.Public | BindingFlags.Instance);
        if (trunkField != null) trunkField.SetValue(treeController, 1.5f);

        // Act
        InvokePrivate(controller, "SetupBrocco2TreeController", treeController, BroccoTreeController_FJG_1_10_3.WindQuality.Better);
        yield return null;

        // Assert
        var matsList = GetPrivateField<List<Material>>(controller, "_mats");
        var paramsList = GetPrivateField<List<Vector3>>(controller, "_matParams");

        Assert.IsNotNull(matsList);
        Assert.IsTrue(matsList.Contains(mat));

        Assert.IsNotNull(paramsList);
        Assert.IsTrue(paramsList.Count > 0);

        Assert.AreEqual(1.5f, paramsList[0].x, 1e-5f);

        yield return null;
    }
}
