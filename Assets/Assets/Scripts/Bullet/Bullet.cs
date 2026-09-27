using UnityEngine;
public class Bullet : MonoBehaviour
{
    public float speed = 40f;
    public Vector3 direction = Vector3.up;
    public GameObject explosionPrefab;

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}