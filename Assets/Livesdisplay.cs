using UnityEngine;
using TMPro;

public class LivesDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private PlayerLives playerLives;

    private void OnEnable()
    {
        playerLives.OnLivesChanged += UpdateLivesText;
        UpdateLivesText(playerLives.CurrentLives);
    }

    private void OnDisable()
    {
        playerLives.OnLivesChanged -= UpdateLivesText;
    }

    private void UpdateLivesText(int lives)
    {
        livesText.text = "Player";
    }
}