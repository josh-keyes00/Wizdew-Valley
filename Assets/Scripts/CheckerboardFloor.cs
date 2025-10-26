using UnityEngine;

[DisallowMultipleComponent]
public class CheckerboardFloor : MonoBehaviour
{
    [Header("Size (tiles from center)")]
    public int halfWidth = 20;
    public int halfHeight = 20;

    [Header("Tile")]
    public float tileSize = 1f;
    public Color colorA = new Color(0.14f, 0.16f, 0.22f); // dark bluish
    public Color colorB = new Color(0.20f, 0.22f, 0.28f); // slightly lighter
    public int sortingOrder = -50; // stay behind the player

    private Sprite _sprite;

    private void Awake()
    {
        Build();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ClearChildren();
            Build();
        }
    }
#endif

    private void Build()
    {
        if (_sprite == null) _sprite = MakeUnitSprite();

        for (int y = -halfHeight; y <= halfHeight; y++)
        {
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                var go = new GameObject($"T_{x}_{y}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0f);
                go.transform.localScale = Vector3.one * tileSize;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.sortingOrder = sortingOrder;
                sr.color = ((x + y) & 1) == 0 ? colorA : colorB;
            }
        }
    }

    private Sprite MakeUnitSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }
}
