// MeshDeformer.cs - Directional drag-based slat bending (no spring physics)
using UnityEngine;

[System.Serializable]
public class MeshDeformerSettings
{
    [Header("Interaction Settings")]
    public float interactionRadius = 1.2f;
    public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Deformation Settings")]
    public float dragInfluence = 0.15f;
    public float responseSpeed = 1.2f;
    public float maxOffset = 0.2f;

    [Header("Spring Ripple Settings")]
    public float springStiffness = 30f;
    public float springDamping = 5f;
}

public class MeshDeformer
{
    private MeshDeformerSettings settings;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private float[] targetOffsets;
    private float[] vertexVelocities;

    public MeshDeformer(MeshDeformerSettings config, Vector3[] originalVerts)
    {
        settings = config;
        originalVertices = originalVerts;
        deformedVertices = new Vector3[originalVerts.Length];
        targetOffsets = new float[originalVerts.Length];
        vertexVelocities = new float[originalVerts.Length];
    }

    public Vector3[] GetDeformedVertices() => deformedVertices;

    public void UpdateDeformation(Vector3 worldInteractionPoint, Transform meshTransform, float dragVelocity, bool isDragging)
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 original = originalVertices[i];
            Vector3 worldVertex = meshTransform.TransformPoint(original);

            float distance = Vector2.Distance(new Vector2(worldVertex.x, worldVertex.y), new Vector2(worldInteractionPoint.x, worldInteractionPoint.y));
            float falloff = Mathf.Clamp01(settings.falloffCurve.Evaluate(distance / settings.interactionRadius));

            if (isDragging && Mathf.Abs(dragVelocity) > 0.01f)
            {
                float targetOffset = falloff * dragVelocity * settings.dragInfluence;
                targetOffset = Mathf.Clamp(targetOffset, -settings.maxOffset, settings.maxOffset);

                targetOffsets[i] = targetOffset;
                vertexVelocities[i] = 0f; // Reset ripple velocity while dragging
                deformedVertices[i] = new Vector3(original.x + targetOffsets[i], original.y, original.z);
            }
            else
            {
                // Apply spring ripple physics
                float offsetX = deformedVertices[i].x - original.x;
                float velocity = vertexVelocities[i];

                float springForce = -settings.springStiffness * offsetX;
                float dampingForce = -settings.springDamping * velocity;
                float force = springForce + dampingForce;

                velocity += force * Time.deltaTime;
                offsetX += velocity * Time.deltaTime;

                vertexVelocities[i] = velocity;
                deformedVertices[i] = new Vector3(original.x + offsetX, original.y, original.z);
            }
        }
    }
}
