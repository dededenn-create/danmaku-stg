using UnityEngine;

public class GameOverEventBridge : MonoBehaviour
{
    [SerializeField] private GameOverController gameOverController;

    private void OnEnable()
    {
        if (PlayerLives.Instance != null)
        {
            PlayerLives.Instance.OnLivesChanged += HandleLivesChanged;
            PlayerLives.Instance.OnGameOver += HandleGameOver;
        }
    }

    private void OnDisable()
    {
        if (PlayerLives.Instance != null)
        {
            PlayerLives.Instance.OnLivesChanged -= HandleLivesChanged;
            PlayerLives.Instance.OnGameOver -= HandleGameOver;
        }
    }

    private void HandleLivesChanged(int remainingLives)
    {
        // 残機に関わらず、被弾したら必ずフラッシュを出す
        // (ゲームオーバーになる最後の1発でも被弾の視覚フィードバックを出すため)
        gameOverController.FlashHit();
    }

    private void HandleGameOver()
    {
        gameOverController.ShowGameOver();
    }
}