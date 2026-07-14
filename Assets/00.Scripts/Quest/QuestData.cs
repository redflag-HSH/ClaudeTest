using UnityEngine;

/// <summary>
/// Stages an accepted quest moves through, in order:
/// 게시판에서 의뢰 수주 → 의뢰인 대화 → 의뢰 진행/해결 → 의뢰인 대화 → 피그말리온과 대화 → 완료
/// </summary>
public enum QuestStage
{
    TalkToClient,       // 2. accepted at the board — go talk to the client
    InProgress,         // 3. objective underway (gameplay advances this stage)
    ReportToClient,     // 4. objective done — report back to the client
    TalkToPygmalion     // 5. finally talk to Pygmalion; advancing past this completes the quest
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/Quest")]
public class QuestData : ScriptableObject
{
    public string questId;
    [TextArea(1, 3)] public string text;

    [Space(10)]
    public Vector2 pinPosition;
    public int dialogID;
    [Tooltip("If true, this quest can be acquired again after being completed. Default (false) = acquirable once.")]
    public bool repeatable;
}
