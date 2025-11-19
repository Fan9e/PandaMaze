using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Basis-klasse for enheds- og integrationstests der bruger Unity-scenen.
/// Hjælper med at oprette og spore midlertidige <see cref="GameObject"/>s og komponenter,
/// så de kan fjernes ved testens afslutning for at undgå sideeffekter mellem tests.
/// </summary>
public abstract class UnityTestBase
{
    /// <summary>
    /// Internt register over alle <see cref="GameObject"/>s der skal destrueres i <see cref="TearDown"/>.
    /// </summary>
    private readonly List<GameObject> _toDestroy = new List<GameObject>();

    /// <summary>
    /// Opretter et nyt <see cref="GameObject"/> og registrerer det til oprydning.
    /// </summary>
    /// <param name="name">Valgfrit navn til det oprettede <see cref="GameObject"/>. Hvis <c>null</c> eller tomt, oprettes uden navn.</param>
    /// <returns>Det oprettede <see cref="GameObject"/>.</returns>
    protected GameObject CreateGameObject(string name = null)
    {
        var go = string.IsNullOrEmpty(name) ? new GameObject() : new GameObject(name);
        _toDestroy.Add(go);
        return go;
    }

    /// <summary>
    /// Opretter et nyt <see cref="GameObject"/>, tilføjer en komponent af typen <typeparamref name="T"/> og returnerer den.
    /// Det oprettede gameobject registreres automatisk til oprydning.
    /// </summary>
    /// <typeparam name="T">Komponenttypen der skal tilføjes. Skal arve fra <see cref="Component"/>.</typeparam>
    /// <param name="name">Valgfrit navn til det oprettede <see cref="GameObject"/>.</param>
    /// <returns>Den tilføjede komponent af typen <typeparamref name="T"/>.</returns>
    protected T CreateComponent<T>(string name = null) where T : Component
    {
        var go = CreateGameObject(name);
        return go.AddComponent<T>();
    }

    /// <summary>
    /// Registrerer et eksisterende <see cref="GameObject"/> til oprydning, hvis det ikke allerede er registreret.
    /// </summary>
    /// <param name="go">Det <see cref="GameObject"/> der skal spores. Hvis <c>null</c>, gøres intet.</param>
    protected void Track(GameObject go)
    {
        if (go == null) return;
        if (!_toDestroy.Contains(go))
            _toDestroy.Add(go);
    }

    /// <summary>
    /// Finder og destruerer alle gameobjects som har en komponent af typen <typeparamref name="T"/>.
    /// Benyttes til at sikre at rester fra tidligere tests fjernes.
    /// </summary>
    /// <typeparam name="T">Komponenttypen som angiver hvilke gameobjects der skal destrueres.</typeparam>
    protected void DestroyAllOfType<T>() where T : Component
    {
        foreach (var comp in UnityEngine.Object.FindObjectsOfType<T>())
        {
            if (comp != null && comp.gameObject != null)
                UnityEngine.Object.DestroyImmediate(comp.gameObject);
        }
    }

    /// <summary>
    /// NUnit TearDown som kører efter hver test.
    /// Destruerer alle tidligere oprettede eller sporede <see cref="GameObject"/>s og rydder op for kendte typer.
    /// Eventuelle fejl under oprydning ignoreres for at sikre at efterfølgende tests kan køre.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        foreach (var go in _toDestroy)
        {
            if (go != null)
                UnityEngine.Object.DestroyImmediate(go);
        }
        _toDestroy.Clear();

        try
        {
            DestroyAllOfType<Monster>();
            DestroyAllOfType<Player>();
            DestroyAllOfType<MonsterHealthUI>();
            DestroyAllOfType<PlayerHealthUI>();
        }
        catch (Exception)
        {

        }
    }
}