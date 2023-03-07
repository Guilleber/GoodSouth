using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderPipelineConfigurator : MonoBehaviour
{
    private static RenderPipelineConfigurator instance;

    public static RenderPipelineConfigurator Instance {
        get {
            if (instance == null) { throw new System.Exception("At least one instance of RenderPipelineConfigurator is required."); }
            return instance;
        }
        private set { instance = value; }
    }

    [Space(10)]
    [Header("Outline")]
    [Space(3)]
    public Material outlineMaterial;
    [Range(0, 10)]
    public int outlineWidth;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance != null) { throw new System.Exception("Can you please stop adding multiple instances of RenderPipelineConfigurator?"); }

        instance = this;
    }
}
