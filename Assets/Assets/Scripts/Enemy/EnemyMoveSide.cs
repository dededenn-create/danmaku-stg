using UnityEngine;

public class EnemyMoveSide : MonoBehaviour
{
    public float moveSpeed = 3f;
    public bool moveRight = true; // true: 左から右へ、false: 右から左へ

    void Update()
    {
        Vector3 direction = moveRight ? Vector3.right : Vector3.left;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
}