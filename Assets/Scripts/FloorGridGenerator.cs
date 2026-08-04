using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloorGridGenerator : MonoBehaviour
{
    [Header("地板预制体")]
    [Tooltip("把做好的单块地板 Prefab 拖到这里")]
    [SerializeField] private GameObject tilePrefab;

    [Header("网格大小")]
    [Min(1)]
    [SerializeField] private int columns = 5;

    [Min(1)]
    [SerializeField] private int rows = 5;

    [Header("单块地板间距")]
    [Min(0.01f)]
    [SerializeField] private float tileSizeX = 2f;

    [Min(0.01f)]
    [SerializeField] private float tileSizeZ = 2f;

    [Header("整体位置")]
    [SerializeField] private float localHeight = 0.01f;

    [Tooltip("勾选后，整片地板会以 FloorGrid 对象的原点为中心")]
    [SerializeField] private bool centerGrid = true;

    [ContextMenu("生成地板")]
    public void GenerateFloor()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("FloorGridGenerator：请先把单块地板 Prefab 拖到 Tile Prefab 槽位。", this);
            return;
        }

        ClearFloor();

        float startX = centerGrid
            ? -((columns - 1) * tileSizeX) / 2f
            : 0f;

        float startZ = centerGrid
            ? -((rows - 1) * tileSizeZ) / 2f
            : 0f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                GameObject tile = CreateTileInstance();

                if (tile == null)
                {
                    continue;
                }

                tile.name = $"FloorTile_{row + 1:D2}_{column + 1:D2}";
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = new Vector3(
                    startX + column * tileSizeX,
                    localHeight,
                    startZ + row * tileSizeZ
                );
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one;
            }
        }

        Debug.Log(
            $"FloorGridGenerator：已生成 {columns} × {rows}，共 {columns * rows} 块地板。",
            this
        );
    }

    [ContextMenu("清空地板")]
    public void ClearFloor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(child);
            }
            else
            {
                Destroy(child);
            }
#else
            Destroy(child);
#endif
        }
    }

    private GameObject CreateTileInstance()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                tilePrefab
            ) as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(
                    instance,
                    "Generate Floor Tile"
                );
            }

            return instance;
        }
#endif

        return Instantiate(tilePrefab);
    }
}
