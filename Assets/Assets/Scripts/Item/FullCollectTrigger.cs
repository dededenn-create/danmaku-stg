using UnityEngine;

// プレイヤーオブジェクトにアタッチする
public class FullCollectTrigger : MonoBehaviour
{
    public float fullCollectLineY = 3f; // このY座標を越えた瞬間、全アイテム回収発動

    private bool wasAboveLine = false; // 前フレームでラインより上にいたか

    void Update()
    {
        bool isAboveLine = transform.position.y >= fullCollectLineY;

        // 「下にいた→上に来た」の瞬間だけ発動(毎フレーム判定にしない)
        if (isAboveLine && !wasAboveLine)
        {
            TriggerFullCollectAll();
        }

        wasAboveLine = isAboveLine;
    }

    void TriggerFullCollectAll()
    {
        // 個数ではなく「重みの合計」で判定する(大アイテムは5個分の重み)
        int totalWeight = 0;
        foreach (var item in PowerItem.activeItems)
        {
            totalWeight += item.itemWeight;
        }

        foreach (var item in PowerItem.activeItems)
        {
            item.TriggerFullCollect(totalWeight);
        }

        // 倍率をCollect欄に表示(新方式:Instantiateなし)
        if (totalWeight > 0)
        {
            float multiplier = ScoreManager.Instance.GetCollectMultiplier(totalWeight);
            ScoreManager.Instance.ShowMultiplier(multiplier);
        }
    }
}