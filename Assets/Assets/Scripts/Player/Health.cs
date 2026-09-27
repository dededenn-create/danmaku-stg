using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    [SerializeField] private bool destroyOnDeath = true;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHP = maxHP;
    }

    public void ResetHP()
    {
        CurrentHP = maxHP;
        IsDead = false;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP -= amount;
        OnDamaged?.Invoke();

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (CurrentHP <= 0)
        {
            IsDead = true;
            OnDeath?.Invoke();

            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}