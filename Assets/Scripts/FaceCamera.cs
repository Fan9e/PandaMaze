//using System;
//using UnityEngine;

//public class FaceCamera : MonoBehaviour
//{
//    private Camera _camera;

//    /// <summary>
//    /// Initialiserer kamera-referencen ved at hente scenens hovedkamera.
//    /// </summary>
//    private void Start()
//    {
//        _camera = Camera.main;
//    }

//    /// <summary>
//    /// Retter objektets orientering ind, så det peger i samme retning som kameraet hver frame.
//    /// </summary>
//    private void LateUpdate()
//    {
//        if (_camera == null) return;

//        transform.forward = _camera.transform.forward;
//    }
//}

using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [Tooltip("Optional: assign a target transform (player). If empty the script will try GameObject tagged 'Player', then Camera.main.")]
    [SerializeField] private Transform targetTransform;

    private void Start()
    {
        if (targetTransform == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                targetTransform = playerGO.transform;
                Debug.Log($"FaceCamera: assigned Player '{playerGO.name}' as target for '{gameObject.name}'.", this);
            }
            else if (Camera.main != null)
            {
                targetTransform = Camera.main.transform;
                Debug.Log($"FaceCamera: no Player found; assigned Camera.main as target for '{gameObject.name}'.", this);
            }
            else
            {
                Debug.LogWarning($"FaceCamera: no target assigned and no Player tag / Camera.main found for '{gameObject.name}'.", this);
            }
        }
        else
        {
            Debug.Log($"FaceCamera: using assigned target '{targetTransform.gameObject.name}' for '{gameObject.name}'.", this);
        }
    }

    private void LateUpdate()
    {
        if (targetTransform == null) return;

        // Face the target position but only rotate around Y (prevent pitching/rolling)
        Vector3 direction = targetTransform.position - transform.position;
        direction.y = 0f; // ignore vertical difference

        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}