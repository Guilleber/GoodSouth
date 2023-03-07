using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFCModule)), CanEditMultipleObjects]
public class WFCModuleEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        WFCModule module = (WFCModule)target;
        if (Application.isPlaying && !module.m_isEmpty)
        {
            Transform transform = module.transform;

            foreach (Vector3Int key in module.m_subModules.Keys)
            {
                module.m_subModules[key].OnSceneGUI(module.m_offsetToCenter, transform);
            }
        }
    }
}