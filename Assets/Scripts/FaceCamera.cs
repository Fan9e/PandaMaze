using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera _camera; 

    /// <summary>
    /// Initialiserer kamera-referencen ved at hente scenens hovedkamera.
    /// </summary>
    private void Start()
    {
        _camera = Camera.main;   
    }

    /// <summary>
    /// Retter objektets orientering ind, så det peger i samme retning som kameraet hver frame.
    /// </summary>
    private void LateUpdate()
    {
        if (_camera == null) return;

        transform.forward = _camera.transform.forward;
    }
}
