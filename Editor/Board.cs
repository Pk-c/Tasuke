
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TasukeChan
{
    [Serializable]
    public class Node
    {
        public string title;
        public Rect rect;
        public Node(string title, Rect rect)
        {
            this.title = title;
            this.rect = rect;
        }
    }


    [Serializable]
    public class ObjectNode : Node
    {
        public int categoryId = -1;
        public Object obj = null;

        public ObjectNode(string title, Object linked, Rect rect) : base(title, rect)
        {
            obj = linked;
        }
    }

    [Serializable]
    public class CategoryNode : Node
    {
        public int id = 0;
        public Color color;

        public CategoryNode( Color col, int catid, string title, Rect rect) : base(title, rect)
        {
            id = catid;
            color = col;
        }
    }

    public class Board : EditorWindow
    {
        public enum Mode
        {
            Select,
            Edit,
            Rezise
        }

        private List<ObjectNode> ObjectNodes = new List<ObjectNode>();
        private List<CategoryNode> CatNodes = new List<CategoryNode>();

        //Style
        private GUIStyle normalNodeStyle = new GUIStyle();
        private GUIStyle normalCategoryStyle = new GUIStyle();
        private GUIStyle selectedCategoryStyle = new GUIStyle();
        private GUIStyle selectedNodeStyle = new GUIStyle();
        private GUIStyle resizeHandleStyle = new GUIStyle();
        private GUIStyle headerCategoryStyle = new GUIStyle();
        private GUIStyle headerTitleStyle = new GUIStyle();

        //Texture
        Texture2D CategoryTex = null;
        Texture2D SelectedCategoryTex = null;
        Texture2D NodeTex = null;
        Texture2D Tasuke = null;
        Texture2D Header = null;

        //Menu
        private int selectedMenuItem = -1;
        private string[] menuItems = new string[] { "Load Board", "Save Board" };
        private GUIContent[] menuOptions;

        private Node selectedNode = null;
        private Node inspectedNode = null;
        private Mode mode = Mode.Select;
        private Vector2 selectOffset;
        private Color PickedColor = Color.white;
        private bool ontoSomething = false;
        private Vector2 lastmousePos;
        private Vector2 panOffset = Vector2.zero;
        private Vector2 gridSize = new Vector2(10, 10);
        private int MaxId = 0;
        private string windowsTitle = "New Tasuke Board";
        private string lastLoadedFilePath = "";
        private float showSaveMessage = 0.0f;


        [MenuItem("Window/TasukeBoard")]
        public static void Init()
        {
            Board window = (Board)EditorWindow.GetWindow(typeof(Board));
            window.Show();
        }

        public static void Load( string path,bool relative = false)
        {
            Board window = (Board)EditorWindow.GetWindow(typeof(Board));
            window.Show();
            window.LoadData(path,relative);
        }

        void OnEnable()
        {
            EditorWindow.GetWindow(typeof(Board)).titleContent = new GUIContent(windowsTitle, "");

            resizeHandleStyle.normal.background = EditorGUIUtility.whiteTexture;
            resizeHandleStyle.normal.textColor = Color.black;
            resizeHandleStyle.alignment = TextAnchor.MiddleCenter;
            resizeHandleStyle.border = new RectOffset(1, 1, 1, 1);

            MonoScript monoScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            string texturePath = System.IO.Path.GetDirectoryName(scriptPath) + "\\EditorRessources\\";

            CategoryTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Category.png");
            SelectedCategoryTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "CategorySelected.png");
            NodeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "NodeSelected.png");
            Tasuke = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Tasuke.png");
            Header = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Header.png");

            // Load icons and set up menu options
            GUIContent loadIcon = EditorGUIUtility.IconContent("Folder Icon");
            GUIContent saveIcon = EditorGUIUtility.IconContent("SaveAs");
            loadIcon.text = "Load Board";
            saveIcon.text = "Save Board";
            menuOptions = new GUIContent[] { loadIcon, saveIcon };
        }

        void DrawHalo(Rect originalRect, Color col)
        {
            float haloThickness = 2; // Thickness of the halo
            Color haloColor = Color.yellow; // Color of the halo

            // Increase the size of the rect to accommodate the halo
            Rect haloRect = originalRect;
            haloRect.x -= haloThickness;
            haloRect.y -= haloThickness;
            haloRect.width += haloThickness * 2;
            haloRect.height += haloThickness * 2;

            // Draw the halo
            EditorGUI.DrawRect(haloRect, haloColor);
        }
        void BuildStyle()
        {
            normalCategoryStyle = new GUIStyle(GUI.skin.box);
            normalCategoryStyle.normal.textColor = Color.white;
            normalCategoryStyle.hover.textColor = Color.white;
            normalCategoryStyle.fontSize = 20;
            normalCategoryStyle.fontStyle = FontStyle.Bold;
            normalCategoryStyle.alignment = TextAnchor.UpperLeft;
            normalCategoryStyle.normal.background = CategoryTex;

            selectedCategoryStyle = new GUIStyle(GUI.skin.box);
            selectedCategoryStyle.normal.textColor = Color.white;
            selectedCategoryStyle.hover.textColor = Color.white;
            selectedCategoryStyle.fontSize = 20;
            selectedCategoryStyle.fontStyle = FontStyle.Bold;
            selectedCategoryStyle.alignment = TextAnchor.UpperLeft;
            selectedCategoryStyle.normal.background = SelectedCategoryTex;

            headerCategoryStyle = new GUIStyle(GUI.skin.box);
            headerCategoryStyle.normal.textColor = Color.white;
            headerCategoryStyle.hover.textColor = Color.white;
            headerCategoryStyle.fontSize = 20;
            headerCategoryStyle.fontStyle = FontStyle.Bold;
            headerCategoryStyle.alignment = TextAnchor.UpperLeft;
            headerCategoryStyle.normal.background = Header;

            headerTitleStyle = new GUIStyle(GUI.skin.box);
            headerTitleStyle.normal.textColor = Color.white;
            headerTitleStyle.hover.textColor = Color.white;
            headerTitleStyle.fontSize = 20;
            headerTitleStyle.fontStyle = FontStyle.Bold;
            headerTitleStyle.alignment = TextAnchor.UpperLeft;

            normalNodeStyle = new GUIStyle(GUI.skin.window);
            normalNodeStyle.fontStyle = FontStyle.Bold;
            normalNodeStyle.normal.textColor = Color.white;
            normalNodeStyle.hover.textColor = Color.white;
            selectedNodeStyle = new GUIStyle(GUI.skin.box);
            selectedNodeStyle.fontStyle = FontStyle.Bold;
            selectedNodeStyle.normal.background = NodeTex;
        }

        void OnGUI()
        {
            BuildStyle();
            
            BeginWindows();

            DrawGrid();

            UpdateContextMenu();

            Event e = Event.current;

            ontoSomething = false;

            for (int i = 0; i < ObjectNodes.Count; i++)
            {
                HandleDrag(ObjectNodes[i], e);
                HandleSelect(i, e);
            }

            for (int i = 0; i < CatNodes.Count; i++)
            {                
                if (selectedNode == CatNodes[i] && mode == Mode.Edit)
                {
                    CatNodes[i].title = GUI.TextField(CatNodes[i].rect, CatNodes[i].title);

                    if(e.keyCode == KeyCode.Return)
                    {
                        selectedNode = null;
                        mode = Mode.Select;
                        Repaint();
                    }
                }
                else
                {
                    HandleEdit(i, e);
                    HandleResize(i, e);
                    HandleColor(i, e);
                    HandleDrag(CatNodes[i], e);
                    DrawCategorie(i, e);
                }
            }

            for (int i = 0; i < ObjectNodes.Count; i++)
            {
                DrawNode(i, e);
            }

            HandleMove(e);

            Repaint();
            EndWindows();

            HandleDragAndDrop();
            HandleRemove(e);

            Rect windowRect = new Rect(position.width - Tasuke.width, position.height - Tasuke.height, Tasuke.width, Tasuke.height);
            GUI.DrawTexture(windowRect, Tasuke);

            DrawMenuBar();
            HandleQuickSave();

            if( showSaveMessage > 0.0f )
            {
                showSaveMessage -= Time.deltaTime;
                GUI.color = Color.white;

                GUIStyle saveStyle = new GUIStyle();
                saveStyle.fontSize = 80;
                saveStyle.fontStyle = FontStyle.Bold;
                saveStyle.normal.textColor = Color.white;
                saveStyle.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("SAVED", saveStyle);
            }
        }

        public void HandleRemove(Event e)
        {
            if( e.keyCode == KeyCode.Delete && mode == Mode.Select )
            {
                if( selectedNode != null)
                {
                    if (selectedNode is CategoryNode)
                    {
                        CatNodes.Remove(selectedNode as CategoryNode);
                    }
                    else if( selectedNode is ObjectNode)
                    {
                        ObjectNodes.Remove(selectedNode as ObjectNode);
                    }
                }

                selectedNode = null;
            }
        }

        public void HandleMove(Event e)
        {
            if (!ontoSomething)
            {
                if (e.type == EventType.MouseDown)
                {
                    inspectedNode = null;
                    lastmousePos = e.mousePosition;
                }

                if( e.type == EventType.MouseDrag) 
                {
                    Vector2 mouseDelta = e.mousePosition - lastmousePos;

                    for ( int n = 0; n < ObjectNodes.Count; n++)
                    {
                        ObjectNodes[n].rect.position += mouseDelta;
                    }

                    for (int n = 0; n < CatNodes.Count; n++)
                    {
                        CatNodes[n].rect.position += mouseDelta;
                    }

                    panOffset += mouseDelta;

                    lastmousePos = e.mousePosition;

                    e.Use();
                }
            }
        }

        public void HandleResize(int id, Event e)
        {
            Rect resizeHandleRect = new Rect(CatNodes[id].rect.xMax, CatNodes[id].rect.yMax, 20, 20);

            GUI.Box(resizeHandleRect, "↘");

            if (e.type == EventType.MouseDrag && (resizeHandleRect.Contains(e.mousePosition) || mode == Mode.Rezise))
            {
                if (selectedNode == CatNodes[id] || selectedNode == null)
                {
                    ontoSomething = true;
                    mode = Mode.Rezise;
                    selectedNode = CatNodes[id];
                    selectedNode.rect.width = Mathf.Max(50, e.mousePosition.x - selectedNode.rect.x);
                    selectedNode.rect.height = Mathf.Max(50, e.mousePosition.y - selectedNode.rect.y);

                    for( int n = 0; n < ObjectNodes.Count; n++)
                    {
                        if( ObjectNodes[n].categoryId == CatNodes[id].id )
                        {
                            if (!CatNodes[id].rect.Contains(ObjectNodes[n].rect.position))
                            {
                                ObjectNodes[n].categoryId = -1;
                            }
                        }
                    }

                    Repaint();
                }
            }

            if (mode == Mode.Rezise && e.type == EventType.MouseUp)
            {
                mode = Mode.Select;
                selectedNode = null;
            }
        }

        public void HandleColor(int id, Event e)
        {
            CategoryNode cnd = (CategoryNode)(CatNodes[id]);

            float colorFieldSize = 20;
            float margin = 5;
            Rect colorFieldRect = new Rect(
                cnd.rect.xMax - colorFieldSize - margin,
                cnd.rect.yMax - colorFieldSize - margin,
                colorFieldSize,
                colorFieldSize);

            // Draw the color field and update the node's color
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUI.ColorField(colorFieldRect, GUIContent.none, cnd.color, false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                cnd.color = newColor;
            }
        }

        public void HandleEdit( int id, Event e)
        {
            if (e.type == EventType.MouseDown && e.clickCount == 2)
            {
                if (CatNodes[id].rect.Contains(e.mousePosition))
                {
                    selectedNode = CatNodes[id];
                    ontoSomething = true;
                    mode = Mode.Edit;
                }
            }
        }

        public void HandleSelect(int id, Event e)
        {
            if (e.type == EventType.MouseDown && mode == Mode.Select && (ObjectNodes[id].rect.Contains(e.mousePosition) || selectedNode == ObjectNodes[id]))
            {
                ObjectNode obj = (ObjectNode)(ObjectNodes[id]);

                inspectedNode = ObjectNodes[id];
                ontoSomething = true;
                Selection.activeObject = obj.obj;
            }
        }


        public void HandleDrag( Node node, Event e)
        {
            Rect rect = node.rect;
            if( node is CategoryNode)
            {
                rect.y -= 32;
                rect.height += 32;
            }

            if (e.type == EventType.MouseDrag && mode == Mode.Select && (rect.Contains(e.mousePosition) || selectedNode == node))
            {
                if (selectedNode == null)
                {
                    selectedNode = node;
                    selectOffset = (rect.position - e.mousePosition);
                }

                ontoSomething = true;

                Vector2 delta = selectedNode.rect.position;
                selectedNode.rect.position = e.mousePosition + selectOffset;
                selectedNode.rect.x = Mathf.Round(selectedNode.rect.x / gridSize.x) * gridSize.x;
                selectedNode.rect.y = Mathf.Round(selectedNode.rect.y / gridSize.y) * gridSize.y;
                delta = selectedNode.rect.position - delta;

                if (selectedNode is ObjectNode)
                {
                    ObjectNode obj = (ObjectNode)(selectedNode);
                    obj.categoryId = -1;

                    for ( int n = 0; n < CatNodes.Count; n++ ) 
                    {             
                        if (CatNodes[n].rect.Contains(e.mousePosition))
                        {
                            obj.categoryId = CatNodes[n].id;
                        }
                    }
                }
                else if(selectedNode is CategoryNode)
                {
                    CategoryNode cNode = (CategoryNode)selectedNode;

                    for ( int n = 0; n < ObjectNodes.Count; n++ )
                    {                        
                        if (ObjectNodes[n].categoryId == cNode.id )
                        {
                            ObjectNodes[n].rect.position += delta;
                        }
                    }
                }
            }

            if (mode == Mode.Select && e.type == EventType.MouseUp)
            {
                mode = Mode.Select;
                selectedNode = null;
            }
        }

        private void DrawCategorie(int id, Event e)
        {
            CategoryNode cnd = CatNodes[id];
            GUI.color = cnd.color;
            GUI.Box(cnd.rect, "", selectedNode == cnd ? selectedCategoryStyle : normalCategoryStyle);
            Rect title = cnd.rect;
            title.height = 32;
            title.y -= 32;
            GUI.Box(title, cnd.title, headerCategoryStyle);
            GUI.color = Color.white;
            GUI.Box(title, cnd.title, headerTitleStyle);
        }

        private void DrawNode(int id,Event e)
        {
            GUI.Box(ObjectNodes[id].rect, ObjectNodes[id].title,normalNodeStyle);            
            ObjectNode node = (ObjectNode)(ObjectNodes[id]);

            if (node != null)
            {
                GUIContent iconContent = EditorGUIUtility.ObjectContent(node.obj, node.obj.GetType());
                Texture iconTexture = iconContent.image;
                if (iconTexture != null)
                {
                    if (node.categoryId != -1)
                    {
                        for (int n = 0; n < CatNodes.Count; n++)
                        {                            
                            if (CatNodes[n].id == node.categoryId)
                            {
                                GUI.color = CatNodes[n].color;
                                break;
                            }
                        }
                    }

                    float iconSize = 30;
                    Rect iconBoxRect = new Rect(node.rect.center.x - iconSize/2.0f, node.rect.y + 13, 40, 40);
                    Rect iconRect = new Rect(
                        iconBoxRect.x + (iconBoxRect.width - iconSize) / 2,
                        iconBoxRect.y + (iconBoxRect.height - iconSize) / 2,
                        iconSize,
                        iconSize);

                    GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit);

                    GUI.color = Color.white;
                }
                
                if (ObjectNodes[id] == selectedNode || ObjectNodes[id] == inspectedNode)
                {
                    GUI.Box(ObjectNodes[id].rect, "", selectedNodeStyle);
                }

                
            }
        }
        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            Rect dropArea = new Rect(0, 0, position.width, position.height);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            CreateNode(draggedObject, evt.mousePosition);
                        }
                    }
                    break;
            }
        }

        private void DrawGrid()
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSize.x);
            int heightDivs = Mathf.CeilToInt(position.height / gridSize.y);

            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);

            Vector3 newOffset = new Vector3(panOffset.x % gridSize.x, panOffset.y % gridSize.y, 0);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSize.x * i, -gridSize.y, 0) + newOffset,
                                 new Vector3(gridSize.x * i, position.height, 0) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-gridSize.x, gridSize.y * j, 0) + newOffset,
                                 new Vector3(position.width, gridSize.y * j, 0) + newOffset);
            }

            Handles.EndGUI();
        }

        #region Node Creation

        void CreateNode( Object obj, Vector2 pos)
        {
            ObjectNodes.Add(new ObjectNode(obj.name, obj, CalculateNodeRect(obj.name,pos)));
        }

        void CreateCategory(Vector2 mousePosition)
        {
            for( int n = 0; n < CatNodes.Count; n++ )            
            {
                if( MaxId < CatNodes[n].id )
                {
                    MaxId = CatNodes[n].id;
                }
            }
            MaxId++;
            CatNodes.Add(new CategoryNode(PickedColor, MaxId,"New Category",new Rect(mousePosition.x, mousePosition.y, 200, 300)));
        }

        Rect CalculateNodeRect(string title, Vector2 pos)
        {
            Vector2 size = normalNodeStyle.CalcSize(new GUIContent(title));
            float width = size.x + 20; // Adding padding
            float height = 50; // Fixed height
            return new Rect(pos.x, pos.y, width, height);
        }

        #endregion

        #region ContextMenu

        private void DrawMenuBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUIStyle style = GUI.skin.window;

            // File Dropdown
            if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
            {
                GenericMenu menu = new GenericMenu();
                GUIStyle menuItemStyle = new GUIStyle(EditorStyles.label);
                menuItemStyle.imagePosition = ImagePosition.ImageLeft; // Position the image to the left of the text

                foreach (GUIContent menuItem in menuOptions)
                {
                    menu.AddItem(menuItem, false, HandleMenuAction, menuItem);
                }

                menu.DropDown(new Rect(0, 0, 0, 16)); // Open the menu at the top-left of the window
            }

            GUILayout.EndHorizontal();
        }

        private void HandleMenuAction(object userData)
        {
            GUIContent menuItem = (GUIContent)userData;
            int index = Array.IndexOf(menuOptions, menuItem);

            switch (index)
            {
                case 0:{ LoadNodes();}break;
                case 1:{ SaveNode();}break;
            }
        }

        private void HandleQuickSave()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.S)
            {
                if (!string.IsNullOrEmpty(lastLoadedFilePath))
                {
                    SaveNode(lastLoadedFilePath);
                }
                else
                {
                    // Optional: Show a message or open the Save File Panel if no file was previously loaded
                    EditorUtility.DisplayDialog("Save Error", "No file loaded to save. Please use Save As option.", "OK");
                }
            }

        }

        private void SaveNode(string filePath = null)
        {
            string path = filePath;

            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel("Save Nodes as Asset", Application.dataPath, "tkboard", "asset");
            }
            else
            {
                showSaveMessage = 20.0f;
            }

            if (!string.IsNullOrEmpty(path))
            {
                TkData nodeWrapper = ScriptableObject.CreateInstance<TkData>();
                nodeWrapper.onodes = ObjectNodes;
                nodeWrapper.cnodes = CatNodes;

                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

                // Save the ScriptableObject as an asset in your project
                AssetDatabase.CreateAsset(nodeWrapper, relativePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void LoadNodes()
        {
            string path = EditorUtility.OpenFilePanel("Load Tasuke Board", Application.dataPath, "asset");
            LoadData(path);
        }

        private void LoadData(string path, bool relative = false)
        {
            if(!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                lastLoadedFilePath = path;
                ObjectNodes.Clear();
                CatNodes.Clear();
                windowsTitle = Path.GetFileNameWithoutExtension(path);
                EditorWindow.GetWindow(typeof(Board)).titleContent = new GUIContent(windowsTitle, "");

                string relativePath = relative ? path : "Assets" + path.Substring(Application.dataPath.Length);
                TkData nodeWrapper = AssetDatabase.LoadAssetAtPath<TkData>(relativePath);

                ObjectNodes = nodeWrapper.onodes;
                CatNodes = nodeWrapper.cnodes;
            }
        }

        public void UpdateContextMenu()
        {
            Event e = Event.current;
            if (e.button == 1 && e.type == EventType.MouseDown)
            {
                ProcessContextMenu(e.mousePosition);
            }
        }

        void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Create Category"), false, () => CreateCategory(mousePosition));
            genericMenu.ShowAsContext();
        }

        #endregion

    }
}