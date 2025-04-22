using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct RipplePoint
{
    public Vector3 position;
    public float strength;
    public float timeCreated;
}

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

    [Header("Anti-Overlap Settings")]
    [Range(0f, 0.2f)]
    public float spacingThreshold = 0.02f; // minimum spacing between lines after bending

    [Header("Anchoring")]
    [Range(0.1f, 5f)]
    public float anchorTension = 2f; // Controls how tightly top/bottom are anchored

    private Vector3[] targetOffsets;            // Target deformation per line
    private bool isDragging = false;
    private bool isDraggingAndMoving = false;

    private Vector3 lastMouseWorldPos;
    private float[] offsetVelocities; 
    private float currentDragX = 0f;
    
    [Header("Falloff Settings")]
    public float yFalloffWeight = 1f; // 0.5 = less vertical influence, 2 = more    


    private List<RipplePoint> ripplePoints = new List<RipplePoint>();

    [Header("Ripple Settings")]
    public float rippleDecayTime = 2.0f;     // How long a ripple lasts
    public float rippleMaxStrength = 3.0f;   // Strength of ripple influence
    public float rippleFalloff = 1.5f;         // Tightness of ripple effect



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

            if (dragDelta.magnitude > 0.01f)
            {
                currentDragX = dragDelta.x / Time.deltaTime;
                lastMouseWorldPos = mouseWorldPos;
                isDraggingAndMoving = true; // ✅ actually moving
            }
            else
            {
                currentDragX = 0f;
                isDraggingAndMoving = false; // ✅ just holding
            }
        }
        else
        {
            currentDragX = Mathf.Lerp(currentDragX, 0f, Time.deltaTime * 5f);
            isDraggingAndMoving = false;
        }



        for (int i = 0; i < gridGenerator.lineCount; i++)
        {
            LineRenderer lr = gridGenerator.lines[i];
            float baseX = gridGenerator.originalXPositions[i];

            // Calculate falloff based on proximity to interaction point
            // Position of interaction point (shifted slightly for fluid drag)
            Vector3 interactionPoint = mouseWorldPos - new Vector3(currentDragX * 0.05f, 0f, 0f);
            // Position of line's center
            Vector3 lineMid = new Vector3(baseX, 0f, 0f);

            // Compute both X and Y distance
            float dx = interactionPoint.x - lineMid.x;
            //float dy = (interactionPoint.y - lineMid.y) * yFalloffWeight;
            // Distance from cursor in 2D space
            //float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float distance = Mathf.Abs(dx);
            // Final 2D falloff curve
            //float falloff = Mathf.Exp(-distance * distance * 4f);
            float falloff = Mathf.Exp(-distance * distance * 2f);
            
            // === ALWAYS assign target offset ===
            float offsetAmount = 0f;            
            
            // === Ripple Trail Effect ===
            for (int r = ripplePoints.Count - 1; r >= 0; r--)
            {
                RipplePoint ripple = ripplePoints[r];
                float age = Time.time - ripple.timeCreated;

                if (age > rippleDecayTime)
                {
                    ripplePoints.RemoveAt(r);
                    continue;
                }

                float rippleProgress = 1f - (age / rippleDecayTime); // fades out
                float rippleDistance = Mathf.Abs(ripple.position.x - lineMid.x);
                float rippleInfluence = Mathf.Exp(-rippleDistance * rippleDistance * rippleFalloff) * rippleProgress;

                // Add ripple bend directionally based on old drag strength and pulse animation
                float wavePulse = Mathf.Sin((age / rippleDecayTime) * Mathf.PI); // 0 → 1 → 0
                offsetAmount += rippleInfluence * rippleMaxStrength * wavePulse * Mathf.Sign(currentDragX);
            }



            if (isDraggingAndMoving)
            {
                offsetAmount = falloff * maxOffset * Mathf.Abs(currentDragX) * Mathf.Sign(currentDragX);
            }
            else
            {
                offsetAmount = 0f; // ✅ No active drag = no bend
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
            // Save current bent position
            float bentX = baseX + smoothedOffsetX;
            // Check against previous line
            if (i > 0)
            {
                float prevBentX = gridGenerator.originalXPositions[i - 1] + (gridGenerator.lines[i - 1].GetPosition(0).x - gridGenerator.originalXPositions[i - 1]);

                float spacing = bentX - prevBentX;
                if (spacing < spacingThreshold)
                {
                    // Apply soft push back
                    float pushAmount = (spacingThreshold - spacing) * 0.5f;
                    smoothedOffsetX += pushAmount;
                }
            }

            // Check against next line
            if (i < gridGenerator.lineCount - 1)
            {
                float nextBentX = gridGenerator.originalXPositions[i + 1] + (gridGenerator.lines[i + 1].GetPosition(0).x - gridGenerator.originalXPositions[i + 1]);

                float spacing = nextBentX - bentX;
                if (spacing < spacingThreshold)
                {
                    float pushAmount = (spacingThreshold - spacing) * 0.5f;
                    smoothedOffsetX -= pushAmount;
                }
            }


            // === APPLY BENDING TO LINE ===
            int points = lr.positionCount;
            for (int j = 0; j < points; j++)
            {
                float t = j / (float)(points - 1);

                // Map cursor Y to line height
                float localY = Mathf.Lerp(-lineHeight / 2f, lineHeight / 2f, t);
                float cursorOffsetY = interactionPoint.y;

                // Calculate distance of each point from cursor Y
                float dy = cursorOffsetY - localY;

                // Bell curve arc: max bend near cursor Y, anchored elsewhere
                float yBias = Mathf.Exp(-dy * dy * 0.1f); // center = 1, edges = 0

                // Combine with anchor curve (optional, or remove to simplify)
                float anchorFalloff = Mathf.Pow(Mathf.Sin(t * Mathf.PI), anchorTension);

                // Final bending strength at this point
                float bendX = smoothedOffsetX * anchorFalloff * yBias;

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

            // 🌀 Create ripple on release
            RipplePoint rp = new RipplePoint
            {
                position = lastMouseWorldPos,
                strength = Mathf.Abs(currentDragX),
                timeCreated = Time.time
            };
            ripplePoints.Add(rp);
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

        foreach (var ripple in ripplePoints)
        {
            float age = Time.time - ripple.timeCreated;
            if (age > rippleDecayTime) continue;

            float progress = 1f - (age / rippleDecayTime);
            Gizmos.color = new Color(0.2f, 0.5f, 1f, progress * 0.2f);
            Gizmos.DrawSphere(ripple.position, interactionRadius * progress);
        }
    }
}