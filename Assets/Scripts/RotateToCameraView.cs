using UnityEngine;

[ExecuteAlways]
public class RotateToCameraView : MonoBehaviour
{
    Camera _camera;

    void Start()
    {
        // Cache the main camera
        if (Camera.main != null)
        {
            _camera = Camera.main;
        }
        else
        {
            // Fallback: find any camera tagged “MainCamera”
            _camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        }
    }

    void LateUpdate()
    {
        if (_camera == null || !_camera.gameObject.activeInHierarchy)
        {
            _camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            return;
        }

        // Option: if you want to lock X/Z axes so the sprite only Y-rotates:
        Vector3 e = _camera.transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }
}
