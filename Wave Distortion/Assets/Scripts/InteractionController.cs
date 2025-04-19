using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public LayerMask interactionLayer;
    public float interactionRadius = 0.5f;
    public float interactionStrength = 1.0f;

    [Header("References")]
    public KineticWallController kineticWall;

    private Camera mainCamera;

    void Start()
    {
        // Find the main camera if not assigned
        mainCamera = Camera.main;
        
        // Find the KineticWallController if not assigned
        if (kineticWall == null)
        {
            kineticWall = FindFirstObjectByType<KineticWallController>();
        }
    }

    void Update()
    {
        // Handle mouse interaction
        HandleMouseInteraction();
        
        // Handle touch interaction
        HandleTouchInteraction();
    }

    void HandleMouseInteraction()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, interactionLayer))
            {
                // If we hit the wall, notify the wall controller
                if (kineticWall != null)
                {
                    kineticWall.ApplyDeformationAtPoint(hit.point, interactionStrength);
                }
            }
            
            //Debug.Log("Hit: " + hit.point);

        }
    }

    void HandleTouchInteraction()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = mainCamera.ScreenPointToRay(touch.position);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, interactionLayer))
            {
                if (kineticWall != null)
                {
                    kineticWall.ApplyDeformationAtPoint(hit.point, interactionStrength);
                }
            }
        }
    }
}