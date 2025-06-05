using System.Collections.Generic;
using UnityEngine;

public class MassSpringCube : MonoBehaviour
{
    [Header("Cube Settings")]
    public float cubeSize = 1f;
    public float massValue = 1f;
    public float springStiffness = 500f;
    public float damping = 0.98f;

    [Header("Simulation")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Initial Position")]
    public Vector3 initialPosition = new Vector3(5.64f, 4.5f, -9.33f);

    private List<MassPoint> masses;
    private List<Spring> springs;

    void Start()
    {
        InitializeCube();
    }

    void InitializeCube()
    {
        masses = new List<MassPoint>();
        springs = new List<Spring>();

        float half = cubeSize / 2f;
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-half, -half, -half),
            new Vector3( half, -half, -half),
            new Vector3( half,  half, -half),
            new Vector3(-half,  half, -half),
            new Vector3(-half, -half,  half),
            new Vector3( half, -half,  half),
            new Vector3( half,  half,  half),
            new Vector3(-half,  half,  half)
        };

        // Add the offset
        for (int i = 0; i < positions.Length; i++)
            positions[i] += initialPosition;

        foreach (var pos in positions)
            masses.Add(new MassPoint(pos, massValue));

        void AddSpring(int a, int b)
        {
            float restLen = Vector3.Distance(masses[a].position, masses[b].position);
            springs.Add(new Spring(a, b, restLen, springStiffness));
        }

        int[,] edges = new int[,]
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };
        for (int i = 0; i < edges.GetLength(0); i++)
            AddSpring(edges[i, 0], edges[i, 1]);

        int[,] diagonals = new int[,]
        {
            {0,2}, {1,3}, {4,6}, {5,7},
            {0,5}, {1,4}, {2,7}, {3,6},
            {0,6}, {1,7}, {2,4}, {3,5}
        };
        for (int i = 0; i < diagonals.GetLength(0); i++)
            AddSpring(diagonals[i, 0], diagonals[i, 1]);
    }

    void Update()
    {
        Simulate(Time.deltaTime);
    }

    void Simulate(float dt)
    {
        foreach (var mass in masses)
            mass.force = Vector3.zero;

        foreach (var spring in springs)
        {
            MassPoint a = masses[spring.pointA];
            MassPoint b = masses[spring.pointB];

            Vector3 delta = b.position - a.position;
            float currentLen = delta.magnitude;
            Vector3 direction = delta.normalized;

            float displacement = currentLen - spring.restLength;
            Vector3 force = spring.stiffness * displacement * direction;

            a.force += force;
            b.force -= force;
        }

        foreach (var mass in masses)
            mass.force += gravity * mass.mass;

        foreach (var mass in masses)
        {
            Vector3 acceleration = mass.force / mass.mass;
            mass.velocity += acceleration * dt;
            mass.velocity *= damping;
            mass.position += mass.velocity * dt;

            // Ground collision: ground at y=0
            if (mass.position.y < 0f)
            {
                // Push the mass point back to y=0
                mass.position.y = 0f;

                // Invert the vertical velocity (simulate bounce), dampen it
                mass.velocity.y *= -0.5f; // Adjust 0.5 for bounce "strength"
            }
        }

    }

    void OnDrawGizmos()
    {
        if (masses == null || springs == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (var spring in springs)
        {
            Vector3 a = masses[spring.pointA].position;
            Vector3 b = masses[spring.pointB].position;
            Gizmos.DrawLine(a, b);
        }

        Gizmos.color = Color.red;
        foreach (var mass in masses)
            Gizmos.DrawSphere(mass.position, 0.04f);
    }
}
