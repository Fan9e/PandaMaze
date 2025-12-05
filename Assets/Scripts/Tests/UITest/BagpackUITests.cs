using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstests for <see cref="BagpackUI"/>.
/// Indeholder opsætning af test-GameObject, mock sprites og assertions for UI-opdateringer.
/// </summary>
public class BagpackUITests
{
    private GameObject root;
    private BagpackUI bagpack;
    private Image[] slotImages;

    /// <summary>
    /// Kører før hver test. Opretter et midlertidigt GameObject-tree, initialiserer BagpackUI-komponenten
    /// og injicerer nødvendige private felter via reflection (panel, knap, slot-images, sprites osv.).
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        root = new GameObject("BagpackRoot");
        bagpack = root.AddComponent<BagpackUI>();

        var panel = new GameObject("InventoryPanel");
        panel.transform.parent = root.transform;
        var buttonGo = new GameObject("BagButton");
        buttonGo.transform.parent = root.transform;
        var button = buttonGo.AddComponent<Button>();

        slotImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var g = new GameObject("SlotImage" + i);
            g.transform.parent = root.transform;
            slotImages[i] = g.AddComponent<Image>();
        }

        SetPrivateField("inventoryPanel", panel);
        SetPrivateField("bagButton", button);
        SetPrivateField("slotImages", slotImages);

        var keySprites = new Sprite[3] { CreateSprite(Color.red), CreateSprite(Color.green), CreateSprite(Color.blue) };
        var weaponSprites = new Sprite[3] { CreateSprite(Color.cyan), CreateSprite(Color.magenta), CreateSprite(Color.yellow) };
        var potionSprite = CreateSprite(Color.gray);
        SetPrivateField("keySprites", keySprites);
        SetPrivateField("weaponSprites", weaponSprites);
        SetPrivateField("potionSprite", potionSprite);

        SetPrivateField("showEmptyPlaceholders", true);
    }

    /// <summary>
    /// Kører efter hver test. Rydder det midlertidige GameObject for at undgå sideeffekter mellem tests.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// Tester at når spilleren ejer en nøgle, sættes den korrekte nøglesprite i det første slot
    /// og at billedets farve er fuldt opac (hvid).
    /// </summary>
    [Test]
    public void SetHasKey_ShowsKeySprite_WhenOwned()
    {
        var keySprites = (Sprite[])GetPrivateField("keySprites");
        bagpack.SetHasKey(true);

        Assert.AreEqual(keySprites[0], slotImages[0].sprite);
        Assert.AreEqual(Color.white, slotImages[0].color);
    }

    /// <summary>
    /// Tester at når spilleren ikke ejer en nøgle og placeholders er slået til, vises placeholder-spriten
    /// med delvis gennemsigtighed i det første slot.
    /// </summary>
    [Test]
    public void SetHasKey_ShowsPlaceholder_WhenNotOwned()
    {
        var placeholder = CreateSprite(Color.black);
        SetPrivateField("emptySlotSprite", placeholder);
        SetPrivateField("showEmptyPlaceholders", true);

        bagpack.SetHasKey(false);

        Assert.AreEqual(placeholder, slotImages[0].sprite);
        Assert.AreEqual(0.5f, slotImages[0].color.a, 1e-6f);
    }

    /// <summary>
    /// Tester at tilføjelse af potions opdaterer den viste potion-tekst og sætter potion-ikonet og farven i potion-slottet.
    /// </summary>
    [Test]
    public void AddPotions_UpdatesPotionText_AndPotionSlot()
    {
        var textGo = new GameObject("PotionText");
        textGo.transform.parent = root.transform;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        SetPrivateField("potionCountText", tmp);

        var potionSprite = (Sprite)GetPrivateField("potionSprite");

        bagpack.AddPotions(2);

        Assert.AreEqual("2", tmp.text);
        Assert.AreEqual(potionSprite, slotImages[1].sprite);
        Assert.AreEqual(Color.white, slotImages[1].color);
    }

    /// <summary>
    /// Hjælper der sætter et privat felt på BagpackUI via reflection.
    /// Kaster en assertion hvis feltet ikke findes (fejl i testen/implementationen).
    /// </summary>
    /// <param name="name">Navnet på det private felt i <see cref="BagpackUI"/>.</param>
    /// <param name="value">Værdien der skal sættes på feltet.</param>
    private void SetPrivateField(string name, object value)
    {
        var f = typeof(BagpackUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(f, $"Field '{name}' not found on BagpackUI");
        f.SetValue(bagpack, value);
    }

    /// <summary>
    /// Henter værdien af et privat felt fra <see cref="BagpackUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt.</param>
    /// <returns>Værdien af det private felt.</returns>
    private object GetPrivateField(string name)
    {
        var f = typeof(BagpackUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(f, $"Field '{name}' not found on BagpackUI");
        return f.GetValue(bagpack);
    }

    /// <summary>
    /// Opretter en simpel firkantet <see cref="Sprite"/> fyldt med én given farve til brug i tests.
    /// </summary>
    /// <param name="c">Den farve sprite'en skal fyldes med.</param>
    /// <returns>En nyoprettet <see cref="Sprite"/>.</returns>
    private Sprite CreateSprite(Color c)
    {
        var tex = new Texture2D(8, 8);
        var pixels = new Color[8 * 8];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
    }
}