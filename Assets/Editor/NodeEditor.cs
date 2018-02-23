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

    List<int> nodeToNodeToAttach = new List<int>();    

    List<int> nodeToOptionToAttach = new List<int>();
    List<int> optionToNodeToAttach = new List<int>();

    Vector2 scrollPosition = Vector2.zero;  

    EditorConfigurationData config;

    private float scale = 1f;

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

    void OnDestroy()
    {
        Selection.selectionChanged -= OnEditorSelectionChanged;
    }

    void OnEditorSelectionChanged()
    {
        //Debug.Log("ChangedSelection");

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

        EditedDialogue.SetAllNodes(CurrentNodes);
        EditedDialogue.SetAllOptions(CurrentOptions);
        EditedDialogue.EditorInfo = EditorInfo;

        EditorUtility.SetDirty(EditedDialogue); 
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
            }
        }
        
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
            }
        }

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
            }
        }

        foreach(DialogueNode nodeFrom in CurrentNodes)
        {
            int from = EditorInfo.NodesIndexes[nodeFrom.NodeID];

            if(nodeFrom.ImmediateNode)
            {
                int targID;
                NodeType targType;

                nodeFrom.GetTarget(out targID, out targType);

                if (targID == Dialogue.ExitDialogue) continue;
                if(targType == NodeType.Node)
                {
                    int to = EditorInfo.NodesIndexes[targID];

                    DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.ImmidiateNodeConnection);
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
                        
            if(optionFrom.NextType == NodeType.Node)
            {
                int to = EditorInfo.NodesIndexes[optionFrom.NextID];
                DrawNodeCurve(EditorInfo.Windows[from], EditorInfo.Windows[to], config.OptionToNodeConnection);
            }
        }

        if(repaint)
        {
            Repaint();
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

                    if (isNode)
                    {                        
                        EditorInfo.Windows[i] =
                            GUILayout.Window(i, EditorInfo.Windows[i], DrawNodeWindow, CurrentNodes[EditorInfo.NodeTypesIDs[i]].CustomID);
                    }
                    else
                    {
                        EditorInfo.Windows[i] =
                            GUILayout.Window(i, EditorInfo.Windows[i], DrawOptionWindow, "Option " + EditorInfo.NodeTypesIDs[i]);
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
        GUILayout.Space(15);
        GUILayout.BeginHorizontal();

        GUILayout.Label("Width: ", GUILayout.Width(100));
        if (GUILayout.Button("+"))
        {
            Rect temp = EditorInfo.Windows[id];

            temp.size += new Vector2(10, 0);
            EditorInfo.Windows[id] = temp;
        }
        if (GUILayout.Button("-"))
        {
            Rect temp = EditorInfo.Windows[id];

            temp.size += new Vector2(-10, 0);
            EditorInfo.Windows[id] = temp;
        }              

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Height: ", GUILayout.Width(100));

        if (GUILayout.Button("+"))
        {
            Rect temp = EditorInfo.Windows[id];

            temp.size += new Vector2(0, 10);
            EditorInfo.Windows[id] = temp;
        }

        if (GUILayout.Button("-"))
        {
            Rect temp = EditorInfo.Windows[id];

            temp.size += new Vector2(0, -10);
            EditorInfo.Windows[id] = temp;
        }
        GUILayout.EndHorizontal();
    }

    void DrawOptionWindow(int id)
    {
        int typeid = EditorInfo.NodeTypesIDs[id];
        DialogueOption currentOption = CurrentOptions[typeid];

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
            }
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Next Node: ", GUILayout.Width(80));            

            bool tempCont = currentOption.NextType != NodeType.Exit;
            string destination =
                (tempCont) ?
                    currentOption.NextID.ToString() :
                    "[EXIT]";
            if (tempCont)
            {
                Rect focus = EditorInfo.Windows[EditorInfo.NodesIndexes[currentOption.NextID]];
                DrawJumpToButton(destination, focus, GUILayout.Width(50));
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
                if (optionToNodeToAttach.Count == 0)
                {
                    if (!tempCont)
                    {
                        if (GUILayout.Button("Set"))
                        {
                            optionToNodeToAttach.Add(typeid);
                            nodeToOptionToAttach.Clear();
                            nodeToNodeToAttach.Clear();
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
                    }
                }
            }
        }
        GUILayout.EndHorizontal();

        currentOption.OptionText =
            EditorGUILayout.TextArea(currentOption.OptionText,
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(config.MinTextAreaHeight), GUILayout.MaxHeight(config.MaxTextAreaHeight),
                GUILayout.ExpandWidth(false), GUILayout.Width(EditorInfo.Windows[id].width - 10)
                );

        DrawResizeButtons(id);

        GUI.DragWindow();
    }

    void DrawNodeWindow(int id)
    {
        int typeid = EditorInfo.NodeTypesIDs[id];
        DialogueNode currentNode = CurrentNodes[typeid];

        #region Connecting Buttons

        if (optionToNodeToAttach.Count == 0)
        {
            if (nodeToNodeToAttach.Count == 0 && nodeToOptionToAttach.Count == 0)
            {
                if (!currentNode.ImmediateNode)
                {
                    if (GUILayout.Button("Connect Node"))
                    {
                        List<int> optsAttached = new List<int>(currentNode.OptionsAttached);

                        nodeToNodeToAttach.Add(typeid);
                        nodeToOptionToAttach.Add(typeid);
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
                                Rect focus = EditorInfo.Windows[EditorInfo.NodesIndexes[nxt]];
                                DrawJumpToButton("Go To", focus, GUILayout.Width(50));
                            }

                            if (GUILayout.Button("Clear"))
                            {
                                //nodeToNodeAttached.Remove(typeid);
                                currentNode.RevertToRegularNode();
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
                        GUIHelpers.GUIHorizontal(
                            delegate ()
                            {
                                if (GUILayout.Button("Cancel Connection"))
                                {
                                    nodeToNodeToAttach.Clear();
                                    nodeToOptionToAttach.Clear();
                                    //immidiateNodeDummy[typeid] = false;
                                }

                                if (GUILayout.Button("Exits Dialogue"))
                                {
                                    nodeToNodeToAttach.Add(-1);
                                    nodeToOptionToAttach.Clear();
                                }
                            }
                            );
                    }
                }
                else
                {
                    if(GUILayout.Button("Cancel Connection"))
                    {
                        nodeToNodeToAttach.Clear();
                        nodeToOptionToAttach.Clear();
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
        
        if (GUILayout.Button("Delete"))
        {
            DeleteNodeWindow(id, typeid);
            return;
        }               

        currentNode.Text =
            EditorGUILayout.TextArea(
                currentNode.Text,
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(config.MinTextAreaHeight), GUILayout.MaxHeight(config.MaxTextAreaHeight),
                GUILayout.ExpandWidth(false), GUILayout.Width(EditorInfo.Windows[id].width - 10)
                );

        #region Opcje Dialogowe

        if (!currentNode.ImmediateNode)
        {   
            if(!EditorInfo.NodesOptionsFoldouts.ContainsKey(typeid))
            {
                EditorInfo.RestoreFoldouts(CurrentNodes.ToArray());
            }
                    
            foreach (int optionIndex in currentNode.OptionsAttached)
            {
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
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
       

        #endregion

        DrawResizeButtons(id);
        GUI.DragWindow();       
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

    void DeleteNodeWindow(int id, int idOfType)
    {
        EditorInfo.Windows.RemoveAt(id);

        EditorInfo.WindowTypes.RemoveAt(id);
        EditorInfo.NodeTypesIDs.RemoveAt(id);
        EditorInfo.NodesIndexes.RemoveAt(idOfType);
        EditorInfo.Nodes--;

        CurrentNodes.RemoveAt(idOfType);
        nodeToNodeToAttach.Clear();
        EditorInfo.NodesOptionsFoldouts.Remove(idOfType);

        nodeToOptionToAttach.Clear();
        optionToNodeToAttach.Clear();
        EditorInfo.NodesOptionsFoldouts.Remove(idOfType);

        for(int i = 0; i < EditorInfo.NodeTypesIDs.Count; i++)
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
        
        foreach(DialogueNode dialNode in CurrentNodes)
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

        WriteDebug("Deleting node " + idOfType + " and it's associations.");        
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
                "NODE EDITOR: diagonalEnd"
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
        AreaBackgroundColor = new Color(0, 0.5f, 0, 0.4f);
    }
}
