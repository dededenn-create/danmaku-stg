using UnityEngine;

public class BombIconDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] bombIcons;
    [SerializeField] private PlayerStatus playerStatus;

    private void Start()
    {
        UpdateIcons(playerStatus.bombCount);
    }

    private void OnEnable()
    {
        if (playerStatus != null)
            playerStatus.OnBombCountChanged += UpdateIcons;
    }

    private void OnDisable()
    {
        if (playerStatus != null)
            playerStatus.OnBombCountChanged -= UpdateIcons;
    }

    private void UpdateIcons(int bombCount)
    {
        for (int i = 0; i < bombIcons.Length; i++)
        {
            bombIcons[i].SetActive(i < bombCount);
        }
    }
}