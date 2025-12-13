using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class MaterialController : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool autoFindOnStart = true;
    [SerializeField] private bool useSharedMaterial = false;

    [Header("Filter Settings")]
    [SerializeField] private bool enableShaderFilter = false;
    [SerializeField] private List<string> shaderNameFilters = new List<string>();

    [SerializeField] private bool logOperations = false;

    public event UnityAction<string, float> _floatChanged;
    public event UnityAction<string, int> _intChanged;
    public event UnityAction<string, Color> _colorChanged;

    private List<MaterialData> cachedMaterials = new List<MaterialData>();

    public int MaterialCount => cachedMaterials.Count;
    public bool UseSharedMaterial => useSharedMaterial;

    private class MaterialData
    {
        public Renderer renderer;
        public Material material;
        public int materialIndex;
        public string shaderName;

        public MaterialData(Renderer renderer, Material material, int index)
        {
            this.renderer = renderer;
            this.material = material;
            this.materialIndex = index;
            this.shaderName = material.shader.name;
        }
    }

    #region Initialization

    private void Start()
    {
        if (autoFindOnStart)
        {
            RefreshMaterials();
        }
    }

    [ContextMenu("Refresh Materials")]
    public void RefreshMaterials()
    {
        CleanupMaterials();

        Renderer[] renderers = includeChildren
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();

        foreach (var renderer in renderers)
        {
            Material[] materials = useSharedMaterial
                ? renderer.sharedMaterials
                : renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                if (enableShaderFilter && !PassesShaderFilter(material))
                    continue;

                cachedMaterials.Add(new MaterialData(renderer, material, i));
            }
        }

        if (logOperations)
            Debug.Log($"🎨 MaterialController: Found {cachedMaterials.Count} materials");
    }

    private bool PassesShaderFilter(Material material)
    {
        if (shaderNameFilters.Count == 0)
            return true;

        string shaderName = material.shader.name;
        return shaderNameFilters.Any(filter => shaderName.Contains(filter));
    }

    #endregion

    #region Set Methods

    public void SetFloat(string propertyName, float value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                data.material.SetFloat(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"✏️ SetFloat: {propertyName} = {value} (applied to {count} materials)");

        _floatChanged?.Invoke(propertyName, value);
    }

    public void SetInt(string propertyName, int value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                data.material.SetInt(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"✏️ SetInt: {propertyName} = {value} (applied to {count} materials)");

        _intChanged?.Invoke(propertyName, value);
    }

    public void SetBool(string propertyName, bool value)
    {
        SetFloat(propertyName, value ? 1f : 0f);
    }

    public void SetColor(string propertyName, Color value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                data.material.SetColor(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"🎨 SetColor: {propertyName} = {value} (applied to {count} materials)");

        _colorChanged?.Invoke(propertyName, value);
    }

    public void SetVector(string propertyName, Vector4 value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                data.material.SetVector(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"📐 SetVector: {propertyName} = {value} (applied to {count} materials)");
    }

    public void SetTexture(string propertyName, Texture value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                data.material.SetTexture(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"🖼️ SetTexture: {propertyName} = {value?.name} (applied to {count} materials)");
    }

    #endregion

    #region Shader-Specific Set Methods

    public void SetFloatForShader(string shaderFilter, string propertyName, float value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null &&
                data.shaderName.Contains(shaderFilter) &&
                data.material.HasProperty(propertyName))
            {
                data.material.SetFloat(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"✏️ SetFloatForShader [{shaderFilter}]: {propertyName} = {value} (applied to {count} materials)");
    }

    public void SetIntForShader(string shaderFilter, string propertyName, int value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null &&
                data.shaderName.Contains(shaderFilter) &&
                data.material.HasProperty(propertyName))
            {
                data.material.SetInt(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"✏️ SetIntForShader [{shaderFilter}]: {propertyName} = {value} (applied to {count} materials)");
    }

    public void SetBoolForShader(string shaderFilter, string propertyName, bool value)
    {
        SetFloatForShader(shaderFilter, propertyName, value ? 1f : 0f);
    }

    public void SetColorForShader(string shaderFilter, string propertyName, Color value)
    {
        int count = 0;

        foreach (var data in cachedMaterials)
        {
            if (data.material != null &&
                data.shaderName.Contains(shaderFilter) &&
                data.material.HasProperty(propertyName))
            {
                data.material.SetColor(propertyName, value);
                count++;
            }
        }

        if (logOperations)
            Debug.Log($"🎨 SetColorForShader [{shaderFilter}]: {propertyName} = {value} (applied to {count} materials)");
    }

    #endregion

    #region Get Methods

    public float GetFloat(string propertyName, float defaultValue = 0f)
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                return data.material.GetFloat(propertyName);
            }
        }

        Debug.LogWarning($"⚠️ Property '{propertyName}' not found in any material");
        return defaultValue;
    }

    public int GetInt(string propertyName, int defaultValue = 0)
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                return data.material.GetInt(propertyName);
            }
        }

        Debug.LogWarning($"⚠️ Property '{propertyName}' not found in any material");
        return defaultValue;
    }

    public bool GetBool(string propertyName, bool defaultValue = false)
    {
        return GetFloat(propertyName, defaultValue ? 1f : 0f) > 0.5f;
    }

    public Color GetColor(string propertyName, Color? defaultValue = null)
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material != null && data.material.HasProperty(propertyName))
            {
                return data.material.GetColor(propertyName);
            }
        }

        Debug.LogWarning($"⚠️ Property '{propertyName}' not found in any material");
        return defaultValue ?? Color.white;
    }

    #endregion

    #region Utility Methods

    public bool HasProperty(string propertyName)
    {
        return cachedMaterials.Any(data =>
            data.material != null && data.material.HasProperty(propertyName));
    }

    public int GetMaterialCountByShader(string shaderFilter)
    {
        return cachedMaterials.Count(data =>
            data.material != null && data.shaderName.Contains(shaderFilter));
    }

    [ContextMenu("List All Shaders")]
    public void ListAllShaders()
    {
        var shaders = cachedMaterials
            .Where(data => data.material != null)
            .Select(data => data.shaderName)
            .Distinct()
            .OrderBy(name => name);

        Debug.Log("📋 Shaders in use:\n" + string.Join("\n", shaders));
    }

    #endregion

    #region Cleanup

    private void CleanupMaterials()
    {
        if (Application.isPlaying && !useSharedMaterial)
        {
            foreach (var data in cachedMaterials)
            {
                if (data.material != null)
                {
                    Destroy(data.material);
                }
            }
        }

        cachedMaterials.Clear();
    }

    private void OnDestroy()
    {
        CleanupMaterials();
    }

    #endregion

    #region Test Methods

    [ContextMenu("Test - Toggle Outline")]
    private void TestToggleOutline()
    {
        bool current = GetBool("_UseOutline", false);
        SetBool("_UseOutline", !current);
    }

    [ContextMenu("Test - Set Outline Width")]
    private void TestSetOutlineWidth()
    {
        SetFloat("_OutlineWidth", 0.2f);
    }

    #endregion
}