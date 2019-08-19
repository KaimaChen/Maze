using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1.建立单元格矩阵，下标对应编号，元素值对应集合，值相同单元格代表属于同一集合，初始时所有值都不同
/// 2.建立两个向量，分别表示所有单元格的右上（也可是左下等等）墙壁，下标对应单元格，值表示墙是否被打通，初始时所有墙未被打通
/// 3.随机选择两个向量中的一堵未被打通的墙（非边缘），找到矩阵中此墙邻接的两单元格。判断元素值是否相等，相等则不打通，然后重复当前步骤；若元素值不等，则打通该墙壁，将矩阵中两集合的元素值统一，然后重复当前步骤
/// </summary>
public class MapManager : MonoBehaviour
{
    public int mWidth = 5;
    public int mHeight = 5;
    public int mMinRoom = 10;
    public Vector2Int mStart;
    public Vector2Int mEnd = new Vector2Int(4, 4);

    private int[,] mMatrix;
    private List<Vector2Int> mRightWall;
    private List<Vector2Int> mTopWall;

    void Start()
    {
        GenMap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mMatrix = null;
            mRightWall = mTopWall = null;
            GenMap();
        }
    }

    private void GenMap()
    {
        //检查
        if (mWidth <= 1 && mHeight <= 1)
        {
            Debug.LogError("地图宽度和长度都需要大于1");
            return;
        }

        if (mStart.x >= mWidth || mStart.y >= mHeight || mEnd.x >= mWidth || mEnd.y >= mHeight)
        {
            Debug.LogError("起点和终点超出范围");
            return;
        }

        //初始化
        int id = 0;
        mMatrix = new int[mWidth, mHeight];
        for (int x = 0; x < mMatrix.GetLength(0); x++)
            for (int y = 0; y < mMatrix.GetLength(1); y++)
                mMatrix[x, y] = id++;

        mRightWall = new List<Vector2Int>();
        for (int x = 0; x < mWidth - 1; x++) //最右边的墙壁不存在
            for (int y = 0; y < mHeight; y++)
                mRightWall.Add(new Vector2Int(x, y));

        mTopWall = new List<Vector2Int>();
        for (int x = 0; x < mWidth; x++)
            for (int y = 0; y < mHeight - 1; y++) //最上边的墙壁不存在
                mTopWall.Add(new Vector2Int(x, y));

        //随机打穿墙壁
        while (mMatrix[mStart.x, mStart.y] != mMatrix[mEnd.x, mEnd.y] || CalcConnectCount(mMatrix, mStart) < mMinRoom)
        {
            if (mTopWall.Count <= 0 && mRightWall.Count <= 0)
            {
                break;
            }

            bool isRight = (mTopWall.Count <= 0 || (mRightWall.Count > 0 && Random.Range(0, 2) == 0));

            int randomIndex = isRight ? Random.Range(0, mRightWall.Count) : Random.Range(0, mTopWall.Count);
            Vector2Int firstPos, secondPos;
            if (isRight)
            {
                firstPos = mRightWall[randomIndex];
                secondPos = firstPos + new Vector2Int(1, 0);
            }
            else
            {
                firstPos = mTopWall[randomIndex];
                secondPos = firstPos + new Vector2Int(0, 1);
            }

            int firstId = mMatrix[firstPos.x, firstPos.y];
            int secondId = mMatrix[secondPos.x, secondPos.y];
            if (firstId != secondId)
            {
                int minId = Mathf.Min(firstId, secondId);
                for (int x = 0; x < mMatrix.GetLength(0); x++)
                    for (int y = 0; y < mMatrix.GetLength(1); y++)
                        if (mMatrix[x, y] == firstId || mMatrix[x, y] == secondId)
                            mMatrix[x, y] = minId;

                if (isRight)
                    mRightWall.RemoveAt(randomIndex);
                else
                    mTopWall.RemoveAt(randomIndex);
            }
        }
    }

    private int CalcConnectCount(int[,] matrix, Vector2Int start)
    {
        int count = 0;
        for (int x = 0; x < matrix.GetLength(0); x++)
        {
            for (int y = 0; y < matrix.GetLength(1); y++)
            {
                if (matrix[x, y] == matrix[start.x, start.y])
                    count++;
            }
        }

        return count;
    }

    #region draw
    private void OnDrawGizmos()
    {
        if (mMatrix == null || mMatrix.GetLength(0) <= 0 || mMatrix.GetLength(1) <= 0)
            return;

        float size = 1;
        float halfSize = size / 2f;
        float bridgeSize = 1f;
        float halfBridgeSize = bridgeSize / 2f;
        float bridgeSmallSize = 0.2f;

        int[,] rightWallMat = new int[mWidth, mHeight];
        for (int i = 0; i < mRightWall.Count; i++)
        {
            Vector2Int pos = mRightWall[i];
            rightWallMat[pos.x, pos.y] = 1;
        }
        for (int y = 0; y < mHeight; y++)
            rightWallMat[mWidth - 1, y] = 1;

        int[,] topWallMat = new int[mWidth, mHeight];
        for (int i = 0; i < mTopWall.Count; i++)
        {
            Vector2Int pos = mTopWall[i];
            topWallMat[pos.x, pos.y] = 1;
        }
        for (int x = 0; x < mWidth; x++)
            topWallMat[x, mHeight - 1] = 1;

        int firstId = mMatrix[0, 0];
        for (int x = 0; x < mMatrix.GetLength(0); x++)
        {
            for (int y = 0; y < mMatrix.GetLength(1); y++)
            {
                int id = mMatrix[x, y];
                Gizmos.color = (id == firstId) ? Color.white : Color.black;
                Vector3 tilePos = new Vector3(x * (size + bridgeSize), y * (size + halfBridgeSize), 0);
                Gizmos.DrawCube(tilePos, new Vector3(size, size, size));

                Gizmos.color = Color.red;

                if (rightWallMat[x, y] == 0)
                {
                    Vector3 pos = tilePos + new Vector3(halfSize + halfBridgeSize, 0, 0);
                    Vector3 pathSize = new Vector3(bridgeSize, bridgeSmallSize, size);
                    Gizmos.DrawCube(pos, pathSize);
                }

                if (topWallMat[x, y] == 0)
                {
                    Vector3 pos = tilePos + new Vector3(0, halfSize + halfBridgeSize, 0);
                    Vector3 pathSize = new Vector3(bridgeSmallSize, bridgeSize, size);
                    Gizmos.DrawCube(pos, pathSize);
                }
            }
        }
    }
    #endregion
}
