using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

/*public class Topology
{
    public Topology() { }

    private List<Mesh> m_meshes = new List<Mesh>();
    private HashSet<Vertex> m_vertices = new HashSet<Vertex>();
    private Dictionary<Vector3, Node> m_nodes = new Dictionary<Vector3, Node>();
    private HashSet<Face> m_faces = new HashSet<Face>();
    private Dictionary<(Vector3, Vector3), Edge> m_edges = new Dictionary<(Vector3, Vector3), Edge>();

    public void Add(List<Mesh> meshes, Transform localTransform, Vector3 localScale)
    {
        foreach (Mesh mesh in meshes)
        {
            Add(mesh, localTransform, localScale);
        }
    }

    public int Add(Mesh mesh, Transform localTransform, Vector3 localScale)
    {
        int meshId = m_meshes.Count;
        List<Vector3> pos = new List<Vector3>();
        mesh.GetVertices(pos);
        List<Vector3> normals = new List<Vector3>();
        mesh.GetNormals(normals);
        List<Vector2> uv = new List<Vector2>();
        mesh.GetUVs(0, uv);
        for (int i = 0; i < pos.Count; i++)
        {
            pos[i] = localTransform.TransformPoint(pos[i]);
            pos[i] = new Vector3((float)System.Math.Round(pos[i].x, 2), (float)System.Math.Round(pos[i].y, 2), (float)System.Math.Round(pos[i].z, 2));
            normals[i] = localTransform.TransformVector(normals[i]);
            Vertex v = new Vertex(pos[i], normals[i], uv[i], meshId);
            m_vertices.Add(v);
            Node n;
            if (m_nodes.ContainsKey(pos[i]))
            {
                n = m_nodes[pos[i]];
                n.AddVertex(v);
            }
            else
            {
                n = new Node(v, m_nodes.Count);
                m_nodes.Add(pos[i], n);
            }
        }

        List<int> triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);
        if (localScale.x < 0) { triangles.Reverse(); }

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3[] p = new Vector3[3] { pos[triangles[i]], pos[triangles[i + 1]], pos[triangles[i + 2]] };
            Face face = new Face(m_nodes[p[0]], m_nodes[p[1]], m_nodes[p[2]], meshId, m_faces.Count);
            m_faces.Add(face);
            for (int j = 0; j < 3; j++)
            {
                (Vector3, Vector3) edgeKey = GetEdgeKey(p[j], p[(j + 1) % 3]);
                if (!m_edges.ContainsKey(edgeKey))
                {
                    m_edges.Add(edgeKey, new Edge(m_nodes[p[j]], m_nodes[p[(j + 1) % 3]]));
                }

                if (face.isWalkable) { m_edges[edgeKey].inOrOut += 1; }
            }
        }

        return meshId;
    }

    public Mesh GetMesh()
    {
        Vector3[] vertices = new Vector3[m_nodes.Count];
        Vector3[] normals = new Vector3[m_nodes.Count];
        foreach (Vector3 pos in m_nodes.Keys)
        {
            Node n = m_nodes[pos];
            vertices[n.nodeId] = n.position;
            normals[n.nodeId] = n.normal;
        }

        int[] triangles = new int[m_faces.Count * 3];
        foreach (Face f in m_faces)
        {
            triangles[3 * f.faceId] = f.nodes[0].nodeId;
            triangles[3 * f.faceId + 1] = f.nodes[1].nodeId;
            triangles[3 * f.faceId + 2] = f.nodes[2].nodeId;
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }

    public List<Texture2D> GetDistanceField()
    {
        
        return null;
    }

    private void ComputeDistanceToEdge()
    {

    }

    public void OnDrawGizmos()
    {
        foreach ((Vector3, Vector3) edgeKey in m_edges.Keys)
        {
            Edge edge = m_edges[edgeKey];
            if (edge.inOrOut == 0)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(edge.nodes[0].position, edge.nodes[1].position);
            }
        }
    }

    public static (Vector3, Vector3) GetEdgeKey(Vector3 p1, Vector3 p2)
    {
        if (p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y) || (p1.x == p2.x && p1.y == p2.y && p1.z < p2.z))
            return (p1, p2);
        return (p2, p1);
    }
}*/

public static class Utils
{
    public static List<Vector3> CalculateSurfaceNormals(List<Vector3> vertices, List<int> triangles)
    {
        List<Vector3> surfaceNormals = new List<Vector3>(); 
        for (int i = 0; i < triangles.Count / 3; i++)
        {
            Vector3 U = vertices[triangles[3 * i + 1]] - vertices[triangles[3 * i]];
            Vector3 V = vertices[triangles[3 * i + 2]] - vertices[triangles[3 * i]];

            Vector3 n = new Vector3(U.y * V.z - U.z * V.y, U.z * V.x - U.x * V.z, U.x * V.y - U.y * V.x);
            surfaceNormals.Add(n.normalized);
        }
        return surfaceNormals;
    }
}
