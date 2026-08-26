using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    [SerializeField] private string speaker;
    [TextArea(2, 5)]
    [SerializeField] private string text;
    [SerializeField] private AudioClip voiceClip;

    public string Speaker => speaker;
    public string Text => text;
    public AudioClip VoiceClip => voiceClip;

    public DialogueLine(string lineSpeaker, string lineText)
        : this(lineSpeaker, lineText, null)
    {
    }

    public DialogueLine(
        string lineSpeaker,
        string lineText,
        AudioClip lineVoiceClip
    )
    {
        speaker = lineSpeaker;
        text = lineText;
        voiceClip = lineVoiceClip;
    }
}

[CreateAssetMenu(
    fileName = "DialogueSequence",
    menuName = "Earth Reshaping/Dialogue Sequence"
)]
public class DialogueSequence : ScriptableObject
{
    [SerializeField] private string sequenceId;
    [SerializeField] private bool lockPlayer = true;
    [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

    public string SequenceId => sequenceId;
    public bool LockPlayer => lockPlayer;
    public DialogueLine[] Lines => lines;

    public void Configure(
        string id,
        bool shouldLockPlayer,
        DialogueLine[] sequenceLines
    )
    {
        sequenceId = id;
        lockPlayer = shouldLockPlayer;
        lines = sequenceLines ?? Array.Empty<DialogueLine>();
    }
}
