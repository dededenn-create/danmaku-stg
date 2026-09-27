using UnityEngine;

public class LivesIconDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private PlayerLives playerLives;

    private void Start()
    {
        UpdateIcons(playerLives.CurrentLives);
    }

    private void OnEnable()
    {
        if (playerLives != null)
            playerLives.OnLivesChanged += UpdateIcons;
    }

    private void OnDisable()
    {
        if (playerLives != null)
            playerLives.OnLivesChanged -= UpdateIcons;
    }

    private void UpdateIcons(int lives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            lifeIcons[i].SetActive(i < lives);
        }
    }
}