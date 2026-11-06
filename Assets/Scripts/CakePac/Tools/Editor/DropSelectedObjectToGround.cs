#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// In the Scene view, press End to move the selected scene object down onto the
/// first collider below it. The selected object's own colliders are ignored.
/// </summary>
[InitializeOnLoad]
public static class DropSelectedObjectToGround
{
    private const float RayStartPadding = 0.01f;
    private const float PositionEpsilon = 0.0001f;

    static DropSelectedObjectToGround()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        Event current = Event.current;
        if (current.type != EventType.KeyDown || current.keyCode != KeyCode.End)
            return;

        if (current.alt || current.control || current.command || current.shift)
            return;

        GameObject selected = Selection.activeGameObject;
        if (selected != null && TryDropToGround(selected))
            current.Use();
    }

    private static bool TryDropToGround(GameObject selected)
    {
        if (selected == null || EditorUtility.IsPersistent(selected))
            return false;

        if (!TryGetWorldBounds(selected, out Bounds bounds))
            return false;

        Vector3 rayOrigin = new Vector3(
            bounds.center.x,
            bounds.max.y + RayStartPadding,
            bounds.center.z);

        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(selected.transform))
                continue;

            float deltaY = hit.point.y - bounds.min.y;

            // This tool only drops objects. It never lifts one to a collider above it.
            if (deltaY >= -PositionEpsilon)
                return false;

            Undo.RecordObject(selected.transform, "Drop Object To Ground");
            selected.transform.position += Vector3.up * deltaY;
            EditorUtility.SetDirty(selected.transform);

            if (selected.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(selected.scene);

            return true;
        }

        return false;
    }

    private static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        bool hasBounds = false;
        bounds = default;

        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (hasBounds)
            return true;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
#endif
