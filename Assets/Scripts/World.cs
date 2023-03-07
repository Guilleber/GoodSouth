using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

public class World : MonoBehaviour
{
    private List<WFCModule> m_moduleList = new List<WFCModule>();
    private WFCSolver m_solver;

    public Vector3Int m_mapSize = new Vector3Int(10, 3, 10);

    public bool m_visualizeGeneration;
    public string[] m_excludeFromTopology;

    [Space(10)]
    [Header("Outline")]
    [Space(3)]
    [Range(0.0f, 0.1f)]
    public float m_outlineAmount;
    public Material m_outlineMaterial;

    [Space(10)]
    [Header("Weather")]
    [Space(3)]
    public bool m_rain = false;
    public GameObject m_rainParticleSystem;
    [Space(3)]
    public bool m_snow = false;
    public GameObject m_snowParticleSystem;
    [Space(3)]
    public Vector2 m_wind;

    [Space(10)]
    [Header("Materials")]
    [Space(3)]
    public Material m_grassMaterial;
    public Material m_rockMaterial;
    public Material m_coastMaterial;
    private Dictionary<string, Material> m_materialDict = new Dictionary<string, Material>();

    [Space(10)]
    [Header("Grass Shadow")]
    [Space(3)]
    // for grass shadow
    public int m_pixelsPerTile = 64;
    public ComputeShader m_grassShadowComputeShader;

    [Space(10)]
    [Header("Navigation")]
    [Space(3)]
    public bool m_visualizeNavigation;
    [Range(0, 90)]
    public int m_maxNavigableAngle = 30;

    private Mesh m_navMesh;
    private List<Vector3> m_navVertices;
    private List<Vector3> m_navNormals;
    private List<int> m_navTriangles;
    private List<Vector3> m_navSurfaceNormals;

    private List<GameObject> m_spawnedModules = new List<GameObject>();
    private List<GameObject> m_spawnedReflections = new List<GameObject>();
    private GameObject m_outline;
    private Transform m_modulesContainer;
    private Transform m_reflectionsContainer;

    private Mesh m_topologyMesh;
    private List<Vector3> m_topologyVertices;
    private List<Vector3> m_topologyNormals;
    private List<int> m_topologyTriangles;
    private List<Vector3> m_topologySurfaceNormals;

    //private Topology m_topology = new Topology();


    private GameObject m_weatherParticuleSystem;

    // For test purposes
    public Material flatFacesShader;

    // Start is called before the first frame update
    void Start()
    {
        WFCParameters.WORLD_OFFSET = new Vector3(m_mapSize.x/2.0f, WFCParameters.WATER_LEVEL, m_mapSize.z/2.0f);

        Debug.Log("World : Building modules...");
        InitModules();
        Debug.Log("World : Done building modules.");

        Debug.Log("Initializing solver...");
        m_solver = new WFCSolver(m_mapSize, m_moduleList);
        Debug.Log("Done initializing solver.");

        Debug.Log("Solving...");
        m_solver.ApplyBorderConstraints(WFCModule.FULL_VCONNECTOR, WFCModule.EMPTY_VCONNECTOR, WFCModule.EMPTY_HCONNECTOR, WFCModule.WATER_HCONNECTOR);
        m_solver.Solve();
        Debug.Log("Solved.");

        Debug.Log("Spawning map...");
        SpawnMap(m_solver.GetMap());
        Debug.Log("Map spawned.");

        Debug.Log("Initializing Weather...");
        InitWeather();
        Debug.Log("Weather Init Done.");

        foreach (RenderPipeline rp in GameObject.FindObjectsOfType<RenderPipeline>())
        {
            rp.OnSpawn();
            rp.AfterAllSpawn();
        }
    }

    private void InitModules()
    {
        foreach (Transform moduleTransform in GameObject.Find("BasicModules").transform)
        {
            WFCModule module = moduleTransform.GetComponent<WFCModule>();
            module.Init();
            m_moduleList.Add(module);
        }

        foreach (Transform moduleTransform in GameObject.Find("WaterModules").transform)
        {
            WFCModule module = moduleTransform.GetComponent<WFCModule>();
            module.Init();
            m_moduleList.Add(module);
        }
    }

    void InitWeather()
    {
        if (!m_rain && !m_snow) { return; }

        m_wind = new Vector2(System.Math.Max(System.Math.Min(m_wind.x, 1), -1), System.Math.Max(System.Math.Min(m_wind.y, 1), -1));
        float thetaZ = (float)(System.Math.Atan(m_wind.x) * (180 / System.Math.PI));
        float thetaX = -(float)(System.Math.Atan(m_wind.y) * (180 / System.Math.PI));

        m_weatherParticuleSystem = GameObject.Instantiate(m_rain ? m_rainParticleSystem : m_snowParticleSystem, new Vector3(), Quaternion.Euler(thetaX, 0.0f, thetaZ));
        m_weatherParticuleSystem.transform.parent = transform;
        m_weatherParticuleSystem.name = m_rain ? "Rain" : "Snow";
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (m_visualizeGeneration)
                m_solver.OnDrawGizmos();
        }
    }

    private void SpawnMap(List<WFCPositionedModule> pModules)
    {
        m_materialDict["Rock"] = m_rockMaterial;
        m_materialDict["Grass"] = m_grassMaterial;
        m_materialDict["Coast"] = m_coastMaterial;

        m_modulesContainer = (new GameObject("Modules")).transform;
        m_modulesContainer.parent = transform;
        m_reflectionsContainer = (new GameObject("Reflections")).transform;
        m_reflectionsContainer.parent = transform;

        foreach (WFCPositionedModule module in pModules)
        {
            SpawnModule(module);
        }

        //BuildTopology();
        //BuildCollider();
        //BuildOutline();
        BuildGrassShadow();
    }

    private void SpawnModule(WFCPositionedModule pModule)
    {
        if (pModule.IsEmpty()) { return; }

        Vector3 pos = pModule.GetPosition() + (Vector3)pModule.GetSize() / 2 - WFCParameters.WORLD_OFFSET;
        GameObject gameObject = GameObject.Instantiate(pModule.SelectSpawnable(), pos, Quaternion.Euler(0, 90 * pModule.GetRotation(), 0), m_modulesContainer);
        Vector3 scale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(pModule.IsFlipped() ? -scale.x : scale.x, scale.y, scale.z);
        gameObject.name = pModule.GetName();

        foreach (string childName in m_materialDict.Keys)
        {
            Transform part = gameObject.transform.Find(childName);
            if (part != null)
            {
                part.GetComponent<MeshRenderer>().material = m_materialDict[childName];
            }
        }

        pos = new Vector3(pos.x, -pos.y, pos.z);
        GameObject reflectionGameObject = GameObject.Instantiate(gameObject, pos, Quaternion.Euler(0, 90 * pModule.GetRotation(), 0), m_reflectionsContainer);
        reflectionGameObject.transform.localScale = new Vector3(pModule.IsFlipped() ? -scale.x : scale.x, -scale.y, scale.z);
        reflectionGameObject.name = pModule.GetName();

        m_spawnedModules.Add(gameObject);
        m_spawnedReflections.Add(reflectionGameObject);
    }

    /*private void BuildTopology()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        int nbVertices = 0;
        foreach (GameObject module in m_spawnedModules)
        {
            foreach (Transform part in module.transform)
            {
                if (m_excludeFromTopology.Contains(part.gameObject.name)) { continue; }

                Mesh mesh = part.GetComponent<MeshFilter>().mesh;
                
                m_topology.Add(mesh, part, module.transform.localScale);
            }
        }
        
        m_topologyMesh = m_topology.GetMesh();
    }*/

    /*private void BuildCollider()
    {
        GameObject go = new GameObject("Collider");
        go.transform.parent = transform;
        go.layer = 3;

        MeshCollider meshCollider = go.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = m_topologyMesh;
    }*/

    /*private void BuildOutline()
    {
        GameObject outline = new GameObject("Outline");
        outline.transform.parent = transform;
        outline.AddComponent<MeshFilter>();
        outline.AddComponent<MeshRenderer>();
        outline.GetComponent<MeshFilter>().mesh = m_topologyMesh;
        outline.GetComponent<MeshRenderer>().material = m_outlineMaterial;
    }*/

    private void BuildGrassShadow()
    {
        int tex_width = m_mapSize.x * m_pixelsPerTile;
        int tex_height = m_mapSize.z * m_pixelsPerTile;
        List<Vector2Int> trianglesToDraw = new List<Vector2Int>();

        foreach (GameObject module in m_spawnedModules)
        {
            Transform grass = module.transform.Find("Grass");
            if (grass == null) { continue; }

            Mesh mesh = grass.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (WFCParameters.WORLD_OFFSET + grass.TransformPoint(vertices[i])) * m_pixelsPerTile;
                uv[i] = new Vector2(vertices[i].x / tex_width, vertices[i].z / tex_height);
            }
            mesh.uv = uv;

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                trianglesToDraw.Add(new Vector2Int((int)vertices[triangles[i]].x, (int)vertices[triangles[i]].z));
                trianglesToDraw.Add(new Vector2Int((int)vertices[triangles[i + 1]].x, (int)vertices[triangles[i + 1]].z));
                trianglesToDraw.Add(new Vector2Int((int)vertices[triangles[i + 2]].x, (int)vertices[triangles[i + 2]].z));
            }
        }

        m_grassShadowComputeShader.SetVector("MapSize", new Vector2(tex_width, tex_height));


        int kernel = m_grassShadowComputeShader.FindKernel("InitSeeds");
        //ComputeBuffer seedsBuffer = new ComputeBuffer(tex_width * tex_height, sizeof(int) * 2);
        //m_grassShadowComputeShader.SetBuffer(kernel, "Seeds", seedsBuffer);
        RenderTexture texture = new RenderTexture(tex_width, tex_height, 32, RenderTextureFormat.ARGBFloat);
        texture.enableRandomWrite = true;
        m_grassShadowComputeShader.SetTexture(kernel, "Result", texture);

        m_grassShadowComputeShader.Dispatch(kernel, tex_width, tex_height, 1);

        kernel = m_grassShadowComputeShader.FindKernel("DrawTriangles");

        Vector2Int[] trianglesArray = trianglesToDraw.ToArray();
        ComputeBuffer trianglesBuffer = new ComputeBuffer(trianglesArray.Length, sizeof(int) * 2);
        trianglesBuffer.SetData(trianglesArray);
        m_grassShadowComputeShader.SetBuffer(kernel, "Triangles", trianglesBuffer);

        //m_grassShadowComputeShader.SetBuffer(kernel, "Seeds", seedsBuffer);
        m_grassShadowComputeShader.SetTexture(kernel, "Result", texture);

        m_grassShadowComputeShader.Dispatch(kernel, trianglesArray.Length / 3, 1, 1);
        trianglesBuffer.Release();

        kernel = m_grassShadowComputeShader.FindKernel("JumpFloodingStep");
        //m_grassShadowComputeShader.SetBuffer(kernel, "Seeds", seedsBuffer);
        m_grassShadowComputeShader.SetTexture(kernel, "Result", texture);

        int start_step = (int)System.Math.Pow(System.Math.Ceiling(System.Math.Log(System.Math.Max(tex_width, tex_height), 2)) - 1, 2);
        for (int step = start_step; step > 0; step /= 2)
        {
            m_grassShadowComputeShader.SetInt("StepSize", step);
            m_grassShadowComputeShader.Dispatch(kernel, tex_width, tex_height, 1);
        }

        kernel = m_grassShadowComputeShader.FindKernel("EncodeDistance");
        m_grassShadowComputeShader.SetTexture(kernel, "Result", texture);
        //m_grassShadowComputeShader.SetBuffer(kernel, "Seeds", seedsBuffer);


        m_grassShadowComputeShader.Dispatch(kernel, tex_width, tex_height, 1);
        //seedsBuffer.Release();

        Texture2D tex2D = RenderTextureToTexture2D(texture);

        m_grassMaterial.SetTexture("_Shadow_Texture", texture);
        m_grassMaterial.SetFloat("_Denormalize_Scale", m_mapSize.x + m_mapSize.z);
        m_grassMaterial.SetFloat("_Pixels_Per_Tile", m_pixelsPerTile);
        SaveTexture(tex2D, Application.dataPath + "/plop" + ".png");
    }

    private static Texture2D RenderTextureToTexture2D(RenderTexture rt)
    {
        RenderTexture oldRT = RenderTexture.active;

        Texture2D tex = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = oldRT;
        return tex;
    }

    private static void SaveTexture(Texture2D tex, string filepath)
    {
        System.IO.File.WriteAllBytes(filepath, tex.EncodeToPNG());
    }

    // Update is called once per frame
    void Update()
    {

    }
}
