using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshRenderPipeline : RenderPipeline
{
    public bool m_hasOutline = true;
    public bool m_hasReflection = true;

    private MeshConstruct m_meshConstruct;
    private GameObject m_reflection;
    private GameObject m_outline;

    protected override void Init(bool isFlipped = false)
    {
        base.Init(isFlipped);
        BuildMeshConstruct(isFlipped);
    }

    protected override void PostProcess()
    {
        if (m_hasOutline) { SpawnOutline(); }

        if (m_hasReflection) { SpawnReflection(); }
    }

    protected virtual void BuildMeshConstruct(bool isFlipped)
    {
        m_meshConstruct = MeshConstruct.FromMesh(GetComponent<MeshFilter>().mesh, transform, isFlipped);
    }

    protected virtual void SpawnOutline()
    {
        Mesh outlineMesh = m_meshConstruct.GetSmoothMesh();
        m_outline = new GameObject("Outline");
        m_outline.transform.parent = transform;
        m_outline.AddComponent<MeshFilter>();
        m_outline.AddComponent<MeshRenderer>();
        m_outline.GetComponent<MeshFilter>().mesh = outlineMesh;
        m_outline.GetComponent<MeshRenderer>().material = m_config.outlineMaterial;
    }

    protected virtual void SpawnReflection()
    {
        Mesh reflectionMesh = m_meshConstruct.GetFlatMesh();
        m_reflection = new GameObject("Reflection");
        Vector3 pos = transform.position;
        pos = new Vector3(pos.x, -pos.y, pos.z);
        m_reflection.transform.localScale = new Vector3(1.0f, -1.0f, 1.0f);
        m_reflection.transform.parent = transform;
        m_reflection.AddComponent<MeshFilter>();
        m_reflection.AddComponent<MeshRenderer>();
        m_reflection.GetComponent<MeshFilter>().mesh = reflectionMesh;
    }
}
