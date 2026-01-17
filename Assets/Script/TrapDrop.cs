using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class TrapZone : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnInterval = 2f;
    
    [Header("Player Interaction")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float climbDisableDuration = 2f;
    [SerializeField] private string playerTag = "Player";
    
    private BoxCollider triggerZone;
    private bool playerInZone = false;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        triggerZone = GetComponent<BoxCollider>();
        triggerZone.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;
            StartSpawning();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
            StopSpawning();
        }
    }

    private void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnTrapsCoroutine());
        }
    }

    private void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnTrapsCoroutine()
    {
        while (playerInZone)
        {
            SpawnTrap();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTrap()
    {
        if (spawnPoints.Count == 0 || trapPrefab == null)
            return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        
        if (spawnPoint != null)
        {
            GameObject trap = Instantiate(trapPrefab, spawnPoint.position, spawnPoint.rotation);
            
            FallingTrap trapComponent = trap.GetComponent<FallingTrap>();
            if (trapComponent == null)
            {
                trapComponent = trap.AddComponent<FallingTrap>();
            }
            
            trapComponent.Initialize(knockbackForce, climbDisableDuration, playerTag);
        }
    }

    private void OnDestroy()
    {
        StopSpawning();
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

        Destroy(gameObject, 10f);
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
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                 collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject, 0.5f);
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
                knockbackDirection.x = 0.3f;
                knockbackDirection.y= 0.3f;
                knockbackDirection.Normalize();
                
                playerController.ApplyKnockback(knockbackDirection, knockbackForce);
            }
        }

        Destroy(gameObject, 0.1f);
    }
}