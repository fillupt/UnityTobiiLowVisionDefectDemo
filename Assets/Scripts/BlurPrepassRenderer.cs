using UnityEngine;

public class BlurPrepassRenderer : MonoBehaviour
{
    private const string KawaseShaderName = "Hidden/KawaseBlur";

    private Material kawaseMaterial;
    private RenderTexture pingRT;
    private RenderTexture pongRT;

    private void Awake()
    {
        Shader kawaseShader = Shader.Find(KawaseShaderName);
        if (kawaseShader != null)
        {
            kawaseMaterial = new Material(kawaseShader);
        }
        else
        {
            Debug.LogError("Kawase blur shader was not found: Hidden/KawaseBlur");
        }
    }

    public void ApplyBlur(Material targetMaterial, Texture sourceTexture, float blurAmount, float blurRadius, int passes)
    {
        if (targetMaterial == null)
        {
            return;
        }

        if (!targetMaterial.HasProperty("_BlurAmount") || !targetMaterial.HasProperty("_BlurTex") || !targetMaterial.HasProperty("_BlurRadius"))
        {
            return;
        }

        blurAmount = Mathf.Clamp01(blurAmount);
        passes = Mathf.Clamp(passes, 0, 8);
        blurRadius = Mathf.Max(0.001f, blurRadius);

        if (kawaseMaterial == null || sourceTexture == null || blurAmount <= 0f || passes <= 0)
        {
            ClearBlur(targetMaterial);
            return;
        }

        EnsureRenderTextures(sourceTexture.width, sourceTexture.height);
        if (pingRT == null || pongRT == null)
        {
            ClearBlur(targetMaterial);
            return;
        }

        Graphics.Blit(sourceTexture, pingRT);

        for (int i = 0; i < passes; i++)
        {
            kawaseMaterial.SetFloat("_Offset", i + 1f);
            Graphics.Blit(pingRT, pongRT, kawaseMaterial);
            SwapRenderTargets();
        }

        targetMaterial.SetTexture("_BlurTex", pingRT);
        targetMaterial.SetFloat("_BlurAmount", blurAmount);
        targetMaterial.SetFloat("_BlurRadius", blurRadius);
    }

    public void ClearBlur(Material targetMaterial)
    {
        if (targetMaterial == null)
        {
            return;
        }

        if (targetMaterial.HasProperty("_BlurAmount"))
        {
            targetMaterial.SetFloat("_BlurAmount", 0f);
        }
    }

    private void EnsureRenderTextures(int width, int height)
    {
        if (pingRT != null && pongRT != null && pingRT.width == width && pingRT.height == height)
        {
            return;
        }

        ReleaseRenderTextures();

        pingRT = CreateRenderTexture(width, height);
        pongRT = CreateRenderTexture(width, height);
    }

    private static RenderTexture CreateRenderTexture(int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }

    private void SwapRenderTargets()
    {
        RenderTexture temp = pingRT;
        pingRT = pongRT;
        pongRT = temp;
    }

    private void OnDestroy()
    {
        ReleaseRenderTextures();

        if (kawaseMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(kawaseMaterial);
            }
            else
            {
                DestroyImmediate(kawaseMaterial);
            }
        }
    }

    private void ReleaseRenderTextures()
    {
        if (pingRT != null)
        {
            pingRT.Release();
            DestroyRenderTexture(pingRT);
            pingRT = null;
        }

        if (pongRT != null)
        {
            pongRT.Release();
            DestroyRenderTexture(pongRT);
            pongRT = null;
        }
    }

    private static void DestroyRenderTexture(RenderTexture rt)
    {
        if (Application.isPlaying)
        {
            Destroy(rt);
        }
        else
        {
            DestroyImmediate(rt);
        }
    }
}
