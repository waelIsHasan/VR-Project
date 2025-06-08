using UnityEngine;
using System.Collections.Generic;

public class MassSpringSphere : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float springStiffness = 500f;
    public float damping = 0.98f;
    public float mass = 1f;
    public float internalPointSpacing = 0.2f;

    private Mesh mesh;
    private Vector3[] surfaceVertices;
    private MassPoint[] massPoints;
    private List<Spring> springs;

    void Start()
    {
        // Get mesh and surface vertices
        mesh = GetComponent<MeshFilter>().mesh;
        surfaceVertices = mesh.vertices;

        // Initialize mass points list (surface + interior)
        List<MassPoint> points = new List<MassPoint>();

        // Surface points
        foreach (Vector3 v in surfaceVertices)
        {
            Vector3 worldPos = transform.TransformPoint(v); // world position
            points.Add(new MassPoint(worldPos, mass));
        }

        // Interior points
        float radius = transform.localScale.x * 0.5f; // uniform scale assumed
        for (float x = -radius; x <= radius; x += internalPointSpacing)
        {
            for (float y = -radius; y <= radius; y += internalPointSpacing)
            {
                for (float z = -radius; z <= radius; z += internalPointSpacing)
                {
                    Vector3 localPos = new Vector3(x, y, z);
                    if (localPos.magnitude <= radius)
                    {
                        Vector3 worldPos = transform.TransformPoint(localPos);
                        points.Add(new MassPoint(worldPos, mass));
                    }
                }
            }
        }

        massPoints = points.ToArray();

        // Create springs
        springs = new List<Spring>();
        float maxDistance = internalPointSpacing * 1.1f; // neighbor threshold

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


        Debug.Log("Initialized: " + massPoints.Length + " mass points, " + springs.Count + " springs.");
    }

    void FixedUpdate()
    {

        // Reset forces
        foreach (MassPoint p in massPoints)
        {
            p.force = Vector3.zero;
            p.force += gravity * p.mass / 100000;

        }

        // Apply spring forces
        foreach (Spring spring in springs)
        {
            MassPoint pA = massPoints[spring.pointA];
            MassPoint pB = massPoints[spring.pointB];

            Vector3 delta = pB.position - pA.position;
            float currentLength = delta.magnitude;
            Vector3 direction = delta.normalized;
            float displacement = currentLength - spring.restLength;

            Vector3 force = spring.stiffness * displacement * direction;

            pA.force += force;
            pB.force -= force;
        }

        // Update velocities and positions
        foreach (MassPoint p in massPoints)
        {
            Vector3 acceleration = p.force / p.mass;
            p.velocity += acceleration * Time.fixedDeltaTime;
            p.velocity *= damping;
            p.position += p.velocity * Time.fixedDeltaTime;
        }

        // Update mesh vertices for surface
        for (int i = 0; i < surfaceVertices.Length; i++)
        {
            Vector3 localPos = transform.InverseTransformPoint(massPoints[i].position);
            surfaceVertices[i] = localPos;
        }

        mesh.vertices = surfaceVertices;
        mesh.RecalculateNormals();
    }

    void OnDrawGizmosSelected()
    {
        if (massPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (MassPoint p in massPoints)
            {
                Gizmos.DrawSphere(p.position, 0.02f);
            }

            Gizmos.color = Color.green;
            if (springs != null)
            {
                foreach (Spring s in springs)
                {
                    Gizmos.DrawLine(massPoints[s.pointA].position, massPoints[s.pointB].position);
                }
            }
        }
    }
}
