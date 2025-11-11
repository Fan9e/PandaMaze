using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    Camera camera;

    void Start()
    {
        camera = Camera.main;   
    }

    void LateUpdate()
    {
        if (camera == null) return;

        transform.forward = camera.transform.forward;
    }
}
