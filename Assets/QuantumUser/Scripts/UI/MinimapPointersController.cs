using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Quantum;

public unsafe class MinimapPointersController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera minimapCamera;       // your top-down minimap camera
    [SerializeField] private RectTransform mapArea;      // the UI RectTransform of the minimap image
    [SerializeField] private GameObject dotPrefab;       // small dot for on-map (has a SpriteRenderer)
    [SerializeField] private GameObject arrowPrefab;     // arrow for off-map (has a SpriteRenderer)
    [SerializeField] private Sprite[] classDotIcons;     // indexed by CharacterClass

    [Header("Settings")]
    [SerializeField] private float edgeBuffer = 10f;     // padding from rect edge

    // Internal pools
    private Dictionary<EntityRef, GameObject> _dots = new();
    private Dictionary<EntityRef, GameObject> _arrows = new();

    void Update()
    {
        if (minimapCamera == null)
        {
            var cameraObj = GameObject.FindGameObjectWithTag("CameraMinimap");
            if (cameraObj != null)
                minimapCamera = cameraObj.GetComponent<Camera>();
            return;
        }

        var frame = QuantumRunner.Default?.Game?.Frames?.Verified;
        if (frame == null) return;

        var used = new HashSet<EntityRef>();

        foreach (var block in frame.Unsafe.GetComponentBlockIterator<PlayerLink>())
        {
            var entity = block.Entity;
            var linkComp = block.Component;
            int classIndex = (int)linkComp->Class;

            // world pos > viewport
            var worldPos = frame.Get<Transform3D>(entity).Position.ToUnityVector3();
            Vector3 viewport = minimapCamera.WorldToViewportPoint(worldPos);
            bool onMap = viewport.z > 0
                         && viewport.x is >= 0 and <= 1
                         && viewport.y is >= 0 and <= 1;

            if (onMap)
            {
                // > Dot
                var dot = GetOrCreate(entity, _dots, dotPrefab);
                dot.SetActive(true);

                // hide arrow
                if (_arrows.TryGetValue(entity, out var existingArrow))
                    existingArrow.SetActive(false);

                // position
                Vector2 localPt = ViewportToLocalPosition(viewport);
                dot.GetComponent<RectTransform>().anchoredPosition = localPt;

                // style: icon + color
                ApplyDotStyle(dot, classIndex);
            }
            else
            {
                // > Arrow
                var arrow = GetOrCreate(entity, _arrows, arrowPrefab);
                arrow.SetActive(true);

                // hide dot
                if (_dots.TryGetValue(entity, out var existingDot))
                    existingDot.SetActive(false);

                // position & clamp
                Vector2 vpClamped = new Vector2(
                    Mathf.Clamp(viewport.x, 0f, 1f),
                    Mathf.Clamp(viewport.y, 0f, 1f)
                );
                Vector2 localPt = ViewportToLocalPosition(new Vector3(vpClamped.x, vpClamped.y, viewport.z));
                localPt = ClampToRectEdge(localPt, mapArea.rect, edgeBuffer);
                arrow.GetComponent<RectTransform>().anchoredPosition = localPt;

                // rotation
                Vector2 dir = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                arrow.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, angle);

                // style: color only
                ApplyArrowStyle(arrow, classIndex);
            }

            used.Add(entity);
        }

        CleanupUnused(_dots, used);
        CleanupUnused(_arrows, used);
    }

    GameObject GetOrCreate(EntityRef key, Dictionary<EntityRef, GameObject> pool, GameObject prefab)
    {
        if (!pool.TryGetValue(key, out var go) || go == null)
        {
            go = Instantiate(prefab, mapArea);
            pool[key] = go;
        }
        return go;
    }

    void CleanupUnused(Dictionary<EntityRef, GameObject> pool, HashSet<EntityRef> used)
    {
        var toRemove = new List<EntityRef>();
        foreach (var kv in pool)
        {
            if (!used.Contains(kv.Key))
            {
                if (kv.Value) Destroy(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var k in toRemove) pool.Remove(k);
    }

    Vector2 ViewportToLocalPosition(Vector3 viewport)
    {
        var r = mapArea.rect;
        float x = (viewport.x - 0.5f) * r.width;
        float y = (viewport.y - 0.5f) * r.height;
        return new Vector2(x, y);
    }

    Vector2 ClampToRectEdge(Vector2 point, Rect rect, float buffer)
    {
        float halfW = rect.width / 2 - buffer;
        float halfH = rect.height / 2 - buffer;
        float x = Mathf.Clamp(point.x, -halfW, halfW);
        float y = Mathf.Clamp(point.y, -halfH, halfH);
        return new Vector2(x, y);
    }

    // Styling Helpers
    void ApplyDotStyle(GameObject dot, int classIndex)
    {
        var sr = dot.transform.GetChild(0).GetComponentInChildren<Image>();
        if (sr != null)
        {
            var icons = classDotIcons;
            var colors = QuantumRunner.Default?.Game?.Configurations.Runtime.ClassColors;
            if (icons != null && icons.Length > classIndex)
                sr.sprite = icons[classIndex];
            if (colors != null && colors.Length > classIndex)
                sr.color = colors[classIndex];
        }
    }

    void ApplyArrowStyle(GameObject arrow, int classIndex)
    {
        var colors = QuantumRunner.Default?.Game?.Configurations.Runtime.ClassColors;
        var sr = arrow.GetComponentInChildren<Image>();
        if (sr != null && colors.Length > classIndex)
        {
            sr.color = colors[classIndex];
        }
    }
}
