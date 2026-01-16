using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class TrapZone : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Header("Player Interaction")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float climbDisableDuration = 2f;
    [SerializeField] private string playerTag = "Player";
    
    private BoxCollider triggerZone;
    private List<GameObject> spawnedTraps = new List<GameObject>();
    private bool hasTriggered = false;

    private void Awake()
    {
        triggerZone = GetComponent<BoxCollider>();
        triggerZone.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (other.CompareTag(playerTag))
        {
            SpawnTraps();
            hasTriggered = true;
        }
    }

    private void SpawnTraps()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && trapPrefab != null)
            {
                GameObject trap = Instantiate(trapPrefab, spawnPoint.position, spawnPoint.rotation);
                
                FallingTrap trapComponent = trap.GetComponent<FallingTrap>();
                if (trapComponent == null)
                {
                    trapComponent = trap.AddComponent<FallingTrap>();
                }
                
                trapComponent.Initialize(knockbackForce, climbDisableDuration, playerTag);
                
                spawnedTraps.Add(trap);
            }
        }
    }

    public void ResetZone()
    {
        foreach (GameObject trap in spawnedTraps)
        {
            if (trap != null)
            {
                Destroy(trap);
            }
        }
        
        spawnedTraps.Clear();
        hasTriggered = false;
    }
}

public class FallingTrap : MonoBehaviour
{
    private Rigidbody rb;
    private float knockbackForce;
    private float climbDisableDuration;
    private string playerTag;
    private bool hasHitPlayer = false;

    public void Initialize(float knockback, float climbDisable, string tag)
    {
        knockbackForce = knockback;
        climbDisableDuration = climbDisable;
        playerTag = tag;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.isKinematic = false;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHitPlayer)
            return;

        if (collision.gameObject.CompareTag(playerTag))
        {
            hasHitPlayer = true;
            HandlePlayerHit(collision.gameObject, collision);
        }
    }

    private void HandlePlayerHit(GameObject player, Collision collision)
    {
        PlayerMovement playerController = player.GetComponent<PlayerMovement>();
        
        if (playerController != null)
        {
            playerController.ForceExitClimb();
            playerController.DisableClimbForSeconds(climbDisableDuration);

            if (!playerController.IsClimbing())
            {
                Vector3 knockbackDirection = (player.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.3f;
                knockbackDirection.Normalize();
                
                playerController.ApplyKnockback(knockbackDirection, knockbackForce);
            }
        }

        Destroy(gameObject, 0.1f);
    }
}