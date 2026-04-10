using UnityEngine;

public enum DialogSide { Left, Right }

[System.Serializable]
public class DialogLine
{
    [Tooltip("Character name shown in the name plate.")]
    public string speakerName;

    [Tooltip("Background color of the name plate for this character.")]
    public Color namePlateColor = new Color(0.18f, 0.44f, 0.80f, 1f);

    [Tooltip("Character bust/portrait sprite. Can be different per line for expressions.")]
    public Sprite portrait;

    [Tooltip("Which side of the screen this character stands on.")]
    public DialogSide side = DialogSide.Left;

    [TextArea(2, 5)]
    public string text;
}

/// <summary>
/// ScriptableObject holding a full dialog conversation.
/// Each line can have a different speaker, portrait, side, and name plate color.
/// Create via: Assets > Create > Dialog > Dialog
/// </summary>
[CreateAssetMenu(fileName = "NewDialog", menuName = "Dialog/Dialog")]
public class Dialog : ScriptableObject
{
    public DialogLine[] lines;
}
