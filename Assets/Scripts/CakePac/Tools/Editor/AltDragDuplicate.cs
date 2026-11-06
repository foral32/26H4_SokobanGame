using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Alt + Move gizmo axis drag duplicates once; empty-space Alt navigation stays native.</summary>
[InitializeOnLoad]
public static class AltDragDuplicate
{
    private const string MenuPath = "Tools/Scene/Alt 拖轴复制";
    private const string PreferenceKey = "CakePac.AltDragDuplicate.Enabled";
    private const string UndoName = "Alt Drag Duplicate";
    private static readonly int ControlHint = "AltDragDuplicate".GetHashCode();
    private static Drag drag;

    private sealed class Drag
    {
        public SceneView view;
        public Vector2 mouseStart;
        public Vector3 pivot, direction;
        public int axis, control, undoGroup = -1;
        public Object[] originalSelection;
        public Transform[] copies;
        public Vector3[] positions;
    }

    static AltDragDuplicate()
    {
        SceneView.beforeSceneGui += OnSceneGUI;
        AssemblyReloadEvents.beforeAssemblyReload += Finish;
        EditorApplication.update += CheckFocus;
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(PreferenceKey, !EditorPrefs.GetBool(PreferenceKey, true));
        Finish();
        SceneView.RepaintAll();
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateMenu()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PreferenceKey, true));
        return true;
    }

    private static void CheckFocus()
    {
        if (drag != null && (drag.view == null || !drag.view.hasFocus
            || EditorApplication.isPlayingOrWillChangePlaymode))
            Finish();
    }

    private static void OnSceneGUI(SceneView view)
    {
        Event evt = Event.current;
        int control = GUIUtility.GetControlID(ControlHint, FocusType.Passive);
        if (drag != null)
        {
            if (view != drag.view) return;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                GUIUtility.hotControl = 0;
                evt.Use();
                return;
            }
            if (evt.rawType == EventType.MouseUp && evt.button == 0)
            {
                Finish();
                GUIUtility.hotControl = 0;
                evt.Use();
                return;
            }
            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                // Native line translation supports perspective and axis constraints.
                float distance = HandleUtility.CalcLineTranslation(
                    drag.mouseStart, evt.mousePosition, drag.pivot, drag.direction);
                if (evt.control || evt.command)
                    distance = Handles.SnapValue(distance, EditorSnapSettings.move[drag.axis]);
                if (Mathf.Abs(distance) > 0.0001f || drag.copies != null)
                {
                    try
                    {
                        if (drag.copies == null) Duplicate();
                        MoveCopies(drag.direction * distance);
                    }
                    catch (System.Exception exception)
                    {
                        Cancel();
                        Debug.LogException(exception);
                    }
                }
                evt.Use();
                view.Repaint();
            }
            return;
        }

        if (!EditorPrefs.GetBool(PreferenceKey, true) || EditorApplication.isPlayingOrWillChangePlaymode
            || Tools.current != Tool.Move || Tools.hidden || GUIUtility.hotControl != 0
            || evt.type != EventType.MouseDown || evt.button != 0 || !evt.alt
            || evt.shift || evt.control || evt.command)
            return;

        Transform[] roots = GetEditableRoots();
        if (roots.Length == 0) return;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        foreach (var root in roots)
            if (stage != null && root.gameObject == stage.prefabContentsRoot)
                return; // Unity does not allow duplicating the isolated prefab root.

        Vector3 pivot = Tools.handlePosition;
        if (view.camera.WorldToViewportPoint(pivot).z <= 0f) return;
        Quaternion rotation = Tools.handleRotation;
        float size = HandleUtility.GetHandleSize(pivot);
        float bestDistance = 8f;
        int bestAxis = -1;
        Vector3 direction = Vector3.zero;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 unit = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            Vector3 candidate = rotation * unit;
            // Exclude the centre and plane handles: only the visible axis shaft/arrow starts duplication.
            float hitDistance = HandleUtility.DistanceToLine(
                pivot + candidate * size * 0.25f, pivot + candidate * size);
            if (hitDistance >= bestDistance) continue;
            bestDistance = hitDistance;
            bestAxis = axis;
            direction = candidate;
        }
        if (bestAxis < 0) return;

        drag = new Drag
        {
            view = view, mouseStart = evt.mousePosition, pivot = pivot,
            direction = direction, axis = bestAxis, control = control, originalSelection = Selection.objects
        };
        GUIUtility.hotControl = control;
        evt.Use();
    }

    private static Transform[] GetEditableRoots()
    {
        var roots = new List<Transform>();
        Transform[] selected = Selection.transforms;
        var set = new HashSet<Transform>(selected);
        foreach (var item in selected)
        {
            if (item == null || EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid()
                || (item.gameObject.hideFlags & HideFlags.NotEditable) != 0) continue;
            bool ancestorSelected = false;
            for (Transform parent = item.parent; parent != null; parent = parent.parent)
                if (set.Contains(parent)) { ancestorSelected = true; break; }
            if (!ancestorSelected) roots.Add(item);
        }
        return roots.ToArray();
    }

    private static void Duplicate()
    {
        Undo.IncrementCurrentGroup();
        drag.undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        Transform[] sources = GetEditableRoots();
        var objects = new GameObject[sources.Length];
        for (int i = 0; i < sources.Length; i++) objects[i] = sources[i].gameObject;
        // The pasteboard command can be ignored while a handle owns hotControl.
        // Use the Unity 6 object API and its returned roots, never Selection as a result.
        GameObject[] duplicated = GameObjectUtility.DuplicateGameObjects(objects);
        if (duplicated == null || duplicated.Length != sources.Length)
            throw new System.InvalidOperationException("Alt 拖轴复制失败：Unity 未返回完整副本。");
        var originals = new HashSet<Transform>(sources);
        drag.copies = new Transform[duplicated.Length];
        for (int i = 0; i < duplicated.Length; i++)
        {
            if (duplicated[i] == null || originals.Contains(duplicated[i].transform))
                throw new System.InvalidOperationException("Alt 拖轴复制失败：禁止移动原物体。");
            drag.copies[i] = duplicated[i].transform;
        }
        Selection.objects = duplicated;
        drag.positions = new Vector3[drag.copies.Length];
        for (int i = 0; i < drag.copies.Length; i++)
            drag.positions[i] = drag.copies[i].position;
    }

    private static void MoveCopies(Vector3 offset)
    {
        Undo.RecordObjects(drag.copies, UndoName);
        for (int i = 0; i < drag.copies.Length; i++)
        {
            Transform copy = drag.copies[i];
            if (copy == null) continue;
            copy.position = drag.positions[i] + offset;
            PrefabUtility.RecordPrefabInstancePropertyModifications(copy);
            EditorSceneManager.MarkSceneDirty(copy.gameObject.scene);
        }
    }

    private static void Cancel()
    {
        Drag cancelled = drag;
        drag = null;
        if (GUIUtility.hotControl == cancelled.control) GUIUtility.hotControl = 0;
        if (cancelled.undoGroup >= 0)
            Undo.RevertAllDownToGroup(cancelled.undoGroup);
        Selection.objects = cancelled.originalSelection;
    }

    private static void Finish()
    {
        if (drag == null) return;
        if (GUIUtility.hotControl == drag.control) GUIUtility.hotControl = 0;
        if (drag.undoGroup >= 0)
        {
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(drag.undoGroup);
        }
        drag = null;
    }
}
