using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RotatingTrapZone : MonoBehaviour
{
    [Header("Trap Zone")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Trap Settings")]
    [SerializeField] private GameObject trapObject;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 25f;
    
    private BoxCollider triggerZone;
    private bool trapActive = false;
    private RotatingTrap rotatingTrapComponent;

    private void Awake()
    {
        triggerZone = GetComponent<BoxCollider>();
        triggerZone.isTrigger = true;
        
        if (trapObject != null)
        {
            rotatingTrapComponent = trapObject.GetComponent<RotatingTrap>();
            if (rotatingTrapComponent == null)
            {
                rotatingTrapComponent = trapObject.AddComponent<RotatingTrap>();
            }
            
            rotatingTrapComponent.Initialize(rotationSpeed, knockbackForce, playerTag);
            rotatingTrapComponent.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            trapActive = true;
            if (rotatingTrapComponent != null)
            {
                rotatingTrapComponent.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            trapActive = false;
            if (rotatingTrapComponent != null)
            {
                rotatingTrapComponent.SetActive(false);
            }
        }
    }
}

public class RotatingTrap : MonoBehaviour
{
    private float rotationSpeed;
    private float knockbackForce;
    private string playerTag;
    private bool isActive = false;

    public void Initialize(float speed, float knockback, string tag)
    {
        rotationSpeed = speed;
        knockbackForce = knockback;
        playerTag = tag;
        
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    private void Update()
    {
        if (isActive)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }

    private void HandlePlayerCollision(GameObject player)
    {
        Vector3 knockbackDirection = transform.forward;
        
        knockbackDirection.y = 0.3f;
        knockbackDirection.Normalize();
        
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        
        if (playerMovement != null)
        {
            playerMovement.EnterKnockbackState(knockbackDirection, knockbackForce, 1f);
        }
        else
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.VelocityChange);
            }
        }
    }
}