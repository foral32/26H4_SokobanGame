using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AltDragDuplicateValidation
{
    private static int passed;
    private static string temporaryPrefabPath;

    [MenuItem("Tools/Scene/验证 Alt 拖轴复制")]
    private static void RunFromMenu()
    {
        if (EditorUtility.DisplayDialog("验证 Alt 拖轴复制",
            "将在独立临时场景验证复制、移动与 Undo/Redo，不保存关卡。\n验证会使用撤销栈，清除现有的 Redo 分支。是否继续？", "运行", "取消"))
            Run();
    }

    [MenuItem("Tools/Scene/验证 Alt 拖轴复制", true)]
    private static bool CanRun()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
            && GUIUtility.hotControl == 0;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("仅用于隔离批处理验证");
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/AltDuplicateBootstrap.unity");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, path);
        try { Run(); }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(path);
        }
    }

    public static void Run()
    {
        if (!CanRun()) throw new InvalidOperationException("请在空闲编辑模式运行验证。");
        var toolType = typeof(AltDragDuplicate);
        if (toolType.GetField("drag", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) != null)
            throw new InvalidOperationException("请先结束当前拖轴操作。");
        Scene previousScene = SceneManager.GetActiveScene();
        UnityEngine.Object[] previousSelection = Selection.objects;
        Event previousEvent = Event.current;
        Scene testScene = default;
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        passed = 0;
        try
        {
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
            var original = new GameObject("Original");
            original.transform.position = new Vector3(2, 3, 4);
            var child = new GameObject("Child");
            child.transform.SetParent(original.transform);
            Selection.objects = new UnityEngine.Object[] { original, child };
            var type = typeof(AltDragDuplicate);
            var dragType = type.GetNestedType("Drag", BindingFlags.NonPublic);
            var drag = Activator.CreateInstance(dragType);
            dragType.GetField("originalSelection").SetValue(drag, Selection.objects);
            dragType.GetField("control").SetValue(drag, 12345);
            type.GetField("drag", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, drag);
            Event.current = new Event { type = EventType.MouseDrag, button = 0, alt = true };
            GUIUtility.hotControl = 12345;
            Call("Duplicate");
            var copies = (Transform[])dragType.GetField("copies").GetValue(drag);
            Check(copies.Length == 1 && copies[0] != original.transform, "duplicate returns new top-level object");
            Check(copies[0].childCount == 1, "selected parent and child duplicated once");
            Call("MoveCopies", Vector3.right * 5f);
            Check(original.transform.position == new Vector3(2, 3, 4), "original stays in place");
            Check(copies[0].position == new Vector3(7, 3, 4), "copy receives drag offset");
            Call("MoveCopies", Vector3.right * 8f);
            Check(copies[0].position == new Vector3(10, 3, 4), "continued drag does not accumulate offset");
#if UNITY_6000_0_OR_NEWER
            var id = copies[0].gameObject.GetEntityId();
#else
            // Unity 2022.3 没有 EntityId：用存活引用判销毁，重做后按场景、名称与位置找回副本。
            Transform tracked = copies[0];
            string trackedName = tracked.name;
#endif
            Call("Finish");
            Undo.PerformUndo();
#if UNITY_6000_0_OR_NEWER
            Check(EditorUtility.EntityIdToObject(id) == null && original != null, "one undo removes copy and keeps original");
#else
            Check(tracked == null && original != null, "one undo removes copy and keeps original");
#endif
            Undo.PerformRedo();
#if UNITY_6000_0_OR_NEWER
            var redone = EditorUtility.EntityIdToObject(id) as GameObject;
#else
            GameObject redone = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsOfType<Transform>())
                if (candidate != original.transform && candidate.gameObject.scene == testScene
                    && candidate.name == trackedName && candidate.position == new Vector3(10, 3, 4))
                { redone = candidate.gameObject; break; }
#endif
            Check(redone != null && redone.transform.position == new Vector3(10, 3, 4), "redo restores copy and final position");

            Selection.objects = new UnityEngine.Object[] { original };
            drag = Activator.CreateInstance(dragType);
            dragType.GetField("originalSelection").SetValue(drag, Selection.objects);
            type.GetField("drag", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, drag);
            Call("Duplicate");
            copies = (Transform[])dragType.GetField("copies").GetValue(drag);
#if UNITY_6000_0_OR_NEWER
            var cancelledId = copies[0].gameObject.GetEntityId();
#else
            Transform cancelledCopy = copies[0];
#endif
            Call("MoveCopies", Vector3.up);
            Call("Cancel");
#if UNITY_6000_0_OR_NEWER
            Check(EditorUtility.EntityIdToObject(cancelledId) == null && Selection.activeGameObject == original, "cancel removes copy and restores selection");
#else
            Check(cancelledCopy == null && Selection.activeGameObject == original, "cancel removes copy and restores selection");
#endif

            temporaryPrefabPath = AssetDatabase.GenerateUniqueAssetPath("Assets/AltDragDuplicateValidation.prefab");
            var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var prefab = PrefabUtility.SaveAsPrefabAsset(sample, temporaryPrefabPath);
            UnityEngine.Object.DestroyImmediate(sample);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.localScale = new Vector3(6, -1.5f, 3.5f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            Selection.activeGameObject = instance;
            drag = Activator.CreateInstance(dragType);
            dragType.GetField("originalSelection").SetValue(drag, Selection.objects);
            dragType.GetField("control").SetValue(drag, 12345);
            type.GetField("drag", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, drag);
            GUIUtility.hotControl = 12345;
            Call("Duplicate");
            copies = (Transform[])dragType.GetField("copies").GetValue(drag);
            Check(PrefabUtility.GetCorrespondingObjectFromSource(copies[0].gameObject) == prefab, "prefab link retained during captured drag");
            Check(copies[0].localScale == instance.transform.localScale, "negative scale override retained");
            Check(copies[0].GetComponent<MeshFilter>().sharedMesh == instance.GetComponent<MeshFilter>().sharedMesh, "mesh reference retained without shape reset");
            Call("Cancel");

            var a = new GameObject("A", typeof(Rigidbody), typeof(FixedJoint));
            var b = new GameObject("B", typeof(Rigidbody));
            a.GetComponent<FixedJoint>().connectedBody = b.GetComponent<Rigidbody>();
            Selection.objects = new UnityEngine.Object[] { a, b };
            drag = Activator.CreateInstance(dragType);
            dragType.GetField("originalSelection").SetValue(drag, Selection.objects);
            dragType.GetField("control").SetValue(drag, 12345);
            type.GetField("drag", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, drag);
            GUIUtility.hotControl = 12345;
            Call("Duplicate");
            copies = (Transform[])dragType.GetField("copies").GetValue(drag);
            Check(copies.Length == 2, "multi-selection returns both copies");
            FixedJoint copiedJoint = null;
            foreach (var copy in copies)
                if (copy.GetComponent<FixedJoint>() != null) copiedJoint = copy.GetComponent<FixedJoint>();
            Check(copiedJoint != null && copiedJoint.connectedBody != null
                && copiedJoint.connectedBody != b.GetComponent<Rigidbody>()
                && Array.Exists(copies, copy => copy == copiedJoint.connectedBody.transform), "cross-object reference remapped to copied selection");
            Call("Cancel");
            Debug.Log($"[Alt 拖轴复制验证] {passed} 项通过；未保存场景。实际鼠标手柄命中需人工验收。");
        }
        catch (Exception e)
        {
            Debug.LogError("[Alt 拖轴复制验证] 失败：" + e);
            throw;
        }
        finally
        {
            Call("Finish");
            GUIUtility.hotControl = 0;
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(testUndoGroup);
            Event.current = previousEvent;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (testScene.IsValid()) EditorSceneManager.CloseScene(testScene, true);
            Selection.objects = previousSelection;
            if (!string.IsNullOrEmpty(temporaryPrefabPath)) AssetDatabase.DeleteAsset(temporaryPrefabPath);
            temporaryPrefabPath = null;
        }
    }

    private static void Call(string name, params object[] args)
    {
        try { typeof(AltDragDuplicate).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed++;
        Debug.Log("ALT_DUPLICATE_CHECK: " + name);
    }
}
