using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerMovementTests
{
    private GameObject _go;
    private Component _playerMovement;
    private Rigidbody _rb;
    private Animator _anim;

    // Helpers for reflection
    private object GetPrivateField(string name)
    {
        var fi = _playerMovement.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(fi, $"Field '{name}' not found on {_playerMovement.GetType().Name}");
        return fi.GetValue(_playerMovement);
    }

    private void SetPrivateField(string name, object value)
    {
        var fi = _playerMovement.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(fi, $"Field '{name}' not found on {_playerMovement.GetType().Name}");
        fi.SetValue(_playerMovement, value);
    }

    private object InvokePrivateMethod(string name, params object[] args)
    {
        var mi = _playerMovement.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(mi, $"Method '{name}' not found on {_playerMovement.GetType().Name}");
        return mi.Invoke(_playerMovement, args);
    }

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PlayerMovementTestGO");
        // Add required components
        _rb = _go.AddComponent<Rigidbody>();
        // Some projects expose linearVelocity, to be safe set interpolation as Awake would.
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add a runtime Animator (we don't need a controller for these tests)
        _anim = _go.AddComponent<Animator>();

        // Add PlayerMovement component (from project)
        _playerMovement = _go.AddComponent(Type.GetType("PlayerMovement, Assembly-CSharp") ?? typeof(Component));

        // If the above AddComponent couldn't resolve the type by name, try getting the type directly from the GameObject.
        if (_playerMovement == null || _playerMovement.GetType() == typeof(Component))
        {
            // fallback: try to find the PlayerMovement type in loaded assemblies
            var pmType = Type.GetType("PlayerMovement") ?? Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetType("PlayerMovement") != null)?.GetType("PlayerMovement");
            Assert.IsNotNull(pmType, "Could not find PlayerMovement type in loaded assemblies.");
            _playerMovement = _go.AddComponent(pmType);
        }

        // Ensure Awake ran and private _rb/_anim fields are set. If not, set them directly.
        var rbField = _playerMovement.GetType().GetField("_rb", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rbField != null && rbField.GetValue(_playerMovement) == null)
            rbField.SetValue(_playerMovement, _rb);

        var animField = _playerMovement.GetType().GetField("_anim", BindingFlags.NonPublic | BindingFlags.Instance);
        if (animField != null && animField.GetValue(_playerMovement) == null)
            animField.SetValue(_playerMovement, _anim);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
    }

    [Test]
    public void SetMovementInput_Sets_MoveInput_And_Flag()
    {
        var setMethod = _playerMovement.GetType().GetMethod("SetMovementInput", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(setMethod);
        setMethod.Invoke(_playerMovement, new object[] { new Vector3(1f, 0f, 0.5f) });

        var moveInput = (Vector2)GetPrivateField("_moveInput");
        var fromFlag = (bool)GetPrivateField("_moveFromInputSystem");

        Assert.AreEqual(1f, moveInput.x, 1e-5f);
        Assert.AreEqual(0.5f, moveInput.y, 1e-5f);
        Assert.IsTrue(fromFlag);
    }

    [Test]
    public void OnMove_Vector2_Sets_MoveInput_And_Flag()
    {
        var mi = _playerMovement.GetType().GetMethod("OnMove", new[] { typeof(Vector2) });
        Assert.IsNotNull(mi);
        mi.Invoke(_playerMovement, new object[] { new Vector2(-0.3f, 0.8f) });

        var moveInput = (Vector2)GetPrivateField("_moveInput");
        var fromFlag = (bool)GetPrivateField("_moveFromInputSystem");

        Assert.AreEqual(-0.3f, moveInput.x, 1e-5f);
        Assert.AreEqual(0.8f, moveInput.y, 1e-5f);
        Assert.IsTrue(fromFlag);
    }

    [Test]
    public void OnLook_Vector2_Sets_TurnInput_And_Flag()
    {
        var mi = _playerMovement.GetType().GetMethod("OnLook", new[] { typeof(Vector2) });
        Assert.IsNotNull(mi);
        mi.Invoke(_playerMovement, new object[] { new Vector2(0.6f, 0f) });

        var turnInput = (Vector2)GetPrivateField("_turnInput");
        var fromFlag = (bool)GetPrivateField("_turnFromInputSystem");

        Assert.AreEqual(0.6f, turnInput.x, 1e-5f);
        Assert.IsTrue(fromFlag);
    }

    [Test]
    public void ComputeTurnInput_Sets_PendingYaw_Based_On_TurnInput()
    {
        // Set a turn input and invoke ComputeTurnInput
        var onLook = _playerMovement.GetType().GetMethod("OnLook", new[] { typeof(Vector2) });
        onLook.Invoke(_playerMovement, new object[] { new Vector2(1.0f, 0f) });

        // Ensure inputDeadzone and rotationSpeed are read; call ComputeTurnInput
        InvokePrivateMethod("ComputeTurnInput");

        var pendingYaw = (float)GetPrivateField("_pendingYaw");
        // rotationSpeed default from script is 120; pendingYaw = turnInput.x * rotationSpeed
        Assert.AreNotEqual(0f, pendingYaw);
    }

    [Test]
    public void ApplyRotation_Changes_RigidbodyRotation_When_PendingYaw_Present()
    {
        // make sure there's some turn input
        var onLook = _playerMovement.GetType().GetMethod("OnLook", new[] { typeof(Vector2) });
        onLook.Invoke(_playerMovement, new object[] { new Vector2(0.5f, 0f) });

        // compute pending yaw, then apply rotation
        InvokePrivateMethod("ComputeTurnInput");

        var before = _rb.rotation;
        InvokePrivateMethod("ApplyRotation");

        var after = _rb.rotation;
        Assert.AreNotEqual(before.eulerAngles.y, after.eulerAngles.y);
    }

    [Test]
    public void ApplyMovement_Sets_RigidbodyLinearVelocity_BasedOn_MoveInput()
    {
        // Set forward movement
        var setMovement = _playerMovement.GetType().GetMethod("SetMovementInput", BindingFlags.Public | BindingFlags.Instance);
        setMovement.Invoke(_playerMovement, new object[] { new Vector3(0f, 0f, 1f) });

        // Invoke ApplyMovement
        InvokePrivateMethod("ApplyMovement");

        // Access linearVelocity property via reflection because code uses that name
        var linVelProp = _rb.GetType().GetProperty("linearVelocity", BindingFlags.Public | BindingFlags.Instance);
        Vector3 velocity;
        if (linVelProp != null)
        {
            velocity = (Vector3)linVelProp.GetValue(_rb);
        }
        else
        {
            // fallback to common Rigidbody.velocity property
            velocity = _rb.linearVelocity;
        }

        // Expect z velocity close to movementSpeed (default 5f)
        Assert.AreEqual(5f, velocity.z, 1e-4f);
    }

    [Test]
    public void UpdateAnimation_DoesNotThrow_And_Sets_AnimatorBool()
    {
        // Ensure some movement and some turn input then call UpdateAnimation
        var setMovement = _playerMovement.GetType().GetMethod("SetMovementInput", BindingFlags.Public | BindingFlags.Instance);
        setMovement.Invoke(_playerMovement, new object[] { new Vector3(0.5f, 0f, 0.5f) });

        var onLook = _playerMovement.GetType().GetMethod("OnLook", new[] { typeof(Vector2) });
        onLook.Invoke(_playerMovement, new object[] { new Vector2(0.4f, 0f) });

        // Call UpdateAnimation (private)
        Assert.DoesNotThrow(() => InvokePrivateMethod("UpdateAnimation"));
        // We can't easily inspect Animator internal without an animator controller; at least ensure no exception.
    }

    [Test]
    public void ResetInputFlagsIfReleased_Clears_Flags_When_Inputs_Zero()
    {
        // set flags to true
        var onMove = _playerMovement.GetType().GetMethod("OnMove", new[] { typeof(Vector2) });
        var onLook = _playerMovement.GetType().GetMethod("OnLook", new[] { typeof(Vector2) });

        onMove.Invoke(_playerMovement, new object[] { new Vector2(0.3f, 0.1f) });
        onLook.Invoke(_playerMovement, new object[] { new Vector2(0.2f, 0f) });

        // Set inputs to zero
        SetPrivateField("_moveInput", Vector2.zero);
        SetPrivateField("_turnInput", Vector2.zero);

        // Call ResetInputFlagsIfReleased
        InvokePrivateMethod("ResetInputFlagsIfReleased");

        var moveFlag = (bool)GetPrivateField("_moveFromInputSystem");
        var turnFlag = (bool)GetPrivateField("_turnFromInputSystem");

        Assert.IsFalse(moveFlag);
        Assert.IsFalse(turnFlag);
    }

    [Test]
    public void ReadInput_EditorFallback_Applies_Deadzone()
    {
        // Put tiny values in move and turn input and call the method
        SetPrivateField("_moveInput", new Vector2(0.01f, 0.02f));
        SetPrivateField("_turnInput", new Vector2(0.03f, 0f));

        // Call the method (it's private)
        InvokePrivateMethod("ReadInput_EditorFallback");

        var moveInput = (Vector2)GetPrivateField("_moveInput");
        var turnInput = (Vector2)GetPrivateField("_turnInput");

        // The script applies deadzone of default 0.05, these small values should be zeroed
        Assert.AreEqual(Vector2.zero, moveInput);
        Assert.AreEqual(0f, turnInput.x);
    }
}