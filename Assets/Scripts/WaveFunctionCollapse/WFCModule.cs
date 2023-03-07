using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Linq;

public class WFCModule : MonoBehaviour
{
    private static Dictionary<string, string> m_hConnectors = new Dictionary<string, string>();
    private static int m_hConnectorIndex = 3;

    private static Dictionary<string, string> m_vConnectors = new Dictionary<string, string>();
    private static int m_vConnectorIndex = 2;

    private static List<SubModule> m_allSubModules = new List<SubModule>();
    private static string m_highlightedConnector;

    public static readonly string EMPTY_HCONNECTOR = "h0s";
    public static readonly string EMPTY_VCONNECTOR = "v0s";
    public static readonly string FULL_HCONNECTOR = "h1s";
    public static readonly string FULL_VCONNECTOR = "v1s";
    public static readonly string FLAT_HCONNECTOR = "h2s";
    public static readonly string WATER_HCONNECTOR = "hws";

    static WFCModule()
    {
        m_hConnectors.Add("", EMPTY_HCONNECTOR);
        m_vConnectors.Add("", EMPTY_VCONNECTOR);

        List<Vector2> crossSection = new List<Vector2>();
        crossSection.Add(new Vector2(-0.5f, -0.5f));
        crossSection.Add(new Vector2(-0.5f, 0.5f));
        crossSection.Add(new Vector2(0.5f, -0.5f));
        crossSection.Add(new Vector2(0.5f, 0.5f));
        string key = WFCModule.CrossSectionToKey(crossSection);
        m_hConnectors.Add(key, FULL_HCONNECTOR);
        m_vConnectors.Add(key, FULL_VCONNECTOR);

        crossSection = new List<Vector2>();
        crossSection.Add(new Vector2(-0.5f, -0.5f));
        crossSection.Add(new Vector2(-0.5f, 0.0f));
        crossSection.Add(new Vector2(0.5f, -0.5f));
        crossSection.Add(new Vector2(0.5f, 0.0f));
        key = WFCModule.CrossSectionToKey(crossSection);
        m_hConnectors.Add(key, FLAT_HCONNECTOR);

        crossSection = new List<Vector2>();
        crossSection.Add(new Vector2(-0.5f, -0.5f));
        crossSection.Add(new Vector2(-0.5f, -0.5f + WFCParameters.WATER_LEVEL));
        crossSection.Add(new Vector2(0.5f, -0.5f));
        crossSection.Add(new Vector2(0.5f, -0.5f + WFCParameters.WATER_LEVEL));
        key = WFCModule.CrossSectionToKey(crossSection);
        m_hConnectors.Add(key, WATER_HCONNECTOR);
    }

    private static void UpdateConnectorHighlight(string connector)
    {
        Debug.Log("Connector '" + connector + "' selected.");
        m_highlightedConnector = connector;
    }

    private static string CrossSectionToKey(List<Vector2> crossSection)
    {
        string key = "";
        foreach (Vector2 v in crossSection)
        {
            key += "(" + v.x.ToString("0.00") + "," + v.y.ToString("0.00") + ")";
        }
        return key;
    }


    [HideInInspector]
    public string m_name;
    public Vector3Int m_size;
    public bool m_isSymmetrical;
    public bool m_isFlippable;

    [Space(10)]
    [Header("Manual Connectors")]
    [Space(5)]
    public bool m_isEmpty = false;
    public string[] m_manualConnectors;

    private Mesh m_mesh;
    private List<Vector3> m_vertices = new List<Vector3>();
    private List<Vector2Int> m_edges = new List<Vector2Int>();
    private List<Vector3> m_normals = new List<Vector3>();

    private bool[,,] m_trueShape;
    public Vector3 m_offsetToCenter;

    private Dictionary<Vector3Int, BorderNode> m_border;
    private List<int> m_borderVertices;
    private List<Vector2Int> m_borderEdges;

    public Dictionary<Vector3Int, SubModule> m_subModules;

    public Dictionary<Vector3Int, string[]> m_connectors;

    [Space(10)]
    [Header("Spawnables")]
    [Space(5)]
    public List<GameObject> m_spawnableObjects;

    [Space(10)]
    [Header("Placement Options")]
    [Space(5)]
    public bool m_isGroundOnly;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void Init()
    {
        m_offsetToCenter = (Vector3)m_size / 2;

        m_name = gameObject.name;

        if (m_isEmpty)
        {
            m_connectors = new Dictionary<Vector3Int, string[]>();
            m_connectors.Add(new Vector3Int(0, 0, 0), m_manualConnectors);
        }
        else
        {
            m_mesh = transform.Find("Mesh").GetComponent<MeshFilter>().mesh;
            this.InitVertices();
            this.InitEdges();
            this.InitNormals();
            this.CleanUp();

            m_trueShape = this.GetTrueShape();
            m_border = this.GetBorder();
            (m_borderVertices, m_borderEdges) = this.GetBorderVertices();
            this.identifyInNodes();

            this.BuildSubModules();
            this.BuildConnectors();
        }

        this.LogSelf();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public bool IsGroundOnly()
    {
        return m_isGroundOnly;
    }

    public bool IsSymmetrical()
    {
        return m_isSymmetrical;
    }

    public bool IsFlippable()
    {
        return m_isFlippable;
    }

    public Mesh GetMesh()
    {
        return m_mesh;
    }

    public string GetName()
    {
        return m_name;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isPlaying && !m_isEmpty)
        {
            Gizmos.color = Color.black;
            foreach (Vector2Int e in m_edges)
            {
                Gizmos.DrawLine(transform.TransformPoint(m_vertices[e.x] + 0.005f * m_normals[e.x] - m_offsetToCenter), transform.TransformPoint(m_vertices[e.y] + 0.005f * m_normals[e.y] - m_offsetToCenter));
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && !m_isEmpty)
        {
            foreach (Vector3Int key in m_subModules.Keys)
            {
                m_subModules[key].OnDrawGizmos(m_offsetToCenter, transform);
            }
        }
    }
#endif

    void LogSelf()
    {
        Debug.Log(m_name);
        Debug.Log("POS X:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.POSX] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.POSX]);
            }
        }
        Debug.Log("POS Y:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.POSY] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.POSY]);
            }
        }
        Debug.Log("POS Z:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.POSZ] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.POSZ]);
            }
        }
        Debug.Log("NEG X:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.NEGX] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.NEGX]);
            }
        }
        Debug.Log("NEG Y:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.NEGY] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.NEGY]);
            }
        }
        Debug.Log("NEG Z:");
        foreach (Vector3Int key in m_connectors.Keys)
        {
            if (m_connectors[key][WFCUtils.NEGZ] != null)
            {
                Debug.Log(key.ToString() + " : " + m_connectors[key][WFCUtils.NEGZ]);
            }
        }
    }

    public GameObject SelectSpawnable()
    {
        return m_spawnableObjects[WFCUtils.randGen.Next(m_spawnableObjects.Count)];
    }

    private void InitVertices()
    {
        m_vertices = new List<Vector3>(m_mesh.vertices);
        for (int i = 0; i < m_vertices.Count; i++)
        {
            m_vertices[i] = new Vector3((float)System.Math.Round(m_vertices[i].x, 2), (float)System.Math.Round(m_vertices[i].z, 2), (float)-System.Math.Round(m_vertices[i].y, 2)) + m_offsetToCenter;
        }
    }

    private void InitEdges()
    {
        int[] triangles = m_mesh.triangles;
        HashSet<Vector2Int> edges = new HashSet<Vector2Int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector2Int e = new Vector2Int(System.Math.Min(triangles[i], triangles[i + 1]), System.Math.Max(triangles[i], triangles[i + 1]));
            edges.Add(e);

            e = new Vector2Int(System.Math.Min(triangles[i + 1], triangles[i + 2]), System.Math.Max(triangles[i + 1], triangles[i + 2]));
            edges.Add(e);

            e = new Vector2Int(System.Math.Min(triangles[i + 2], triangles[i]), System.Math.Max(triangles[i + 2], triangles[i]));
            edges.Add(e);
        }
        m_edges =  new List<Vector2Int>(edges);
    }

    private void InitNormals()
    {
        m_normals = new List<Vector3>(m_mesh.normals);
        for (int i = 0; i < m_normals.Count; i++)
        {
            m_normals[i] = new Vector3(m_normals[i].x, m_normals[i].z, -m_normals[i].y);
        }
    }

    private void CleanUp()
    {
        /*
         * This function removes artefacts of the transfert between Blender and Unity such as duplicated vertices.
         */

        if (m_vertices == null || m_edges == null || m_normals == null) { throw new System.ArgumentException("Vertices, edges and normals should be initialized before calling CleanUp."); }

        List<Vector3> newNormals = new List<Vector3>();
        List<Vector3> newVertices = new List<Vector3>();
        List<int> count = new List<int>();
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
        Dictionary<Vector3, int> vertexOrigins = new Dictionary<Vector3, int>();

        for (int i = 0; i < m_vertices.Count; i++)
        {
            if (!vertexOrigins.ContainsKey(m_vertices[i]))
            {
                vertexOrigins.Add(m_vertices[i], newVertices.Count);
                newVertices.Add(m_vertices[i]);
                newNormals.Add(m_normals[i]);
                count.Add(1);
            }
            else
            {
                newNormals[vertexOrigins[m_vertices[i]]] += m_normals[i];
                count[vertexOrigins[m_vertices[i]]] += 1;
            }
            vertexMapping.Add(i, vertexOrigins[m_vertices[i]]);
        }

        for (int i = 0; i < newNormals.Count; i++)
        {
            newNormals[i] /= count[i];
        }

        for (int i = 0; i < m_edges.Count; i++)
        {
            m_edges[i] = new Vector2Int(vertexMapping[m_edges[i].x], vertexMapping[m_edges[i].y]);
        }

        m_vertices = newVertices;
        m_normals = newNormals;
    }

    private bool[,,] GetTrueShape()
    {
        /*
         * computes a boolean array expressing which submodules are empty.
         */

        bool[,,] trueShape = new bool[m_size.x, m_size.y, m_size.z];

        foreach (Vector2Int e in m_edges)
        {
            Vector3 midEdge = (m_vertices[e.x] + m_vertices[e.y]) / 2;
            if ((int)midEdge.x != midEdge.x && (int)midEdge.y != midEdge.y && (int)midEdge.z != midEdge.z)
                trueShape[(int)midEdge.x, (int)midEdge.y, (int)midEdge.z] = true;
        }
        return trueShape;
    }

    private bool isInsideShape(int x, int y, int z)
    {
        if (x == -1 || y == -1 || z == -1) { return false; }
        if (x == m_size.x || y == m_size.y || z == m_size.z) { return false; }
        return m_trueShape[x, y, z];
    }

    private bool isBorderNode(int x, int y, int z)
    {
        bool allTrue = isInsideShape(x, y, z) &&
            isInsideShape(x - 1, y, z) &&
            isInsideShape(x, y - 1, z) &&
            isInsideShape(x, y, z - 1) &&
            isInsideShape(x - 1, y - 1, z) &&
            isInsideShape(x - 1, y, z - 1) &&
            isInsideShape(x, y - 1, z - 1) &&
            isInsideShape(x - 1, y - 1, z - 1);

        bool allFalse = !isInsideShape(x, y, z) &&
            !isInsideShape(x - 1, y, z) &&
            !isInsideShape(x, y - 1, z) &&
            !isInsideShape(x, y, z - 1) &&
            !isInsideShape(x - 1, y - 1, z) &&
            !isInsideShape(x - 1, y, z - 1) &&
            !isInsideShape(x, y - 1, z - 1) &&
            !isInsideShape(x - 1, y - 1, z - 1);

        return !allTrue && !allFalse;
    }

    private bool isBorderEdge(int x, int y, int z, char dir)
    {
        bool isInnerEdge, isOuterEdge;
        switch (dir)
        {
            case 'X':
                isInnerEdge = isInsideShape(x, y, z) &&
                    isInsideShape(x, y - 1, z) &&
                    isInsideShape(x, y, z - 1) &&
                    isInsideShape(x, y - 1, z - 1);

                isOuterEdge = !isInsideShape(x, y, z) &&
                    !isInsideShape(x, y - 1, z) &&
                    !isInsideShape(x, y, z - 1) &&
                    !isInsideShape(x, y - 1, z - 1);

                return !isOuterEdge && !isInnerEdge;
            case 'Y':
                isInnerEdge = isInsideShape(x, y, z) &&
                    isInsideShape(x - 1, y, z) &&
                    isInsideShape(x, y, z - 1) &&
                    isInsideShape(x - 1, y, z - 1);

                isOuterEdge = !isInsideShape(x, y, z) &&
                    !isInsideShape(x - 1, y, z) &&
                    !isInsideShape(x, y, z - 1) &&
                    !isInsideShape(x - 1, y, z - 1);

                return !isOuterEdge && !isInnerEdge;
            case 'Z':
                isInnerEdge = isInsideShape(x, y, z) &&
                    isInsideShape(x - 1, y, z) &&
                    isInsideShape(x, y - 1, z) &&
                    isInsideShape(x - 1, y - 1, z);

                isOuterEdge = !isInsideShape(x, y, z) &&
                    !isInsideShape(x - 1, y, z) &&
                    !isInsideShape(x, y - 1, z) &&
                    !isInsideShape(x - 1, y - 1, z);

                return !isOuterEdge && !isInnerEdge;
            default:
                throw new System.ArgumentException("dir should be 'X', 'Y' or 'Z'");
        }
    }

    private bool isBorderVertex(float x, float y, float z)
    {
        if (x == (int)x)
        {
            if (y == (int)y)
            {
                if (z == (int)z)
                {
                    return isBorderNode((int)x, (int)y, (int)z);
                }

                return isBorderEdge((int)x, (int)y, (int)z, 'Z');
            }

            if (z == (int)z)
            {
                return isBorderEdge((int)x, (int)y, (int)z, 'Y');
            }

            return isInsideShape((int)x, (int)y, (int)z) != isInsideShape((int)x - 1, (int)y, (int)z);
        }

        if (y == (int)y)
        {
            if (z == (int)z)
            {
                return isBorderEdge((int)x, (int)y, (int)z, 'X');
            }

            return isInsideShape((int)x, (int)y, (int)z) != isInsideShape((int)x, (int)y - 1, (int)z);
        }

        if (z == (int)z)
        {
            return isInsideShape((int)x, (int)y, (int)z) != isInsideShape((int)x, (int)y, (int)z - 1);
        }

        return false;
    }

    private class BorderNode
    {
        public List<BorderNode> neighbors = new List<BorderNode>();
        public int inOrOut = 0;
        public Vector3Int position { get; }
        public int vertexIndex = -1;
        public int[] connectedVertices = new int[6];

        public BorderNode(Vector3Int position)
        {
            this.position = position;
            for (int i = 0; i < 6; i++)
            {
                connectedVertices[i] = -1;
            }
        }

        public void propagate()
        {
            if (inOrOut == 0) { return; }

            foreach (BorderNode other in neighbors)
            {
                if (other.inOrOut == 0)
                {
                    other.inOrOut = this.inOrOut;
                    other.propagate();
                }
            }
        }
    }

    private Dictionary<Vector3Int, BorderNode> GetBorder()
    {
        Dictionary<Vector3Int, BorderNode> border = new Dictionary<Vector3Int, BorderNode>();
        for (int x = 0; x <= m_size.x; x++)
        {
            for (int y = 0; y <= m_size.y; y++)
            {
                for (int z = 0; z <= m_size.z; z++)
                {
                    if (isBorderNode(x, y, z))
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        BorderNode node = new BorderNode(pos);

                        Vector3Int posOther = new Vector3Int(x - 1, y, z);
                        if (border.ContainsKey(posOther))
                        {
                            if (isBorderEdge(x - 1, y, z, 'X'))
                            {
                                BorderNode other = border[posOther];
                                node.neighbors.Add(other);
                                other.neighbors.Add(node);
                            }
                        }

                        posOther = new Vector3Int(x, y - 1, z);
                        if (border.ContainsKey(posOther))
                        {
                            if (isBorderEdge(x, y - 1, z, 'Y'))
                            {
                                BorderNode other = border[posOther];
                                node.neighbors.Add(other);
                                other.neighbors.Add(node);
                            }
                        }

                        posOther = new Vector3Int(x, y, z - 1);
                        if (border.ContainsKey(posOther))
                        {
                            if (isBorderEdge(x, y, z - 1, 'Z'))
                            {
                                BorderNode other = border[posOther];
                                node.neighbors.Add(other);
                                other.neighbors.Add(node);
                            }
                        }
                        border.Add(pos, node);
                    }
                }
            }
        }
        return border;
    }

    private (List<int>, List<Vector2Int>) GetBorderVertices()
    {
        HashSet<int> borderVertices = new HashSet<int>();
        for (int i = 0; i < m_vertices.Count; i++)
        {
            if (isBorderVertex(m_vertices[i].x, m_vertices[i].y, m_vertices[i].z))
            {
                borderVertices.Add(i);
            }
        }

        List<Vector2Int> borderEdges = new List<Vector2Int>();
        foreach (Vector2Int e in m_edges)
        {
            if (borderVertices.Contains(e.x) && borderVertices.Contains(e.y))
            {
                Vector3 v1 = m_vertices[e.x];
                Vector3 v2 = m_vertices[e.y];
                if ((int)v1.x == v1.x && v1.x == v2.x) { borderEdges.Add(e); }
                else if ((int)v1.y == v1.y && v1.y == v2.y) { borderEdges.Add(e); }
                else if ((int)v1.z == v1.z && v1.z == v2.z) { borderEdges.Add(e); }
            }
        }

        return (borderVertices.ToList(), borderEdges);
    }

    private void identifyInNodes()
    {
        m_borderVertices.Sort(delegate (int i, int j) {
            Vector3 v1 = m_vertices[i];
            Vector3 v2 = m_vertices[j];
            if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y) || (v1.x == v2.x && v1.y == v2.y && v1.z < v2.z)) { return -1; }
            else if (v1.x > v2.x || (v1.x == v2.x && v1.y > v2.y) || (v1.x == v2.x && v1.y == v2.y && v1.z > v2.z)) { return 1; }
            return 0;
        });

        foreach (int vIndex in m_borderVertices)
        {
            Vector3 v = m_vertices[vIndex];
            if ((int)v.x == v.x)
            {
                if ((int)v.y == v.y)
                {
                    BorderNode node1 = m_border[new Vector3Int((int)v.x, (int)v.y, (int)v.z)];
                    BorderNode node2 = m_border[new Vector3Int((int)v.x, (int)v.y, (int)v.z + 1)];
                    node2.inOrOut = -System.Math.Sign(m_normals[vIndex].z);
                    if (node2.inOrOut == 1) { node2.connectedVertices[WFCUtils.NEGZ] = vIndex; }
                    if (node1.inOrOut == 0)
                    {
                        node1.inOrOut = -node2.inOrOut;
                    }
                    if (node1.inOrOut == 1 && node1.connectedVertices[WFCUtils.POSZ] == -1) { node1.connectedVertices[WFCUtils.POSZ] = vIndex; }
                }
                else if ((int)v.z == v.z)
                {
                    BorderNode node1 = m_border[new Vector3Int((int)v.x, (int)v.y, (int)v.z)];
                    BorderNode node2 = m_border[new Vector3Int((int)v.x, (int)v.y + 1, (int)v.z)];
                    node2.inOrOut = -System.Math.Sign(m_normals[vIndex].y);
                    if (node2.inOrOut == 1) { node2.connectedVertices[WFCUtils.NEGY] = vIndex; }
                    if (node1.inOrOut == 0)
                    {
                        node1.inOrOut = -node2.inOrOut;
                    }
                    if (node1.inOrOut == 1 && node1.connectedVertices[WFCUtils.POSY] == -1 ) { node1.connectedVertices[WFCUtils.POSY] = vIndex; }
                }
            }
            else if ((int)v.y == v.y)
            {
                if ((int)v.z == v.z)
                {
                    BorderNode node1 = m_border[new Vector3Int((int)v.x, (int)v.y, (int)v.z)];
                    BorderNode node2 = m_border[new Vector3Int((int)v.x + 1, (int)v.y, (int)v.z)];
                    node2.inOrOut = -System.Math.Sign(m_normals[vIndex].x);
                    if (node2.inOrOut == 1) { node2.connectedVertices[WFCUtils.NEGX] = vIndex; }
                    if (node1.inOrOut == 0)
                    {
                        node1.inOrOut = -node2.inOrOut;
                    }
                    if (node1.inOrOut == 1 && node1.connectedVertices[WFCUtils.POSX] == -1) { node1.connectedVertices[WFCUtils.POSX] = vIndex; }
                }
            }
        }

        foreach (Vector3Int key in m_border.Keys)
        {
            m_border[key].propagate();
        }

        List<Vector3> nodeVertices = new List<Vector3>();
        foreach (Vector3Int key in m_border.Keys)
        {
            BorderNode node = m_border[key];
            if (node.inOrOut == 1)
            {
                node.vertexIndex = nodeVertices.Count + m_vertices.Count;
                nodeVertices.Add((Vector3)node.position);
                m_borderVertices.Add(node.vertexIndex);

                foreach (BorderNode other in node.neighbors)
                {
                    if (other.inOrOut == 1 && other.vertexIndex != -1)
                    {
                        m_borderEdges.Add(new Vector2Int(node.vertexIndex, other.vertexIndex));
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    if (node.connectedVertices[i] != -1) { m_borderEdges.Add(new Vector2Int(node.vertexIndex, node.connectedVertices[i])); }
                }
            }
        }

        m_vertices.AddRange(nodeVertices);
    }

    public class SubModule
    {
        private List<Vector3> m_vertices = new List<Vector3>();
        private Dictionary<int, int> m_verticeIndexMap = new Dictionary<int, int>();
        private List<Vector2Int> m_edges = new List<Vector2Int>();
        private readonly Vector3 m_offset = new Vector3(0.5f, 0.5f, 0.5f);
        private Vector3Int m_position;
        private List<int> m_nodeIndices = new List<int>();

        private HashSet<int>[] m_verticesBySides = new HashSet<int>[6];
        public string[] m_connectors { get; } = new string[6];

        public SubModule(Vector3Int pos)
        {
            m_position = pos;
            m_allSubModules.Add(this);
        }

        public void AddVertex(Vector3 vertex, int index)
        {
            m_verticeIndexMap.Add(index, m_vertices.Count);
            if ((int)vertex.x == vertex.x && (int)vertex.y == vertex.y && (int)vertex.z == vertex.z) { m_nodeIndices.Add(m_vertices.Count); }
            m_vertices.Add(vertex - m_offset);
        }

        public void PickEdges(List<Vector2Int> edges)
        {
            foreach (Vector2Int e in edges)
            {
                if (m_verticeIndexMap.ContainsKey(e.x) && m_verticeIndexMap.ContainsKey(e.y))
                {
                    m_edges.Add(new Vector2Int(m_verticeIndexMap[e.x], m_verticeIndexMap[e.y]));
                }
            }
        }

        public void BuildConnectors(int dir)
        {
            m_verticesBySides[dir] = new HashSet<int>();
            for (int i = 0; i < m_vertices.Count; i++)
            {
                Vector3 v = m_vertices[i];
                if (v.x * WFCUtils.dirOffset[dir].x + v.y * WFCUtils.dirOffset[dir].y + v.z * WFCUtils.dirOffset[dir].z == 0.5f) { m_verticesBySides[dir].Add(i); }
            }

            if (dir == WFCUtils.NEGY || dir == WFCUtils.POSY)
            {
                m_connectors[dir] = GetVConnector(GetProjection(m_verticesBySides[dir], dir));
            }
            else
            {
                m_connectors[dir] = GetHConnector(GetProjection(m_verticesBySides[dir], dir));
            }
        }

        private List<Vector2> GetProjection(HashSet<int> vertexIndices, int dir)
        {
            List<Vector2> projection = new List<Vector2>();
            foreach (int i in vertexIndices)
            {
                Vector3 v = m_vertices[i];
                switch (dir)
                {
                    case WFCUtils.NEGX:
                        projection.Add(new Vector2(-v.z, v.y));
                        break;
                    case WFCUtils.POSX:
                        projection.Add(new Vector2(v.z, v.y));
                        break;
                    case WFCUtils.NEGY:
                    case WFCUtils.POSY:
                        projection.Add(new Vector2(v.x, v.z));
                        break;
                    case WFCUtils.NEGZ:
                        projection.Add(new Vector2(v.x, v.y));
                        break;
                    case WFCUtils.POSZ:
                        projection.Add(new Vector2(-v.x, v.y));
                        break;
                    default:
                        throw new System.ArgumentException("Invalid 'dir' argument.");
                }
            }
            return projection;
        }

        private List<Vector2> FlipCrossSection(List<Vector2> crossSection)
        {
            for (int i = 0; i < crossSection.Count; i++)
            {
                crossSection[i] = new Vector2(-crossSection.ElementAt(i).x, crossSection.ElementAt(i).y);
            }
            return crossSection;
        }

        private string GetHConnector(List<Vector2> crossSection)
        {
            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string key = WFCModule.CrossSectionToKey(crossSection);
            if (m_hConnectors.ContainsKey(key))
                return m_hConnectors[key];

            crossSection = FlipCrossSection(crossSection);

            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string reverseKey = WFCModule.CrossSectionToKey(crossSection);
            if (m_hConnectors.ContainsKey(reverseKey))
                return m_hConnectors[reverseKey] + "f";

            if (key == reverseKey)
            {
                m_hConnectors[key] = "h" + m_hConnectorIndex + "s";
                m_hConnectorIndex += 1;
                return m_hConnectors[key];
            }

            m_hConnectors[key] = "h" + m_hConnectorIndex.ToString();
            m_hConnectorIndex += 1;
            return m_hConnectors[key];
        }

        private List<Vector2> RotateCrossSection(List<Vector2> crossSection)
        {
            for (int i = 0; i < crossSection.Count; i++)
            {
                crossSection[i] = new Vector2(-crossSection[i].y, crossSection[i].x);
            }
            return crossSection;
        }

        private string GetVConnector(List<Vector2> crossSection)
        {
            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string key0 = WFCModule.CrossSectionToKey(crossSection);
            if (m_vConnectors.ContainsKey(key0))
                return m_vConnectors[key0] + (m_vConnectors[key0][m_vConnectors[key0].Length - 1] == 's' ? "" : "-0");

            crossSection = this.RotateCrossSection(crossSection);

            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string key1 = WFCModule.CrossSectionToKey(crossSection);
            if (m_vConnectors.ContainsKey(key1))
                return m_vConnectors[key1] + (m_vConnectors[key1][m_vConnectors[key1].Length - 1] == 's' ? "" : "-1");

            crossSection = this.RotateCrossSection(crossSection);

            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string key2 = WFCModule.CrossSectionToKey(crossSection);
            if (m_vConnectors.ContainsKey(key2))
                return m_vConnectors[key2] + (m_vConnectors[key2][m_vConnectors[key2].Length - 1] == 's' ? "" : "-2");

            crossSection = this.RotateCrossSection(crossSection);

            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string key3 = WFCModule.CrossSectionToKey(crossSection);
            if (m_vConnectors.ContainsKey(key3))
                return m_vConnectors[key3] + (m_vConnectors[key3][m_vConnectors[key3].Length - 1] == 's' ? "" : "-3");

            if (key0 == key1)
            {
                m_vConnectors.Add(key0, "v" + m_vConnectorIndex + "s");
                m_vConnectorIndex += 1;
                return m_vConnectors[key0];
            }

            //Try fliping the key

            crossSection = RotateCrossSection(crossSection);
            crossSection = FlipCrossSection(crossSection);

            crossSection.Sort(delegate (Vector2 v1, Vector2 v2)
            {
                if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
                    return -1;

                if (v2.x < v1.x || (v2.x == v1.x && v2.y < v1.y))
                    return 1;

                return 0;
            });

            string keyFlip = WFCModule.CrossSectionToKey(crossSection);

            if (keyFlip != key0 && keyFlip != key1 && keyFlip != key2 && keyFlip != key3)
            {
                m_vConnectors.Add(keyFlip, "v" + m_vConnectorIndex + "f");
            }

            //Default case -> create a new key

            if (keyFlip == key0)
            {
                m_vConnectors.Add(key0, "v" + m_vConnectorIndex + "x");
                m_vConnectorIndex += 1;
                return m_vConnectors[key0] + "-0";
            }

            if (keyFlip == key2)
            {
                m_vConnectors.Add(key0, "v" + m_vConnectorIndex + "y");
                m_vConnectorIndex += 1;
                return m_vConnectors[key0] + "-0";
            }

            m_vConnectors.Add(key0, "v" + m_vConnectorIndex);
            m_vConnectorIndex += 1;
            return m_vConnectors[key0] + "-0";
        }

        #if UNITY_EDITOR
        public void OnDrawGizmos(Vector3 offsetToCenter, Transform transform)
        {
            Gizmos.color = Color.white;
            foreach (int nodeIndex in m_nodeIndices)
            {
                Gizmos.DrawSphere(transform.TransformPoint((Vector3)m_position + m_vertices[nodeIndex] * 0.95f + m_offset - offsetToCenter), 0.05f);
            }

            foreach (Vector2Int e in m_edges)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.TransformPoint((Vector3)m_position + m_vertices[e.x] * 0.95f + m_offset - offsetToCenter),
                        transform.TransformPoint((Vector3)m_position + m_vertices[e.y] * 0.95f + m_offset - offsetToCenter));

                if (m_highlightedConnector != null)
                {
                    for (int dir = 0; dir < 6; dir++)
                    {
                        if (m_connectors[dir] != null && m_verticesBySides[dir].Contains(e.x) && m_verticesBySides[dir].Contains(e.y))
                        {
                            if (WFCUtils.AreLooselyCompatible(m_connectors[dir], m_highlightedConnector))
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawLine(transform.TransformPoint((Vector3)m_position + m_vertices[e.x] * 0.95f + m_offset - offsetToCenter),
                                    transform.TransformPoint((Vector3)m_position + m_vertices[e.y] * 0.95f + m_offset - offsetToCenter));
                            }
                            else if (m_connectors[dir] == m_highlightedConnector)
                            {
                                Gizmos.color = Color.blue;
                                Gizmos.DrawLine(transform.TransformPoint((Vector3)m_position + m_vertices[e.x] * 0.95f + m_offset - offsetToCenter),
                                    transform.TransformPoint((Vector3)m_position + m_vertices[e.y] * 0.95f + m_offset - offsetToCenter));
                            }
                            else if ((dir == WFCUtils.POSY || dir == WFCUtils.NEGY) && WFCUtils.AreLooselyCompatible(WFCUtils.FlipVConnector(m_connectors[dir]), m_highlightedConnector))
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawLine(transform.TransformPoint((Vector3)m_position + m_vertices[e.x] * 0.95f + m_offset - offsetToCenter),
                                    transform.TransformPoint((Vector3)m_position + m_vertices[e.y] * 0.95f + m_offset - offsetToCenter));
                            }
                        }
                    }
                }
            }
        }

        public void OnSceneGUI(Vector3 offsetToCenter, Transform transform)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                if (m_connectors[dir] != null && m_connectors[dir] != "v0s" && m_connectors[dir] != "h0s")
                {
                    Handles.color = Color.white;
                    if (Handles.Button(transform.TransformPoint((Vector3)m_position + (Vector3)WFCUtils.dirOffset[dir] * 0.475f + m_offset - offsetToCenter),
                            Quaternion.identity,
                            0.05f,
                            0.1f,
                            Handles.SphereHandleCap)) { UpdateConnectorHighlight(m_connectors[dir]); }
                }
            }
        }
        #endif
    }

    private void BuildSubModules()
    {
        m_subModules = new Dictionary<Vector3Int, SubModule>();
        for (int x = 0; x < m_size.x; x++)
        {
            for (int y = 0; y < m_size.y; y++)
            {
                for (int z = 0; z < m_size.z; z++)
                {
                    if (m_trueShape[x, y, z])
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        m_subModules.Add(pos, new SubModule(pos));
                    }
                }
            }
        }

        foreach (int i in m_borderVertices)
        {
            Vector3 v = m_vertices[i];
            int intX = (int)v.x;
            int intY = (int)v.y;
            int intZ = (int)v.z;
            if (intX == v.x)
            {
                if (intY == v.y)
                {
                    if (intZ == v.z)
                    {
                        if (isInsideShape(intX, intY, intZ - 1))
                        {
                            m_subModules[new Vector3Int(intX, intY, intZ - 1)].AddVertex(v - new Vector3(intX, intY, intZ - 1), i);
                        }

                        if (isInsideShape(intX - 1, intY, intZ - 1))
                        {
                            m_subModules[new Vector3Int(intX - 1, intY, intZ - 1)].AddVertex(v - new Vector3(intX - 1, intY, intZ - 1), i);
                        }

                        if (isInsideShape(intX, intY - 1, intZ - 1))
                        {
                            m_subModules[new Vector3Int(intX, intY - 1, intZ - 1)].AddVertex(v - new Vector3(intX, intY - 1, intZ - 1), i);
                        }

                        if (isInsideShape(intX - 1, intY - 1, intZ - 1))
                        {
                            m_subModules[new Vector3Int(intX - 1, intY - 1, intZ - 1)].AddVertex(v - new Vector3(intX - 1, intY - 1, intZ - 1), i);
                        }
                    }

                    if (isInsideShape(intX, intY - 1, intZ))
                    {
                        m_subModules[new Vector3Int(intX, intY - 1, intZ)].AddVertex(v - new Vector3(intX, intY - 1, intZ), i);
                    }

                    if (isInsideShape(intX - 1, intY - 1, intZ))
                    {
                        m_subModules[new Vector3Int(intX - 1, intY - 1, intZ)].AddVertex(v - new Vector3(intX - 1, intY - 1, intZ), i);
                    }
                } 
                else if (intZ == v.z)
                {
                    if (isInsideShape(intX, intY, intZ - 1))
                    {
                        m_subModules[new Vector3Int(intX, intY, intZ - 1)].AddVertex(v - new Vector3(intX, intY, intZ - 1), i);
                    }

                    if (isInsideShape(intX - 1, intY, intZ - 1))
                    {
                        m_subModules[new Vector3Int(intX - 1, intY, intZ - 1)].AddVertex(v - new Vector3(intX - 1, intY, intZ - 1), i);
                    }
                }

                if (isInsideShape(intX, intY, intZ))
                {
                    m_subModules[new Vector3Int(intX, intY, intZ)].AddVertex(v - new Vector3(intX, intY, intZ), i);
                }

                if (isInsideShape(intX - 1, intY, intZ))
                {
                    m_subModules[new Vector3Int(intX - 1, intY, intZ)].AddVertex(v - new Vector3(intX - 1, intY, intZ), i);
                }
            }
            else if (intY == v.y)
            {
                if (intZ == v.z)
                {
                    if (isInsideShape(intX, intY, intZ - 1))
                    {
                        m_subModules[new Vector3Int(intX, intY, intZ - 1)].AddVertex(v - new Vector3(intX, intY, intZ - 1), i);
                    }

                    if (isInsideShape(intX, intY - 1, intZ - 1))
                    {
                        m_subModules[new Vector3Int(intX, intY - 1, intZ - 1)].AddVertex(v - new Vector3(intX, intY - 1, intZ - 1), i);
                    }
                }

                if (isInsideShape(intX, intY, intZ))
                {
                    m_subModules[new Vector3Int(intX, intY, intZ)].AddVertex(v - new Vector3(intX, intY, intZ), i);
                }

                if (isInsideShape(intX, intY - 1, intZ))
                {
                    m_subModules[new Vector3Int(intX, intY  - 1, intZ)].AddVertex(v - new Vector3(intX, intY - 1, intZ), i);
                }
            }
            else if (intZ == v.z)
            {
                if (isInsideShape(intX, intY, intZ))
                {
                    m_subModules[new Vector3Int(intX, intY, intZ)].AddVertex(v - new Vector3(intX, intY, intZ), i);
                }

                if (isInsideShape(intX, intY, intZ - 1))
                {
                    m_subModules[new Vector3Int(intX, intY, intZ - 1)].AddVertex(v - new Vector3(intX, intY, intZ - 1), i);
                }
            }
        }

        foreach (Vector3Int key in m_subModules.Keys)
        {
            m_subModules[key].PickEdges(m_borderEdges);
            for (int dir = 0; dir < 6; dir++)
            {
                Vector3Int otherPos = key + WFCUtils.dirOffset[dir];
                if (!isInsideShape(otherPos.x, otherPos.y, otherPos.z)) { m_subModules[key].BuildConnectors(dir); }
            }
        }
    }

    private void BuildConnectors()
    {
        m_connectors = new Dictionary<Vector3Int, string[]>();

        foreach (Vector3Int key in m_subModules.Keys)
        {
            m_connectors.Add(key, m_subModules[key].m_connectors);
        }
    }
}
