using UnityEngine;

public class LineGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int lineCount = 100;                 // Total number of vertical lines
    public float lineSpacing = 0.1f;            // Space between lines
    public float lineHeight = 5f;               // Height of each line

    [Header("Line Settings")]
    public Material lineMaterial;               // Assign a simple unlit material
    public float lineWidth = 0.02f;

    [HideInInspector]
    public LineRenderer[] lines;
    public float[] originalXPositions;

    void Start()
    {
        GenerateGrid();
    }


    //confirm this code is working, temporarily add a sine wave motion
    void Update()
    {
        /*
        float time = Time.time;

        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer lr = lines[i];
            float x = i * lineSpacing;

            // Wave offset using sine
            float offset = Mathf.Sin(time * 2f + i * 0.1f) * 0.2f;

            Vector3 start = new Vector3(x + offset, -lineHeight / 2f, 0f);
            Vector3 end = new Vector3(x + offset, lineHeight / 2f, 0f);
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
        */
    }


    void GenerateGrid()
    {
        transform.position = Vector3.zero; // 🔒 Just in case

        lines = new LineRenderer[lineCount];
        originalXPositions = new float[lineCount];


        float totalWidth = (lineCount - 1) * lineSpacing;
        float halfWidth = totalWidth / 2f;

        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject("Line_" + i);
            lineObj.transform.parent = this.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 10;
            lr.material = lineMaterial;
            lr.widthMultiplier = lineWidth;
            lr.useWorldSpace = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            float x = (i * lineSpacing) - halfWidth;
            originalXPositions[i] = x;


            Vector3 start = new Vector3(x, -lineHeight / 2f, 0f);
            Vector3 end = new Vector3(x, lineHeight / 2f, 0f);

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            lines[i] = lr;
        }
    }


}
