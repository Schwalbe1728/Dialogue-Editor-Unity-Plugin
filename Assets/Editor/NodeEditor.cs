using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

public class NodeEditor : EditorWindow {

    enum NodeType
    {
        Node,
        Option
    }

    List<Rect> windows = new List<Rect>();
    List<NodeType> windowTypes = new List<NodeType>();
    List<int> nodeTypesIDs = new List<int>();
    List<int> optionsIndexes = new List<int>();
    List<int> nodesIndexes = new List<int>();
    int nodes = 0;
    int options = 0;

    //List<Rect> nodeWindows = new List<Rect>();
    List<int> nodeToNodeToAttach = new List<int>();
    Dictionary<int, int> nodeToNodeAttached = new Dictionary<int, int>();
    List<bool> immidiateNodeDummy = new List<bool>();
    List<string> nodeTextDummy = new List<string>();
    Dictionary<int, HashSet<int>> nodesOptions = new Dictionary<int, HashSet<int>>();

    //List<Rect> optionWindows = new List<Rect>();
    List<int> nodeToOptionToAttach = new List<int>();
    List<int> optionToNodeToAttach = new List<int>();
    Dictionary<int, int> optionToNodeAttached = new Dictionary<int, int>();
    List<bool> isOneUseOption = new List<bool>();
    List<string> optionTextDummy = new List<string>();

    Vector2 scrollPosition = Vector2.zero;    

    EditorConfigurationData config;// = new EditorConfigurationData();

    private float scale = 1f;

    List<string> debugMessages = new List<string>();
    int selectedDebugMessage = -1;

    [MenuItem("Window/Node editor")]
    static void ShowEditor()
    {
        NodeEditor editor = EditorWindow.GetWindow<NodeEditor>();

        //InitStyles();
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

    void OnGUI()
    {
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
            if (!config.ConfigurationOpened)
            {
                DrawEditorArea();
            }
            else
            {
                DrawConfigurationMenu();                
            }
        }
        GUILayout.EndArea();                                     
    }

    void UpdateCurves()
    {
        if (nodeToNodeToAttach.Count == 2)
        {
            if (!nodeToNodeAttached.ContainsKey(nodeToNodeToAttach[0]))
            {
                nodeToNodeAttached.Add(nodeToNodeToAttach[0], nodeToNodeToAttach[1]);
            }
            nodeToNodeToAttach.Clear();
        }

        if (nodeToNodeAttached.Count >= 1)
        {
            foreach (int windowIndex in nodeToNodeAttached.Keys)
            {
                if (nodeToNodeAttached[windowIndex] == -1) continue;

                int from = nodesIndexes[windowIndex];
                int to = nodesIndexes[nodeToNodeAttached[windowIndex]];

                DrawNodeCurve(windows[from], windows[to], config.ImmidiateNodeConnection);
            }
        }

        if(nodeToOptionToAttach.Count == 2)
        {
            if(!nodesOptions.ContainsKey(nodeToOptionToAttach[0]))
            {
                nodesOptions.Add(nodeToOptionToAttach[0], new HashSet<int>());
            }

            if(!nodesOptions[nodeToOptionToAttach[0]].Contains(nodeToOptionToAttach[1]))
            {
                nodesOptions[nodeToOptionToAttach[0]].Add(nodeToOptionToAttach[1]);
            }

            nodeToOptionToAttach.Clear();
        }

        if(nodesOptions.Count >= 1)
        {
            foreach(int nodeIndex in nodesOptions.Keys)
            {
                foreach(int optionIndex in nodesOptions[nodeIndex])
                {
                    int from = nodesIndexes[nodeIndex];
                    int to = optionsIndexes[optionIndex];

                    DrawNodeCurve(windows[from], windows[to], config.NodeToOptionConnection);
                }
            }
        }

        if(optionToNodeToAttach.Count == 2)
        {
            if(!optionToNodeAttached.ContainsKey(optionToNodeToAttach[0]))
            {
                optionToNodeAttached.Add(optionToNodeToAttach[0], optionToNodeToAttach[1]);
            }

            optionToNodeToAttach.Clear();
        }

        if(optionToNodeAttached.Count >= 1)
        {
            foreach(int optionIndex in optionToNodeAttached.Keys)
            {
                int from = optionsIndexes[optionIndex];
                int to = nodesIndexes[optionToNodeAttached[optionIndex]];

                if (to == -1) continue;

                DrawNodeCurve(windows[from], windows[to], config.OptionToNodeConnection);
            }
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

        for (int i = 0; i < windows.Count; i++)
        {
            border.Encapsulate(scale * windows[i].max);
            border.Encapsulate(scale * windows[i].min);
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

                UpdateCurves();

                for (int i = 0; i < windows.Count; i++)
                {
                    bool isNode = windowTypes[i] == NodeType.Node;

                    if (isNode)
                    {
                        windows[i] =
                            GUILayout.Window(i, windows[i], DrawNodeWindow, "Node " + nodeTypesIDs[i]);
                    }
                    else
                    {
                        windows[i] =
                            GUILayout.Window(i, windows[i], DrawOptionWindow, "Option " + nodeTypesIDs[i]);
                    }
                }

                //GUI.DragWindow();

            }
            //GUIUtility.ScaleAroundPivot(Vector2.one, scrollPosition);
            EndWindows();
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawConfigurationMenu()
    {
        BeginWindows();
        {            
            config.RectHandle = GUILayout.Window(0, config.RectHandle, config.DrawMenu, "Configuration");
        }
        EndWindows();
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
            if (GUILayout.Button("Create Node"))
            {
                windows.Add(new Rect(10 + scrollPosition.x, 10 + scrollPosition.y, 200, 5));
                windowTypes.Add(NodeType.Node);
                nodeTypesIDs.Add(nodes++);
                nodesIndexes.Add(windows.Count - 1);

                immidiateNodeDummy.Add(false);
                nodeTextDummy.Add("");
                WriteDebug("Adding node");
            }

            if (GUILayout.Button("Create Option"))
            {
                windows.Add(new Rect(10 + scrollPosition.x, 10 + scrollPosition.y, 200, 5));
                windowTypes.Add(NodeType.Option);
                nodeTypesIDs.Add(options++);
                optionsIndexes.Add(windows.Count - 1);

                isOneUseOption.Add(false);
                optionTextDummy.Add("");
                WriteDebug("Adding option");
            }

            if (GUILayout.Button("Sort"))
            {
                WriteDebug("Not Implemented Function");
            }

            if (GUILayout.Button("Configure"))
            {                
                config.ConfigurationOpened = true;
                config.RectHandle.center = scrollPosition + config.RectHandle.size / 2 + new Vector2(20,20);
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
            Rect temp = windows[id];

            temp.size += new Vector2(10, 0);
            windows[id] = temp;
        }
        if (GUILayout.Button("-"))
        {
            Rect temp = windows[id];

            temp.size += new Vector2(-10, 0);
            windows[id] = temp;
        }              

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Height: ", GUILayout.Width(100));

        if (GUILayout.Button("+"))
        {
            Rect temp = windows[id];

            temp.size += new Vector2(0, 10);
            windows[id] = temp;
        }

        if (GUILayout.Button("-"))
        {
            Rect temp = windows[id];

            temp.size += new Vector2(0, -10);
            windows[id] = temp;
        }
        GUILayout.EndHorizontal();
    }

    void DrawOptionWindow(int id)
    {
        int typeid = nodeTypesIDs[id];

        //GUILayout.Label("Option " + id);

        if(nodeToOptionToAttach.Count == 1)
        {
            if(GUILayout.Button("Connect"))
            {
                nodeToOptionToAttach.Add(typeid);
            }
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Next Node: ", GUILayout.Width(80));

            bool tempCont = optionToNodeAttached.ContainsKey(typeid);
            string destination =
                (tempCont) ?
                    optionToNodeAttached[typeid].ToString() :
                    "[EXIT]";
            if (tempCont)
            {
                Rect focus = windows[nodesIndexes[optionToNodeAttached[typeid]]];
                DrawJumpToButton(destination, focus, GUILayout.Width(50));
            }
            else
            {
                GUILayout.Label(destination, GUILayout.Width(50));
            }

            if(optionToNodeAttached.ContainsKey(typeid))
            {
                if(GUILayout.Button("Clear"))
                {
                    optionToNodeAttached.Remove(typeid);
                }
            }
            else
            {
                if (optionToNodeToAttach.Count == 0)
                {
                    if (!optionToNodeAttached.ContainsKey(typeid))
                    {
                        if (GUILayout.Button("Set"))
                        {
                            optionToNodeToAttach.Add(typeid);
                        }
                    }
                    else
                    {
                        if(GUILayout.Button("Clear"))
                        {
                            //optionToNodeToAttach.Clear();
                            optionToNodeAttached.Remove(typeid);
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

        optionTextDummy[typeid] =
            EditorGUILayout.TextArea(optionTextDummy[typeid],
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(50),
                GUILayout.ExpandWidth(false), GUILayout.Width(windows[id].width - 10)
                );

        DrawResizeButtons(id);

        GUI.DragWindow();
    }

    void DrawNodeWindow(int id)
    {
        int typeid = nodeTypesIDs[id];

        #region Connect To Option Handling

        if(optionToNodeToAttach.Count > 0 && !optionToNodeAttached.ContainsKey(optionToNodeToAttach[0]) )
        {
            if(GUILayout.Button("Connect"))
            {
                optionToNodeToAttach.Add(typeid);
            }
        }
        else
        #endregion

        #region Immidiate Node Handling          
        if (!immidiateNodeDummy[typeid] || !nodeToNodeAttached.ContainsKey(typeid))
        {
            if (nodeToNodeToAttach.Count != 1)
            {
                if (GUILayout.Button("Make Immidiate Node"))
                {
                    nodeToNodeToAttach.Add(typeid);
                    immidiateNodeDummy[typeid] = true;

                    //nodesOptions.Remove(typeid);
                }
            }
            else
            {
                if (!nodeToNodeToAttach.Contains(typeid))
                {
                    if (GUILayout.Button("Connect"))
                    {
                        nodeToNodeToAttach.Add(typeid);
                        nodesOptions.Remove(nodeToNodeToAttach[0]);
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Cancel Connection"))
                        {
                            nodeToNodeToAttach.Clear();
                        }

                        if (GUILayout.Button("Exits Dialogue"))
                        {
                            nodeToNodeToAttach.Add(-1);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            if (nodeToNodeToAttach.Count == 1 && !nodeToNodeToAttach.Contains(typeid))
            {
                if (GUILayout.Button("Connect"))
                {
                    nodeToNodeToAttach.Add(typeid);
                    nodesOptions.Remove(nodeToNodeToAttach[0]);
                }
            }

            GUILayout.BeginHorizontal();
            {
                int nxt = nodeToNodeAttached[typeid];
                GUILayout.Label("Next Node: " + ((nxt == -1)? "[EXIT]" : nxt.ToString()));

                if (GUILayout.Button("Clear"))
                {
                    WriteDebug("WARNING: Removing previously set immidiate node connection.");

                    nodeToNodeAttached.Remove(typeid);
                    nodeToNodeToAttach.Remove(typeid);
                    immidiateNodeDummy[typeid] = false;
                }
            }
            GUILayout.EndHorizontal();        
        }
        #endregion

        if (GUILayout.Button("Delete"))
        {
            DeleteNodeWindow(id, typeid);
            return;
        }               

        nodeTextDummy[typeid] =
            EditorGUILayout.TextArea(
                nodeTextDummy[typeid],
                config.TextAreaStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(50),
                GUILayout.ExpandWidth(false), GUILayout.Width(windows[id].width - 10)
                );

        #region Opcje Dialogowe

        if(!immidiateNodeDummy[typeid])
        {
            if(GUILayout.Button("Add Option"))
            {
                nodeToOptionToAttach.Add(typeid);
            }

            if (nodesOptions.ContainsKey(typeid))
            {
                int[] keys = new int[nodesOptions[typeid].Count];
                nodesOptions[typeid].CopyTo(keys, 0);

                foreach (int optionIndex in keys)
                {
                    GUILayout.BeginHorizontal();
                    {
                        string label = "Option: " + optionIndex + ", ";
                        Rect focus = windows[optionsIndexes[optionIndex]];

                        DrawJumpToButton(label, focus);

                        //GUILayout.Label(label);

                        label =
                            "Dest.: " +
                                ((optionToNodeAttached.ContainsKey(optionIndex)) ?
                                    optionToNodeAttached[optionIndex].ToString() :
                                    "[EXIT]");

                        bool tempCont = optionToNodeAttached.ContainsKey(optionIndex);

                        if (tempCont)
                        {
                            focus = windows[nodesIndexes[optionToNodeAttached[optionIndex]]];
                            DrawJumpToButton(label, focus);
                        }
                        else
                        {
                            GUILayout.Label(label);
                        }

                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            //TODO: ZMIENIĆ PĘTLĘ - JEŚLI TUTAJ USUNĘ TO BĘDZIE SIĘ SYPAĆ
                            nodesOptions[typeid].Remove(optionIndex);
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
        //TODO: Usuwanie połączeń z opcjami

        windows.RemoveAt(id);
        windowTypes.RemoveAt(id);
        nodeTypesIDs.RemoveAt(id);
        nodesIndexes.RemoveAt(idOfType);
        nodes--;

        nodeToNodeAttached.Remove(idOfType);
        nodeToNodeToAttach.Clear();
        immidiateNodeDummy.RemoveAt(idOfType);
        nodeTextDummy.RemoveAt(idOfType);
        nodesOptions.Remove(idOfType);

        nodeToOptionToAttach.Clear();
        optionToNodeToAttach.Clear();
        nodesOptions.Remove(idOfType);        

        for(int i = 0; i < nodeTypesIDs.Count; i++)
        {
            if(windowTypes[i] == NodeType.Node && nodeTypesIDs[i] > idOfType)
            {
                nodeTypesIDs[i]--;
            }
        }

        for(int i = idOfType; i < nodesIndexes.Count; i++)
        {
            nodesIndexes[i]--;
        }

        for(int i = optionsIndexes.FindIndex(x => x > id); i < optionsIndexes.Count && i >= 0; i++)
        {
            optionsIndexes[i]--;
        }

        int[] keys = new int[nodeToNodeAttached.Count];
        nodeToNodeAttached.Keys.CopyTo(keys, 0);
        for(int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            int value = nodeToNodeAttached[key];

            nodeToNodeAttached.Remove(key);

            if (key > idOfType) key--;
            if (value > idOfType) value--;

            nodeToNodeAttached.Add(key, value);
        }

        keys = new int[nodesOptions.Count];
        nodesOptions.Keys.CopyTo(keys, 0);
        for(int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            HashSet<int> value = nodesOptions[key];

            if(key > idOfType)
            {
                nodesOptions.Remove(key);
                key--;
                nodesOptions.Add(key, value);
            }
        }

        keys = new int[optionToNodeAttached.Count];
        optionToNodeAttached.Keys.CopyTo(keys, 0);

        int ind = 0;
        for(ind = 0; ind < keys.Length; ind++)
        {
            int k = keys[ind];

            if (optionToNodeAttached[k] == idOfType)
            {
                optionToNodeAttached.Remove(k);
            }
            else
            {
                if (optionToNodeAttached[k] > idOfType)
                {
                    optionToNodeAttached[k]--;
                }
            }
        }

        keys = new int[nodeToNodeAttached.Count];
        nodeToNodeAttached.Keys.CopyTo(keys, 0);

        for(int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            int value = nodeToNodeAttached[key];

            if(value == idOfType)
            {
                nodeToNodeAttached.Remove(key);
                immidiateNodeDummy[key] = false;                
            }
            else
            {
                if(nodeToNodeAttached[key] > idOfType)
                {
                    nodeToNodeAttached[key]--;
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
        //Handles.CircleHandleCap(1, endPos - Vector3.right * arrowSize , Quaternion.identity, arrowSize, EventType.Repaint);        
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

        float curveModifier = 0.9f;

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

    private class EditorConfigurationData
    {
        public bool ConfigurationOpened;

        public bool DiagonalStartPoints = false;
        public bool DiagonalEndPoints = false;

        public Color ImmidiateNodeConnection;
        public Color NodeToOptionConnection;
        public Color OptionToNodeConnection;
        public Color AreaBackgroundColor;

        public GUIStyle BoundingBoxStyle = null;
        public GUIStyle EditorAreaBackgroundStyle = null;
        public GUIStyle TextAreaStyle = null;

        public Rect RectHandle;

        private EditorConfigurationData Defaults;
        private bool Connections = false;
        private bool Backgrounds = false;

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
            if(Defaults != null)
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

        public void DrawMenu(int id)
        {            
            Connections = EditorGUILayout.Foldout(Connections, "Connections: ");

            if(Connections)
            {
                DrawColorChanger("Immidiate Node Connection: ", ref ImmidiateNodeConnection);
                EditorPrefs.SetString(preferencesKeys[0], ColorToString(ImmidiateNodeConnection));

                DrawColorChanger("Node To Option Connection: ", ref NodeToOptionConnection);
                EditorPrefs.SetString(preferencesKeys[1], ColorToString(NodeToOptionConnection));

                DrawColorChanger("Option To Node Connection: ", ref OptionToNodeConnection);
                EditorPrefs.SetString(preferencesKeys[2], ColorToString(OptionToNodeConnection));

                GUILayout.BeginHorizontal();
                GUILayout.Label("Diagonal Start Points: ", GUILayout.Width(175));
                DiagonalStartPoints = EditorGUILayout.Toggle(DiagonalStartPoints);
                EditorPrefs.SetBool(preferencesKeys[4], DiagonalStartPoints);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Diagonal End Points: ", GUILayout.Width(175));
                DiagonalEndPoints = EditorGUILayout.Toggle(DiagonalEndPoints);
                EditorPrefs.SetBool(preferencesKeys[5], DiagonalEndPoints);
                GUILayout.EndHorizontal();
            }

            Backgrounds = EditorGUILayout.Foldout(Backgrounds, "Backgrounds: ");

            if(Backgrounds)
            { 
                DrawColorChanger("Editor Background: ", ref AreaBackgroundColor);
                EditorPrefs.SetString(preferencesKeys[3], ColorToString(AreaBackgroundColor));
                InitStyles(true);
            }

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            {
                if(GUILayout.Button("Close"))
                {
                    ConfigurationOpened = false;                    
                }

                if(GUILayout.Button("Restore"))
                {
                    RestoreDefaults();
                }
            }
            GUILayout.EndHorizontal();            
            GUI.DragWindow();
        }

        private bool PreferencesExist()
        {
            bool result = true;

            foreach(string key in preferencesKeys)
            {
                result &= EditorPrefs.HasKey(key);
            }

            return result;
        }

        private void RestorePreferences()
        {
            if(!TryParseFromString(EditorPrefs.GetString(preferencesKeys[0]), out ImmidiateNodeConnection  ))
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

        private string ColorToString(Color color)
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

        private bool TryParseFromString(string colorText, out Color col)
        {
            string[] seperated = colorText.Split(new char[] { '#' }, System.StringSplitOptions.RemoveEmptyEntries);

            float red = -1;
            float green = -1;
            float blue = -1;
            float alpha = -1;

            bool success = true;

            if((success = float.TryParse(seperated[0], out red) ))
            {
                if(success && ( success = float.TryParse(seperated[1], out green) ))
                {
                    if (success && (success = float.TryParse(seperated[2], out blue)))
                    {
                        if (success && (success = float.TryParse(seperated[3], out alpha)))
                        {

                        }
                    }
                }
            }

            col = (success)? new Color(red, green, blue, alpha) : new Color();            

            return success;
        }

        private void DrawColorChanger(string labelText, ref Color color)
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            {
                GUILayout.Label(labelText, GUILayout.Width(175));
                color = EditorGUILayout.ColorField(color, GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
        }

        private void InitStyles(bool forced = false)
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
}
