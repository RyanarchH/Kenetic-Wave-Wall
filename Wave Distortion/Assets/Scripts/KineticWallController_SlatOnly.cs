using UnityEngine;
using System.Collections.Generic;

public class KineticWallController : MonoBehaviour 
{
    [Header("Panel Configuration")]
    public int columnCount = 200;
    public float responseRadius = 0.5f;

    [Header("Physics Properties")]
    public float stiffness = 10f;
    public float dampening = 0.5f;
    public float maxDeformationDepth = 1.0f;

    [Header("References")]
    public Material slatMaterial;

    [Header("Performance")]
    public bool useGPUInstancing = true;
    public bool use3DSlats = true;

    private GameObject[] slats;
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    private List<List<Matrix4x4>> batchedMatrices;
    private float[] vertexVelocities;
    private Mesh slatMesh;
    
    
    void Awake()
    {
        InitializeSlats();
        Debug.Log("Slats Initialized");
    }

    void InitializeSlats()
    {
        slats = new GameObject[columnCount];
        matrices = new Matrix4x4[columnCount];
        propertyBlock = new MaterialPropertyBlock();
        batchedMatrices = new List<List<Matrix4x4>>();

        int batchSize = 1000;
        for (int i = 0; i < columnCount; i += batchSize)
        {
            int count = Mathf.Min(batchSize, columnCount - i);
            batchedMatrices.Add(new List<Matrix4x4>(count));
        }

        GameObject slatPrototype = CreateVerticalElement(Vector3.zero, 1, 1, 1);
        slatMesh = slatPrototype.GetComponent<MeshFilter>().sharedMesh;

        float wallWidth = 10f;
        float wallHeight = 4f;
        float slatThickness = 0.02f;
        float slatWidth = wallWidth / columnCount;

        for (int i = 0; i < columnCount; i++)
        {
            float xPos = (i * slatWidth) - (wallWidth / 2) + (slatWidth / 2);
            Vector3 position = new Vector3(xPos, wallHeight / 2, 0);

            if (useGPUInstancing)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(
                    position,
                    Quaternion.identity,
                    new Vector3(slatWidth * 0.9f, wallHeight, slatThickness)
                );
                matrices[i] = matrix;

                int batchIndex = i / 1000;
                int batchOffset = i % 1000;
                while (batchedMatrices[batchIndex].Count <= batchOffset)
                {
                    batchedMatrices[batchIndex].Add(Matrix4x4.identity);
                }
                batchedMatrices[batchIndex][batchOffset] = matrix;
            }
            else
            {
                GameObject slat = Instantiate(slatPrototype, position, Quaternion.identity, transform);
                slat.transform.localScale = new Vector3(slatWidth * 0.9f, wallHeight, slatThickness);
                slat.name = "Slat_" + i;
                slats[i] = slat;
            }
        }

        DestroyImmediate(slatPrototype);
        vertexVelocities = new float[columnCount];
    }

    GameObject CreateVerticalElement(Vector3 position, float width, float height, float thickness)
    {
        GameObject element = GameObject.CreatePrimitive(PrimitiveType.Cube);
        element.transform.parent = this.transform;
        element.transform.position = position;
        element.transform.localScale = new Vector3(width * 0.9f, height, thickness);

        Material instancedMaterial = new Material(slatMaterial);
        if (useGPUInstancing)
        {
            instancedMaterial.enableInstancing = true;
        }
        element.GetComponent<Renderer>().material = instancedMaterial;

        return element;
    }

    void Update()
    {
        UpdateSlatDeformation();

        if (useGPUInstancing)
        {
            RenderWithInstancing();
            
        }

        

    }

    void RenderWithInstancing()
    {
        if (slatMaterial == null) return;

        for (int i = 0; i < columnCount; i++)
        {
            float xPos = matrices[i].GetColumn(3).x;
            float yPos = matrices[i].GetColumn(3).y;
            float zPos = vertexVelocities[i];

            
            /*Quaternion rotation = Quaternion.identity;
            if (i > 0 && i < columnCount - 1)
            {
                float prevZ = vertexVelocities[i - 1];
                float nextZ = vertexVelocities[i + 1];
                float angleY = Mathf.Atan2(nextZ - prevZ, 0.1f) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0, angleY, 0);
            }*/
            Quaternion rotation = Quaternion.identity; // ✅ disables tilt so we isolate shader-based bend



            Vector3 scale = new Vector3(
                matrices[i].GetColumn(0).magnitude,
                matrices[i].GetColumn(1).magnitude,
                matrices[i].GetColumn(2).magnitude
            );

            matrices[i] = Matrix4x4.TRS(new Vector3(xPos, yPos, zPos), rotation, scale);

            int batchIndex = i / 1000;
            int batchOffset = i % 1000;
            batchedMatrices[batchIndex][batchOffset] = matrices[i];
        }

        float testBend = Mathf.Sin(Time.time * 2f) * 0.5f;
        propertyBlock.SetFloat("_Bend", testBend);

        for (int b = 0; b < batchedMatrices.Count; b++)
        {
            propertyBlock.SetFloat("_Bend", 0.5f); // constant value to see curve
            Graphics.DrawMeshInstanced(
                slatMesh,
                0,
                slatMaterial,
                batchedMatrices[b].ToArray(),
                batchedMatrices[b].Count,
                propertyBlock
            );
        }

    }

    void UpdateSlatDeformation()
    {
        for (int i = 0; i < columnCount; i++)
        {
            float currentPos = vertexVelocities[i];
            float springForce = -currentPos * stiffness;
            vertexVelocities[i] += springForce * Time.deltaTime;
            vertexVelocities[i] *= (1f - dampening * Time.deltaTime);

            if (i > 0 && i < columnCount - 1)
            {
                float leftForce = vertexVelocities[i - 1] - vertexVelocities[i];
                float rightForce = vertexVelocities[i + 1] - vertexVelocities[i];
                vertexVelocities[i] += (leftForce + rightForce) * 0.1f;
            }

            if (!useGPUInstancing && slats[i] != null)
            {
                Vector3 pos = slats[i].transform.position;
                pos.z = vertexVelocities[i];
                slats[i].transform.position = pos;

                float prevZ = vertexVelocities[Mathf.Max(0, i - 1)];
                float nextZ = vertexVelocities[Mathf.Min(columnCount - 1, i + 1)];
                float angleY = Mathf.Atan2(nextZ - prevZ, 0.1f) * Mathf.Rad2Deg;
                slats[i].transform.rotation = Quaternion.Euler(0, angleY, 0);
            }
        }
    }

    public void ApplyDeformationAtPoint(Vector3 worldPoint, float strength)
    {
        for (int i = 0; i < columnCount; i++)
        {
            Vector3 slatPos = useGPUInstancing
                ? matrices[i].GetColumn(3)
                : slats[i].transform.position;

            float distance = Mathf.Abs(slatPos.x - worldPoint.x);

            if (distance <= responseRadius)
            {
                float deformationFactor = 1 - (distance / responseRadius);
                float deformationAmount = maxDeformationDepth * deformationFactor * strength;
                vertexVelocities[i] += deformationAmount * Time.deltaTime * 50f;

                Debug.Log($"Deformed slat {i} by {deformationAmount}");
            }

            
        }

        // Track the most recent interaction point for directional bending
        //lastInteractionPoint = worldPoint;
    }
}
