using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using System.Linq;
using System;

namespace ArdenfallEditor.Utility
{
    public class AssetLibraryWindow : EditorWindow
    {
        [MenuItem("Tools/Prefab Library")]
        public static void OpenWindow()
        {
            AssetLibraryWindow window = AssetLibraryWindow.GetWindow<AssetLibraryWindow>();
            window.titleContent = new GUIContent("Prefab Library");
        }

        public static GUIStyle toggleButtonStyleNormal = null;
        public static GUIStyle toggleButtonStyleToggled = null;

        public int buttonSize = 80;

        private List<AssetLibraryTool> tools;
        private int currentTool;
        private void OnEnable()
        {
            var types = TypeCache.GetTypesDerivedFrom<AssetLibraryTool>().ToList();

            tools = new List<AssetLibraryTool>();
            foreach (var type in types)
            {
                if(!type.IsAbstract)
                    tools.Add((AssetLibraryTool)Activator.CreateInstance(type));
            }

            foreach (var tool in tools)
            {
                tool.Init();
                tool.SetRedraw(this.Repaint);
            }

            currentTool = 0;
        }

        protected void OnGUI()
        {
            BuildStyles();

            DrawTopbar();
            tools[currentTool].DrawAssetList(position.height,buttonSize);

        }

        private void Update()
        {
            //if(this.position.Contains(Event.current.mousePosition))
                Repaint();
        }

        private void DrawTopbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            //Tool selector
            if (GUILayout.Button(tools[currentTool].ToolName(), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GenericMenu menu = new GenericMenu();

                for(int i = 0; i < tools.Count; i++)
                {
                    int index = i;
                    menu.AddItem(new GUIContent(tools[i].ToolName()), currentTool == i, () => { SelectTool(index); });
                }

                menu.ShowAsContext();
            }

            tools[currentTool].DrawTopbar();

            EditorGUILayout.EndHorizontal();
        }

        private void SelectTool(int index)
        {
            currentTool = index;
        }

        protected void OnDestroy()
        {
            for (int i = 0; i < tools.Count; i++)
                tools[i].OnDestroy();
        }

        private void BuildStyles()
        {
            if(toggleButtonStyleNormal == null)
                toggleButtonStyleNormal = "button";

            if(toggleButtonStyleToggled == null)
            {
                toggleButtonStyleToggled = new GUIStyle(toggleButtonStyleNormal);
                toggleButtonStyleToggled.normal.background = toggleButtonStyleNormal.active.background;
                toggleButtonStyleToggled.hover.background = toggleButtonStyleNormal.active.background;
                toggleButtonStyleToggled.focused.background = toggleButtonStyleNormal.active.background;
                toggleButtonStyleToggled.active.background = toggleButtonStyleNormal.active.background;
            }
        }

    }

    public static class SceneDragAndDrop
    {
        private static readonly int sceneDragHint = "SceneDragAndDrop".GetHashCode();
        private const string DRAG_ID = "SceneDragAndDrop";

        private static readonly UnityEngine.Object[] emptyObjects = new UnityEngine.Object[0];
        private static readonly string[] emptyPaths = new string[0];

        private struct DragData
        {
            public DragData(ISceneDragReceiver receiver,object data)
            {
                this.receiver = receiver;
                this.data = data;
            }

            public ISceneDragReceiver receiver;
            public object data;
        }

        public static void StartDrag(ISceneDragReceiver receiver, string title,object data=null)
        {
            //stop any drag before starting a new one

            StopDrag();

            if (receiver != null)
            {
                //make sure we release any control from something that has it
                //this is done because SceneView delegate needs DragEvents!

                GUIUtility.hotControl = 0;

                //do the necessary steps to start a drag
                //we set the GenericData to our receiver so it can handle

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = emptyObjects;
                DragAndDrop.paths = emptyPaths;
                DragAndDrop.SetGenericData(DRAG_ID, new DragData(receiver,data));

                receiver.StartDrag(data);

                //start drag and listen for Scene drop

                DragAndDrop.StartDrag(title);

                SceneView.duringSceneGui += OnSceneGUI;
            }
        }

        public static void StopDrag()
        {
            //cleanup delegate
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            //get a controlId so we can grab events

            int controlId = GUIUtility.GetControlID(sceneDragHint, FocusType.Passive);

            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlId);

            ISceneDragReceiver receiver;
            DragData dragData;

            switch (eventType)
            {

                case EventType.DragPerform:
                case EventType.DragUpdated:

                    //check that GenericData is the expected type
                    //if not, we do nothing
                    //it would seem that whenever a Drag is started, GenericData is cleared, so we don't have to explicitly clear it ourself

                    try
                    {
                        dragData = (DragData)DragAndDrop.GetGenericData(DRAG_ID);
                        receiver = dragData.receiver;
                    } catch
                    {
                        return;
                    }

                    if (receiver != null)
                    {
                        //let receiver handle the drag functionality

                        DragAndDrop.visualMode = receiver.UpdateDrag(evt, eventType, dragData.data);

                        //perform drag if accepted

                        if (eventType == EventType.DragPerform && DragAndDrop.visualMode != DragAndDropVisualMode.None)
                        {

                            receiver.PerformDrag(evt, dragData.data);

                            DragAndDrop.AcceptDrag();
                            DragAndDrop.SetGenericData(DRAG_ID, default(ISceneDragReceiver));

                            //we can safely stop listening to scene gui now

                            StopDrag();
                        }

                        evt.Use();
                    }

                    break;

                case EventType.DragExited:

                    //Drag exited, This can happen when:
                    // - focus left the SceneView
                    // - user cancelled manually (Escape Key)
                    // - user released mouse
                    //So we want to inform the receiver (if any) that is was cancelled, and it can handle appropriatley
                    try
                    {
                        dragData = (DragData)DragAndDrop.GetGenericData(DRAG_ID);
                        receiver = dragData.receiver;
                    } catch { return; }

                    if (receiver != null)
                    {

                        receiver.StopDrag(dragData.data);
                        evt.Use();
                    }

                    break;
            }
        }
    }

    public interface ISceneDragReceiver
    {

        void StartDrag(object data);
        void StopDrag(object data);

        DragAndDropVisualMode UpdateDrag(Event evt, EventType eventType, object data);

        void PerformDrag(Event evt, object data);
    }

}