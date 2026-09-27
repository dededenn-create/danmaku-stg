using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEvent
{
    public float triggerTime;
    public GameObject enemyPrefab;
    public Vector2 spawnPosition;
    public bool hasTriggered;

    [Header("これがtrueだと、出したこの敵が死ぬまで次のイベントに進まない")]
    public bool isBlockingBoss;
}

public class StageManager : MonoBehaviour
{
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();

    [Header("デバッグ用：ここを変えるとその時間から開始できる")]
    public float debugStartTime = 0f;

    private float stageTimer;
    private GameObject currentBlockingEnemy;

    void Start()
    {
        stageTimer = debugStartTime;

        foreach (var spawnEvent in spawnEvents)
        {
            if (spawnEvent.triggerTime < debugStartTime)
            {
                spawnEvent.hasTriggered = true;
            }
        }
    }

    void Update()
    {
        if (currentBlockingEnemy != null)
        {
            return;
        }

        stageTimer += Time.deltaTime;

        foreach (var spawnEvent in spawnEvents)
        {
            if (!spawnEvent.hasTriggered && stageTimer >= spawnEvent.triggerTime)
            {
                SpawnEnemy(spawnEvent);
                spawnEvent.hasTriggered = true;

                if (spawnEvent.isBlockingBoss)
                {
                    break;
                }
            }
        }
    }

    void SpawnEnemy(SpawnEvent spawnEvent)
    {
        GameObject enemy = Instantiate(spawnEvent.enemyPrefab, spawnEvent.spawnPosition, Quaternion.identity);

        if (spawnEvent.isBlockingBoss)
        {
            currentBlockingEnemy = enemy;
        }
    }

    // ★これが今回のエラーの原因。このメソッドが足りてない
    public void ClearBlockingEnemy()
    {
        currentBlockingEnemy = null;
    }
}