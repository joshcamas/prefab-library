using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace ArdenfallEditor.Utility
{
    public abstract class AssetLibraryTool : ISceneDragReceiver
    {
        private GameObject ghostObject;
        private GameObject ghostObjectPrefab;
        private bool isDragging;

        private Vector2 scroll;
        private Action Redraw;

        private int hoveredItem;
        private int hoveredItemDirection;
        private Vector2 hoveredPosition;
        private GUIStyle hoverAreaStyle;

        protected GUIStyle HoverAreaStyle
        {
            get
            {
                if(hoverAreaStyle == null || hoverAreaStyle.normal.background == null)
                {
                    hoverAreaStyle = new GUIStyle();
                    hoverAreaStyle.normal.background = AssetLibraryTool.CreatePixelTexture(new Color(.22f, .22f, .22f));
                }

                return hoverAreaStyle;
            }
        }


        protected struct LibraryItem
        {
            public Texture2D thumbnail;
            public string tooltip;
            public bool isSelected;
        }

        public void SetRedraw(Action Redraw)
        {
            this.Redraw = Redraw;
        }

        public abstract string ToolName();

        public abstract void Init();

        public abstract void DrawTopbar();

        public abstract void OnDestroy();

        protected abstract int GetItemCount();

        protected abstract LibraryItem GetItem(int index);

        protected abstract void OnClickItem(int index);

        protected virtual bool EnableHoverContent()
        {
            return false;
        }

        protected virtual void DrawHoverContainer(Vector2 position, int direction, int index)
        {
            float buttonWidth = 80;
            float width = 300;

            var rect = new Rect(position.x, position.y, width, 200);

            if (direction == 0)
                rect.x = position.x - buttonWidth/2 + 10;

            if (direction == 1)
                rect.x = position.x - width / 4;

            else if (direction == 2)
                rect.x = position.x - width / 2;

            else if (direction == 3)
                rect.x = position.x - width / 1.5f;

            else if (direction == 4)
                rect.x = position.x - width + buttonWidth/2 - 10;

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            //if(direction == 1 || direction == 2)
            //    GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(HoverAreaStyle);

            try
            {
                DrawHoverContent(index);
            }
            catch { }

            GUILayout.EndVertical();

            //SirenixEditorGUI.DrawBorders(GUILayoutUtility.GetLastRect(), 1);

            //if (direction == 0 || direction == 1)
            //GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        protected virtual void DrawHoverContent(int index)
        {
        }

        protected virtual bool EnableDragIntoScene()
        {
            return false;
        }

        protected virtual GameObject CreateGhostPrefab(int index)
        {
            return null;
        }

        protected virtual void OnPlaceInScene(int index, Vector3 position)
        {

        }

        public void DrawAssetList(float windowHeight, int buttonSize)
        {
            float windowWidth = EditorGUIUtility.currentViewWidth;
            int numberOfCols = (int)((windowWidth - 5) / (buttonSize));
            int numberOfRows = GetItemCount() / numberOfCols + 1;

            Rect fullRect = EditorGUILayout.GetControlRect(false, buttonSize * numberOfRows, GUILayout.Width(windowWidth - 25));

            Rect viewRect = new Rect(fullRect.x, fullRect.y, fullRect.width + 15, windowHeight - fullRect.y);

            scroll = GUI.BeginScrollView(viewRect, scroll, fullRect, false, true);

            Rect scrolledViewRect = new Rect(viewRect.position + scroll, viewRect.size);

            int row = 0;
            int col = 0;
            int c = 0;

            bool isVisible = hoveredItem != -1;

            if (Event.current.type != EventType.Layout)
                hoveredItem = -1;

            for (int i = 0; i < GetItemCount(); i++)
            {
                if (GetItem(i).thumbnail == null)
                    continue;

                if (c % numberOfCols == 0 && c != 0)
                {
                    col = 0;
                    row++;
                }
                else if (c != 0)
                {
                    col++;
                }

                Rect buttonRect = new Rect(fullRect.x + col * buttonSize, fullRect.y + row * buttonSize, buttonSize, buttonSize);

                if (buttonRect.Overlaps(scrolledViewRect))
                    DrawAssetButton(buttonRect, i);

                if (buttonRect.Contains(Event.current.mousePosition) && scrolledViewRect.Contains(Event.current.mousePosition))
                {
                    hoveredPosition = buttonRect.position + new Vector2(buttonRect.size.x/2, buttonRect.size.y) - scroll;
                    hoveredItem = i;

                    if (col == 0)
                        hoveredItemDirection = 0;
                    
                    else if (col == 1)
                        hoveredItemDirection = 1;
                    else if ((c + 2) % numberOfCols == 0)
                        hoveredItemDirection = 3;
                    else if ((c + 1) % numberOfCols == 0)
                        hoveredItemDirection = 4;
                    else
                        hoveredItemDirection = 2;
                }
                
                c++;
            }

            GUI.EndScrollView();
            if(hoveredItem != -1 && EnableHoverContent() && !(!isVisible && Event.current.type == EventType.Repaint))
            {
                DrawHoverContainer(hoveredPosition, hoveredItemDirection, hoveredItem);
            }

        }

        private bool DrawAssetButton(Rect rect, int itemIndex)
        {
            LibraryItem item = GetItem(itemIndex);

            if (item.thumbnail == null)
                return false;

            bool selected = item.isSelected;

            //Select in inspector
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                    selected = true;

                else if (Event.current.type == EventType.MouseUp)
                {
                    selected = true;

                    OnClickItem(itemIndex);

                    if (!isDragging)
                        Event.current.Use();
                }
                else
                {
                    
                }
            }

            //Drag and drop
            if (EnableDragIntoScene())
            {
                if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                    SceneDragAndDrop.StartDrag(this, "ring_and_ding", itemIndex);
            }

            var style = selected ? AssetLibraryWindow.toggleButtonStyleToggled : AssetLibraryWindow.toggleButtonStyleNormal;
            GUI.Box(rect, new GUIContent(item.thumbnail, item.tooltip), style);

            return true;
        }

        protected string SingleSelectDropdown(string label, string selected, List<string> options, GUIStyle style, params GUILayoutOption[] guiOptions)
        {
            if (options.Count == 0)
                return null;

            int index = options.IndexOf(selected);

            int newIndex = EditorGUILayout.Popup(label, index, options.ToArray(), style, guiOptions);

            if (newIndex >= 0 && newIndex < options.Count)
                return options[newIndex];
            else
                return null;
        }


        protected List<string> MultiSelectDropdown(string label, List<string> selected, List<string> options, GUIStyle style, params GUILayoutOption[] guiOptions)
        {
            if (options == null || options.Count == 0)
                return new List<string>();

            int mask = 0;

            if (selected == null)
                selected = new List<string>();

            for (int i = 0; i < options.Count; i++)
            {
                if (selected.Contains(options[i]))
                    mask |= 1 << i;
            }

            if (label != null)
                mask = EditorGUILayout.MaskField(label, mask, options.ToArray(), style, guiOptions);
            else
                mask = EditorGUILayout.MaskField(mask, options.ToArray(), style, guiOptions);

            List<string> newSelected = new List<string>();

            for (int i = 0; i < options.Count; i++)
            {
                if ((mask & (1 << i)) != 0)
                    newSelected.Add(options[i]);
            }

            return newSelected;
        }


        void ISceneDragReceiver.StartDrag(object data)
        {
            isDragging = true;

            if (ghostObject != null)
                GameObject.DestroyImmediate(ghostObject);
        }

        void ISceneDragReceiver.StopDrag(object data)
        {
            isDragging = false;

            if (ghostObject != null)
                GameObject.DestroyImmediate(ghostObject);
        }

        DragAndDropVisualMode ISceneDragReceiver.UpdateDrag(Event evt, EventType eventType, object data)
        {
            int index = (int)data;
            Vector3 mousePosition = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (SceneView.lastActiveSceneView == null)
                return DragAndDropVisualMode.Generic;

            if (ghostObject == null)
            {
                ghostObject = CreateGhostPrefab(index);
                ghostObject.transform.rotation = Quaternion.identity;
                ghostObject.hideFlags = HideFlags.HideAndDontSave;
            }
            Vector3 hitPoint;

            if (Raycast(ray, 1000, ghostObject, out hitPoint))
                ghostObject.transform.position = hitPoint;
            else
                ghostObject.transform.position = ray.origin + ray.direction * 10; 

            return DragAndDropVisualMode.Generic;
        }

        void ISceneDragReceiver.PerformDrag(Event evt, object data)
        {
            isDragging = false;

            int index = (int)data;

            if (SceneView.lastActiveSceneView == null)
                return;

            Vector3 mousePosition = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Vector3 hitPoint;

            if (Raycast(ray, 1000, ghostObject, out hitPoint))
                OnPlaceInScene(index, hitPoint);
            else
                OnPlaceInScene(index, ray.origin + ray.direction * 10);

            if (ghostObject != null)
                GameObject.DestroyImmediate(ghostObject);

        }

        private bool Raycast(Ray ray, float distance, GameObject ignoreObject, out Vector3 hitPoint)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray.origin, ray.direction, distance);

            //Find closest point 
            int closestHit = -1;
            float closestDistance = 0;

            for(int i = 0; i < hits.Length; i++)
            {
                if(ignoreObject != null)
                {
                    if (hits[i].collider.gameObject == ignoreObject)
                        continue;

                    Transform test = hits[i].collider.transform;
                    bool isChild = false;

                    while(test.parent != null)
                    {
                        if (test.parent == ignoreObject.transform)
                        {
                            isChild = true;
                            break;
                        }
                        test = test.parent;
                    }

                    if (isChild)
                        continue;
                }
                
                if(closestHit == -1 || Vector3.Distance(hits[i].point,ray.origin) < closestDistance)
                {
                    closestDistance = Vector3.Distance(hits[i].point, ray.origin);
                    closestHit = i;
                }
            }

            if(closestHit == -1)
            {
                hitPoint = Vector3.zero;
                return false;
            }

            hitPoint = hits[closestHit].point;
            return true;
        }


        public static Texture2D CreatePixelTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }


    }
}
 