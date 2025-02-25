#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Linq;
using System.IO;

namespace TasukeChan
{
    [Serializable]
    public class TNode
    {
        public string title;
        public Rect rect;
        public TNode(string title, Rect rect)
        {
            this.title = title;
            this.rect = rect;
        }
    }

    [Serializable]
    public class TObjectNode : TNode
    {
        public int categoryId = -1;
        public Object obj = null;

        public TObjectNode(string title, Object linked, Rect rect) : base(title, rect)
        {
            obj = linked;
        }
    }

    [Serializable]
    public class TCategoryNode : TNode
    {
        public int id = 0;
        public Color color;

        public TCategoryNode(Color col, int catid, string title, Rect rect) : base(title, rect)
        {
            id = catid;
            color = new Color(col.r, col.g, col.b, col.a);
        }
    }

    [InitializeOnLoad]
    public class TasukeBoard : EditorWindow
    {
        //Zoom
        private const float gridSize = 20;
        private const float kZoomMin = 0.5f;
        private const float kZoomMax = 2.0f;
        private Rect _zoomArea;
        private float _zoom = 1.0f;
        private Vector2 _zoomCoordsOrigin = Vector2.zero;

        private List<TNode> nodes = new List<TNode>();
        private List<TCategoryNode> catnodes = new List<TCategoryNode>();
        private List<TNode> selectedNodes = new List<TNode>();

        //Selection
        private bool isSelecting = false;
        private Vector2 selectionStart;
        private Vector2 selectionEnd;

        //Style
        private GUIStyle normalNodeStyle = new GUIStyle();
        private GUIStyle normalCategoryStyle = new GUIStyle();
        private GUIStyle selectedCategoryStyle = new GUIStyle();
        private GUIStyle selectedNodeStyle = new GUIStyle();
        private GUIStyle resizeHandleStyle = new GUIStyle();
        private GUIStyle headerCategoryStyle = new GUIStyle();
        private GUIStyle headerTitleStyle = new GUIStyle();
        private GUIStyle textStyle = new GUIStyle();
        private GUIStyle backgroundStyle = new GUIStyle();

        //Texture
        Texture2D CategoryTex = null;
        Texture2D SelectedCategoryTex = null;
        Texture2D NodeTex = null;
        Texture2D Tasuke = null;
        Texture2D Header = null;
        Texture2D RoundTexture = null;

        //Category
        private Color PickedColor = Color.white;
        private int MaxId = 0;
        private TCategoryNode ReziseId = null;
        private bool isRezising = false;
        private bool horizontal = false;
        private bool vertical = false;
        private TCategoryNode EditNode = null;
        private bool isDragging = false;
        private GUIContent[] menuOptions;

        //Params
        private string windowsTitle = "New Tasuke Board";
        private string lastLoadedFilePath = "";
        private float showSaveMessage = 0.0f;
        private string lastpath = string.Empty;
        private bool styleInit = false;

        #region Initialization

        [MenuItem("Window/Tasuke!")]
        public static void Init()
        {
            TasukeBoard window = (TasukeBoard)EditorWindow.GetWindow(typeof(TasukeBoard));
            window.minSize = new Vector2(600.0f, 300.0f);
            window.wantsMouseMove = true;
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void RegisterDomainReload()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            ResetWindowState();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                ResetWindowState();
            }
        }

        private static void ResetWindowState()
        {
            TasukeBoard[] windows = Resources.FindObjectsOfTypeAll<TasukeBoard>();
            foreach (TasukeBoard window in windows)
            {
                window.ResetState();
            }
        }

        private void ResetState()
        {
            isSelecting = false;
            isDragging = false;
            isRezising = false;
            EditNode = null;
            ReziseId = null;
            horizontal = false;
            vertical = false;
            styleInit = false;

            LoadTextures();
            
            selectedNodes.Clear();

            if (!string.IsNullOrEmpty(lastLoadedFilePath) && System.IO.File.Exists(lastLoadedFilePath))
            {
                nodes.Clear();
                catnodes.Clear();
                LoadData(lastLoadedFilePath, true);
            }

            Repaint();
        }


        public static void Load(string path, bool relative = false)
        {
            TasukeBoard window = (TasukeBoard)EditorWindow.GetWindow(typeof(TasukeBoard));
            window.minSize = new Vector2(600.0f, 300.0f);
            window.wantsMouseMove = true;
            window.Show();
            window.LoadData(path, relative);
        }

        public void OnDisable()
        {
            styleInit = false;
            selectedNodes.Clear();
            nodes.Clear();
            catnodes.Clear();
        }

        public void LoadTextures()
        {
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
            RoundTexture = MakeRoundedRectTexture(400, 200, 20, new Color(0.2f, 0.2f, 0.2f, 0.8f));

            // Load icons and set up menu options
            GUIContent loadIcon = EditorGUIUtility.IconContent("Folder Icon");
            GUIContent saveIcon = EditorGUIUtility.IconContent("SaveAs");
            loadIcon.text = "Load Board";
            saveIcon.text = "Save Board";
            menuOptions = new GUIContent[] { loadIcon, saveIcon };
        }

        void OnEnable()
        {
            LoadTextures();
            styleInit = false;
            wantsMouseMove = true;
            this.Focus();
            Repaint();
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

            textStyle = new GUIStyle();
            textStyle.fontSize = 40;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
            backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = RoundTexture;
        }
        #endregion

        #region Zoom
        private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return (screenCoords - _zoomArea.TopLeft()) / _zoom + _zoomCoordsOrigin;
        }

        private Vector2 ConvertZoomCoordsToScreenCoords(Vector2 zoomCoords)
        {
            return (zoomCoords - _zoomCoordsOrigin) * _zoom + _zoomArea.TopLeft();
        }

        private Rect GetZoomedRectOffset(Rect rect)
        {
            return new Rect(rect.x - _zoomCoordsOrigin.x, rect.y - _zoomCoordsOrigin.y, rect.width, rect.height);
        }

        private Rect GetZoomedRect(Rect originalRect)
        {
            Vector2 topLeft = ConvertScreenCoordsToZoomCoords(originalRect.position);
            Vector2 size = originalRect.size / _zoom;
            return new Rect(topLeft, size);
        }

        private void DrawNonZoomArea()
        {
            float windowWidth = position.width;
            float windowHeight = position.height;

            // Reference resolution
            float referenceWidth = 1920.0f;
            float referenceHeight = 1080.0f;

            // Calculate the scaling factor based on the reference resolution
            float scaleFactor = Mathf.Min(windowWidth / referenceWidth, windowHeight / referenceHeight);

            // Calculate the new dimensions of the texture based on the scaling factor
            float drawWidth = Tasuke.width * scaleFactor;
            float drawHeight = Tasuke.height * scaleFactor;

            // Ensure the texture does not exceed its original size
            drawWidth = Mathf.Min(drawWidth, Tasuke.width);
            drawHeight = Mathf.Min(drawHeight, Tasuke.height);

            // Position the texture at the bottom-right corner
            float drawX = windowWidth - drawWidth;
            float drawY = windowHeight - drawHeight;

            Rect windowRect = new Rect(drawX, drawY, drawWidth, drawHeight);
            GUI.DrawTexture(windowRect, Tasuke);
        }

        private void HandleZoomAndPan()
        {
            if (EditNode != null || isRezising)
                return;

            // Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
            // as the zoom center instead of the top left corner of the zoom area. This is achieved by
            // maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
            if (Event.current.type == EventType.ScrollWheel)
            {
                Vector2 screenCoordsMousePos = Event.current.mousePosition;
                Vector2 delta = Event.current.delta;
                Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
                float zoomDelta = -delta.y / 150.0f;
                float oldZoom = _zoom;
                _zoom += zoomDelta;
                _zoom = Mathf.Clamp(_zoom, kZoomMin, kZoomMax);
                _zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - (oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin);

                Event.current.Use();
            }

            // Allow moving the zoom area's origin by dragging with the middle mouse button or dragging
            // with the left mouse button with Alt pressed.
            if (Event.current.type == EventType.MouseDrag &&
                (Event.current.button == 0 && Event.current.modifiers == EventModifiers.Alt) ||
                Event.current.button == 2)
            {
                Vector2 delta = Event.current.delta;
                delta /= _zoom;
                _zoomCoordsOrigin -= delta;
            }
        }
        #endregion

        #region Selection

        private void HandleSelectEvent(Event e, Vector2 mousePosition)
        {
            if (isRezising || EditNode != null)
                return;

            switch (e.type)
            {
                case EventType.MouseDown:

                    if (e.button == 0)
                    {
                        TNode nodeUnderMouse = GetNodeUnderMouse(mousePosition);

                        if (nodeUnderMouse != null && selectedNodes.Contains(nodeUnderMouse))
                        {
                            isDragging = true;
                        }
                        else if (nodeUnderMouse != null)
                        {
                            selectedNodes.Clear();
                            selectedNodes.Add(nodeUnderMouse);
                            isDragging = true;

                            if (nodeUnderMouse is TObjectNode)
                            {
                                Selection.objects = new Object[1] { ((TObjectNode)(nodeUnderMouse)).obj };
                            }
                        }
                        else
                        {
                            selectionStart = mousePosition;
                            selectionEnd = mousePosition;
                            isSelecting = true;
                            selectedNodes.Clear();
                        }

                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        isRezising = false;

                        if (isSelecting)
                        {
                            SelectNodesInRect(mousePosition);
                            isSelecting = false;
                        }
                        isDragging = false;

                        CheckNodeCategories();
                    }
                    break;
                case EventType.MouseDrag:

                    if (e.button == 0)
                    {
                        if (isDragging)
                        {
                            DragSelectedNode(e.delta / _zoom);
                            e.Use();
                        }
                        else if (isSelecting)
                        {
                            selectionEnd = mousePosition;
                            e.Use();
                        }
                    }
                    break;
            }
        }

        private void CheckNodeCategories()
        {
            foreach( TNode obj in nodes )
            {
                if (obj is TObjectNode)
                {
                    TObjectNode tobj = (TObjectNode)(obj);
                    tobj.categoryId = -1;
                    Rect zoomed = GetZoomedRectOffset(tobj.rect);

                    for (int c = 0; c < catnodes.Count; c++)
                    {
                        Rect catzoomed = GetZoomedRectOffset(catnodes[c].rect);
                        if (catzoomed.Overlaps(zoomed))
                        {
                            tobj.categoryId = catnodes[c].id;
                        }
                    }
                }
            }
        }

        private TNode GetNodeUnderMouse(Vector2 mousePosition)
        {
            TNode foundNode = null;

            foreach (var node in nodes)
            {
                if (node is TObjectNode && node.rect.Contains(mousePosition))
                {
                    return node;
                }
            }

            if (foundNode == null)
            {
                foreach (var node in nodes)
                {
                    if (!(node is TObjectNode) && node.rect.Contains(mousePosition))
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        private void SelectNodesInRect(Vector2 mousePosition)
        {
            Rect selectionRect = GetSelectionRect(mousePosition);
            Rect zoomedSelectionRect = GetZoomedRect(selectionRect);

            bool foundObjectNode = false;

            foreach (var node in nodes)
            {
                if (node is TObjectNode)
                {
                    Rect zoomedNodeRect = GetZoomedRect(node.rect);
                    if (zoomedSelectionRect.Overlaps(zoomedNodeRect))
                    {
                        selectedNodes.Add(node);
                        foundObjectNode = true;
                    }
                }
            }

            if (!foundObjectNode)
            {
                foreach (var node in nodes)
                {
                    if (!(node is TObjectNode))
                    {
                        Rect zoomedNodeRect = GetZoomedRect(node.rect);
                        if (zoomedSelectionRect.Overlaps(zoomedNodeRect))
                        {
                            selectedNodes.Add(node);
                        }
                    }
                }
            }

            List<Object> objects = new List<Object>();
            foreach( TObjectNode node in selectedNodes.OfType<TObjectNode>() )
            {
                objects.Add(node.obj);
            }

            Selection.objects = objects.ToArray();
        }

        private void DrawSelectionRect()
        {
            if (isSelecting)
            {
                // Convert the selection rectangle corners back to screen coordinates for drawing
                var screenTopLeft = ConvertZoomCoordsToScreenCoords(Vector2.Min(selectionStart, selectionEnd));
                var screenBottomRight = ConvertZoomCoordsToScreenCoords(Vector2.Max(selectionStart, selectionEnd));
                Rect rect = Rect.MinMaxRect(screenTopLeft.x, screenTopLeft.y, screenBottomRight.x, screenBottomRight.y);

                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 1f, 0.2f));
            }
        }

        private Rect GetSelectionRect(Vector2 mousePosition)
        {
            var topLeft = Vector2.Min(selectionStart, mousePosition);
            var bottomRight = Vector2.Max(selectionStart, mousePosition);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private void DragSelectedNode(Vector2 delta)
        {
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                selectedNodes[i].rect.position += delta;

                if(selectedNodes[i] is TCategoryNode ) 
                {
                    TCategoryNode cat = (TCategoryNode)selectedNodes[i];

                    for ( int n = 0; n < nodes.Count; n++ )
                    {
                        if (nodes[n] is TObjectNode)
                        {
                            TObjectNode obj = (TObjectNode)(nodes[n]);
                            if (obj.categoryId == cat.id)
                            {
                                nodes[n].rect.position += delta;
                            }
                        }
                    }
                }
            }
        }

        public void HandleRemove(Event e)
        {
            if (e.keyCode == KeyCode.Delete)
            {
                foreach (var node in selectedNodes)
                {
                    if (node is TCategoryNode)
                    {
                        catnodes.Remove((TCategoryNode)(node));
                    }

                    nodes.Remove(node);
                }

                selectedNodes.Clear();
            }
        }

        #endregion

        #region Node Creation

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
                            CreateNode(draggedObject, ConvertScreenCoordsToZoomCoords(evt.mousePosition));
                        }
                    }
                    break;
            }
        }

        void CreateNode(Object obj, Vector2 pos)
        {
            TObjectNode no = new TObjectNode(obj.name, obj, CalculateNodeRect(obj.name, pos));
            nodes.Add(no);
        }

        Rect CalculateNodeRect(string title, Vector2 pos)
        {
            Vector2 size = normalNodeStyle.CalcSize(new GUIContent(title));
            float width = size.x + 20; // Adding padding
            float height = 50; // Fixed height
            return new Rect(pos.x, pos.y, width, height);
        }

        #endregion

        #region Category

        public void HandleResize(TCategoryNode cat, Event e)
        {
            Rect rect = GetZoomedRectOffset(cat.rect);

            float resizeHandleSize = Mathf.Max(10.0f, 20.0f * (kZoomMax - _zoom));
            float edgeMargin = 20.0f; // Distance from the edge to start resizing

            Rect bottomRightHandleRect = new Rect(
                rect.xMax - resizeHandleSize,
                rect.yMax - resizeHandleSize,
                resizeHandleSize*2.0f,
                resizeHandleSize*2.0f);

            Rect bottomEdgeRect = new Rect(
                rect.xMin + edgeMargin,
                rect.yMax - resizeHandleSize,
                rect.width - edgeMargin * 2,
                resizeHandleSize);

            Rect rightEdgeRect = new Rect(
                rect.xMax - resizeHandleSize,
                rect.yMin + edgeMargin,
                resizeHandleSize,
                rect.height - edgeMargin * 2);

            // Draw the resize handles
            EditorGUIUtility.AddCursorRect(bottomEdgeRect, MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(rightEdgeRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(bottomRightHandleRect, MouseCursor.ResizeUpLeft);


            if (!isRezising)
            {
                // Check for mouse events related to resizing
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (bottomRightHandleRect.Contains(e.mousePosition))
                    {
                        horizontal = true;
                        vertical = true;
                        ReziseId = cat;
                        isRezising = true;
                        e.Use();
                    }
                    else if (bottomEdgeRect.Contains(e.mousePosition))
                    {
                        vertical = true;
                        ReziseId = cat;
                        isRezising = true;
                        e.Use();
                    }
                    else if (rightEdgeRect.Contains(e.mousePosition))
                    {
                        horizontal = true;
                        ReziseId = cat;
                        isRezising = true;
                        e.Use();
                    }
                }
            }

            if (isRezising && cat == ReziseId)
            {
                if ( e.type == EventType.MouseDrag)
                {
                    Vector2 zoomedMousePos = e.mousePosition;

                    float newWidth = rect.width;
                    float newHeight = rect.height;

                    if (horizontal)
                    {
                        newWidth = Mathf.Max(50, zoomedMousePos.x - rect.x);
                    }

                    if (vertical)
                    {
                        newHeight = Mathf.Max(50, zoomedMousePos.y - rect.y);
                    }

                    cat.rect.size = new Vector2(newWidth, newHeight);

                    CheckNodeCategories();

                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    isRezising = false;
                    horizontal = false;
                    vertical = false;
                    e.Use();
                }
            }
        }

        public void HandleEdit(TCategoryNode node, Event e)
        {
            Rect zoomed = GetZoomedRectOffset(node.rect);

            if (EditNode != null)
            {
                if (EditNode == node)
                {
                    node.title = GUI.TextField(new Rect(zoomed.x, zoomed.y, zoomed.width, 20), node.title);

                    if (e.keyCode == KeyCode.Return)
                    {
                        EditNode = null;
                    }
                }
            }
            else
            {
                if ( selectedNodes.Contains(node) && e.type == EventType.MouseDown && e.clickCount == 2)
                {
                    EditNode = node;
                }
            }
        }

        public void HandleColor(TCategoryNode cnd, Event e)
        {
            Rect rect = GetZoomedRectOffset(cnd.rect);

            float colorFieldSize = 20;
            float margin = 5;
            Rect colorFieldRect = new Rect(
                rect.xMin + colorFieldSize/2.0f,
                rect.yMax - colorFieldSize - margin,
                colorFieldSize,
                colorFieldSize);

            // Draw the color field and update the node's color
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUI.ColorField(colorFieldRect, GUIContent.none, cnd.color, false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                cnd.color = new Color(newColor.r, newColor.g, newColor.b, newColor.a);
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
            genericMenu.AddItem(new GUIContent("Create Category"), false, () => CreateCategory( ConvertScreenCoordsToZoomCoords(mousePosition)));
            genericMenu.ShowAsContext();
        }

        void CreateCategory(Vector2 mousePosition)
        {
            List<int> catId = new List<int>();
            catId.Clear();
            foreach (var catnode in catnodes)
            {
                catId.Add(catnode.id);
            }

            while(catId.Contains(MaxId))
            {
                MaxId++;
            }

            TCategoryNode tcn = new TCategoryNode(PickedColor, MaxId, "New Category", new Rect(mousePosition.x, mousePosition.y, 200, 300));
            catnodes.Add(tcn);
            nodes.Add(tcn);
        }

        #endregion

        #region Update & Draw

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

        private void DrawNodes()
        {
            nodes.Sort((x, y) =>
            {
                if (x is TCategoryNode && y is TObjectNode)
                {
                    return -1; // x comes before y
                }
                else if (x is TObjectNode && y is TCategoryNode)
                {
                    return 1; // x comes after y
                }
                else
                {
                    return 0; // x and y are treated as equal (or are of the same type)
                }
            });

            foreach (var node in nodes)
            {
                bool selected = (selectedNodes.Contains(node));
                Rect zoomed = GetZoomedRectOffset(node.rect);

                if (node is TObjectNode)
                {
                    TObjectNode objectNode = (TObjectNode)node;
                    GUI.Box(zoomed, node.title, normalNodeStyle);

                    if (selected)
                    {
                        GUI.Box(zoomed, node.title, selectedNodeStyle);
                    }

                    GUIContent iconContent = EditorGUIUtility.ObjectContent(objectNode.obj, objectNode.GetType());
                    Texture iconTexture = iconContent.image;
                    if (iconTexture != null)
                    {
                        if (objectNode.categoryId != -1)
                        {
                            for (int n = 0; n < catnodes.Count; n++)
                            {
                                if (catnodes[n].id == objectNode.categoryId)
                                {
                                    GUI.color = catnodes[n].color;
                                    break;
                                }
                            }
                        }

                        float iconSize = 30;
                        Rect iconBoxRect = new Rect(zoomed.center.x - iconSize / 2.0f, zoomed.y + 13, 40, 40);
                        Rect iconRect = new Rect(
                            iconBoxRect.x + (iconBoxRect.width - iconSize) / 2,
                            iconBoxRect.y + (iconBoxRect.height - iconSize) / 2,
                            iconSize,
                            iconSize);

                        GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit);
                        GUI.color = Color.white;
                    }
                }
                else if (node is TCategoryNode)
                {
                    TCategoryNode cnd = (TCategoryNode)(node);
                    GUI.color = cnd.color;

                    float ratio = 32.0f * (node.rect.height / zoomed.height);

                    GUI.Box(zoomed, "", selected ? selectedCategoryStyle : normalCategoryStyle);
                    Rect title = new Rect(
                        zoomed.x,
                        zoomed.y - ratio,
                        zoomed.width,
                        ratio
                    );

                    GUI.Box(title, cnd.title, headerCategoryStyle);
                    GUI.color = Color.white;
                    GUI.Box(title, cnd.title, headerTitleStyle);

                    HandleResize(cnd, Event.current);
                    HandleEdit(cnd, Event.current);

                    if(selected)
                    {
                        HandleColor(cnd, Event.current);
                    }
                }
            }
        }


        private void DrawZoomArea()
        {
            EditorZoomArea.Begin(_zoom, _zoomArea);
            DrawNodes();
            EditorZoomArea.End();
        }

        private void DrawGrid()
        {
            float size = gridSize * _zoom;

            int widthDivs = Mathf.CeilToInt(position.width / size);
            int heightDivs = Mathf.CeilToInt(position.height / size);

            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);

            Vector3 newOffset = -new Vector3(_zoomCoordsOrigin.x % size, _zoomCoordsOrigin.y % size, 0);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(size * i, -size, 0) + newOffset,
                                 new Vector3(size * i, position.height, 0) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-size, size * j, 0) + newOffset,
                                 new Vector3(position.width, size * j, 0) + newOffset);
            }

            Handles.EndGUI();
        }

        public void OnGUI()
        {
            if (Application.isPlaying)
                HandlePlayMode();
            else
            {
                if(!styleInit)
                {
                    BuildStyle();
                    styleInit = true;
                }

                _zoomArea = new Rect(0.0f, 0.0f, position.width, position.height);

                HandleZoomAndPan();

                Vector2 zoomedMousePosition = ConvertScreenCoordsToZoomCoords(Event.current.mousePosition);

                DrawMenuBar();
                DrawZoomArea();
                HandleSelectEvent(Event.current, zoomedMousePosition);
                DrawGrid();
                DrawNonZoomArea();
                DrawSelectionRect();
                HandleDragAndDrop();
                UpdateContextMenu();
                HandleRemove(Event.current);
                HandleQuickSave();
                Repaint();
            }
        }
        #endregion

        #region SaveMenu

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
                case 0: { LoadNodes(); } break;
                case 1: { SaveNode(); } break;
            }
        }

        private void HandleQuickSave()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.S)
            {
                if (!string.IsNullOrEmpty(lastLoadedFilePath))
                {
                    SaveNode(lastLoadedFilePath, true);
                }
                else
                {
                    // Optional: Show a message or open the Save File Panel if no file was previously loaded
                    EditorUtility.DisplayDialog("Save Error", "No file loaded to save. Please use Save As option.", "OK");
                }
            }

            if (showSaveMessage > 0.0f)
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

        public void HandlePlayMode()
        {
            float windowWidth = position.width;
            float windowHeight = position.height;
            Vector2 textSize = textStyle.CalcSize(new GUIContent("APPLICATION IS PLAYING"));
            Rect backgroundRect = new Rect((windowWidth - textSize.x - 40) / 2, (windowHeight - textSize.y - 20) / 2, textSize.x + 40, textSize.y + 20);
            Rect textRect = new Rect((windowWidth - textSize.x) / 2, (windowHeight - textSize.y) / 2, textSize.x, textSize.y);
            GUI.Box(backgroundRect, GUIContent.none, backgroundStyle);
            GUI.Label(textRect, "APPLICATION IS PLAYING", textStyle);
        }

        private Texture2D MakeRoundedRectTexture(int width, int height, int cornerRadius, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate distances to the nearest edge
                    int xDistance = Mathf.Min(x, width - x);
                    int yDistance = Mathf.Min(y, height - y);

                    // Calculate distance to the nearest corner
                    float distanceToCorner = Mathf.Sqrt(Mathf.Pow(xDistance - cornerRadius, 2) + Mathf.Pow(yDistance - cornerRadius, 2));

                    // If inside rounded corner area, set alpha to zero
                    if (xDistance < cornerRadius && yDistance < cornerRadius && distanceToCorner > cornerRadius)
                    {
                        pixels[x + y * width] = Color.clear;
                    }
                    else
                    {
                        pixels[x + y * width] = color;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private void SaveNode(string filePath = null, bool relative = false)
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
                nodeWrapper.onodes = nodes.OfType<TObjectNode>().ToList();
                nodeWrapper.cnodes = nodes.OfType<TCategoryNode>().ToList();

                string relativePath = relative ? path : "Assets" + path.Substring(Application.dataPath.Length);

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
            lastpath = path;
            selectedNodes.Clear();
            nodes.Clear();
            catnodes.Clear();

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                nodes.Clear();
                catnodes.Clear();
                windowsTitle = Path.GetFileNameWithoutExtension(path);
                EditorWindow.GetWindow(typeof(TasukeBoard)).titleContent = new GUIContent(windowsTitle, "");

                string relativePath = relative ? path : "Assets" + path.Substring(Application.dataPath.Length);
                lastLoadedFilePath = relativePath;
                TkData nodeWrapper = AssetDatabase.LoadAssetAtPath<TkData>(relativePath);

                foreach (TCategoryNode node in nodeWrapper.cnodes)
                {
                    catnodes.Add(node);
                    nodes.Add(node);
                }

                foreach ( TNode node in nodeWrapper.onodes)
                {
                    nodes.Add(node);
                }
            }
        }

        #endregion
    }
}
#endif