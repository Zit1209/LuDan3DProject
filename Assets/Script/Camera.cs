using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraRotationSync : MonoBehaviour
{
    [Header("Cinemachine Reference")]
    [SerializeField] private CinemachineCamera freeLookCamera;

    private void LateUpdate()
    {
        if (freeLookCamera == null)
            return;

        CinemachineOrbitalFollow orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        
        if (orbitalFollow != null)
        {
            float cameraYRotation = orbitalFollow.HorizontalAxis.Value;
            
            transform.rotation = Quaternion.Euler(0f, cameraYRotation, 0f);
        }
    }
}