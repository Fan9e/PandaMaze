using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement3D : MonoBehaviour
{
    public float speed = 5f;
    public float inputDeadzone = 0.05f;   // små udsving = stå stille

    Rigidbody rb;
    Animator anim;
    Vector3 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();   // <- henter Animator på Panda
    }

    void Update()
    {
        var camF = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        var camR = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        input = camR * h + camF * v;
        if (input.magnitude < inputDeadzone) input = Vector3.zero; // stå stille
        else input = input.normalized;

        if (anim)
            anim.SetBool("IsWalking", input.sqrMagnitude > 0.0001f); // true når der er input
    }

    void FixedUpdate()
    {
        Vector3 v = input * speed;
        v.y = rb.linearVelocity.y;     // behold evt. gravitation
        rb.linearVelocity = v;
    }
}

