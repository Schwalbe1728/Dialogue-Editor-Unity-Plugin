using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue")]
public class Dialogue : ScriptableObject
{
    public const int ExitDialogue = -1;

    public string Name { get { return this.name; } }

    private List<DialogueOption> Options;
    
    public DialogueOption[] GetAllOptions()
    {
        if(Options == null)
        {
            Options = new List<DialogueOption>();
        }

        return Options.ToArray();
    }

    public DialogueOption GetOption(int id)
    {
        return
            (id >= 0 && id < Options.Count) ?
                Options[id] : null;
    }    
}

[System.Serializable]
public class DialogueOption
{
    private int optionID;
    private int nextID = Dialogue.ExitDialogue;
    private NodeType nextType = NodeType.Exit;

    public int OptionID { get { return optionID; } set { if (value >= 0) optionID = value; } }

    [HideInInspector]
    public string OptionText;

    [HideInInspector]
    public bool VisitOnce;

    [HideInInspector]
    public int NextID { get { return nextID; } }
    public NodeType NextType { get { return nextType; } }

    public void SetNextNodeExit()
    {
        nextID = Dialogue.ExitDialogue;
        nextType = NodeType.Exit;
    }

    public void SetNext(int nID, NodeType type)
    {
        if(nID >= 0 && type != NodeType.Exit)
        {
            nextID = nID;
            nextType = type;
        }
    }
}

public enum NodeType
{
    Exit,
    Node,
    Option,
    Condition
}