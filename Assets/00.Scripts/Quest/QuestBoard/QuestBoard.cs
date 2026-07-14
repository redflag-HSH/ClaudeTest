using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class QuestBoard : MonoBehaviour
{
    [SerializeField] GameObject pinPrefab;
    void Start()
    {
        Show(false);
    }
    public void Show(bool onoff)
    {
        gameObject.SetActive(onoff);
        if (onoff)
            SetUpQuestPins();
        else
            PlayerControl.Instance.SetInputEnabled(true);
    }
    void SetUpQuestPins()
    {
        // Show(true) can run repeatedly (interact key stays live while the
        // board is open) — rebuild pins from scratch each time.
        foreach (QuestPin old in GetComponentsInChildren<QuestPin>(true))
            Destroy(old.gameObject);

        foreach (QuestData q in QuestManager.Instance.AvailableQuests)
        {
            GameObject ig = Instantiate(pinPrefab, transform);
            QuestPin pin = ig.GetComponent<QuestPin>();
            // pinPosition is an offset in local UI units from the board's center.
            ig.GetComponent<RectTransform>().localPosition = q.pinPosition;
            pin.quest = q;
            pin.dialog = DialogSystem.Instance != null ? DialogSystem.Instance.GetDialogById(q.dialogID) : null;
        }
    }
}
