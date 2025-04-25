using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGridGenerator : MonoBehaviour
{
    public int widthSegments = 100;  // Number of vertical lines
    public int heightSegments = 30;  // Resolution along height
    public float width = 10f;
    public float height = 5f;

    private Mesh mesh;

    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    private float[] vertexVelocities;
    private float[] targetOffsets;
    private bool isDragging;
    private Vector3 lastMouseWorldPos;
    private float currentDragX;

    public MeshDeformerSettings deformerSettings; // Exposed in Inspector
    private MeshDeformer meshDeformer; 

    [SerializeField] private float dragSmoothing = 5f;




    void Start()
    {
        GeneratePlane();
        //vertexVelocities = new float[originalVertices.Length];
        //targetOffsets = new float[originalVertices.Length];

        meshDeformer = new MeshDeformer(deformerSettings, originalVertices);


    }

    void Update()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMouseWorldPos = mouseWorldPos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 delta = mouseWorldPos - lastMouseWorldPos;
            float targetDragX = delta.x / Time.deltaTime;

            currentDragX = Mathf.Lerp(currentDragX, targetDragX, Time.deltaTime * dragSmoothing);

            lastMouseWorldPos = mouseWorldPos;
        }
        else
        {
            currentDragX = Mathf.Lerp(currentDragX, 0f, Time.deltaTime * dragSmoothing);
        }

        
        if (originalVertices == null || originalVertices.Length == 0)
            return;
        
        meshDeformer.UpdateDeformation(GetMouseWorldPosition(), transform, currentDragX, isDragging);

        mesh.vertices = meshDeformer.GetDeformedVertices();
        mesh.RecalculateNormals(); // Optional if you want lighting

        //below is manual deformation loop
        /*
        // Copy original to working array
        for (int i = 0; i < originalVertices.Length; i++)
        {
            deformedVertices[i] = originalVertices[i];
        }

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 original = originalVertices[i];
            Vector3 worldVertex = transform.TransformPoint(original);

            float dx = mouseWorldPos.x - worldVertex.x;
            float dy = mouseWorldPos.y - worldVertex.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float falloff = Mathf.Exp(-distance * distance * 2f); // controls radius

            float heightArc = Mathf.Sin((original.y + height / 2f) / height * Mathf.PI);

            float targetOffset = 0f;

            if (isDragging && Mathf.Abs(currentDragX) > 0.01f)
            {
                targetOffset = falloff * currentDragX * 0.5f * heightArc;
            }

            targetOffsets[i] = Mathf.Lerp(targetOffsets[i], targetOffset, Time.deltaTime * 10f); // smooth target

            // === Spring physics ===
            float offsetX = deformedVertices[i].x - original.x;
            float velocity = vertexVelocities[i];
            float force = (targetOffsets[i] - offsetX) * 100f - velocity * 10f; // stiffness & damping
            velocity += force * Time.deltaTime;
            offsetX += velocity * Time.deltaTime;
            vertexVelocities[i] = velocity;

            // Apply new position
            deformedVertices[i] = new Vector3(original.x + offsetX, original.y, original.z);
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals(); // optional if using lighting
        */
    }


    void GeneratePlane()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Plane";

        GetComponent<MeshFilter>().mesh = mesh;

        int vertCountX = widthSegments + 1;
        int vertCountY = heightSegments + 1;

        Vector3[] vertices = new Vector3[vertCountX * vertCountY];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[widthSegments * heightSegments * 6];

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Generate vertices and UVs
        for (int y = 0; y < vertCountY; y++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                int index = y * vertCountX + x;
                float px = Mathf.Lerp(-halfWidth, halfWidth, x / (float)widthSegments);
                float py = Mathf.Lerp(-halfHeight, halfHeight, y / (float)heightSegments);
                vertices[index] = new Vector3(px, py, 0f);
                uv[index] = new Vector2(x / (float)widthSegments, y / (float)heightSegments);
            }
        }

        // Generate triangle indices
        int ti = 0;
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int i = y * vertCountX + x;
                triangles[ti++] = i;
                triangles[ti++] = i + vertCountX;
                triangles[ti++] = i + 1;

                triangles[ti++] = i + 1;
                triangles[ti++] = i + vertCountX;
                triangles[ti++] = i + vertCountX + 1;
            }
        }

        mesh.vertices = vertices;
        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z); // distance to mesh
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}