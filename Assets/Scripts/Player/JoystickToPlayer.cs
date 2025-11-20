using UnityEngine;

public class JoystickToPlayer : MonoBehaviour
{
    public PlayerMovement player;      // Pandaen
    public bl_Joystick joystick;       // Joystick component

    [Tooltip("Multiply joystick input (use <1 to slow movement on mobile).")]
    public float inputMultiplier = 1f;

    private void Update()
    {
        if (player == null || joystick == null)
        {
            Debug.Log("JoystickToPlayer: Mangler refs!");
            return;
        }

        Vector2 input = new Vector2(joystick.Horizontal, joystick.Vertical) * inputMultiplier;

        // Clamp magnitude so we don't accidentally send >1 on some devices
        if (input.sqrMagnitude > 1f)
            input = input.normalized;

        Debug.Log($"JoystickToPlayer UPDATE (platform={Application.platform}) ? {input}");

        player.SetJoystickInput(input);
    }
}