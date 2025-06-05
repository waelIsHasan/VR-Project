using UnityEngine;

public class MeshDeformer : MonoBehaviour
{
    public float deformationStrength = 0.1f;
    public float deformationRadius = 0.5f;

    private Mesh mesh;
    private Vector3[] originalVertices, modifiedVertices;
    private MeshCollider meshCollider;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic(); // Optimize for frequent updates
        modifiedVertices = mesh.vertices;
        meshCollider = GetComponent<MeshCollider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 localPoint = transform.InverseTransformPoint(contact.point);
            for (int i = 0; i < modifiedVertices.Length; i++)
            {
                Vector3 vertex = modifiedVertices[i];
                float distance = Vector3.Distance(vertex, localPoint);
                if (distance < deformationRadius)
                {
                    float falloff = 1 - (distance / deformationRadius);
                    float displacement = deformationStrength * falloff * falloff;
                    modifiedVertices[i].y -= displacement;
                }
            }
        }
        ApplyMeshChanges();
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh; // Update collider
    }
}