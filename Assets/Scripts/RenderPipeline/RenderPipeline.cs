using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RenderPipeline : MonoBehaviour
{
    protected RenderPipelineConfigurator m_config;

    public void OnSpawn()
    {
        Vector3 scale = transform.localScale;

        foreach (RenderPipeline rp in GetComponentsInChildren(typeof(RenderPipeline))) { rp.Init(scale.x * scale.y * scale.z < 0); }
    }

    protected virtual void Init(bool isFlipped = false)
    {
        m_config = RenderPipelineConfigurator.Instance;
    }

    public virtual void AfterAllSpawn()
    {
        foreach (RenderPipeline rp in GetComponentsInChildren(typeof(RenderPipeline))) { rp.PostProcess(); }
    }

    protected virtual void PostProcess() { }
}
