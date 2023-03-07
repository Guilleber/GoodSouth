using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;


public static class WFCParameters
{
    public static Vector3 WORLD_OFFSET;
    public static float WATER_LEVEL = 0.47f;
}

public static class WFCUtils
{
    public const int POSX = 0;
    public const int POSY = 1;
    public const int POSZ = 2;
    public const int NEGX = 3;
    public const int NEGY = 4;
    public const int NEGZ = 5;

    public static readonly Vector3Int[] dirOffset = new Vector3Int[] {
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, -1)
    };

    public static System.Random randGen = new System.Random(System.DateTime.Now.Millisecond);

    public static bool AreCompatible(string c1, string c2)
    {
        if (c1[0] == 'v' || c1[c1.Length - 1] == 's') { return c1 == c2; }
        if (c1[c1.Length - 1] == 'f') { return c1.Substring(0, c1.Length - 1) == c2; }
        return c2.Substring(0, c2.Length - 1) == c1;
    }

    public static bool AreLooselyCompatible(string c1, string c2)
    {
        if (c1[0] == 'v') { return c1.Substring(0, c1.Length - 2) == c2.Substring(0, c2.Length - 2); }
        if (c1[c1.Length - 1] == 's') { return c1 == c2; }
        if (c1[c1.Length - 1] == 'f') { return c1.Substring(0, c1.Length - 1) == c2; }
        return c2.Substring(0, c2.Length - 1) == c1;
    }

    public static string FlipVConnector(string c)
    {
        if (c == null) { return null; }
        if (c[0] != 'v') { throw new System.ArgumentException(c + " is not a valid vertical connector."); }
        if (c[c.Length - 1] == 's') { return c; }
        
        int rot = int.Parse(c[c.Length - 1].ToString());
        if ((c[c.Length - 3] == 'x' && (rot == 0 || rot == 2)) || (c[c.Length - 3] == 'y' && (rot == 1 || rot == 3))) { return c; }
        else if (c[c.Length - 3] == 'x' || c[c.Length - 3] == 'y') { return c.Substring(0, c.Length - 1) + (rot + 2) % 4; }

        string rawConnector;
        if (c[c.Length - 3] == 'f') { rawConnector = c.Substring(0, c.Length - 3); }
        else { rawConnector = c.Substring(0, c.Length - 2) + 'f'; }

        if (rot == 1 || rot == 3) { rot = (rot + 2) % 4; }
        return rawConnector + '-' + rot;
    }

    public static string FlipHConnector(string c)
    {
        if (c == null) { return null; }
        if (c[0] != 'h') { throw new System.ArgumentException(c + " is not a valid horizontal connector."); }
        if (c[c.Length - 1] == 's') { return c; }
        if (c[c.Length - 1] == 'f') { return c.Substring(0, c.Length - 1); }
        return c + 'f';
    }

    public static string RotateVConnector(string c)
    {
        if (c == null) { return null; }
        if (c[0] != 'v') { throw new System.ArgumentException(c + " is not a valid vertical connector."); }
        if (c[c.Length - 1] == 's') { return c; }
        return c.Substring(0, c.Length - 1) + ((int.Parse(c[c.Length - 1].ToString()) + 1) % 4).ToString();
    }

    public static string FlipConnector(string c)
    {
        if (c == null) { return null; }
        if (c[0] == 'h') { return FlipHConnector(c); }
        return c;
    }

    /*public static void FlipMesh(Mesh mesh, int axis)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        if (axis == 0) {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(-vertices[i].x, vertices[i].y, vertices[i].z);
                normals[i] = new Vector3(-normals[i].x, normals[i].y, normals[i].z);
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(vertices[i].x, -vertices[i].y, vertices[i].z);
                normals[i] = new Vector3(normals[i].x, -normals[i].y, normals[i].z);
            }
        }
        else
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(vertices[i].x, vertices[i].y, -vertices[i].z);
                normals[i] = new Vector3(normals[i].x, normals[i].y, -normals[i].z);
            }
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
    }*/

    public static Dictionary<Vector3Int, string[]> DeepCopyConnectors(Dictionary<Vector3Int, string[]> connectors)
    {
        Dictionary<Vector3Int, string[]> newConnectors = new Dictionary<Vector3Int, string[]>();
        foreach (Vector3Int key in connectors.Keys)
        {
            newConnectors.Add(key, (string[])connectors[key].Clone());
        }
        return newConnectors;
    }

    public static bool IsValidPosition(Vector3Int pos, Vector3Int mapSize)
    {
        if (pos.x < 0 || pos.x >= mapSize.x) { return false; }
        if (pos.y < 0 || pos.y >= mapSize.y) { return false; }
        if (pos.z < 0 || pos.z >= mapSize.z) { return false; }
        return true;
    }

    public static float Dist(Vector3 v1, Vector3 v2)
    {
        return (float)System.Math.Sqrt(System.Math.Pow(v1.x - v2.x, 2) + System.Math.Pow(v1.y - v2.y, 2) + System.Math.Pow(v1.z - v2.z, 2));
    }

    public static List<int> MergeByDistance(List<Vector3> vertices, float threshold)
    {
        List<int> res = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (Dist(vertices[i], vertices[j]) <= threshold)
                {
                    res.Add(j);
                    break;
                }
            }
        }
        return res;
    }
}

public class WFCPositionedModule
{
    private WFCModule m_module { get; }
    private int m_rotation = 0;
    private bool m_isFlipped = false;
    private Vector3Int m_position;
    private Vector3Int m_size;
    private Dictionary<Vector3Int, string[]> m_connectors;

    public WFCPositionedModule(WFCModule module, Vector3Int position = new Vector3Int(), int rotation = 0, bool flip = false)
    {
        m_module = module;
        m_position = position;
        m_size = module.m_size;
        m_connectors = WFCUtils.DeepCopyConnectors(module.m_connectors);
        if (flip) { Flip(); }
        for (int i = 0; i < rotation; i++)
        {
            this.Rotate();
        }
    }

    public WFCPositionedModule(WFCPositionedModule other, int addRotation = 0)
    {
        m_module = other.m_module;
        m_position = other.m_position;
        m_size = other.m_size;
        m_connectors = WFCUtils.DeepCopyConnectors(other.m_connectors);
        m_rotation = other.m_rotation;
        m_isFlipped = other.m_isFlipped;
        for (int i = 0; i < addRotation; i++)
        {
            this.Rotate();
        }
    }

    public Vector3Int GetPosition()
    {
        return m_position;
    }

    public string GetConnector(Vector3Int relPos, int dir)
    {
        return m_connectors[relPos][dir];
    }

    public Vector3Int GetSize()
    {
        return m_size;
    }

    public bool IsSymmetrical()
    {
        return m_module.m_isSymmetrical;
    }

    public bool IsEmpty()
    {
        return m_module.m_isEmpty;
    }

    public bool IsFlipped()
    {
        return m_isFlipped;
    }

    public int GetRotation()
    {
        return m_rotation;
    }

    public GameObject SelectSpawnable()
    {
        return m_module.SelectSpawnable();
    }

    public bool IsGroundOnly()
    {
        return m_module.IsGroundOnly();
    }

    public string GetName()
    {
        return m_module.m_name + " - " + m_position.ToString();
    }

    public Dictionary<Vector3Int, string[]>.KeyCollection GetSubModulesPositions()
    {
        return m_connectors.Keys;
    }

    public (GameObject, GameObject) Spawn(Transform parent, Dictionary<string, Material> materialDict)
    {
        if (m_module.m_isEmpty) { throw new System.ArgumentException("Cannot spawn an empty object"); }

        Vector3 pos = m_position + (Vector3)m_size / 2 - WFCParameters.WORLD_OFFSET;
        GameObject gameObject = GameObject.Instantiate(m_module.SelectSpawnable(), pos, Quaternion.Euler(0, 90 * m_rotation, 0), parent);
        Vector3 scale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(m_isFlipped ? -scale.x : scale.x, scale.y, scale.z);

        foreach (string childName in materialDict.Keys)
        {
            Transform part = gameObject.transform.Find(childName);
            if (part != null)
            {
                part.GetComponent<MeshRenderer>().material = materialDict[childName];
            }
        }

        pos = new Vector3(pos.x, -pos.y, pos.z);
        GameObject reflectionGameObject = GameObject.Instantiate(gameObject, pos, Quaternion.Euler(0, 90 * m_rotation, 0), parent);
        reflectionGameObject.transform.localScale = new Vector3(m_isFlipped ? -scale.x : scale.x, -scale.y, scale.z);
        return (gameObject, reflectionGameObject);
    }

    public void OnDrawGizmos()
    {
        if (!m_module.m_isEmpty)
        {
            Vector3 pos = m_position + m_module.m_offsetToCenter - WFCParameters.WORLD_OFFSET;
            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            Gizmos.DrawMesh(m_module.GetMesh(), pos, Quaternion.Euler(-90, 90 * m_rotation, 0));
        }
    }

    private void Rotate()
    {
        m_rotation += 1;

        Dictionary<Vector3Int, string[]> newConnectors = new Dictionary<Vector3Int, string[]>();
        foreach (Vector3Int key in m_connectors.Keys)
        {
            string[] connectors = m_connectors[key];

            // Rotate Y axis connectors
            connectors[WFCUtils.POSY] = WFCUtils.RotateVConnector(connectors[WFCUtils.POSY]);
            connectors[WFCUtils.NEGY] = WFCUtils.RotateVConnector(connectors[WFCUtils.NEGY]);

            // Rotate other connectors
            string temp = connectors[WFCUtils.POSX];
            connectors[WFCUtils.POSX] = connectors[WFCUtils.POSZ];
            connectors[WFCUtils.POSZ] = connectors[WFCUtils.NEGX];
            connectors[WFCUtils.NEGX] = connectors[WFCUtils.NEGZ];
            connectors[WFCUtils.NEGZ] = temp;

            // Rotate position vectors
            newConnectors.Add(new Vector3Int(key.z, key.y, m_size.x - 1 - key.x), connectors);
        }
        m_connectors = newConnectors;

        m_size = new Vector3Int(m_size.z, m_size.y, m_size.x);
    }

    private void Flip()
    {
        m_isFlipped = !m_isFlipped;

        Dictionary<Vector3Int, string[]> newConnectors = new Dictionary<Vector3Int, string[]>();
        foreach (Vector3Int key in m_connectors.Keys)
        {
            string[] connectors = m_connectors[key];

            // Flip vertical connectors
            connectors[WFCUtils.POSY] = WFCUtils.FlipVConnector(connectors[WFCUtils.POSY]);
            connectors[WFCUtils.NEGY] = WFCUtils.FlipVConnector(connectors[WFCUtils.NEGY]);

            // Flip other connectors
            string temp = connectors[WFCUtils.POSX];
            connectors[WFCUtils.POSX] = WFCUtils.FlipHConnector(connectors[WFCUtils.NEGX]);
            connectors[WFCUtils.NEGX] = WFCUtils.FlipHConnector(temp);

            connectors[WFCUtils.POSZ] = WFCUtils.FlipHConnector(connectors[WFCUtils.POSZ]);
            connectors[WFCUtils.NEGZ] = WFCUtils.FlipHConnector(connectors[WFCUtils.NEGZ]);

            // Flip position vectors
            newConnectors.Add(new Vector3Int(m_size.x - 1 - key.x, key.y, key.z), connectors);
        }
        m_connectors = newConnectors;
    }

    public bool IsPossible(Vector3Int mapSize)
    {
        Vector3Int topRightCorner = m_position + m_size;
        if (topRightCorner.x <= mapSize.x && topRightCorner.y <= mapSize.y && topRightCorner.z <= mapSize.z) { return true; }
        else { return false; }
    }
}
