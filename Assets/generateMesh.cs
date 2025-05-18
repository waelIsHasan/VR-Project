using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DeformableMesh : MonoBehaviour
{
    public int gridSize = 30;
    public float cellSize = 1f;

    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateGrid(mesh);
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void CreateGrid(Mesh mesh)
    {
        Vector3[] vertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[gridSize * gridSize * 6];

        for (int i = 0, z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++, i++)
            {
                vertices[i] = new Vector3(x * cellSize, 0, z * cellSize);
                uv[i] = new Vector2((float)x / gridSize, (float)z / gridSize);
            }
        }

        for (int ti = 0, vi = 0, z = 0; z < gridSize; z++, vi++)
        {
            for (int x = 0; x < gridSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + gridSize + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + gridSize + 1;
                triangles[ti + 5] = vi + gridSize + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}