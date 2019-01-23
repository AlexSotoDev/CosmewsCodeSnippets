using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARGrid : MonoBehaviour
{
    [Header("Grid dimensions")]
    [SerializeField]
    [Tooltip("Z size in world space")]
    public int length;
    [SerializeField]
    [Tooltip("X size in world space")]
    public int width;
    [SerializeField]
    [Tooltip("Y size in world space")]
    public int height;

    //[SerializeField]
    //float gridCellSize;
    //[SerializeField]
    //float unitSize;

    [Header("Physical Space Dimensions(meters)")]
    [SerializeField]
    float realWorldLength = 2f;
    [SerializeField]
    float realWorldWidth = 2f;
    [SerializeField]
    float realWorldHeight = 2.5f;

    [SerializeField]
    GameObject gridNodePrefab;

    [SerializeField]
    GameObject gridParent;

    [SerializeField]
    GameObject gridParentPrefab;

    [SerializeField]
    public GameObject[,,] grid;

    //public GameObject[,,] Grid
    //{
    //    get
    //    {
    //        return grid;
    //    }
    //}


    // Use this for initialization
    void Awake()
    {
        CreateGrid(width, height, length);
    }

    public void CreateGrid(int x, int y, int z)
    {
        DeleteGrid();

        //if (gridParent == null)
        //{
        gridParent = Instantiate(gridParentPrefab, this.transform);
        //}

        grid = new GameObject[x, y, z];

        float xUnitSize = realWorldWidth / (x - 1);
        float yUnitSize = realWorldHeight / (y - 1);
        float zUnitSize = realWorldLength / (z - 1);

        float naturalXOffset = ((x - 1) * xUnitSize * .5f);
        float naturalYOffset = yUnitSize * .5f;
        float naturalZOffset = ((z - 1) * zUnitSize * .5f);

        //Debug.Log("Grid length: " + grid.Length);
        //Debug.Log("UnitSize: " + xUnitSize);

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                for (int k = 0; k < z; k++)
                {
                    grid[i, j, k] = Instantiate(gridNodePrefab, gridParent.transform.position, Quaternion.identity, gridParent.transform);
                    grid[i, j, k].transform.localPosition = new Vector3(i * xUnitSize - naturalXOffset, j * yUnitSize + naturalYOffset, k * zUnitSize - naturalZOffset);
                    grid[i, j, k].transform.localScale = new Vector3(xUnitSize, yUnitSize, zUnitSize);
                    grid[i, j, k].GetComponent<GridCell>().SetGridPos(i, j, k);
                }
            }
        }
    }

    public void DeleteGrid()
    {
        if (gridParent != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(gridParent.gameObject);
#else
            Destroy(gridParent.gameObject);
#endif

        }

    }
}



