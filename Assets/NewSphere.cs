using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class NewSphere : MonoBehaviour
{
    [Header("Physics Settings")]
    public float springStiffness = 3000f;
    public float damping = 0.99f;
    public float mass = 1f;
    public float internalPointSpacing = 0.01f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public int solverIterations = 5;

    private Mesh mesh;
    private Vector3[] surfaceVertices;
    private MassPoint[] massPoints;
    private List<Spring> springs;
    private Vector3[] previousPositions;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        surfaceVertices = mesh.vertices;

        List<MassPoint> points = new List<MassPoint>();

        // Add surface vertices
        foreach (var v in surfaceVertices)
        {
            Vector3 worldPos = transform.TransformPoint(v);
            points.Add(new MassPoint(worldPos, mass));
        }

        // Add interior points (forming a volume)
        float radius = transform.localScale.x * 0.5f;
        for (float x = -radius; x <= radius; x += internalPointSpacing)
        {
            for (float y = -radius; y <= radius; y += internalPointSpacing)
            {
                for (float z = -radius; z <= radius; z += internalPointSpacing)
                {
                    Vector3 local = new Vector3(x, y, z);
                    if (local.magnitude <= radius)
                    {
                        Vector3 world = transform.TransformPoint(local);
                        points.Add(new MassPoint(world, mass));
                    }
                }
            }
        }

        massPoints = points.ToArray();
        previousPositions = new Vector3[massPoints.Length];
        for (int i = 0; i < massPoints.Length; i++)
            previousPositions[i] = massPoints[i].position;

        // Build springs
        springs = new List<Spring>();
        float maxDistance = internalPointSpacing * 1.05f;

        for (int i = 0; i < massPoints.Length; i++)
        {
            for (int j = i + 1; j < massPoints.Length; j++)
            {
                float dist = Vector3.Distance(massPoints[i].position, massPoints[j].position);
                if (dist <= maxDistance)
                {
                    springs.Add(new Spring(i, j, dist, springStiffness));
                }
            }
        }

        Debug.Log($"Initialized {massPoints.Length} points and {springs.Count} springs");
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Verlet Integration Step
        for (int i = 0; i < massPoints.Length; i++)
        {
            var p = massPoints[i];
            Vector3 temp = p.position;

            Vector3 acceleration = gravity;
            p.position += (p.position - previousPositions[i]) * damping + acceleration * dt * dt;

            previousPositions[i] = temp;
        }

        // Solve constraints (spring length preservation)
        for (int iter = 0; iter < solverIterations; iter++)
        {
            foreach (var s in springs)
            {
                MassPoint a = massPoints[s.pointA];
                MassPoint b = massPoints[s.pointB];

                Vector3 delta = b.position - a.position;
                float currentLength = delta.magnitude;
                if (currentLength == 0f) continue;

                float diff = (currentLength - s.restLength);
                Vector3 correction = (diff / currentLength) * delta * 0.5f;

                a.position += correction * (1f / a.mass);
                b.position -= correction * (1f / b.mass);
            }
        }

        // Update mesh surface
        for (int i = 0; i < surfaceVertices.Length; i++)
        {
            Vector3 localPos = transform.InverseTransformPoint(massPoints[i].position);
            surfaceVertices[i] = localPos;
        }

        mesh.vertices = surfaceVertices;
        mesh.RecalculateNormals();


        // 3. Handle collisions with ground plane
        float groundY = 0f;
        for (int i = 0; i < massPoints.Length; i++)
        {
            var p = massPoints[i];
            if (p.position.y < groundY)
            {
                // Push above ground
                p.position.y = groundY;

                // Reflect velocity for bounce (optional)
                Vector3 v = (p.position - previousPositions[i]) / dt;
                v.y = -v.y * 0.5f; // reduce Y velocity to simulate energy loss
                previousPositions[i] = p.position - v * dt;
            }
        }


    }

    void OnDrawGizmosSelected()
    {
        if (massPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var p in massPoints)
            {
                Gizmos.DrawSphere(p.position, 0.015f);
            }

            Gizmos.color = Color.green;
            foreach (var s in springs)
            {
                Gizmos.DrawLine(massPoints[s.pointA].position, massPoints[s.pointB].position);
            }
        }
    }
}
