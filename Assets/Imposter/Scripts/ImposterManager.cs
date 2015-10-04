using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ImposterManager : MonoBehaviour
{
    public enum CachingBehaviour { discardInvisibleImpostors, cacheInvisibleIposters, preloadAndCacheInvisibleImposters }

    public static ImposterManager instance;

    public bool active = true;
    public int imposterLayer = 30;
    public bool useMainCamera = true;
    public Camera mainCamera;
    public int antialiasing = 2;

    public bool castShadow = false;
    public Light mainLight;

    public CachingBehaviour cachingBehaviour;
    public float preLoadFovFactor = 2.0f;
    public int preloadRate = 5;

    public List<Imposter> imposters;
    public List<ImposterTexture> imposterTextures = new List<ImposterTexture>();
    public List<ImposterTexture> freeImposterTextures = new List<ImposterTexture>();
    public float textureMemory = 0.0f;

    public ImposterProxy proxyPrefab;
    public Camera imposterRenderingCamera;

    private Plane[] cameraPlanes, cachingCameraPlanes;
    private bool lastCastShadow;
    private List<Imposter> invalidImposters = new List<Imposter>();
    private Camera cachingCamera;
    private int preloadCounter = 0;

    void Awake()
    {
        instance = this;
        lastCastShadow = castShadow;

        if (useMainCamera)
        {
            mainCamera = Camera.main;
        }
    }

	// Use this for initialization
	void Start () 
    {
        SetupCachingCamera();
	}

    void SetupCachingCamera()
    {
        GameObject go = new GameObject("Imposter Caching Camera");
        cachingCamera = go.AddComponent<Camera>();

        cachingCamera.orthographic = ImposterManager.instance.mainCamera.orthographic;
        cachingCamera.orthographicSize = ImposterManager.instance.mainCamera.orthographicSize;
        cachingCamera.fieldOfView = ImposterManager.instance.mainCamera.fieldOfView * preLoadFovFactor;
        cachingCamera.nearClipPlane = ImposterManager.instance.mainCamera.nearClipPlane;
        cachingCamera.farClipPlane = ImposterManager.instance.mainCamera.farClipPlane;
        cachingCamera.transform.position = ImposterManager.instance.mainCamera.transform.position;
        cachingCamera.transform.rotation = ImposterManager.instance.mainCamera.transform.rotation;
        cachingCamera.enabled = false;

        go.transform.SetParent(ImposterManager.instance.mainCamera.transform);
    }

    void LateUpdate()
    {
        preloadCounter = 0;
        cameraPlanes = GeometryUtility.CalculateFrustumPlanes(ImposterManager.instance.mainCamera);
        cachingCameraPlanes = GeometryUtility.CalculateFrustumPlanes(cachingCamera);
        castShadow = castShadow && mainLight != null && mainLight.enabled && mainLight.type == LightType.Directional && mainLight.shadows != LightShadows.None;

        //Update Imposters
        foreach (Imposter imposter in imposters)
        {
            if (imposter != null)
            {
                imposter.UpdateImposter();
            }
            else
            {
                invalidImposters.Add(imposter);
            }
        }

        //Delete invalid Imposters
        if (invalidImposters.Count > 0)
        {
            for (int i = 0; i < invalidImposters.Count; i++)
            {
                imposters.Remove(invalidImposters[i]);
            }

            invalidImposters.Clear();
        }
    }

    public bool isVisible(Bounds bound)
    {
        return GeometryUtility.TestPlanesAABB(cameraPlanes, bound);
    }

    public bool isVisibleInCache(Bounds bound)
    {
        return GeometryUtility.TestPlanesAABB(cachingCameraPlanes, bound);
    }

    public ImposterTexture getRenderTexture(ImposterProxy newOwner, int size)
    {
        //Reuse Texture?
        for(int i=0; i<freeImposterTextures.Count; i++)
        {
            ImposterTexture texture = freeImposterTextures[i];

            if (texture.size == size)
            {
                freeImposterTextures.Remove(texture);
                texture.owner = newOwner;

                return texture;
            }
        }

        //Create new Texture
        RenderTexture renderTexture = new RenderTexture(size, size, 16);
        renderTexture.antiAliasing = antialiasing;

        ImposterTexture imposterTexture = new ImposterTexture();
        imposterTexture.owner = newOwner;
        imposterTexture.size = size;
        imposterTexture.createdTime = imposterTexture.lastUsedTime = Time.frameCount;
        imposterTexture.texture = renderTexture;
        imposterTextures.Add(imposterTexture);

        textureMemory += imposterTexture.getMemoryAmount();

        return imposterTexture;
    }

    public void giveBackRenderTexture(ImposterTexture imposterTexture)
    {
        if (imposterTexture.texture == null)
        {
            Debug.LogError("Texture given back is empty!");
            return;
        }

        freeImposterTextures.Add(imposterTexture);
    }

    public void garbageCollect()
    {
        foreach (ImposterTexture imposterTexture in freeImposterTextures)
        {
            imposterTextures.Remove(imposterTexture);
            textureMemory -= imposterTexture.getMemoryAmount();
            Destroy(imposterTexture.texture);
        }

        freeImposterTextures.Clear();
    }

    public bool getPreloadLock(Imposter imposter)
    {
        if (preloadCounter++ < preloadRate) return true;
        else return false;
    }

    void Clear()
    {
        foreach(Imposter imposter in imposters)
        {
            if (imposter != null)
            {
                Destroy(imposter.gameObject);
            }
        }

        foreach (ImposterTexture imposterTexture in imposterTextures)
        {
            textureMemory -= imposterTexture.getMemoryAmount();
            Destroy(imposterTexture.texture);
        }

        imposters.Clear();
        imposterTextures.Clear();
        freeImposterTextures.Clear();
    }
}
