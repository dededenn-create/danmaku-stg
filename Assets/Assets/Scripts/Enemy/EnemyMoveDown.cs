using UnityEngine;

public class EnemyMoveDown : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float stopY = 3f;

    void Update()
    {
        if (transform.position.y > stopY)
        {
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        }
    }
}