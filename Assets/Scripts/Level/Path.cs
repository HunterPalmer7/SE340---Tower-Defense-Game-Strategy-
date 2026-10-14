using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class Path : MonoBehaviour
{
    public static Path Instance;
    public float width = 0.6f;

    public int Count => transform.childCount;
    public Vector3 Point(int i) => transform.GetChild(i).position;

    void Awake() { Instance = this; }

    void Update()
    {
        var lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = transform.childCount;
        for (int i = 0; i < transform.childCount; i++)
            lr.SetPosition(i, transform.GetChild(i).position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < transform.childCount; i++)
        {
            Gizmos.DrawSphere(Point(i), 0.15f);
            if (i + 1 < transform.childCount) Gizmos.DrawLine(Point(i), Point(i + 1));
        }
    }
}