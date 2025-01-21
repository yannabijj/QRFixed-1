using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SetNavigationTarget : MonoBehaviour
{
    [SerializeField]
    private Camera topDownCamera; // Top-down camera
    [SerializeField]
    private GameObject navTargetObject;
    [SerializeField]
    private float fixedHeightOffset = 0.5f;

    private NavMeshPath path;
    private LineRenderer line;
    private bool isNavigating = false;
    private float destinationThreshold = 1.0f;
    private Camera arCamera;

    private void Start()
    {
        path = new NavMeshPath();
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        arCamera = Camera.main;

        if (arCamera == null)
        {
            Debug.LogError("AR Camera not found.");
        }
    }

    private void Update()
    {
        if (isNavigating)
        {
            UpdateNavigationLine();
        }
    }

    public void UpdateTargetPosition(Vector3 targetPosition)
    {
        navTargetObject.transform.position = targetPosition;
        bool pathFound = NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);

        if (pathFound && path.corners.Length > 0)
        {
            isNavigating = true;
            line.enabled = true;
            DrawPath();
            StartCoroutine(CheckIfDestinationReached(targetPosition));
        }
        else
        {
            ClearNavigationLine();
        }
    }

    private void UpdateNavigationLine()
    {
        if (path.corners.Length > 0)
        {
            bool pathFound = NavMesh.CalculatePath(transform.position, navTargetObject.transform.position, NavMesh.AllAreas, path);
            if (pathFound && path.corners.Length > 0)
            {
                AdjustPathHeight();
                line.positionCount = path.corners.Length;
                line.SetPositions(path.corners);
            }
            else
            {
                ClearNavigationLine();
            }
        }
    }

    private void DrawPath()
    {
        AdjustPathHeight();
        line.positionCount = path.corners.Length;
        line.SetPositions(path.corners);
    }

    private void AdjustPathHeight()
    {
        float cameraHeight = arCamera.transform.position.y;
        for (int i = 0; i < path.corners.Length; i++)
        {
            path.corners[i].y = cameraHeight - fixedHeightOffset;
        }
    }

    private IEnumerator CheckIfDestinationReached(Vector3 targetPosition)
    {
        while (isNavigating)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= destinationThreshold)
            {
                ClearNavigationLine();
                isNavigating = false;
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ClearNavigationLine()
    {
        line.positionCount = 0;
        line.enabled = false;
    }
}
