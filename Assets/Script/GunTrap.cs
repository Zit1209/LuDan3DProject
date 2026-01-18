using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class BulletTrapZone : MonoBehaviour
{
    [Header("Trap Zone")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float fireRate = 1f;
    
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 20f;
    [SerializeField] private float knockbackDuration = 1f;
    
    private BoxCollider triggerZone;
    private bool playerInZone = false;
    private Coroutine fireCoroutine;

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
            StartFiring();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
            StopFiring();
        }
    }

    private void StartFiring()
    {
        if (fireCoroutine == null)
        {
            fireCoroutine = StartCoroutine(FireBulletsCoroutine());
        }
    }

    private void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    private IEnumerator FireBulletsCoroutine()
    {
        while (playerInZone)
        {
            SpawnBullet();
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void SpawnBullet()
    {
        if (bulletPrefab == null || startPoint == null || endPoint == null)
            return;

        GameObject bullet = Instantiate(bulletPrefab, startPoint.position, Quaternion.identity);
        
        TrapBullet bulletComponent = bullet.GetComponent<TrapBullet>();
        if (bulletComponent == null)
        {
            bulletComponent = bullet.AddComponent<TrapBullet>();
        }
        
        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        
        bulletComponent.Initialize(direction, bulletSpeed, distance, knockbackForce, knockbackDuration, playerTag);
    }

    private void OnDestroy()
    {
        StopFiring();
    }
}

public class TrapBullet : MonoBehaviour
{
    private Vector3 moveDirection;
    private float speed;
    private float maxDistance;
    private float knockbackForce;
    private float knockbackDuration;
    private string playerTag;
    private float traveledDistance = 0f;
    private Vector3 startPosition;
    private bool hasHitTarget = false;

    public void Initialize(Vector3 direction, float bulletSpeed, float distance, float knockback, float duration, string tag)
    {
        moveDirection = direction;
        speed = bulletSpeed;
        maxDistance = distance;
        knockbackForce = knockback;
        knockbackDuration = duration;
        playerTag = tag;
        startPosition = transform.position;

        transform.rotation = Quaternion.LookRotation(direction);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.1f;
        }
        col.isTrigger = true;
    }

    private void Update()
    {
        if (hasHitTarget)
            return;

        transform.position += moveDirection * speed * Time.deltaTime;
        
        traveledDistance = Vector3.Distance(startPosition, transform.position);
        
        if (traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitTarget)
            return;

        if (other.CompareTag(playerTag))
        {
            hasHitTarget = true;
            HandlePlayerHit(other.gameObject);
        }
    }

    private void HandlePlayerHit(GameObject player)
    {
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            Vector3 knockbackDirection = moveDirection.normalized;
            playerMovement.EnterKnockbackState(knockbackDirection, knockbackForce, knockbackDuration);
        }

        Destroy(gameObject);
    }
}