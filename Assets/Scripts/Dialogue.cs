using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue")]
public class Dialogue : ScriptableObject
{
    public const int ExitDialogue = -1;

    public string Name { get { return this.name; } }

    [SerializeField]
    private int startNodeID = 0;    
    private int currentNodeID;

    [SerializeField]
    private List<DialogueOption> Options;
    [SerializeField]
    private List<DialogueNode> Nodes;
    [SerializeField]
    private List<ConditionNode> Conditions;

    [SerializeField]
    private DialogueEditorInfo editorInfo;
    
    public DialogueEditorInfo EditorInfo { get { return editorInfo; } set { editorInfo = value; } }

    #region Options
    public DialogueOption[] GetAllOptions()
    {
        if(Options == null)
        {
            Options = new List<DialogueOption>();
        }

        return Options.ToArray();
    }

    public void SetAllOptions(DialogueOption[] opts)
    {
        Options = new List<DialogueOption>(opts);
    }

    public void SetAllOptions(List<DialogueOption> opts)
    {
        Options = opts;
    }

    public DialogueOption GetOption(int id)
    {
        return
            (id >= 0 && id < Options.Count) ?
                Options[id] : null;
    }

    public void DeleteOption(int nr)
    {
        if(Options != null && Options.Count < nr && Options.Count >= 0)
        {
            Options.RemoveAt(nr);

            for(int i = nr; i < Options.Count; i++)
            {
                Options[i].OptionID = i;
            }
        }
    }
    #endregion
    #region Nodes
    public DialogueNode[] GetAllNodes()
    {
        if (Nodes == null)
        {
            Nodes = new List<DialogueNode>();
        }

        return Nodes.ToArray();
    }

    public void SetAllNodes(DialogueNode[] nods)
    {
        Nodes = new List<DialogueNode>(nods);
    }

    public void SetAllNodes(List<DialogueNode> nods)
    {
        if(nods.Count == 0) Debug.Log("nods == 0!!!");

        Nodes = nods;
    }

    public DialogueNode GetNode(int id)
    {
        return
            (id >= 0 && id < Nodes.Count) ?
                Nodes[id] : null;
    }

    public void DeleteNode(int nr)
    {
        if (Nodes != null && Nodes.Count < nr && Nodes.Count >= 0)
        {
            Nodes.RemoveAt(nr);

            for (int i = nr; i < Nodes.Count; i++)
            {
                Nodes[i].NodeID = i;
            }
        }
    }
    #endregion

    public void StartDialogue()
    {
        currentNodeID = startNodeID;
    }

    public bool DialogueFinished { get { return currentNodeID == Dialogue.ExitDialogue; } }
    public DialogueNode CurrentNode { get { return Nodes[currentNodeID]; } }

    public DialogueOption[] CurrentNodeOptions
    {
        get
        {
            List<DialogueOption> result = new List<DialogueOption>();

            foreach(int optIndex in CurrentNode.OptionsAttached)
            {
                if(Options[optIndex].CanDisplay)
                {
                    result.Add(Options[optIndex]);
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// Sets next node, assuming that the current node is an immediate node.
    /// </summary>
    public void Next()
    {
        if(!CurrentNode.ImmediateNode)
        {
            throw new System.ArgumentException("Node is not an immediate node!");
        }

        NodeType targetType;
        int targetID;

        CurrentNode.GetTarget(out targetID, out targetType);

        while(targetType == NodeType.Condition)
        {            
            if(Conditions[targetID].ConditionTest(out targetID, out targetType))
            {
                //  test success event
            }
            else
            {
                //  test failure event
            }
        }

        if(targetType == NodeType.Node)
        {
            currentNodeID = targetID;
        }
        else
        {
            throw new System.ArgumentException("Illegal node type");
        }
    }

    /// <summary>
    /// Sets next node, assuming that the current node is a normal node, and one of the available
    /// dialogue options has been chosen as an answer to what has been said in the current node.
    /// </summary>
    /// <param name="chosenAnswer"></param>
    public void Next(DialogueOption chosenAnswer)
    {
        if(CurrentNode.ImmediateNode)
        {
            throw new System.ArgumentException("Node is an immediate node - operation illegal!");
        }

        int targetID = chosenAnswer.NextID;
        NodeType targetType = chosenAnswer.NextType;

        while(targetType == NodeType.Condition)
        {
            if( Conditions[targetID].ConditionTest(out targetID, out targetType) )
            {
                // test success event
            }
            else
            {
                // test failure event
            }
        }

        currentNodeID = targetID;
    }
}

[System.Serializable]
public class DialogueEditorInfo
{
    public List<Rect> Windows;
    public List<NodeType> WindowTypes;
    public List<int> NodeTypesIDs;
    public List<int> OptionsIndexes;
    public List<int> NodesIndexes;
    public int Nodes;
    public int Options;
    public Dictionary<int, Dictionary<int, bool>> NodesOptionsFoldouts;

    public DialogueEditorInfo()
    {
        Windows = new List<Rect>();
        WindowTypes = new List<NodeType>();
        NodeTypesIDs = new List<int>();
        OptionsIndexes = new List<int>();
        NodesIndexes = new List<int>();
        Nodes = 0;
        Options = 0;
        NodesOptionsFoldouts = new Dictionary<int, Dictionary<int, bool>>();
    }

    public void RestoreFoldouts(DialogueNode[] nodes)
    {
        NodesOptionsFoldouts.Clear();

        foreach(DialogueNode node in nodes)
        {
            if(!node.ImmediateNode && node.OptionsAttached != null)
            {
                NodesOptionsFoldouts.Add(node.NodeID, new Dictionary<int, bool>());

                foreach(int option in node.OptionsAttached)
                {
                    NodesOptionsFoldouts[node.NodeID].Add(option, false);
                }
            }
        }
    }

}

[System.Serializable]
public class DialogueNode
{
    [SerializeField]
    private int nodeID;
    [SerializeField]
    private bool isImmediateNode;
    [SerializeField]
    private int nextID = Dialogue.ExitDialogue;
    [SerializeField]
    private NodeType nextType = NodeType.Exit;

    [SerializeField]
    private string nodeText;
    [SerializeField]
    private string customID;

    [SerializeField]
    private List<int> optionsIndexesList;

    public int NodeID { get { return nodeID; } set { if (value >= 0) nodeID = value; } }    
    public string Text { get { return nodeText; } set { nodeText = value; } }
    public string CustomID
    {
        get
        {
            return (customID != null && !customID.Equals("")) ? 
                customID : 
                "Node " + nodeID.ToString();
        }

        set
        {
            customID = value;
        }
    }

    public DialogueNode()
    {
        optionsIndexesList = new List<int>();
    }

    #region Options Attached
    public int[] OptionsAttached
    {
        get
        {
            int[] result =
                (!ImmediateNode && optionsIndexesList != null) ?
                    optionsIndexesList.ToArray() : null;

            return result;
        }

        set
        {
            optionsIndexesList = new List<int>(value);
        }
    }

    public void ClearOptionsAttached()
    {
        optionsIndexesList.Clear();
    }
    #endregion
    #region Immediate Nodes
    public bool ImmediateNode { get { return isImmediateNode; } }

    public void RevertToRegularNode()
    {
        isImmediateNode = false;
    }

    public void MakeImmediateNode()
    {
        isImmediateNode = true;
    }

    public void SetImmediateNodeTarget(int nID, NodeType nType)
    {
        if(isImmediateNode && nID >= 0 && nType != NodeType.Option && nType != NodeType.Exit )
        {
            nextID = nID;
            nextType = nType;
        }
    }

    public void SetImmediateNodeTargetExit()
    {
        if(isImmediateNode)
        {
            nextID = Dialogue.ExitDialogue;
            nextType = NodeType.Exit;
        }
    }

    public void GetTarget(out int targetID, out NodeType targetType)
    {
        targetID = this.nextID;
        targetType = this.nextType;
    }
    #endregion
}

[System.Serializable]
public class DialogueOption
{
    [SerializeField]
    private int optionID;
    [SerializeField]
    private int nextID = Dialogue.ExitDialogue;
    [SerializeField]
    private NodeType nextType = NodeType.Exit;

    [SerializeField]
    private bool alreadyVisited = false;
    [SerializeField]
    private ConditionNode entryCondition;
    [SerializeField]
    private bool entryConditionSet = false;

    public int OptionID { get { return optionID; } set { if (value >= 0) optionID = value; } }

    [HideInInspector]
    public string OptionText;

    [HideInInspector]
    public bool VisitOnce;

    [HideInInspector]
    public int NextID { get { return nextID; } }
    public NodeType NextType { get { return nextType; } }

    public bool CanDisplay
    {
        get
        {
            return
                (!VisitOnce || !alreadyVisited) &&
                (entryCondition == null || entryCondition.ConditionTest());
        }
    }

    public string Visit()
    {
        alreadyVisited = true;
        return OptionText;
    }

    public ConditionNode EntryCondition { get { return entryCondition; } }
    public bool EntryConditionSet { get { return entryConditionSet; } }

    public void SetEntryCondition(ConditionNode condition)
    {
        entryCondition = condition;
        entryConditionSet = entryCondition != null;
    }

    public void ClearEntryCondition() { entryCondition = null; entryConditionSet = false; }

    public void SetNextNodeExit()
    {
        nextID = Dialogue.ExitDialogue;
        nextType = NodeType.Exit;
    }

    public void SetNext(int nID, NodeType type)
    {
        if(nID >= 0 && type != NodeType.Exit && type != NodeType.Option)
        {            
            nextID = nID;
            nextType = type;
        }
    }
}

[System.Serializable]
public class ConditionNode
{
    protected int conditionID;

    protected int targetIDIfPassed = Dialogue.ExitDialogue;
    protected NodeType targetTypeIfPassed = NodeType.Exit;

    protected int targetIDIfFailed = Dialogue.ExitDialogue;
    protected NodeType targetTypeIfFailed = NodeType.Exit;

    public int ConditionID { get { return conditionID; } set { if (value >= 0) conditionID = value; } }

    public virtual bool ConditionTest()
    {
        return true;
    }

    public bool ConditionTest(out int targetID, out NodeType targetType)
    {
        bool result = ConditionTest();

        targetID =
            (result) ?
                targetIDIfPassed : targetIDIfFailed;

        targetType =
            (result) ?
                targetTypeIfPassed : targetTypeIfFailed;

        return result;
    }
}

public enum NodeType
{
    Exit,
    Node,
    Option,
    Condition
}