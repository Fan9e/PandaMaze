using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class UIMessageManagerTests
{
    private GameObject _gameObject;
    private UIMessageManager _manager;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("UIMessageManager_GO");
        _manager = _gameObject.AddComponent<UIMessageManager>();
    }

    [TearDown]
    public void TearDown()
    {
        // clear singleton to avoid bleed between tests
        var prop = typeof(UIMessageManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var setMethod = prop?.GetSetMethod(true);
        setMethod?.Invoke(null, new object[] { null });

        if (_gameObject != null)
            Object.DestroyImmediate(_gameObject);
    }

    static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    static object InvokePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        return method.Invoke(target, null);
    }

    [Test]
    public void Awake_SetsInstanceAndHidesRoot()
    {
        // Arrange
        var messageRoot = new GameObject("messageRoot");
        messageRoot.SetActive(true);
        SetPrivateField(_manager, "messageRoot", messageRoot);

        // Act - call Awake manually to exercise initialization logic
        InvokePrivateMethod(_manager, "Awake");

        // Assert
        Assert.AreEqual(_manager, UIMessageManager.Instance, "Awake should assign the singleton Instance.");
        Assert.IsFalse(messageRoot.activeSelf, "messageRoot should be hidden after Awake.");
    }

    [UnityTest]
    public IEnumerator ShowMessage_SetsTextAndHidesAfterDuration()
    {
        // Arrange
        var messageRoot = new GameObject("messageRoot");
        messageRoot.SetActive(false);
        SetPrivateField(_manager, "messageRoot", messageRoot);

        var textObj = new GameObject("tmpText");
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        SetPrivateField(_manager, "messageText", tmp);

        // Ensure Awake logic is applied (sets root inactive if needed and singleton)
        InvokePrivateMethod(_manager, "Awake");

        // Act
        _manager.ShowMessage("HelloWorld", 0.1f);

        // Immediate assertions
        Assert.AreEqual("HelloWorld", tmp.text, "ShowMessage should set messageText.text.");
        Assert.IsTrue(messageRoot.activeSelf, "ShowMessage should set messageRoot active.");

        // Wait longer than the duration so coroutine can run
        yield return new WaitForSeconds(0.15f);

        // Assert after hide
        Assert.IsFalse(messageRoot.activeSelf, "messageRoot should be hidden after duration.");

        // _currentRoutine should be null now
        var routine = typeof(UIMessageManager).GetField("_currentRoutine", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
        Assert.IsNull(routine, "_currentRoutine should be null after coroutine completes.");

        // Cleanup created objects
        Object.DestroyImmediate(textObj);
        Object.DestroyImmediate(messageRoot);
    }

    [UnityTest]
    public IEnumerator ShowMessage_StopsPreviousCoroutine_WhenCalledAgain()
    {
        // Arrange
        var messageRoot = new GameObject("messageRoot");
        SetPrivateField(_manager, "messageRoot", messageRoot);

        var textObj = new GameObject("tmpText");
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        SetPrivateField(_manager, "messageText", tmp);

        InvokePrivateMethod(_manager, "Awake");

        // Act - start a long message
        _manager.ShowMessage("first", 1f);
        // allow coroutine to start
        yield return null;

        // Immediately show second, short message (should cancel the first)
        _manager.ShowMessage("second", 0.05f);

        Assert.AreEqual("second", tmp.text, "Second ShowMessage should replace the text and cancel previous coroutine.");
        Assert.IsTrue(messageRoot.activeSelf, "messageRoot should remain active after second ShowMessage.");

        // Wait longer than the second duration to ensure it hides
        yield return new WaitForSeconds(0.1f);

        Assert.IsFalse(messageRoot.activeSelf, "messageRoot should be hidden after the second (short) duration.");

        // Cleanup
        Object.DestroyImmediate(textObj);
        Object.DestroyImmediate(messageRoot);
    }
}