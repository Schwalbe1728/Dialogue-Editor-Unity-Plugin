using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

public class NodeEditor : EditorWindow
{
    Dialogue EditedDialogue;
    DialogueEditorInfo EditorInfo;
    List<DialogueNode> CurrentNodes;
    List<DialogueOption> CurrentOptions;
    List<ConditionNode> CurrentConditions;

    List<int> nodeToNodeToAttach = new List<int>();                 //Immediate node -> Dialogue Node

    List<int> nodeToOptionToAttach = new List<int>();               //Dialogue Node -> Option
    List<int> optionToNodeToAttach = new List<int>();               //Option -> Dialogue Node

    List<int> nodeToConditionToAttach = new List<int>();            //Immediate node -> Condition
    List<int> optionToConditionToAttach = new List<int>();          //Option -> Condition
    List<int> conditionSuccessToConditionToAttach = new List<int>();   //Condition -> [Condition Success] -> Condition
    List<int> conditionSuccessToNodeToAttach = new List<int>();     //Condition -> [Condition Success] -> Node
    List<int> conditionFailToConditionToAttach = new List<int>();   //Condition -> [Condition Failure] -> Condition
    List<int> conditionFailToNodeToAttach = new List<int>();        //Condition -> [Condition Failure] -> Node
    List<int> conditionToEntryOption = new List<int>();             //Condition -> Entry Condition value of Option    

    Vector2 scrollPosition = Vector2.zero;

    EditorConfigurationData config;

    private float scale = 1f;
    private float DragAreaMargin = 8f;

    private bool resizing = false;
    private int windowResizedIndex = -1;

    List<string> debugMessages = new List<string>();
    int selectedDebugMessage = -1;

    [MenuItem("Window/Node editor")]
    static void ShowEditor()
    {
        NodeEditor editor = EditorWindow.GetWindow<NodeEditor>(); 
        editor.wantsMouseMove = true;
        //InitStyles();
        Selection.selectionChanged -= editor.OnEditorSelectionChanged;
        Selection.selectionChanged += editor.OnEditorSelectionChanged;

        editor.OnEditorSelectionChanged();
    }        

    void OnFocus()
    {
        Selection.selectionChanged -= OnEditorSelectionChanged;
        Selection.selectionChanged += OnEditorSelectionChanged;
    }

    void OnDestroy()
    {
        SaveChanges("Node Editor On Destroy");
        Selection.selectionChanged -= OnEditorSelectionChanged;        
    }

    void OnEditorSelectionChanged()
    {
        //Debug.Log("ChangedSelection");

        resizing = false;

        if (Selection.activeObject is Dialogue)
        {
            EditedDialogue = Selection.activeObject as Dialogue;
            EditorInfo = EditedDialogue.EditorInfo;

            if (EditorInfo == null)
            {
                EditorInfo = new DialogueEditorInfo();
            }
            else 
            {
                EditorInfo.RestoreFoldouts(EditedDialogue.GetAllNodes());
            }

            Repaint();
        }        
    }

    void OnGUI()
    {        
        if(EditedDialogue == null || EditorInfo == null)
        {
            GUILayout.Label("Please, select a Dialogue to edit!", EditorStyles.boldLabel);
            return;
        }        

        CurrentNodes = new List<DialogueNode>(EditedDialogue.GetAllNodes());
        CurrentOptions = new List<DialogueOption>(EditedDialogue.GetAllOptions());
        CurrentConditions = new List<ConditionNode>(EditedDialogue.GetAllConditions());

        if(config == null)
        {
            config = new EditorConfigurationData();
        }               
                  
        GUILayout.BeginArea(new Rect(5, 5, position.width-10, 20));
        {
            DrawEditorMenu();
        }
        GUILayout.EndArea();

        DrawDebug();
        /*
        GUILayout.BeginArea(new Rect(5, 30, 150, 20));
        {
            DrawZoom();
        }
        GUILayout.EndArea();
        */              

        GUILayout.BeginArea(new Rect(5, 30, position.width - 10, position.height - 35 - 20), config.EditorAreaBackgroundStyle);
        {            
            DrawEditorArea();
        }
        GUILayout.EndArea();
    }

    void SaveChanges(string undoTitle)
    {
        if (EditedDialogue != null)
        {
            //Undo.RecordObject(EditedDialogue, undoTitle);

            EditedDialogue.SetAllNodes(CurrentNodes);
            EditedDialogue.SetAllOptions(CurrentOptions);
            EditedDialogue.SetAllConditions(CurrentConditions);
            EditedDialogue.EditorInfo = EditorInfo;

            EditorUtility.SetDirty(EditedDialogue);
            //AssetDatabase.SaveAssets();
            //Debug.Log("Saving changes");
        }
    }

    void UpdateCurves()
    {
        Event currentEvent = Event.current;
        Rect cursorRect = new Rect(currentEvent.mousePosition, new Vector2(5, 5));

        foreach(Rect win in EditorInfo.Windows)
        {
            if(win.Contains(currentEvent.mousePosition))
            {
                cursorRect = win;
            }
        }

        bool repaint = false;
        bool save = false;

        #region Immediate Node connections creation
        if (nodeToNodeToAttach.Count == 1)
        {            
            int from = EditorInfo.NodesIndexes[nodeToNodeToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.ImmidiateNodeConnection);
            repaint = true;                       
        }
        else
        {            
            if (nodeToNodeToAttach.Count == 2)
            {
                DialogueNode node = CurrentNodes[nodeToNodeToAttach[0]];
                node.MakeImmediateNode();
                if( nodeToNodeToAttach[1] == Dialogue.ExitDialogue )
                {
                    node.SetImmediateNodeTargetExit();
                }
                else
                {
                    node.SetImmediateNodeTarget(nodeToNodeToAttach[1], NodeType.Node);
                }

                nodeToNodeToAttach.Clear();
                nodeToConditionToAttach.Clear();
                nodeToOptionToAttach.Clear();
                save = true;
            }
        }
        #endregion
        #region Node To Option Connections creation
        if (nodeToOptionToAttach.Count == 1)
        {
            int from = EditorInfo.NodesIndexes[nodeToOptionToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.NodeToOptionConnection);
            repaint = true;
        }
        else
        {
            if (nodeToOptionToAttach.Count == 2)
            {
                DialogueNode node = CurrentNodes[nodeToOptionToAttach[0]];
                List<int> optionsAttachedToNode = new List<int>(node.OptionsAttached);

                if(!EditorInfo.NodesOptionsFoldouts.ContainsKey(node.NodeID))
                {
                    EditorInfo.NodesOptionsFoldouts.Add(node.NodeID, new Dictionary<int, bool>());
                }

                if(!optionsAttachedToNode.Contains(nodeToOptionToAttach[1]))
                {
                    optionsAttachedToNode.Add(nodeToOptionToAttach[1]);
                    node.OptionsAttached = optionsAttachedToNode.ToArray();

                    EditorInfo.NodesOptionsFoldouts[node.NodeID].Add(nodeToOptionToAttach[1], false);
                }

                nodeToOptionToAttach.Clear();
                save = true;
            }
        }
        #endregion
        #region Option To Node Connection creation
        if (optionToNodeToAttach.Count == 1)
        {
            int from = EditorInfo.OptionsIndexes[optionToNodeToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.OptionToNodeConnection);
            repaint = true;
        }
        else
        {
            if (optionToNodeToAttach.Count == 2)
            {
                DialogueOption option = CurrentOptions[optionToNodeToAttach[0]];

                if (optionToNodeToAttach[1] != Dialogue.ExitDialogue)
                {
                    option.SetNext(optionToNodeToAttach[1], NodeType.Node);
                }
                else
                {
                    option.SetNextNodeExit();
                }

                optionToNodeToAttach.Clear();
                save = true;
            }
        }
        #endregion
        
        #region Conditions Connections
        #region Immediate Node -> Condition

        if(nodeToConditionToAttach.Count == 1)
        {
            int from = EditorInfo.NodesIndexes[nodeToConditionToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.ToConditionConnection);
            repaint = true;
        }
        else
        {
            if(nodeToConditionToAttach.Count == 2)
            {
                DialogueNode nodeFrom = CurrentNodes[nodeToConditionToAttach[0]];
                nodeFrom.MakeImmediateNode();
                nodeFrom.SetImmediateNodeTarget(nodeToConditionToAttach[1], NodeType.Condition);

                nodeToConditionToAttach.Clear();
                nodeToNodeToAttach.Clear();
                nodeToOptionToAttach.Clear();
            }
        }

        #region Option -> Condtion
        if(optionToConditionToAttach.Count == 1)
        {
            int from = EditorInfo.OptionsIndexes[optionToConditionToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.ToConditionConnection);
            repaint = true;
        }
        else
        {
            if(optionToConditionToAttach.Count == 2)
            {
                DialogueOption optionFrom = CurrentOptions[optionToConditionToAttach[0]];
                optionFrom.SetNext(optionToConditionToAttach[1], NodeType.Condition);

                optionToConditionToAttach.Clear();
                optionToNodeToAttach.Clear();                
            }
        }
        #endregion

        #endregion
        #region Condition -> [Success] -> Condition

        if (conditionSuccessToConditionToAttach.Count == 1)
        {
            int from = EditorInfo.ConditionsIndexes[conditionSuccessToConditionToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.FromSuccesConnection);
            repaint = true;
        }
        else
        {
            if (conditionSuccessToConditionToAttach.Count == 2)
            {
                ConditionNode nodeFrom = CurrentConditions[conditionSuccessToConditionToAttach[0]];
                nodeFrom.SetSuccessTarget(conditionSuccessToConditionToAttach[1], NodeType.Condition);

                conditionSuccessToConditionToAttach.Clear();
            }
        }

        #endregion
        #region Condition -> [Success] -> Node

        if (conditionSuccessToNodeToAttach.Count == 1)
        {
            int from = EditorInfo.ConditionsIndexes[conditionSuccessToNodeToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.FromSuccesConnection);
            repaint = true;
        }
        else
        {
            if (conditionSuccessToNodeToAttach.Count == 2)
            {
                ConditionNode nodeFrom = CurrentConditions[conditionSuccessToNodeToAttach[0]];
                nodeFrom.SetSuccessTarget(conditionSuccessToNodeToAttach[1], NodeType.Node);

                conditionSuccessToNodeToAttach.Clear();
            }
        }

        #endregion
        #region Condition -> [Failure] -> Condition

        if (conditionFailToConditionToAttach.Count == 1)
        {
            int from = EditorInfo.ConditionsIndexes[conditionFailToConditionToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.FromFailureConnection);
            repaint = true;
        }
        else
        {
            if (conditionFailToConditionToAttach.Count == 2)
            {
                ConditionNode nodeFrom = CurrentConditions[conditionFailToConditionToAttach[0]];
                nodeFrom.SetFailureTarget(conditionFailToConditionToAttach[1], NodeType.Condition);

                conditionFailToConditionToAttach.Clear();
            }
        }

        #endregion
        #region Condition -> [Failure] -> Node

        if (conditionFailToNodeToAttach.Count == 1)
        {
            int from = EditorInfo.ConditionsIndexes[conditionFailToNodeToAttach[0]];

            DrawNodeCurve(EditorInfo.Windows[from], cursorRect, config.FromFailureConnection);
            repaint = true;
        }
        else
        {
            if (conditionFailToNodeToAttach.Count == 2)
            {
                ConditionNode nodeFrom = CurrentConditions[conditionFailToNodeToAttach[0]];
                nodeFrom.SetFailureTarget(conditionFailToNodeToAttach[1], NodeType.Node);

                conditionFailToNodeToAttach.Clear();
            }
        }

        #endregion
        #region Condition -> Entry Option

        if (conditionToEntryOption.Count == 1)
        {
            int to = EditorInfo.ConditionsIndexes[conditionToEntryOption[0]];

            DrawNodeCurve(cursorRect, EditorInfo.Windows[to], config.EntryConditionConnection);
            repaint = true;
        }
        else
        {
            if (conditionToEntryOption.Count == 2)
            {
                ConditionNode nodeTo = CurrentConditions[conditionToEntryOption[0]];
                DialogueOption optionFrom = CurrentOptions[conditionToEntryOption[1]];
                //nodeFrom.SetFailureTarget(conditionToFailConditionToAttach[1], NodeType.Condition);

                optionFrom.SetEntryCondition(nodeTo);                

                conditionToEntryOption.Clear();
            }
        }

        #endregion
        #endregion

        #region Drawing established connections TOFINISH
        foreach (DialogueNode nodeFrom in CurrentNodes)
        {
            int from = EditorInfo.NodesIndexes[nodeFrom.NodeID];

            if(nodeFrom.ImmediateNode)
            {
                int targID;
                NodeType targType;

                nodeFrom.GetTarget(out targID, out targType);

                if (targID == Dialogue.ExitDialogue) continue;

                if (targType == NodeType.Node)
                {
                    int to = EditorInfo.NodesIndexes[targID];
                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.ImmidiateNodeConnection);
                }

                if(targType == NodeType.Condition)
                {
                    int to = EditorInfo.ConditionsIndexes[targID];
                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.ToConditionConnection);
                }
            }
            else
            {                
                foreach(int optInd in nodeFrom.OptionsAttached)
                {
                    int to = EditorInfo.OptionsIndexes[optInd];
                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.NodeToOptionConnection);
                }
            }
        }

        foreach(DialogueOption optionFrom in CurrentOptions)
        {            
            int from = EditorInfo.OptionsIndexes[optionFrom.OptionID];
            
            if(optionFrom.EntryConditionSet)
            {
                int entryCondition = EditorInfo.ConditionsIndexes[optionFrom.EntryCondition.ConditionID];
                DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[entryCondition], config.EntryConditionConnection);
            }

            int to = -1;  
                                 
            switch(optionFrom.NextType)
            {
                case NodeType.Node:
                    to = EditorInfo.NodesIndexes[optionFrom.NextID];
                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.OptionToNodeConnection);
                    break;

                case NodeType.Condition:
                    to = EditorInfo.ConditionsIndexes[optionFrom.NextID];
                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.ToConditionConnection);
                    break;
            }
        }

        foreach(ConditionNode conditionFrom in CurrentConditions)
        {
            int from = EditorInfo.ConditionsIndexes[conditionFrom.ConditionID];
            int to = Dialogue.ExitDialogue;

            switch(conditionFrom.SuccessTargetType)
            {
                case NodeType.Condition:
                    to = EditorInfo.ConditionsIndexes[conditionFrom.SuccessTarget];
                    break;

                case NodeType.Node:
                    to = EditorInfo.NodesIndexes[conditionFrom.SuccessTarget];
                    break;
            }

            if(to != Dialogue.ExitDialogue)
            {
                DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.FromSuccesConnection);
            }

            to = Dialogue.ExitDialogue;

            switch (conditionFrom.FailureTargetType)
            {
                case NodeType.Condition:
                    to = EditorInfo.ConditionsIndexes[conditionFrom.FailureTarget];
                    break;

                case NodeType.Node:
                    to = EditorInfo.NodesIndexes[conditionFrom.FailureTarget];
                    break;
            }

            if (to != Dialogue.ExitDialogue)
            {
                DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.FromFailureConnection);
            }
        }

        #endregion

        if (repaint)
        {
            Repaint();
        }

        if(save)
        {
            SaveChanges("Update Curves");
        }
    }

    [System.Obsolete]
    void DrawZoom()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Zoom: ");
        GUILayout.Label((scale * 100).ToString("n0") + "%");
        
        if(GUILayout.Button("+") && scale < 2f)
        {
            scale += 0.05f;
        }

        if (GUILayout.Button("-") && scale > 0.1)
        {
            scale -= 0.05f;
        }

        GUILayout.EndHorizontal();
    }

    void DrawEditorArea()
    {
        Bounds border = new Bounds();

        for (int i = 0; i < EditorInfo.Windows.Count; i++)
        {
            border.Encapsulate(EditorInfo.Windows[i].max);
            border.Encapsulate(EditorInfo.Windows[i].min);
        }

        scrollPosition =
            EditorGUILayout.BeginScrollView(
                    scrollPosition,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true)
                );
        {
            GUILayout.Box("", config.BoundingBoxStyle, GUILayout.Width(border.size.x), GUILayout.Height(border.size.y));

            BeginWindows();
            {
                GUIUtility.ScaleAroundPivot(Vector2.one * scale, scrollPosition + position.size / 2);                

                for (int i = 0; i < EditorInfo.Windows.Count; i++)
                {
                    bool isNode = EditorInfo.WindowTypes[i] == NodeType.Node;
                    bool isOption = EditorInfo.WindowTypes[i] == NodeType.Option;

                    if (isNode)
                    {                        
                        EditorInfo.Windows[i] =
                            GUILayout.Window(i, EditorInfo.Windows[i], DrawNodeWindow, CurrentNodes[EditorInfo.NodeTypesIDs[i]].CustomID);
                    }
                    else
                    {
                        if (isOption)
                        {
                            EditorInfo.Windows[i] =
                                GUILayout.Window(i, EditorInfo.Windows[i], DrawOptionWindow, "Option " + EditorInfo.NodeTypesIDs[i]);
                        }
                        else
                        {
                            EditorInfo.Windows[i] =
                                GUILayout.Window(i, EditorInfo.Windows[i], DrawConditionWindow, "Condition " + EditorInfo.NodeTypesIDs[i]);
                        }
                    }
                }

                UpdateCurves();
            }
            //GUIUtility.ScaleAroundPivot(Vector2.one, scrollPosition);
            EndWindows();
        }
        EditorGUILayout.EndScrollView();
    }    

    void DrawDebug()
    {
        if (debugMessages.Count > 0 && selectedDebugMessage >= 0)
        {
            GUILayout.BeginArea(new Rect(10, position.height - 20, position.width - 20, 20));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(debugMessages[selectedDebugMessage], GUILayout.Width(0.7f * (position.width-20)), GUILayout.MaxWidth(position.width-20-40));                    
                    
                    if(GUILayout.Button("+", GUILayout.Height(15)) && selectedDebugMessage < debugMessages.Count -1 )
                    {
                        selectedDebugMessage++;
                    }

                    if (GUILayout.Button("-", GUILayout.Height(15)) && selectedDebugMessage > 0)
                    {
                        selectedDebugMessage--;
                    }                                       
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
    }

    void DrawEditorMenu()
    {
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Create Dialogue Node"))
            {
                DialogueNode newNode = new DialogueNode();
                newNode.NodeID = EditorInfo.Nodes;
                CurrentNodes.Add(newNode);

                EditorInfo.Windows.Add(new Rect(10 + scrollPosition.x, 10 + scrollPosition.y, 200, 5));
                EditorInfo.WindowTypes.Add(NodeType.Node);
                EditorInfo.NodeTypesIDs.Add(EditorInfo.Nodes++);
                EditorInfo.NodesIndexes.Add(EditorInfo.Windows.Count - 1);                
                                
                WriteDebug("Adding node");

                SaveChanges("Create Dialogue Node");
            }

            if (GUILayout.Button("Create Dialogue Option"))
            {
                DialogueOption newOption = new DialogueOption();
                newOption.OptionID = EditorInfo.Options;
                CurrentOptions.Add(newOption);

                EditorInfo.Windows.Add(new Rect(10 + scrollPosition.x, 10 + scrollPosition.y, 200, 5));
                EditorInfo.WindowTypes.Add(NodeType.Option);
                EditorInfo.NodeTypesIDs.Add(EditorInfo.Options++);
                EditorInfo.OptionsIndexes.Add(EditorInfo.Windows.Count - 1);

                WriteDebug("Adding option");

                SaveChanges("Create Dialogue Option");
            }

            if(GUILayout.Button("Create Condition Node"))
            {
                ConditionNode newCondition = new ConditionNode();                                
                newCondition.ConditionID = EditorInfo.Conditions;                
                CurrentConditions.Add(newCondition);

                EditorInfo.Windows.Add(new Rect(10 + scrollPosition.x, 10 + scrollPosition.y, 200, 5));
                EditorInfo.WindowTypes.Add(NodeType.Condition);
                EditorInfo.NodeTypesIDs.Add(EditorInfo.Conditions++);
                EditorInfo.ConditionsIndexes.Add(EditorInfo.Windows.Count - 1);

                if (newCondition.SetRandomizerCondition(0, 10, 5) == null) Debug.Log("Uh-oh...");
                

                SaveChanges("Create Condition Node");
            }

            if (GUILayout.Button("Sort"))
            {
                WriteDebug("Not Implemented Function");
            }

            if (GUILayout.Button("Configure"))
            {
                ConfigurationWindow.ShowConfigMenu(config, this.Repaint);
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawResizeButtons(int id)
    {
        if(resizing)
        {
            if (windowResizedIndex == id)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    Rect tempWindow = EditorInfo.Windows[id];
                    tempWindow.size += Event.current.delta;
                    EditorInfo.Windows[id] = tempWindow;

                    Repaint();
                }

                resizing = EditorGUILayout.ToggleLeft("Resize", resizing, GUILayout.Width(80));
            }
        }
        else
        {
            resizing = EditorGUILayout.ToggleLeft("Resize", resizing, GUILayout.Width(80));

            if(resizing)
            {
                windowResizedIndex = id;
            }
        }
    }

    void DrawOptionWindow(int id)
    {
        int typeid = EditorInfo.NodeTypesIDs[id];
        DialogueOption currentOption = CurrentOptions[typeid];
        //bool save = false;

        #region Option's Target Setting
        if (nodeToOptionToAttach.Count == 1)
        {
            DialogueNode nodeAwaiting = CurrentNodes[nodeToOptionToAttach[0]];
            List<int> optionsAttached = new List<int>(nodeAwaiting.OptionsAttached);

            if(!optionsAttached.Contains(typeid) && GUILayout.Button("Connect"))
            {
                nodeToOptionToAttach.Add(typeid);
                if(nodeToNodeToAttach.Count > 0)
                {
                    nodeAwaiting.RevertToRegularNode();
                }
                nodeToNodeToAttach.Clear();
                nodeToConditionToAttach.Clear();
            }
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Next Node: ");            
            
            bool tempCont = currentOption.NextType != NodeType.Exit;
            string destination =
                (tempCont) ?
                    currentOption.NextID.ToString() :
                    "[EXIT]";
            if (tempCont)
            {
                GUILayout.Label(destination);

                Rect focus =
                    (currentOption.NextType == NodeType.Node) ?
                        EditorInfo.Windows[EditorInfo.NodesIndexes[currentOption.NextID]] :
                        EditorInfo.Windows[EditorInfo.ConditionsIndexes[currentOption.NextID]];

                DrawJumpToButton("Go To", focus, GUILayout.Width(50));
            }
            else
            {
                GUILayout.Label(destination, GUILayout.Width(50));
            }

            if(tempCont)
            {
                if(GUILayout.Button("Clear"))
                {
                    currentOption.SetNextNodeExit();
                }
            }
            else
            {
                if (optionToNodeToAttach.Count == 0 && optionToConditionToAttach.Count == 0)
                {
                    if (!tempCont)
                    {
                        if (GUILayout.Button("Set"))
                        {
                            optionToNodeToAttach.Add(typeid);
                            optionToConditionToAttach.Add(typeid);

                            nodeToOptionToAttach.Clear();
                            nodeToNodeToAttach.Clear();
                            nodeToConditionToAttach.Clear();
                        }
                    }
                    else
                    {
                        if(GUILayout.Button("Clear"))
                        {
                            currentOption.SetNextNodeExit();
                        }
                    }
                }
                else
                {
                    if(GUILayout.Button("Cancel"))
                    {
                        optionToNodeToAttach.Clear();
                        optionToConditionToAttach.Clear();
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        #endregion
        #region Visit Once

        currentOption.VisitOnce = EditorGUILayout.Toggle("Visit Once: ", currentOption.VisitOnce);

        #endregion
        #region Connect Entry Condition
          
        if (!AnyConditionAwaitingConnection())
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Entry Condition: ");

                if (!currentOption.EntryConditionSet)
                {
                    GUILayout.Label("[none]");
                }
                else
                {
                    DrawJumpToButton("Go To", EditorInfo.Windows[EditorInfo.ConditionsIndexes[currentOption.EntryCondition.ConditionID]]);

                    if (GUILayout.Button("x"))
                    {
                        currentOption.ClearEntryCondition();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            if(!currentOption.EntryConditionSet && GUILayout.Button("Connect As Entry Condition"))
            {
                conditionToEntryOption.Add(typeid);

                conditionFailToConditionToAttach.Clear();
                conditionFailToNodeToAttach.Clear();
                conditionSuccessToConditionToAttach.Clear();
                conditionSuccessToNodeToAttach.Clear();                
            }
        }                
        #endregion
        #region Option Text        
        string prev = currentOption.OptionText;
        
        currentOption.OptionText =
            EditorGUILayout.TextArea(currentOption.OptionText,
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(config.MinTextAreaHeight), GUILayout.MaxHeight(config.MaxTextAreaHeight),
                GUILayout.ExpandWidth(false), GUILayout.Width(EditorInfo.Windows[id].width - 2 * DragAreaMargin)
                );

        //save = save || ( currentOption.OptionText != null && !currentOption.OptionText.Equals(prev));
        #endregion

        GUILayout.BeginHorizontal();
        {
            DrawResizeButtons(id);

            if(GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                DeleteOptionWindow(id, typeid);
                SaveChanges("Delete dialogue option");
                return;
            }
        }
        GUILayout.EndHorizontal();

        if (!resizing)
        {
            Vector2 margin = new Vector2(DragAreaMargin, DragAreaMargin);
            GUI.DragWindow(new Rect(Vector2.zero, EditorInfo.Windows[id].size - margin));
        }

        if(/*save*/ GUI.changed )
        {
            SaveChanges("Draw Option Window");
        }
    }    

    void DrawNodeWindow(int id)
    {
        int typeid = EditorInfo.NodeTypesIDs[id];
        DialogueNode currentNode = CurrentNodes[typeid];

        bool save = false;

        #region Connecting Buttons

        if(AnyConditionAwaitingConnection())
        {
            if (conditionFailToNodeToAttach.Count == 1 && GUILayout.Button("Connect Node To Condition Failure"))
            {
                conditionFailToNodeToAttach.Add(typeid);

                conditionFailToConditionToAttach.Clear();
                conditionSuccessToConditionToAttach.Clear();
                conditionSuccessToNodeToAttach.Clear();
                conditionToEntryOption.Clear();                
            }

            if(conditionSuccessToNodeToAttach.Count == 1 && GUILayout.Button("Connect Node To Condition Success"))
            {
                conditionSuccessToNodeToAttach.Add(typeid);

                conditionFailToNodeToAttach.Clear();
                conditionFailToConditionToAttach.Clear();
                conditionSuccessToConditionToAttach.Clear();                
                conditionToEntryOption.Clear();
            }
        }

        if (optionToNodeToAttach.Count == 0)
        {
            if (nodeToNodeToAttach.Count == 0 && nodeToOptionToAttach.Count == 0)
            {
                if (!currentNode.ImmediateNode)
                {
                    if (!AnyConditionAwaitingConnection() && GUILayout.Button("Connect Node"))
                    {
                        //List<int> optsAttached = new List<int>(currentNode.OptionsAttached);

                        nodeToNodeToAttach.Add(typeid);
                        nodeToOptionToAttach.Add(typeid);
                        nodeToConditionToAttach.Add(typeid);
                    }
                }
                else
                {
                    GUIHelpers.GUIHorizontal(
                        delegate ()
                        {
                            GUILayout.Label("Next Node:");

                            int nxt;
                            NodeType nxtType;
                            currentNode.GetTarget(out nxt, out nxtType);

                            bool nextIsExit = nxt == Dialogue.ExitDialogue;
                            string nextString = (nextIsExit) ? "[EXIT]" : nxt.ToString();

                            GUILayout.Label(nextString);

                            if (!nextIsExit)
                            {
                                Rect focus =
                                    (nxtType == NodeType.Node) ?
                                        EditorInfo.Windows[EditorInfo.NodesIndexes[nxt]] :
                                        ((nxtType == NodeType.Exit) ? EditorInfo.Windows[EditorInfo.ConditionsIndexes[nxt]] :
                                            new Rect(0, 0, 1, 1)
                                        );
                                DrawJumpToButton("Go To", focus, GUILayout.Width(50));
                            }

                            if (GUILayout.Button("Clear"))
                            {
                                //nodeToNodeAttached.Remove(typeid);
                                currentNode.RevertToRegularNode();
                                save = true;
                            }
                        }
                        );
                }
            }
            else
            {
                if (nodeToNodeToAttach.Count == 1)
                {
                    int nextId; NodeType nextType;
                    currentNode.GetTarget(out nextId, out nextType);

                    if (nodeToNodeToAttach[0] != typeid && 
                        (!currentNode.ImmediateNode || nextId != nodeToNodeToAttach[0] ))
                    {
                        if (GUILayout.Button("Connect As Immediate Node"))
                        {
                            nodeToNodeToAttach.Add(typeid);
                            nodeToOptionToAttach.Clear();
                        }
                    }
                    else
                    {                        
                        if (nodeToNodeToAttach[0] == typeid)
                        {
                            GUIHelpers.GUIHorizontal(
                              delegate ()
                              {
                                  if (GUILayout.Button("Cancel Connection"))
                                  {
                                      nodeToNodeToAttach.Clear();
                                      nodeToOptionToAttach.Clear();
                                      nodeToConditionToAttach.Clear();
                                  }

                                  if (GUILayout.Button("Exits Dialogue"))
                                  {
                                      nodeToNodeToAttach.Add(-1);
                                      nodeToOptionToAttach.Clear();
                                      nodeToConditionToAttach.Clear();
                                  }
                              }
                              );
                        }
                    }
                }
                else
                {
                    if(GUILayout.Button("Cancel Connection"))
                    {
                        nodeToNodeToAttach.Clear();
                        nodeToOptionToAttach.Clear();
                        nodeToConditionToAttach.Clear();
                    }
                }
            }
        }
        else
        {
            if(optionToNodeToAttach.Count == 1)
            {
                if(GUILayout.Button("Make Option's Target"))
                {
                    optionToNodeToAttach.Add(typeid);
                }
            }
        }
        #endregion
        
        /*
        if (GUILayout.Button("Delete"))
        {
            DeleteNodeWindow(id, typeid);
            SaveChanges("Delete Node");
            return;
        }
        */
        string prevText = currentNode.Text;        

        currentNode.Text =
            EditorGUILayout.TextArea(
                currentNode.Text,
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(config.MinTextAreaHeight), GUILayout.MaxHeight(config.MaxTextAreaHeight),
                GUILayout.ExpandWidth(false), GUILayout.Width(EditorInfo.Windows[id].width - 2 * DragAreaMargin)
                );

        save = save || (currentNode.Text != null && !currentNode.Text.Equals(prevText));

        #region Opcje Dialogowe

        if (!currentNode.ImmediateNode)
        {
            bool restoreFoldout = !EditorInfo.NodesOptionsFoldouts.ContainsKey(typeid);

            foreach(int optionIndex in currentNode.OptionsAttached)
            {
                if (restoreFoldout)
                {
                    EditorInfo.RestoreFoldouts(CurrentNodes.ToArray());
                    break;
                }

                restoreFoldout |= !EditorInfo.NodesOptionsFoldouts[typeid].ContainsKey(optionIndex);
            }                       
                    
            foreach (int optionIndex in currentNode.OptionsAttached)
            {
                if(!EditorInfo.NodesOptionsFoldouts.ContainsKey(typeid) || !EditorInfo.NodesOptionsFoldouts[typeid].ContainsKey(optionIndex))
                {
                    Debug.Log( "Zawiera type id " + typeid + ": " + EditorInfo.NodesOptionsFoldouts.ContainsKey(typeid));
                    Debug.Log("Zawiera option id " + optionIndex + ": " + EditorInfo.NodesOptionsFoldouts[typeid].ContainsKey(optionIndex));
                }

                EditorInfo.NodesOptionsFoldouts[typeid][optionIndex] =
                    EditorGUILayout.Foldout(
                        EditorInfo.NodesOptionsFoldouts[typeid][optionIndex],
                        "Option: " + optionIndex, true
                        );

                if (EditorInfo.NodesOptionsFoldouts[typeid][optionIndex])
                {
                    Rect foldoutRect = EditorGUILayout.BeginHorizontal(config.FoldoutInteriorStyle);
                    {
                        DialogueOption currentOption = CurrentOptions[optionIndex];

                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Option: ", GUILayout.Width(75));
                                GUILayout.Label(optionIndex.ToString());
                                GUILayout.FlexibleSpace();
                                DrawJumpToButton("Go To", EditorInfo.Windows[EditorInfo.OptionsIndexes[optionIndex]], GUILayout.Width(50));
                            }
                            GUILayout.EndHorizontal();                            

                            GUILayout.BeginHorizontal();
                            {                                                                                                
                                GUILayout.Label("Destination: ", GUILayout.Width(75));
                                if (currentOption.NextType == NodeType.Node)
                                {
                                    int to = currentOption.NextID; //optionToNodeAttached[optionIndex];
                                    GUILayout.Label(to.ToString());
                                    GUILayout.FlexibleSpace();
                                    DrawJumpToButton("Go To", EditorInfo.Windows[EditorInfo.NodesIndexes[to]], GUILayout.Width(50));
                                }
                                else
                                {
                                    GUILayout.Label("[EXIT]");
                                    GUILayout.FlexibleSpace();
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Text: ", GUILayout.Width(75));
                                StringBuilder textToShow = new StringBuilder(currentOption.OptionText);

                                if (textToShow.Length > config.MaxQuotasLength)
                                {
                                    textToShow.Length = config.MaxQuotasLength - 3;
                                    textToShow.Append("...");
                                }

                                GUILayout.Label("\"" + textToShow + "\"", config.WrappedLabelStyle);
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();

                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            List<int> optionsAttachedWithout =
                                new List<int>(currentNode.OptionsAttached);

                            optionsAttachedWithout.Remove(optionIndex);
                            currentNode.OptionsAttached = optionsAttachedWithout.ToArray();

                            EditorInfo.NodesOptionsFoldouts[typeid].Remove(optionIndex);

                            save = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }


        #endregion        

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        {
            DrawResizeButtons(id);            
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                DeleteNodeWindow(id, typeid);
                SaveChanges("Delete Node");
                return;
            }
        }
        GUILayout.EndHorizontal();

        if (!resizing)
        {
            Vector2 margin = new Vector2(DragAreaMargin, DragAreaMargin);
            GUI.DragWindow(new Rect(Vector2.zero, EditorInfo.Windows[id].size - margin));
        }

        if (save)
        {
            SaveChanges("Draw Node Window");
        }
    }    

    void DrawConditionWindow(int id)
    {
        int typeID = EditorInfo.NodeTypesIDs[id];
        ConditionNode currentCondition = CurrentConditions[typeID];

        if((AnyConditionAwaitingConnection() || nodeToConditionToAttach.Count == 1 || optionToConditionToAttach.Count == 1) && !ThisConditionAwaitingConnection(typeID))
        {
            if(nodeToConditionToAttach.Count == 1 && GUILayout.Button("Connect Node To Condition"))
            {
                nodeToConditionToAttach.Add(typeID);                
            }

            if (optionToConditionToAttach.Count == 1 && GUILayout.Button("Connect Option To Condition"))
            {
                optionToConditionToAttach.Add(typeID);
            }

            if (conditionFailToConditionToAttach.Count == 1 && GUILayout.Button("Connect Failure To Condition"))
            {
                conditionFailToConditionToAttach.Add(typeID);

                nodeToConditionToAttach.Clear();
                conditionToEntryOption.Clear();
                conditionFailToNodeToAttach.Clear();
            }

            if (conditionSuccessToConditionToAttach.Count == 1 && GUILayout.Button("Connect Success To Condition"))
            {
                conditionSuccessToConditionToAttach.Add(typeID);

                nodeToConditionToAttach.Clear();
                conditionToEntryOption.Clear();
                conditionSuccessToNodeToAttach.Clear();
            }
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Target If Success: ", GUILayout.Width(110));

            if(currentCondition.SuccessTargetType == NodeType.Exit)
            {
                GUILayout.Label("[EXIT]", GUILayout.Width(50));
            }
            else
            {
                string nodeType = currentCondition.SuccessTargetType.ToString(true);
                string targetId = currentCondition.SuccessTarget.ToString();

                //GUILayout.Label();
                int windowIndex =
                    (currentCondition.SuccessTargetType == NodeType.Condition) ?
                        EditorInfo.ConditionsIndexes[currentCondition.SuccessTarget] :
                            ((currentCondition.SuccessTargetType == NodeType.Node) ?
                                EditorInfo.NodesIndexes[currentCondition.SuccessTarget] :
                                EditorInfo.OptionsIndexes[currentCondition.SuccessTarget]
                            );

                DrawJumpToButton(nodeType + " " + targetId, EditorInfo.Windows[windowIndex]);
            }

            if(!AnyConditionAwaitingConnection() && GUILayout.Button("Set"))
            {
                conditionToEntryOption.Add(typeID);
                conditionSuccessToConditionToAttach.Add(typeID);
                conditionSuccessToNodeToAttach.Add(typeID);
            }

            if (ThisConditionSuccessAwaitingConnection(typeID) && GUILayout.Button("Set [EXIT]"))
            {
                ClearConditionToAttachLists();
                currentCondition.SetSuccessTarget(Dialogue.ExitDialogue, NodeType.Exit);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Target If Failure: ", GUILayout.Width(110));

            if (currentCondition.FailureTargetType == NodeType.Exit)
            {
                GUILayout.Label("[EXIT]", GUILayout.Width(50));
            }
            else
            {
                string nodeType = currentCondition.FailureTargetType.ToString(true);
                string targetId = currentCondition.FailureTarget.ToString();

                //GUILayout.Label(nodeType + " " + targetId);

                int windowIndex =
                        (currentCondition.FailureTargetType == NodeType.Condition) ?
                            EditorInfo.ConditionsIndexes[currentCondition.FailureTarget] :
                                ((currentCondition.FailureTargetType == NodeType.Node) ?
                                    EditorInfo.NodesIndexes[currentCondition.FailureTarget] :
                                    EditorInfo.OptionsIndexes[currentCondition.FailureTarget]
                                );

                DrawJumpToButton(nodeType + " " + targetId, EditorInfo.Windows[windowIndex]);
            }

            if (!AnyConditionAwaitingConnection() && GUILayout.Button("Set"))
            {
                conditionToEntryOption.Add(typeID);
                conditionFailToConditionToAttach.Add(typeID);
                conditionFailToNodeToAttach.Add(typeID);
            }

            if(ThisConditionFailAwaitingConnection(typeID) && GUILayout.Button("Set [EXIT]"))
            {
                ClearConditionToAttachLists();

                currentCondition.SetFailureTarget(Dialogue.ExitDialogue, NodeType.Exit);
            }
        }
        GUILayout.EndHorizontal();        

        if (ThisConditionAwaitingConnection(typeID) && GUILayout.Button("Cancel Connection"))
        {
            ClearConditionToAttachLists();
        }

        if(GUILayout.Button("Delete Condition"))
        {
            DeleteConditionWindow(id, typeID);
            SaveChanges("Delete Condition");
            return;
        }

        GUI.DragWindow();
    }

    void ClearConditionToAttachLists()
    {
        conditionSuccessToNodeToAttach.Clear();
        conditionSuccessToConditionToAttach.Clear();
        conditionFailToNodeToAttach.Clear();
        conditionFailToConditionToAttach.Clear();
        conditionToEntryOption.Clear();
    }

    bool AnyConditionAwaitingConnection()
    {
        return
            conditionToEntryOption.Count == 1 ||
            conditionSuccessToNodeToAttach.Count == 1 ||
            conditionSuccessToConditionToAttach.Count == 1 ||
            conditionFailToConditionToAttach.Count == 1 ||
            conditionFailToNodeToAttach.Count == 1;
    }

    bool ThisConditionAwaitingConnection(int idOfType)
    {
        return
            (conditionToEntryOption.Count == 1 && conditionToEntryOption[0] == idOfType) ||
            (conditionSuccessToNodeToAttach.Count == 1 && conditionSuccessToNodeToAttach[0] == idOfType) ||
            (conditionSuccessToConditionToAttach.Count == 1 && conditionSuccessToConditionToAttach[0] == idOfType) ||
            (conditionFailToConditionToAttach.Count == 1 && conditionFailToConditionToAttach[0] == idOfType) ||
            (conditionFailToNodeToAttach.Count == 1 && conditionFailToNodeToAttach[0] == idOfType);
    }

    bool ThisConditionSuccessAwaitingConnection(int idOfType)
    {
        return
            (conditionSuccessToNodeToAttach.Count == 1 && conditionSuccessToNodeToAttach[0] == idOfType) ||
            (conditionSuccessToConditionToAttach.Count == 1 && conditionSuccessToConditionToAttach[0] == idOfType);
    }

    bool ThisConditionFailAwaitingConnection(int idOfType)
    {
        return
            (conditionFailToConditionToAttach.Count == 1 && conditionFailToConditionToAttach[0] == idOfType) ||
            (conditionFailToNodeToAttach.Count == 1 && conditionFailToNodeToAttach[0] == idOfType);
    }

    void FocusOnRect(Rect rect)
    {
        scrollPosition = rect.center - new Vector2(position.width/2, position.height/2);
    }

    bool DrawJumpToButton(string label, Rect jumpToRect, params GUILayoutOption[] param)
    {
        bool result = GUILayout.Button(label, param);        

        if(result)
        {
            FocusOnRect(jumpToRect);
        }

        return result;
    }

    void DeleteOptionWindow(int id, int idOfType)
    {
        EditorInfo.Windows.RemoveAt(id);
        EditorInfo.WindowTypes.RemoveAt(id);
        EditorInfo.NodeTypesIDs.RemoveAt(id);
        EditorInfo.OptionsIndexes.RemoveAt(idOfType);
        EditorInfo.Options--;

        CurrentOptions.RemoveAt(idOfType);
        nodeToNodeToAttach.Clear();
        nodeToConditionToAttach.Clear();
        nodeToOptionToAttach.Clear();
        optionToNodeToAttach.Clear();
        ClearConditionToAttachLists();
        
        foreach(int nodeIndex in EditorInfo.NodesOptionsFoldouts.Keys)
        {
            if(EditorInfo.NodesOptionsFoldouts[nodeIndex].ContainsKey(idOfType))
            {
                EditorInfo.NodesOptionsFoldouts[nodeIndex].Remove(idOfType);
            }
        }

        for (int i = 0; i < EditorInfo.NodeTypesIDs.Count; i++)
        {
            if (EditorInfo.WindowTypes[i] == NodeType.Option && EditorInfo.NodeTypesIDs[i] > idOfType)
            {
                EditorInfo.NodeTypesIDs[i]--;
            }
        }

        for (int i = idOfType; i < EditorInfo.OptionsIndexes.Count; i++)
        {
            EditorInfo.OptionsIndexes[i]--;
        }

        for (int i = EditorInfo.NodesIndexes.FindIndex(x => x > id); i < EditorInfo.NodesIndexes.Count && i >= 0; i++)
        {
            EditorInfo.NodesIndexes[i]--;
        }

        for (int i = EditorInfo.ConditionsIndexes.FindIndex(x => x > id); i < EditorInfo.ConditionsIndexes.Count && i >= 0; i++)
        {
            EditorInfo.ConditionsIndexes[i]--;
        }

        foreach (DialogueOption opt in CurrentOptions)
        {
            if(opt.OptionID > idOfType)
            {
                opt.OptionID--;
            }
        }

        foreach(DialogueNode dialNode in CurrentNodes)
        {
            if (!dialNode.ImmediateNode && dialNode.OptionsAttached != null)
            {
                List<int> optionsAttached = new List<int>(dialNode.OptionsAttached);
                optionsAttached.Remove(idOfType);

                for (int i = 0; i < optionsAttached.Count; i++)
                {
                    if (optionsAttached[i] > idOfType)
                    {
                        optionsAttached[i]--;
                    }
                }

                dialNode.OptionsAttached = optionsAttached.ToArray();
            }
        }

        int[] keys = new int[EditorInfo.NodesOptionsFoldouts.Count];
        EditorInfo.NodesOptionsFoldouts.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            int[] optionsKeys = new int[EditorInfo.NodesOptionsFoldouts[keys[i]].Count];
            EditorInfo.NodesOptionsFoldouts[keys[i]].Keys.CopyTo(optionsKeys, 0);

            for (int j = 0; j < optionsKeys.Length; j++)
            {
                int key = optionsKeys[j];

                if (key > idOfType)
                {
                    bool value = EditorInfo.NodesOptionsFoldouts[keys[i]][key];
                    EditorInfo.NodesOptionsFoldouts[keys[i]].Remove(key);
                    key--;
                    EditorInfo.NodesOptionsFoldouts[keys[i]].Add(key, value);
                }
            }
        }

        WriteDebug("Deleting option " + idOfType + " and it's associations.");
    }

    void DeleteNodeWindow(int id, int idOfType)
    {
        EditorInfo.Windows.RemoveAt(id);
        EditorInfo.WindowTypes.RemoveAt(id);
        EditorInfo.NodeTypesIDs.RemoveAt(id);
        EditorInfo.NodesIndexes.RemoveAt(idOfType);
        EditorInfo.NodesOptionsFoldouts.Remove(idOfType);
        EditorInfo.Nodes--;

        CurrentNodes.RemoveAt(idOfType);
        nodeToNodeToAttach.Clear();
        nodeToConditionToAttach.Clear();
        nodeToOptionToAttach.Clear();
        optionToNodeToAttach.Clear();
        ClearConditionToAttachLists();

        for (int i = 0; i < EditorInfo.NodeTypesIDs.Count; i++)
        {
            if(EditorInfo.WindowTypes[i] == NodeType.Node && EditorInfo.NodeTypesIDs[i] > idOfType)
            {
                EditorInfo.NodeTypesIDs[i]--;
            }
        }

        for(int i = idOfType; i < EditorInfo.NodesIndexes.Count; i++)
        {
            EditorInfo.NodesIndexes[i]--;
        }

        for(int i = EditorInfo.OptionsIndexes.FindIndex(x => x > id); i < EditorInfo.OptionsIndexes.Count && i >= 0; i++)
        {
            EditorInfo.OptionsIndexes[i]--;
        }

        for (int i = EditorInfo.ConditionsIndexes.FindIndex(x => x > id); i < EditorInfo.ConditionsIndexes.Count && i >= 0; i++)
        {
            EditorInfo.ConditionsIndexes[i]--;
        }


        foreach (DialogueNode dialNode in CurrentNodes)
        {
            if(dialNode.NodeID > idOfType)
            {
                dialNode.NodeID--;
            }

            if(dialNode.ImmediateNode)
            {
                int targID; NodeType targType;
                dialNode.GetTarget(out targID, out targType);

                if(targType == NodeType.Node)
                {
                    if (targID > idOfType)
                    {
                        dialNode.SetImmediateNodeTarget(targID - 1, targType);
                    }
                    else
                    {
                        if(targID == idOfType)
                        {
                            dialNode.RevertToRegularNode();
                        }
                    }
                }
            }
        }

        int[] keys = new int[EditorInfo.NodesOptionsFoldouts.Count];
        EditorInfo.NodesOptionsFoldouts.Keys.CopyTo(keys, 0);
        for(int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            Dictionary<int, bool> tempVal = EditorInfo.NodesOptionsFoldouts[key];

            if (key > idOfType)
            {              
                EditorInfo.NodesOptionsFoldouts.Remove(key);
                key--;
                EditorInfo.NodesOptionsFoldouts.Add(key, tempVal);
            }
        }

        foreach(DialogueOption opt in CurrentOptions)
        {
            if(opt.NextType == NodeType.Node)
            {
                if(opt.NextID > idOfType)
                {
                    opt.SetNext(opt.NextID - 1, opt.NextType);
                }
                else
                {
                    if(opt.NextID == idOfType)
                    {
                        opt.SetNextNodeExit();
                    }
                }
            }
        }

        foreach(ConditionNode cond in CurrentConditions)
        {
            if(cond.SuccessTargetType == NodeType.Node)
            {
                if (cond.SuccessTarget == idOfType)
                {
                    cond.SetSuccessTarget(Dialogue.ExitDialogue, NodeType.Exit);
                }
                else
                {
                    if(cond.SuccessTarget > idOfType)
                    {
                        cond.SetSuccessTarget(cond.SuccessTarget - 1, NodeType.Node);
                    }
                }
            }

            if (cond.FailureTargetType == NodeType.Node)
            {
                if (cond.FailureTarget == idOfType)
                {
                    cond.SetFailureTarget(Dialogue.ExitDialogue, NodeType.Exit);
                }
                else
                {
                    if(cond.FailureTarget > idOfType)
                    {
                        cond.SetFailureTarget(cond.FailureTarget - 1, NodeType.Node);
                    }
                }
            }
        }

        WriteDebug("Deleting node " + idOfType + " and it's associations.");        
    }

    void DeleteConditionWindow(int id, int idOfType)
    {
        EditorInfo.Windows.RemoveAt(id);

        EditorInfo.WindowTypes.RemoveAt(id);
        EditorInfo.NodeTypesIDs.RemoveAt(id);
        EditorInfo.ConditionsIndexes.RemoveAt(idOfType);
        EditorInfo.Conditions--;

        CurrentConditions.RemoveAt(idOfType);
        nodeToNodeToAttach.Clear();
        nodeToConditionToAttach.Clear();

        nodeToOptionToAttach.Clear();
        optionToNodeToAttach.Clear();
        ClearConditionToAttachLists();

        for (int i = 0; i < EditorInfo.NodeTypesIDs.Count; i++)
        {
            if (EditorInfo.WindowTypes[i] == NodeType.Condition && EditorInfo.NodeTypesIDs[i] > idOfType)
            {
                EditorInfo.NodeTypesIDs[i]--;
            }
        }

        for (int i = idOfType; i < EditorInfo.ConditionsIndexes.Count; i++)
        {
            EditorInfo.ConditionsIndexes[i]--;
        }

        for (int i = EditorInfo.OptionsIndexes.FindIndex(x => x > id); i < EditorInfo.OptionsIndexes.Count && i >= 0; i++)
        {
            EditorInfo.OptionsIndexes[i]--;
        }

        for (int i = EditorInfo.NodesIndexes.FindIndex(x => x > id); i < EditorInfo.NodesIndexes.Count && i >= 0; i++)
        {
            EditorInfo.NodesIndexes[i]--;
        }

        foreach (DialogueOption option in CurrentOptions)
        {
            if (option.EntryConditionSet)
            {
                if (option.EntryCondition.ConditionID == idOfType)
                {
                    Debug.Log("Usuwanie połączenia");
                    option.ClearEntryCondition();
                }
            }

            if(option.NextType == NodeType.Condition)
            {
                if(option.NextID == idOfType)
                {
                    option.SetNextNodeExit();
                }
                else
                {
                    if(option.NextID > idOfType)
                    {
                        option.SetNext(option.NextID - 1, NodeType.Condition);
                    }
                }
            }
        }

        foreach(DialogueNode node in CurrentNodes)
        {
            int nxt; NodeType nxtType;
            node.GetTarget(out nxt, out nxtType);

            if(node.ImmediateNode && nxtType == NodeType.Condition)
            {
                if(nxt == idOfType)
                {
                    node.RevertToRegularNode();
                }
                else
                {
                    node.SetImmediateNodeTarget(nxt - 1, nxtType);
                }
            }
        }

        foreach (ConditionNode condition in CurrentConditions)
        {            
            if(condition.ConditionID > idOfType)
            {
                condition.ConditionID--;
                Debug.Log("zmniejszenie ID warunku");
            }

            if(condition.FailureTargetType == NodeType.Condition)
            {
                if (condition.FailureTarget > idOfType)
                {
                    condition.SetFailureTarget(condition.FailureTarget - 1, NodeType.Condition);
                }
                else
                {
                    if(condition.FailureTarget == idOfType)
                    {
                        condition.SetFailureTarget(Dialogue.ExitDialogue, NodeType.Exit);
                    }
                }
            }

            if(condition.SuccessTargetType == NodeType.Condition)
            {
                if(condition.SuccessTarget > idOfType)
                {
                    condition.SetSuccessTarget(condition.SuccessTarget - 1, NodeType.Condition);
                }
                else
                {
                    if(condition.SuccessTarget == idOfType)
                    {
                        condition.SetSuccessTarget(Dialogue.ExitDialogue, NodeType.Exit);
                    }
                }
            }
        }        
    }

    void DrawNodeCurve(Rect start, Rect end)
    {
        DrawNodeCurve(start, end, Color.black);
    }

    void DrawNodeCurve(Rect start, Rect end, Color curveColor)
    {
        Vector3 startPos;// = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos;// = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 tanStartMod;
        Vector3 tanEndMod;
        AlignCurve(start, end, out startPos, out endPos, out tanStartMod, out tanEndMod);        

        Vector3 startTan = startPos + tanStartMod * 50;
        Vector3 endTan = endPos + tanEndMod * 50;        

        float arrowSize = 10f;

        Handles.DrawBezier(startPos, endPos, startTan, endTan, curveColor, null, 2);
        Handles.color = curveColor;
        Handles.DrawSolidArc(endPos, Vector3.back, Quaternion.AngleAxis(22.5f, Vector3.forward) * tanEndMod , 45f , arrowSize);
    }

    void AlignCurve(Rect start, Rect end, out Vector3 startPos, out Vector3 endPos, out Vector3 startDir, out Vector3 endDir)
    {
        //TODO: zamienić na lepsze wykrywanie punktów na podstawie wzajemnego położenia
        //to jest rozwiązanie na pałę i na szybko

        List<Vector2> startRectPotentials = new List<Vector2>();

        if (config.DiagonalStartPoints)
        {
            startRectPotentials.Add(start.min);
            startRectPotentials.Add(new Vector2(start.x, start.y + start.height));
            startRectPotentials.Add(new Vector2(start.x + start.width, start.y));
            startRectPotentials.Add(start.max);
        }
        startRectPotentials.Add(new Vector2(start.x, start.center.y));
        startRectPotentials.Add(new Vector2(start.x + start.width, start.center.y));
        startRectPotentials.Add(new Vector2(start.center.x, start.y + start.height));
        startRectPotentials.Add(new Vector2(start.center.x, start.y));

        List<Vector2> endRectPotentials = new List<Vector2>();

        if (config.DiagonalEndPoints)
        {
            endRectPotentials.Add(end.min);
            endRectPotentials.Add(new Vector2(end.x, end.y + end.height));
            endRectPotentials.Add(new Vector2(end.x + end.width, end.y));
            endRectPotentials.Add(end.max);
        }
        endRectPotentials.Add(new Vector2(end.x, end.center.y));
        endRectPotentials.Add(new Vector2(end.x + end.width, end.center.y));
        endRectPotentials.Add(new Vector2(end.center.x, end.y + end.height));
        endRectPotentials.Add(new Vector2(end.center.x, end.y));
            

        int startPotCount = startRectPotentials.Count;
        int endPotCount = endRectPotentials.Count;

        float[,] distances = new float[startPotCount, endPotCount];
        float minDist = float.MaxValue;
        int minDistX = 999;
        int minDistY = 999;

        float curveModifier = 1f;

        for(int x = 0; x < startPotCount; x++)
        {
            for(int y = 0; y < endPotCount; y++ )
            {
                distances[x, y] =
                    Vector2.Distance(startRectPotentials[x], endRectPotentials[y]);

                if(minDist > curveModifier * distances[x,y])
                {
                    minDist = distances[x, y];
                    minDistX = x;
                    minDistY = y;
                }
            }
        }

        Vector2 startChosen = startRectPotentials[minDistX];
        Vector2 endChosen = endRectPotentials[minDistY];

        startPos = new Vector3(startChosen.x, startChosen.y);
        endPos = new Vector3(endChosen.x, endChosen.y);

        startChosen -= start.center;
        endChosen -= end.center;

        startDir = new Vector3(startChosen.x, startChosen.y);
        endDir = new Vector3(endChosen.x, endChosen.y);

        startDir.Normalize();
        endDir.Normalize();
    }

    void WriteDebug(string message, bool forceScroll = true)
    {
        debugMessages.Add( debugMessages.Count.ToString() + ": " + message);

        if(forceScroll)
        {
            selectedDebugMessage = debugMessages.Count - 1;
        }
    }    
}

public class EditorConfigurationData
{
    public bool ConfigurationOpened;

    public bool DiagonalStartPoints = false;
    public bool DiagonalEndPoints = false;

    public int MaxQuotasLength = 49;
    public int MaxTextAreaHeight = 150;
    public int MinTextAreaHeight = 50;

    public Color ImmidiateNodeConnection;
    public Color NodeToOptionConnection;
    public Color OptionToNodeConnection;

    public Color ToConditionConnection;
    public Color FromSuccesConnection;
    public Color FromFailureConnection;
    public Color EntryConditionConnection;

    public Color AreaBackgroundColor;    

    public GUIStyle BoundingBoxStyle = null;
    public GUIStyle EditorAreaBackgroundStyle = null;
    public GUIStyle TextAreaStyle = null;
    public GUIStyle FoldoutInteriorStyle = null;
    public GUIStyle WrappedLabelStyle = null;

    public Rect RectHandle;

    private EditorConfigurationData Defaults;
    //private bool Connections = false;
    //private bool Backgrounds = false;

    private string[] preferencesKeys =
        new string[]
        {
                "NODE EDITOR: immidiateConnection",
                "NODE EDITOR: nodeToOption",
                "NODE EDITOR: optionToNode",
                "NODE EDITOR: editorBackground",
                "NODE EDITOR: diagonalStart",
                "NODE EDITOR: diagonalEnd",
                "NODE EDITOR: conditionConnection",
                "NODE EDITOR: fromSuccess",
                "NODE EDITOR: fromFailure",
                "NODE EDITOR: entryCondition"
        };

    public string[] PreferencesKeys { get { return preferencesKeys; } }

    public EditorConfigurationData()
    {
        Defaults = new EditorConfigurationData(false);

        if (!PreferencesExist())
        {
            RestoreDefaults();
        }
        else
        {
            RestorePreferences();
        }

        RectHandle = new Rect(20 * Vector2.one, new Vector2(250, 30));
    }

    public void RestoreDefaults()
    {
        if (Defaults != null)
        {
            ImmidiateNodeConnection = Defaults.ImmidiateNodeConnection;
            NodeToOptionConnection = Defaults.NodeToOptionConnection;
            OptionToNodeConnection = Defaults.OptionToNodeConnection;

            ToConditionConnection = Defaults.ToConditionConnection;
            FromSuccesConnection = Defaults.FromSuccesConnection;
            FromFailureConnection = Defaults.FromFailureConnection;
            EntryConditionConnection = Defaults.EntryConditionConnection;

            AreaBackgroundColor = Defaults.AreaBackgroundColor;
            DiagonalEndPoints = false;
            DiagonalStartPoints = false;

            InitStyles(true);
        }
    }    

    public bool PreferencesExist()
    {
        bool result = true;

        foreach (string key in preferencesKeys)
        {
            result &= EditorPrefs.HasKey(key);
        }

        return result;
    }

    public void RestorePreferences()
    {
        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[0]), out ImmidiateNodeConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[1]), out NodeToOptionConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[2]), out OptionToNodeConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[3]), out AreaBackgroundColor))
        {
            Debug.Log("Wtf");
        }

        if(!TryParseFromString(EditorPrefs.GetString(preferencesKeys[6]), out ToConditionConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[7]), out FromSuccesConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[8]), out FromFailureConnection))
        {
            Debug.Log("Wtf");
        }

        if (!TryParseFromString(EditorPrefs.GetString(preferencesKeys[9]), out EntryConditionConnection))
        {
            Debug.Log("Wtf");
        }

        DiagonalStartPoints = EditorPrefs.GetBool(preferencesKeys[4], false);
        DiagonalEndPoints = EditorPrefs.GetBool(preferencesKeys[5], false);

        InitStyles(true);
    }

    public static string ColorToString(Color color)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(color.r);
        sb.Append("#");
        sb.Append(color.g);
        sb.Append("#");
        sb.Append(color.b);
        sb.Append("#");
        sb.Append(color.a);

        return sb.ToString();
    }

    public static bool TryParseFromString(string colorText, out Color col)
    {
        string[] seperated = colorText.Split(new char[] { '#' }, System.StringSplitOptions.RemoveEmptyEntries);

        float red = -1;
        float green = -1;
        float blue = -1;
        float alpha = -1;

        bool success = true;

        if ((success = float.TryParse(seperated[0], out red)))
        {
            if (success && (success = float.TryParse(seperated[1], out green)))
            {
                if (success && (success = float.TryParse(seperated[2], out blue)))
                {
                    if (success && (success = float.TryParse(seperated[3], out alpha)))
                    {

                    }
                }
            }
        }

        col = (success) ? new Color(red, green, blue, alpha) : new Color();

        return success;
    }    

    public void InitStyles(bool forced = false)
    {
        if (forced || BoundingBoxStyle == null)
        {
            BoundingBoxStyle = new GUIStyle();
            BoundingBoxStyle.normal.background = MakeTex(1, 1, new Color(0, 0, 0, 0));
        }

        if (forced || EditorAreaBackgroundStyle == null)
        {
            EditorAreaBackgroundStyle = new GUIStyle();
            EditorAreaBackgroundStyle.normal.background = MakeTex(1, 1, AreaBackgroundColor);
        }

        if (forced || TextAreaStyle == null)
        {
            TextAreaStyle = new GUIStyle(GUI.skin.textArea);
            TextAreaStyle.wordWrap = true;
            //textAreaStyle.fixedWidth = 200;           
        }

        if(forced || FoldoutInteriorStyle == null)
        {
            FoldoutInteriorStyle = new GUIStyle();
            FoldoutInteriorStyle.margin = new RectOffset(20, 5, 0, 0);
            FoldoutInteriorStyle.clipping = TextClipping.Clip;
            FoldoutInteriorStyle.stretchWidth = false;           
        }

        if(forced || WrappedLabelStyle == null)
        {
            WrappedLabelStyle = new GUIStyle();
            WrappedLabelStyle.wordWrap = true;
            WrappedLabelStyle.clipping = TextClipping.Clip;
            WrappedLabelStyle.fontStyle = FontStyle.Italic;
            WrappedLabelStyle.stretchWidth = false;            
        }
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private EditorConfigurationData(bool dummy)
    {
        ConfigurationOpened = dummy;

        ImmidiateNodeConnection = Color.red;
        NodeToOptionConnection = Color.black;
        OptionToNodeConnection = Color.blue;

        ToConditionConnection = Color.yellow;
        FromSuccesConnection = Color.yellow;
        FromFailureConnection = Color.yellow;
        EntryConditionConnection = Color.yellow;

        AreaBackgroundColor = new Color(0, 0.5f, 0, 0.4f);
    }
}
