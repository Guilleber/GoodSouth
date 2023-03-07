using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    public Vertex(Vector3 position, Vector3 normal, Vector2 uv, int meshId)
    {
        this.position = position;
        this.normal = normal;
        this.uv = uv;
        this.meshId = meshId;
    }

    public Vector3 position { get; init; }
    public Vector3 normal { get; init; }
    public Vector2 uv { get; init; }
    public int meshId { get; init; }
}

public class Node
{
    public Node(Vertex v)
    {
        this.position = v.position;
        this.normal = v.normal;
        this.vertices.Add(v);
    }

    public List<Vertex> vertices { get; private set; } = new List<Vertex>();
    public Vector3 position { get; private set; }
    public Vector3 normal { get; private set; }
    public List<Edge> edges { get; private set; } = new List<Edge>();
    public List<Face> faces { get; private set; } = new List<Face>();

    public float dist2Edge = 0.0f;

    public void AddVertex(Vertex v)
    {
        vertices.Add(v);
        normal = (normal + v.normal).normalized;
    }

    public void AddEdge(Edge e) { edges.Add(e); }

    public void AddFace(Face f) { faces.Add(f); }

    public Vertex Find(int meshId)
    {
        foreach (Vertex v in vertices)
        {
            if (v.meshId == meshId) { return v; }
        }
        return null;
    }
}

public class Edge
{
    public Edge(Node n1, Node n2)
    {
        this.nodes = new Node[2] { n1, n2 };
    }

    public Node[] nodes { get; private set; }
    public sbyte inOrOut = -1;

    public Vector3 Project(Vector3 p)
    {
        Vector3 AB = nodes[1].position - nodes[0].position;
        Vector3 AP = p - nodes[0].position;
        float r = Vector3.Dot(AB, AP) / Vector3.Dot(AB, AB);
        if (r < 0) { r = 0; }
        else if (r > 1) { r = 1; }
        return nodes[0].position + r * AB;
    }
}

public class Face
{
    public Node[] nodes { get; private set; }
    public Vector3 normal { get; private set; }
    public int meshId { get; init; }
    public bool isWalkable { get; private set; }

    private Matrix2x2 inv_T;
    private Vector2 r3;

    public Face(Node n1, Node n2, Node n3, int meshId)
    {
        this.nodes = new Node[] { n1, n2, n3 };
        this.normal = Face.CalculateSurfaceNormal(n1.position, n2.position, n3.position);
        this.meshId = meshId;
        this.isWalkable = Vector3.Angle(this.normal, Vector3.up) < 45;

        // Init baricentric projection matrix

        Vertex v1 = n1.Find(meshId);
        Vertex v2 = n2.Find(meshId);
        Vertex v3 = n3.Find(meshId);

        Matrix2x2 T = new Matrix2x2(new float[2, 2]);
        T[0, 0] = v1.uv.x - v3.uv.x;
        T[0, 1] = v2.uv.x - v3.uv.x;
        T[1, 0] = v1.uv.y - v3.uv.y;
        T[1, 1] = v2.uv.y - v3.uv.y;
        r3 = v3.uv;
        inv_T = T.inverse;
    }

    public Vector3 GetUVBaricentricCoord(Vector2 pos)
    {
        Vector2 lambda = inv_T * (pos - r3);
        return new Vector3(lambda.x, lambda.y, 1 - lambda.x - lambda.y);
    }

    public static Vector3 CalculateSurfaceNormal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 U = p2 - p1;
        Vector3 V = p3 - p1;

        Vector3 n = new Vector3(U.y * V.z - U.z * V.y, U.z * V.x - U.x * V.z, U.x * V.y - U.y * V.x);
        return n.normalized;
    }
}

public readonly struct Matrix2x2
{
    private float[,] data { get; init; }

    public Matrix2x2(float[,] data)
    {
        this.data = data;
    }

    public Matrix2x2 inverse
    {
        get
        {
            float det = data[0, 0] * data[1, 1] - data[1, 0] * data[0, 1];
            float[,] invData = new float[2, 2];
            invData[0, 0] = data[1, 1] / det;
            invData[0, 1] = -data[0, 1] / det;
            invData[1, 0] = -data[1, 0] / det;
            invData[1, 1] = data[0, 0] / det;
            return new Matrix2x2(invData);
        }
    }

    public float this[int i, int j]
    {
        get { return data[i, j]; }
        set { data[i, j] = value; }
    }

    public static Vector2 operator *(Matrix2x2 mat, Vector2 vec)
    {
        float x = mat[0, 0] * vec.x + mat[0, 1] * vec.y;
        float y = mat[1, 0] * vec.x + mat[1, 1] * vec.y;
        return new Vector2(x, y);
    }
}

public class MeshConstruct
{
    internal HashSet<Vertex> vertices = new HashSet<Vertex>();
    internal Dictionary<Vector3, Node> nodes = new Dictionary<Vector3, Node>();
    internal HashSet<Face> faces = new HashSet<Face>();
    internal Dictionary<(Vector3, Vector3), Edge> edges = new Dictionary<(Vector3, Vector3), Edge>();

    public Mesh GetSmoothMesh()
    {
        Dictionary<Node, int> nodeMap = new Dictionary<Node, int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        foreach (Vector3 pos in nodes.Keys)
        {
            Node n = nodes[pos];
            nodeMap[n] = vertices.Count;
            vertices.Add(n.position);
            normals.Add(n.normal);
        }

        List<int> triangles = new List<int>();
        foreach (Face f in faces)
        {
            triangles.Add(nodeMap[f.nodes[0]]);
            triangles.Add(nodeMap[f.nodes[1]]);
            triangles.Add(nodeMap[f.nodes[2]]);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }

    public Mesh GetFlatMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        foreach (Face f in faces)
        {
            for (int i = 0; i < 3; i++)
            {
                triangles.Add(vertices.Count);
                vertices.Add(f.nodes[i].position);
                normals.Add(f.normal);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }

    public static MeshConstruct FromMesh(Mesh mesh, Transform localTransform, bool isFlipped = false)
    {
        MeshConstruct mc = new MeshConstruct();
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
            Vertex v = new Vertex(pos[i], normals[i], uv[i], 0);
            mc.vertices.Add(v);
            Node n;
            if (mc.nodes.ContainsKey(pos[i]))
            {
                n = mc.nodes[pos[i]];
                n.AddVertex(v);
            }
            else
            {
                n = new Node(v);
                mc.nodes.Add(pos[i], n);
            }
        }

        List<int> triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);
        if (isFlipped) { triangles.Reverse(); }

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3[] p = new Vector3[3] { pos[triangles[i]], pos[triangles[i + 1]], pos[triangles[i + 2]] };
            Face face = new Face(mc.nodes[p[0]], mc.nodes[p[1]], mc.nodes[p[2]], 0);
            mc.faces.Add(face);
            for (int j = 0; j < 3; j++)
            {
                (Vector3, Vector3) edgeKey = GetEdgeKey(p[j], p[(j + 1) % 3]);
                if (!mc.edges.ContainsKey(edgeKey))
                {
                    mc.edges.Add(edgeKey, new Edge(mc.nodes[p[j]], mc.nodes[p[(j + 1) % 3]]));
                }

                if (face.isWalkable) { mc.edges[edgeKey].inOrOut += 1; }
            }
        }

        return mc;
    }

    public static (Vector3, Vector3) GetEdgeKey(Vector3 p1, Vector3 p2)
    {
        if (p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y) || (p1.x == p2.x && p1.y == p2.y && p1.z < p2.z))
            return (p1, p2);
        return (p2, p1);
    }
}

public class FragmentedMeshConstruct : MeshConstruct
{
    public MeshConstruct AddMeshFragment(Mesh mesh, Transform localTransform, bool isFlipped = false)
    {
        MeshConstruct fragment = new MeshConstruct();
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
            Vertex v = new Vertex(pos[i], normals[i], uv[i], 0);
            vertices.Add(v);
            fragment.vertices.Add(v);
            Node n;
            if (nodes.ContainsKey(pos[i]))
            {
                n = nodes[pos[i]];
                n.AddVertex(v);
            }
            else
            {
                n = new Node(v);
                nodes.Add(pos[i], n);
            }

            if (!fragment.nodes.ContainsKey(pos[i])) { fragment.nodes.Add(pos[i], n); }
        }

        List<int> triangles = new List<int>();
        mesh.GetTriangles(triangles, 0);
        if (isFlipped) { triangles.Reverse(); }

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3[] p = new Vector3[3] { pos[triangles[i]], pos[triangles[i + 1]], pos[triangles[i + 2]] };
            Face face = new Face(nodes[p[0]], nodes[p[1]], nodes[p[2]], 0);
            faces.Add(face);
            fragment.faces.Add(face);
            for (int j = 0; j < 3; j++)
            {
                (Vector3, Vector3) edgeKey = GetEdgeKey(p[j], p[(j + 1) % 3]);
                if (!edges.ContainsKey(edgeKey))
                {
                    edges.Add(edgeKey, new Edge(nodes[p[j]], nodes[p[(j + 1) % 3]]));
                }

                if (!fragment.edges.ContainsKey(edgeKey)) { fragment.edges.Add(edgeKey, edges[edgeKey]); }

                if (face.isWalkable) { edges[edgeKey].inOrOut += 1; }
            }
        }

        return fragment;
    }
}
