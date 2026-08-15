using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            Transform cameraTransform =
                player.transform.Find("Camera");

            if (cameraTransform != null)
            {
                mainCamera =
                    cameraTransform.GetComponent<Camera>();
            }
        }

        if (mainCamera == null)
        {
            Debug.LogWarning(
                "Could not find Camera under Player."
            );
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            //mainCamera must be null?
            return;

        Vector3 direction =
            transform.position - mainCamera.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }
    }
}