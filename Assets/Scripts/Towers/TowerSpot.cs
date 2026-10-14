using UnityEngine;

/// <summary>
/// A spot on the map where a tower can be built. Clicking it asks BuildManager to place
/// a tower here using whatever targeting behaviour the player currently has selected.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TowerSpot : MonoBehaviour
{
    public Tower tower;   // null until something is built here
    public bool IsEmpty => tower == null;

    private void OnMouseDown()
    {
        if (!IsEmpty)
        {
            if (BuildManager.Instance != null)
                BuildManager.Instance.SelectTower(tower);
            else
                Debug.LogWarning("No BuildManager in the scene.");
            return;
        }

        if (BuildManager.Instance == null)
        {
            Debug.LogWarning("No BuildManager in the scene.");
            return;
        }

        BuildManager.Instance.BuildOn(this);
    }
}
