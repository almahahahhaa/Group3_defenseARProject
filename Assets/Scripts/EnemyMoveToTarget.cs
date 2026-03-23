using UnityEngine;

public class EnemyMoveToTarget : MonoBehaviour
{
    public string targetTag = "Burj_Khalifa";
    public float moveSpeed = 0.1f;
    public float stopDistance = 0.01f;
    private Transform target;

    void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);

        if (obj != null)
        {
            target = obj.transform;
            Debug.Log("Target Found: " + obj.name);
        }
        else
        {
            Debug.LogError("Target NOT found!");
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;

        transform.position += dir * moveSpeed * Time.deltaTime;
        Vector3 distance = target.position - transform.position;
        // transform.LookAt(target);
        
    }

    public void OnCollisionEnter(Collision collision)
    {
          if(collision.gameObject.CompareTag(targetTag))
        {
            Debug.Log("Enemy hit the target!");
            // Call TakeDamage on the target's health script
            BurjKhalifaHealth health = collision.gameObject.GetComponent<BurjKhalifaHealth>();
            if (health != null)
            {
                health.TakeDamage();
            }
            else
            {
                Debug.LogError("Target does not have a health component!");
            }
            // Destroy the enemy after hitting the target
            Destroy(gameObject);
        }
    }
}