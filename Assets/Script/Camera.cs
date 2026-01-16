using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraRotationSync : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool invertX = false;

    private Transform playerTransform;
    private float currentYRotation;

    private void Awake()
    {
        playerTransform = transform;
        
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        }
    }

    private void Start()
    {
        if (freeLookCamera != null)
        {
            currentYRotation = playerTransform.eulerAngles.y;
            freeLookCamera.m_XAxis.Value = currentYRotation;
        }
    }

    private void LateUpdate()
    {
        if (freeLookCamera == null)
            return;

        float cameraYRotation = freeLookCamera.m_XAxis.Value;
        
        playerTransform.rotation = Quaternion.Euler(0f, cameraYRotation, 0f);
        
        currentYRotation = cameraYRotation;
    }
}