using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float speed;
    public float slowSpeed;
    public float fireRate = 0.2f;

    [Header("Hitbox")]
    public SpriteRenderer hitboxRenderer; // hitboxオブジェクトのSpriteRenderer
    public float fadeSpeed = 8f;
    public GameObject bulletPrefab;

    private float nextFireTime = 0f;
    private float hitboxAlpha = 0f;

    void Update()
    {
        float x = 0;
        float y = 0;

        if (Keyboard.current.leftArrowKey.isPressed) x = -1;
        if (Keyboard.current.rightArrowKey.isPressed) x = 1;
        if (Keyboard.current.upArrowKey.isPressed) y = 1;
        if (Keyboard.current.downArrowKey.isPressed) y = -1;

        bool slow = Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = slow ? slowSpeed : speed;

        transform.position += new Vector3(x, y, 0) * currentSpeed * Time.deltaTime;

        UpdateHitboxVisual(slow);

        if (Keyboard.current.zKey.isPressed)
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void UpdateHitboxVisual(bool slow)
    {
        float target = slow ? 1f : 0f;
        hitboxAlpha = Mathf.MoveTowards(hitboxAlpha, target, fadeSpeed * Time.deltaTime);

        Color c = hitboxRenderer.color;
        c.a = hitboxAlpha;
        hitboxRenderer.color = c;
    }
    
    public void HideHitboxImmediately()
    {
        hitboxAlpha = 0f;
        if (hitboxRenderer != null)
        {
            Color c = hitboxRenderer.color;
            c.a = hitboxAlpha;
            hitboxRenderer.color = c;
        }
    }

    public static PlayerMove Instance;

    void Awake()
    {
        Instance = this;
    }

    private int shotLevel = 1;

    public void OnLevelChanged(int newLevel)
    {
        shotLevel = newLevel;
    }

    private float BulletSpeed = 60; // デフォルトの弾速

    public AudioSource audioSource;
    public AudioClip shotSound;

    void Shoot()
    {
        audioSource.PlayOneShot(shotSound);

        switch (shotLevel)
        {  
            case 1:
                SpawnBulletStraight(-0.2f);
                SpawnBulletStraight(0.2f);
                break;
            case 2:
                SpawnBulletStraight(-0.2f);
                SpawnBulletStraight(0.2f);
                SpawnBullet(-10f);
                SpawnBullet(10f);
                break;
            case 3:
                SpawnBulletStraight(-0.2f);
                SpawnBulletStraight(0.2f);
                SpawnBullet(-10f);
                SpawnBullet(10f);
                SpawnBullet(20f);
                SpawnBullet(-20f);
                break;
            case 4:
                SpawnBulletStraight(-0.2f);
                SpawnBulletStraight(0.2f);
                SpawnBullet(-10f);
                SpawnBullet(10f);
                SpawnBullet(20f);
                SpawnBullet(-20f);
                SpawnBullet(-30f);
                SpawnBullet(30f);
                break;
        }
    }

    void SpawnBulletStraight(float xOffset)
    {
        Vector3 pos = transform.position + new Vector3(xOffset, 0.5f, 0);
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);

        // 弾自身に「どの方向に飛ぶか」を伝える
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetDirection(Vector3.up);
            bulletScript.SetSpeed(BulletSpeed); // 速度を渡す
        }
    }

    void SpawnBullet(float angleOffset)
    {
         Quaternion rotation = Quaternion.Euler(0, 0, -angleOffset); // Z軸回転で角度をつける
         Vector3 direction = rotation * Vector3.up;

         Vector3 pos = transform.position + Vector3.up * 0.5f;
         GameObject bullet = Instantiate(bulletPrefab, pos, rotation);

         // 弾自身に「どの方向に飛ぶか」を伝える
         Bullet bulletScript = bullet.GetComponent<Bullet>();
         if (bulletScript != null)
         {
             bulletScript.SetDirection(direction);
             bulletScript.SetSpeed(BulletSpeed); // 速度を渡す
         }
    }
}