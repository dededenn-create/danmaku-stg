using UnityEngine;
public class EnemyShooter_Aim : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform Player;
    public float fireRate = 5f;
    public float bulletSpeed = 3f;
    float nextFire;
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Player = playerObj.transform;
        }
    }
    void Update()
    {
        if (Player == null) return;
        if (Time.time >= nextFire)
        {
            Vector3 dir = (Player.position - transform.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.SetVelocity(dir * bulletSpeed);
            }
            nextFire = Time.time + fireRate;
        }
    }
}