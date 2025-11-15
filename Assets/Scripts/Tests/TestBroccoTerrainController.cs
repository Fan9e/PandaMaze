using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Broccoli.Controller;


public class BroccoTerrainControllerPlayModeTests
{
    GameObject go;
    BroccoTerrainController_FJG_1_10_3 controller;
    List<Object> createdObjects;
    BindingFlags allInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [SetUp]
    public void SetUp()
    {
        createdObjects = new List<Object>();
        go = new GameObject("TerrainControllerGO");
        createdObjects.Add(go);
        controller = go.AddComponent<BroccoTerrainController_FJG_1_10_3>();
        createdObjects.Add(controller);

        // Prevent Update() from throwing before individual tests set arrays.
        SetPrivateField(controller, "broccoMaterials", new Material[0]);
        SetPrivateField(controller, "broccoMaterialParams", new Vector3[0]);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in createdObjects)
        {
            if (obj is GameObject g) Object.DestroyImmediate(g);
            else Object.DestroyImmediate(obj);
        }
    }

    // Reflection helpers
    object InvokePrivate(object instance, string name, params object[] args)
    {
        var mi = instance.GetType().GetMethod(name, allInstance);
        Assert.IsNotNull(mi, $"Method {name} not found");
        return mi.Invoke(instance, args);
    }
    T GetPrivateField<T>(object instance, string name)
    {
        var fi = instance.GetType().GetField(name, allInstance);
        Assert.IsNotNull(fi, $"Field {name} not found");
        return (T)fi.GetValue(instance);
    }
    void SetPrivateField(object instance, string name, object value)
    {
        var fi = instance.GetType().GetField(name, allInstance);
        Assert.IsNotNull(fi, $"Field {name} not found");
        fi.SetValue(instance, value);
    }

    [UnityTest]
    public IEnumerator UpdateWind_AppliesValuesToControllerAndMaterialProperties()
    {
        // Arrange: create a material and expose it to controller arrays
        var mat = new Material(Shader.Find("Standard"));
        createdObjects.Add(mat);
        SetPrivateField(controller, "broccoMaterials", new Material[] { mat });
        SetPrivateField(controller, "broccoMaterialParams", new Vector3[] { new Vector3(1.0f, 0f, 0f) });

        // Act: update wind
        Vector3 dir = new Vector3(0.1f, 0.2f, 0.3f);
        controller.UpdateWind(1.5f, 0.25f, dir);
        // allow one frame for any internal updates
        yield return null;

        // Assert: controller fields updated
        Assert.AreEqual(1.5f, GetPrivateField<float>(controller, "valueWindMain"), 1e-6f);
        Assert.AreEqual(0.25f, GetPrivateField<float>(controller, "valueWindTurbulence"), 1e-6f);
        var dirField = GetPrivateField<Vector3>(controller, "valueWindDirection");
        Assert.AreEqual(dir.x, dirField.x, 1e-6f);
        Assert.AreEqual(dir.y, dirField.y, 1e-6f);
        Assert.AreEqual(dir.z, dirField.z, 1e-6f);

        // Material should have been written with ST wind global vector by ApplyBroccoTreeWind
        float baseWindAmplitude = GetPrivateField<float>(controller, "baseWindAmplitude");
        var stGlobal = mat.GetVector("_ST_WindGlobal");
        Assert.AreEqual(baseWindAmplitude * 1.5f, stGlobal.y, 1e-5f);

        yield return null;
    }

    [UnityTest]
    public IEnumerator GetWindZoneValues_PicksDirectionalWindZone()
    {
        // Arrange: create a directional WindZone in scene
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

    [UnityTest]
    public IEnumerator SetupBrocco2TreeController_RegistersMaterialForUpdates()
    {
        // Arrange: create a prefab-like object with renderer and BroccoTreeController component
        var prefab = new GameObject("TreePrefab");
        createdObjects.Add(prefab);
        var renderer = prefab.AddComponent<MeshRenderer>();
        createdObjects.Add(renderer);
        prefab.AddComponent<MeshFilter>();
        var mat = new Material(Shader.Find("Standard"));
        createdObjects.Add(mat);
        renderer.sharedMaterials = new Material[] { mat };

        // Add a BroccoTreeController_FJG_1_10_3 component (from runtime code)
        var treeController = prefab.AddComponent<BroccoTreeController_FJG_1_10_3>();
        createdObjects.Add(treeController);

        // set trunkBending if it exists (public field)
        var tcType = typeof(BroccoTreeController_FJG_1_10_3);
        var trunkField = tcType.GetField("trunkBending", BindingFlags.Public | BindingFlags.Instance);
        if (trunkField != null) trunkField.SetValue(treeController, 1.5f);

        // Act: invoke private SetupBrocco2TreeController
        InvokePrivate(controller, "SetupBrocco2TreeController", treeController, BroccoTreeController_FJG_1_10_3.WindQuality.Better);
        yield return null;

        // Assert: internal lists should contain the material and parameters
        var matsList = GetPrivateField<List<Material>>(controller, "_mats");
        var paramsList = GetPrivateField<List<Vector3>>(controller, "_matParams");
        Assert.IsNotNull(matsList);
        Assert.IsTrue(matsList.Contains(mat));
        Assert.IsNotNull(paramsList);
        Assert.IsTrue(paramsList.Count > 0);
        // trunkBending was stored in x component
        Assert.AreEqual(1.5f, paramsList[0].x, 1e-5f);

        yield return null;
    }
}