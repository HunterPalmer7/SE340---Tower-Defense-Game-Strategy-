using UnityEngine;

/// <summary>
/// Draws the dashed center line along the Path (wireframe look). Lives NEXT TO the Path object, not under it,
/// because Path treats every child as a waypoint.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PathCenterLine : MonoBehaviour
{
    private void Update()
    {
        Path path = Path.Instance != null ? Path.Instance : FindFirstObjectByType<Path>();
        if (path == null) return;

        var lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            lr.SetPosition(i, path.Point(i));
    }
}
