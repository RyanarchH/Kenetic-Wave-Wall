using UnityEngine;

public class LineDeformer : MonoBehaviour
{
    [Header("References")]
    public LineGridGenerator gridGenerator;     // Assign your grid in inspector

    [Header("Deformation Settings")]
    public float interactionRadius = 1.0f;      // Radius of mouse influence
    public float maxOffset = 2f;              // How far lines bend
    public float smoothSpeed = 10f;             // Speed of deformation smoothing
    public float arcSharpness = 1.0f;           // How curved each line is
    public float wiggleAmount = 0.05f;          // Optional dynamic wiggle
    public float wiggleSpeed = 3f;              // Speed of wiggle motion

    [Header("Spring Settings")]
    public float stiffness = 100f;
    public float damping = 15f;   

    [Header("Anchoring")]
    [Range(0.1f, 5f)]
    public float anchorTension = 2f; // Controls how tightly top/bottom are anchored

    private Vector3[] targetOffsets;            // Target deformation per line
    private bool isDragging = false;
    private Vector3 lastMouseWorldPos;
    private float[] offsetVelocities; 
    private float currentDragX = 0f;





    void Start()
    {
        targetOffsets = new Vector3[gridGenerator.lineCount];
        offsetVelocities = new float[gridGenerator.lineCount];

    }

    void Update()
    {
        HandleMouseInput();

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float lineHeight = gridGenerator.lineHeight;

        // === DRAG VELOCITY TRACKING ===
        if (isDragging)
        {
            Vector3 dragDelta = mouseWorldPos - lastMouseWorldPos;
            currentDragX = dragDelta.x / Time.deltaTime;
            lastMouseWorldPos = mouseWorldPos;
        }
        else
        {
            // Smooth decay after release
            currentDragX = Mathf.Lerp(currentDragX, 0f, Time.deltaTime * 5f);
        }

        for (int i = 0; i < gridGenerator.lineCount; i++)
        {
            LineRenderer lr = gridGenerator.lines[i];
            float baseX = gridGenerator.originalXPositions[i];

            // Calculate falloff based on proximity to interaction point
            Vector3 lineMid = new Vector3(baseX, 0f, 0f);
            Vector3 interactionPoint = mouseWorldPos - new Vector3(currentDragX * 0.05f, 0f, 0f); // small trailing effect
            float distance = Mathf.Abs(interactionPoint.x - lineMid.x);
            float falloff = Mathf.Exp(-distance * distance * 4f);

            // === ALWAYS assign target offset ===
            float offsetAmount = 0f;

            if (isDragging)
            {
                offsetAmount = falloff * maxOffset * Mathf.Clamp(currentDragX, -1f, 1f);
            }

            targetOffsets[i] = new Vector3(offsetAmount, 0f, 0f);

            // === SPRING PHYSICS RETURN ===
            float currentOffsetX = lr.GetPosition(0).x - baseX;
            float targetOffsetX = targetOffsets[i].x;

            float velocity = offsetVelocities[i];
            float force = (targetOffsetX - currentOffsetX) * stiffness - velocity * damping;
            velocity += force * Time.deltaTime;
            currentOffsetX += velocity * Time.deltaTime;
            offsetVelocities[i] = velocity;

            // Optional: clean snap to zero when very close
            if (!isDragging && Mathf.Abs(currentOffsetX) < 0.001f)
            {
                currentOffsetX = 0f;
                offsetVelocities[i] = 0f;
            }

            float smoothedOffsetX = currentOffsetX;

            // === APPLY BENDING TO LINE ===
            int points = lr.positionCount;
            for (int j = 0; j < points; j++)
            {
                float t = j / (float)(points - 1); // 0 (bottom) → 1 (top)

                // Anchoring using bell curve
                float anchorFalloff = Mathf.Pow(Mathf.Sin(t * Mathf.PI), anchorTension);
                float bendX = smoothedOffsetX * anchorFalloff;

                // Optional wiggle effect during drag
                if (isDragging && wiggleAmount > 0f)
                {
                    float timeWave = Mathf.Sin(Time.time * wiggleSpeed + i * 0.2f);
                    bendX += timeWave * wiggleAmount * anchorFalloff;
                }

                float y = Mathf.Lerp(-lineHeight / 2f, lineHeight / 2f, t);
                lr.SetPosition(j, new Vector3(baseX + bendX, y, 0f));
            }
        }
    }


    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMouseWorldPos = GetMouseWorldPosition();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z); // distance to Z=0
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawSphere(lastMouseWorldPos, interactionRadius);
    }
}
