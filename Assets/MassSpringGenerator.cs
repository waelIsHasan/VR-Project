using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class MassSpringGenerator : MonoBehaviour
{
    [Header("Prefabs & Settings")]
    public GameObject massPointPrefab;
    public float springStiffness = 500f;
    public float springDamping = 5f;

    [Header("Reduction")]
    [Range(1, 10)]
    public int vertexStep = 1;               // only use every Nth vertex
    [Tooltip("Maximum number of mass points (0 = unlimited)")]
    public int maxMassPoints = 0;            // cap total points via inspector
    [Header("Spring Filter")]
    [Tooltip("Skip springs longer than this length (0 = no limit)")]
    public float maxSpringLength = 0f;       // 0 means unlimited length

    private List<Rigidbody> massPoints = new List<Rigidbody>();
    private HashSet<(int, int)> createdSprings = new HashSet<(int, int)>();

    [ContextMenu("Generate Mass-Spring System")]
    public void Generate()
    {
        if (massPointPrefab == null)
        {
            Debug.LogError("Assign a MassPoint prefab first!");
            return;
        }

        // Clear previous points
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);
        massPoints.Clear();
        createdSprings.Clear();

        // Get mesh data
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // Map original vertex index -> new index in massPoints
        Dictionary<int, int> indexMap = new Dictionary<int, int>();
        for (int i = 0; i < verts.Length; i += vertexStep)
        {
            if (maxMassPoints > 0 && massPoints.Count >= maxMassPoints)
                break;

            Vector3 worldPos = transform.TransformPoint(verts[i]);
            GameObject mp = (GameObject)PrefabUtility.InstantiatePrefab(massPointPrefab, transform);
            mp.transform.position = worldPos;
            mp.name = "MassPoint_" + i;
            Rigidbody rb = mp.GetComponent<Rigidbody>() ?? mp.AddComponent<Rigidbody>();
            massPoints.Add(rb);
            indexMap[i] = massPoints.Count - 1;
        }

        // Connect spring joints along each triangle edge
        for (int t = 0; t < tris.Length; t += 3)
        {
            TryAddSpring(tris[t], tris[t + 1], indexMap);
            TryAddSpring(tris[t + 1], tris[t + 2], indexMap);
            TryAddSpring(tris[t + 2], tris[t], indexMap);
        }

        Debug.Log($"Generated {massPoints.Count} mass points and {createdSprings.Count} springs.");
    }

    private void TryAddSpring(int a, int b, Dictionary<int, int> map)
    {
        if (!map.ContainsKey(a) || !map.ContainsKey(b)) return;
        int ia = map[a], ib = map[b];
        int i = Mathf.Min(ia, ib), j = Mathf.Max(ia, ib);
        if (createdSprings.Contains((i, j))) return;

        Vector3 posA = massPoints[ia].position;
        Vector3 posB = massPoints[ib].position;
        float restLen = Vector3.Distance(posA, posB);
        if (maxSpringLength > 0f && restLen > maxSpringLength)
            return; // skip long springs

        createdSprings.Add((i, j));
        SpringJoint sj = massPoints[ia].gameObject.AddComponent<SpringJoint>();
        sj.connectedBody = massPoints[ib];
        sj.spring = springStiffness;
        sj.damper = springDamping;
        sj.minDistance = sj.maxDistance = restLen;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var rb in massPoints)
        {
            foreach (SpringJoint sj in rb.GetComponents<SpringJoint>())
            {
                if (sj.connectedBody != null)
                    Gizmos.DrawLine(rb.position, sj.connectedBody.position);
            }
        }
    }
}
