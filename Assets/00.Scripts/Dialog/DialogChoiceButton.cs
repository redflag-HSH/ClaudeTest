using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DialogChoiceButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;

    public void Setup(string text, System.Action onPicked)
    {
        if (label != null) label.text = text;
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onPicked());
    }
}
