using UnityEngine;

public class MassPoint
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 force;
    public float mass;

    public MassPoint(Vector3 pos, float m)
    {
        position = pos;
        mass = m;
        velocity = Vector3.zero;
        force = Vector3.zero;
    }
}
