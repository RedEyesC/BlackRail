
using System;

namespace GameFramework.Recast
{
    public class RecastHeightField
    {
        public static void RcRasterizeTriangles(float[] verts, int[] tris, AREATYPE[] areas, RcHeightfield hf)
        {
            float[] vert1 = new float[3];
            float[] vert2 = new float[3];
            float[] vert3 = new float[3];

            int numTris = tris.Length / 3;
            for (int i = 0; i < numTris; i++)
            {
                vert1[0] = verts[tris[i * 3] * 3];
                vert1[1] = verts[tris[i * 3] * 3 + 1];
                vert1[2] = verts[tris[i * 3] * 3 + 2];

                vert2[0] = verts[tris[i * 3 + 1] * 3];
                vert2[1] = verts[tris[i * 3 + 1] * 3 + 1];
                vert2[2] = verts[tris[i * 3 + 1] * 3 + 2];

                vert3[0] = verts[tris[i * 3 + 2] * 3];
                vert3[1] = verts[tris[i * 3 + 2] * 3 + 1];
                vert3[2] = verts[tris[i * 3 + 2] * 3 + 2];

                if (!RasterizeTri(vert1, vert2, vert3, areas[i], hf))
                {
                    RecastUtility.LogError("rcRasterizeTriangles: Out of memory.");
                }

            }

        }

        private static bool RasterizeTri(float[] v0, float[] v1, float[] v2, AREATYPE areaType, RcHeightfield hf)
        {
            float[] triBBMin = new float[3];
            RecastUtility.RcVcopy(triBBMin, v0);
            RecastUtility.RcVmin(triBBMin, v1);
            RecastUtility.RcVmin(triBBMin, v2);

            float[] triBBMax = new float[3];
            RecastUtility.RcVcopy(triBBMax, v0);
            RecastUtility.RcVmax(triBBMax, v1);
            RecastUtility.RcVmax(triBBMax, v2);

            float[] hfBBMin = hf.minBounds;
            float[] hfBBMax = hf.maxBounds;

            float cellSize = hf.cellSize;
            float inverseCellSize = 1 / hf.cellSize;
            float inverseCellHeight = 1 / hf.cellHeight;

            float by = hfBBMax[1] - hfBBMin[1];

            // 三角形的包围盒不在目标包围盒范围内就放弃这个三角形
            if (!RecastUtility.OverlapBounds(triBBMin, triBBMax, hfBBMin, hfBBMax))
            {
                return true;
            }

            //在xz平面上，以体素为单位的长度，z轴的最大值和最小值。以包围盒的z为0点,
            int z0 = ((int)((triBBMin[2] - hfBBMin[2]) * inverseCellSize));
            int z1 = ((int)((triBBMax[2] - hfBBMin[2]) * inverseCellSize));


            z0 = Math.Clamp(z0, -1, hf.height - 1);
            z1 = Math.Clamp(z1, 0, hf.height - 1);


            //三角形被正方形切割最多切割出7边形
            float[] nvInList = new float[7 * 3];

            Array.Copy(v0, 0, nvInList, 0, 3);
            Array.Copy(v1, 0, nvInList, 3, 3);
            Array.Copy(v2, 0, nvInList, 6, 3);


            int nvIn = 3;
            //按z轴开始切割
            for (int z = z0; z <= z1; ++z)
            {

                float cellZ = hfBBMin[2] + (float)z * cellSize;

                //切割三角形，nvRowList 为切割线下半部分的顶点 ,输出的nvInList 为切割线上半部分顶点
                RecastUtility.DividePoly(nvInList, nvIn, cellZ + cellSize, RcAxis.AXIS_Z, out int nvRow, out nvIn, out float[] nvRowList, out nvInList);

                //没切割到东西，顶点小于3构不成多边形
                if (nvRow < 3)
                {
                    continue;
                }

                if (z < 0)
                {
                    continue;
                }


                //重复之前的步骤，按x轴切割
                float minX = nvRowList[0];
                float maxX = nvRowList[0];

                for (int vert = 1; vert < nvRow; ++vert)
                {

                    float x = nvRowList[vert * 3];

                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);

                }

                int x0 = ((int)((minX - hfBBMin[0]) * inverseCellSize));
                int x1 = ((int)((maxX - hfBBMin[0]) * inverseCellSize));

                //recastnavigation 里x0 >= hf.width ,但是本地测试单独一个正方体体素化的时候，假如多了=，会缺失一面体素
                if (x1 < 0 || x0 > hf.width)
                {
                    continue;
                }
                x0 = Math.Clamp(x0, -1, hf.width - 1);
                x1 = Math.Clamp(x1, 0, hf.width - 1);


                int nvIn2 = nvRow;

                for (int x = x0; x <= x1; ++x)
                {
                    float cellX = hfBBMin[0] + (float)x * cellSize;
                    RecastUtility.DividePoly(nvRowList, nvIn2, cellX + cellSize, RcAxis.AXIS_X, out int nvRow2, out nvIn2, out float[] p1InList, out nvRowList);

                    if (nvRow2 < 3)
                    {
                        continue;
                    }

                    if (x < 0)
                    {
                        continue;
                    }

                    float spanMin = p1InList[1];
                    float spanMax = p1InList[1];

                    for (int vert = 1; vert < nvRow2; ++vert)
                    {

                        float y = p1InList[vert * 3 + 1];

                        spanMin = Math.Min(spanMin, y);
                        spanMax = Math.Max(spanMax, y);

                    }

                    //坐标Y 平移到最小包围盒底部
                    spanMin -= hfBBMin[1];
                    spanMax -= hfBBMin[1];


                    // 假如在包围盒外，舍弃
                    if (spanMax < 0.0f)
                    {
                        continue;
                    }

                   
                    if (spanMin > by)
                    {
                        continue;
                    }

                    // 截取包围盒内的部分
                    if (spanMin < 0.0f)
                    {
                        spanMin = 0;
                    }
                    if (spanMax > by)
                    {
                        spanMax = by;
                    }

                    int spanMinCellIndex = (int)Math.Max(Math.Floor(spanMin * inverseCellHeight), 0);
                    int spanMaxCellIndex = (int)Math.Max(Math.Ceiling(spanMax * inverseCellHeight), spanMinCellIndex + 1);

                    // 加入高度场
                    if (!AddSpan(hf, x, z, spanMinCellIndex, spanMaxCellIndex, areaType))
                    {
                        return false;
                    }
                }



            }
            return true;
        }


        private static bool AddSpan(RcHeightfield hf, int x, int z, int min, int max, AREATYPE areaType)
        {
            RcSpan newSpan = new RcSpan(min, max, areaType);

            int columnIndex = x + z * hf.width;

            //缓存比newSpan小的体素的列表
            RcSpan previousSpan = null;
            //进行比较的体素
            RcSpan currentSpan = hf.spans[columnIndex];

            while (currentSpan != null)
            {
                //newSpan位置在currentSpan之下
                if (currentSpan.min > newSpan.max)
                {
                    break;
                }

                //newSpan位置在currentSpan之上
                if (currentSpan.max < newSpan.min)
                {
                    //切换到链表的下一个体素
                    previousSpan = currentSpan;
                    currentSpan = currentSpan.next;
                }
                //newSpan位置与currentSpan重叠,合并newSpan和currentSpan
                else
                {
                    if (currentSpan.min < newSpan.min)
                    {
                        newSpan.min = currentSpan.min;
                    }
                    if (currentSpan.max > newSpan.max)
                    {
                        newSpan.max = currentSpan.max;
                    }

                    if (Math.Abs(newSpan.max - currentSpan.max) < hf.walkableClimb)
                    {
                        newSpan.areaID = (AREATYPE)Math.Max((int)newSpan.areaID, (int)currentSpan.areaID);
                    }

                    //从链表中释放currentSpan
                    RcSpan next = currentSpan.next;
                    if (previousSpan != null)
                    {
                        previousSpan.next = next;
                    }
                    else
                    {
                        hf.spans[columnIndex] = next;
                    }

                    //切换currentSpan 进行下一轮比较
                    currentSpan = next;
                }

            }

            if (previousSpan != null)
            {
                //将newSpan插入 previousSpan 的后面
                newSpan.next = previousSpan.next;
                previousSpan.next = newSpan;
            }
            else
            {
                newSpan.next = hf.spans[columnIndex];
                hf.spans[columnIndex] = newSpan;
            }

            return true;
        }


        public static void RcFilterLowHangingWalkableObstacles(RcHeightfield hf)
        {
            int xSize = hf.width;
            int zSize = hf.height;

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    RcSpan previousSpan = null;
                    bool previousWasWalkable = false;
                    AREATYPE previousArea = 0;

                    //上下两个span，下span可走，上span不可走，并且上下span的上表面相差不超过walkClimb，则把上span也改为可走
                    for (RcSpan span = hf.spans[x + z * xSize]; span != null; previousSpan = span, span = span.next)
                    {
                        bool walkable = span.areaID == AREATYPE.Walke;


                        if (!walkable && previousWasWalkable)
                        {
                            if (Math.Abs(span.max - previousSpan.max) <= hf.walkableClimb)
                            {
                                span.areaID = previousArea;
                            }
                        }

                        previousWasWalkable = walkable;
                        previousArea = span.areaID;
                    }
                }
            }
        }

        public static void RcFilterLedgeSpans(RcHeightfield hf)
        {
            int xSize = hf.width;
            int zSize = hf.height;



            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (RcSpan span = hf.spans[x + z * xSize]; span != null; span = span.next)
                    {
                        //跳过不可行走区域
                        if (span.areaID == AREATYPE.None)
                        {
                            continue;
                        }

                        int bot = span.max;
                        int top = span.next != null ? span.next.min : RecastConfig.MAX_HEIGHT;


                        float minNeighborHeight = RecastConfig.MAX_HEIGHT;

                        //相邻span的上表面的最大值与最小值
                        int accessibleNeighborMinHeight = span.max;
                        int accessibleNeighborMaxHeight = span.max;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int dx = x + RecastUtility.RcGetDirOffsetX(direction);
                            int dy = z + RecastUtility.RcGetDirOffsetY(direction);


                            if (dx < 0 || dy < 0 || dx >= xSize || dy >= zSize)
                            {
                                //边缘情况认为，minNeighborHeight 一定小于 -hf.walkableClimb而已
                                minNeighborHeight = Math.Min(minNeighborHeight, -hf.walkableClimb - bot);
                                continue;
                            }


                            RcSpan neighborSpan = hf.spans[dx + dy * xSize];
                            int neighborBot = -hf.walkableClimb;
                            int neighborTop = neighborSpan != null ? neighborSpan.min : RecastConfig.MAX_HEIGHT;

                            //先单独按neighborSpan不存在处理一次minNeighborHeight的值
                            if (Math.Min(top, neighborTop) - Math.Max(bot, neighborBot) > hf.walkableHeight)
                            {
                                minNeighborHeight = Math.Min(minNeighborHeight, neighborBot - bot);
                            }

                            for (neighborSpan = hf.spans[dx + dy * xSize]; neighborSpan != null; neighborSpan = neighborSpan.next)
                            {
                                neighborBot = neighborSpan.max;
                                neighborTop = neighborSpan.next != null ? neighborSpan.next.min : RecastConfig.MAX_HEIGHT;

                                // 只处理与上层之间宽度大于walkableHeight的部分
                                if (Math.Min(top, neighborTop) - Math.Max(bot, neighborBot) > hf.walkableHeight)
                                {

                                    minNeighborHeight = Math.Min(minNeighborHeight, neighborBot - bot);

                                    //寻找相邻的span与自身高度差小于WalkableClimb
                                    if (Math.Abs(neighborBot - bot) <= hf.walkableClimb)
                                    {
                                        if (neighborBot < accessibleNeighborMinHeight) accessibleNeighborMinHeight = neighborBot;
                                        if (neighborBot > accessibleNeighborMaxHeight) accessibleNeighborMaxHeight = neighborBot;
                                    }

                                }
                            }
                        }

                        //相邻span与自身的高度差，说明span高于相邻span 大于WalkableClimb  minNeighborHeight < -hf.walkableClimb => bot - neighborBot > hf.walkableClimb
                        if (minNeighborHeight < -hf.walkableClimb)
                        {
                            span.areaID = AREATYPE.None;
                        }
                        //邻居span之间的上表面高度差超过walkClimb，说明span处于比较陡峭的地方，则把span标记为不可行走。
                        else if ((accessibleNeighborMaxHeight - accessibleNeighborMinHeight) > hf.walkableClimb)
                        {
                            span.areaID = AREATYPE.None;
                        }
                    }
                }
            }
        }


        public static void RcFilterWalkableLowHeightSpans(RcHeightfield hf)
        {
            int xSize = hf.width;
            int zSize = hf.height;

            //如果上下两个span之间的空隙小于等于walkHeight，则把下span标记为不可行走。
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (RcSpan span = hf.spans[x + z * xSize]; span != null; span = span.next)
                    {
                        int bot = span.max;
                        int top = span.next != null ? span.next.min : RecastConfig.MAX_HEIGHT;
                        if ((top - bot) < hf.walkableHeight)
                        {
                            span.areaID = AREATYPE.None;
                        }
                    }
                }
            }
        }

        public static void RcBuildCompactHeightfield(RcHeightfield hf, RcCompactHeightfield chf)
        {
            int currentCellIndex = 0;
            int numColumns = hf.width * hf.height;

            for (int columnIndex = 0; columnIndex < numColumns; ++columnIndex)
            {
                RcSpan span = hf.spans[columnIndex];

                CompactCell cell = new CompactCell(0, 0);

                chf.cells[columnIndex] = cell;

                if (span == null)
                {
                    continue;
                }

                cell.index = currentCellIndex;

                for (; span != null; span = span.next)
                {
                    if (span.areaID != AREATYPE.None)
                    {
                        int bot = span.max;
                        int top = span.next != null ? span.next.min : RecastConfig.MAX_HEIGHT;

                        chf.spans[currentCellIndex] = new CompactSpan(Math.Clamp(bot, 0, RecastConfig.MAX_HEIGHT), Math.Clamp(top - bot, 0, RecastConfig.MAX_HEIGHT), span.areaID);
                        chf.areas[currentCellIndex] = span.areaID;

                        currentCellIndex++;
                        cell.count++;
                    }
                }
            }


            int zSize = hf.height;
            int xSize = hf.width;
            int maxLayerIndex = 0;
            int zStride = xSize;
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    for (int i = cell.index, ni = (cell.index + cell.count); i < ni; ++i)
                    {
                        CompactSpan span = chf.spans[i];

                        for (int dir = 0; dir < 4; ++dir)
                        {

                            RecastUtility.RcSetCon(span, dir, RecastConfig.RC_NOT_CONNECTED);
                            int neighborX = x + RecastUtility.RcGetDirOffsetX(dir);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(dir);

                            if (neighborX < 0 || neighborZ < 0 || neighborX >= xSize || neighborZ >= zSize)
                            {
                                continue;
                            }


                            CompactCell neighborCell = chf.cells[neighborX + neighborZ * zStride];

                            for (int k = neighborCell.index, nk = neighborCell.index + neighborCell.count; k < nk; ++k)
                            {
                                CompactSpan neighborSpan = chf.spans[k];
                                int bot = Math.Max(span.y, neighborSpan.y);
                                int top = Math.Min(span.y + span.h, neighborSpan.y + neighborSpan.h);


                                //与相邻的空心体素的之间的高度大于行走高度，之间的落差小于可攀爬高度
                                if (((top - bot) >= chf.walkableHeight) && (Math.Abs(neighborSpan.y - span.y) <= chf.walkableClimb))
                                {
                                    // Mark direction as walkable.
                                    int layerIndex = k - neighborCell.index;
                                    if (layerIndex < 0 || layerIndex > RecastConfig.MAX_LAYERS)
                                    {
                                        maxLayerIndex = Math.Max(maxLayerIndex, layerIndex);
                                        continue;
                                    }

                                    RecastUtility.RcSetCon(span, dir, layerIndex);
                                    break;
                                }

                            }
                        }

                    }
                }
            }
            if (maxLayerIndex > RecastConfig.MAX_LAYERS)
            {
                RecastUtility.LogErrorFormat(string.Format("rcBuildCompactHeightfield: Heightfield has too many layers %d (max: %d)", maxLayerIndex, RecastConfig.MAX_LAYERS));
            }
        }


        public static void RcErodeWalkableArea(RcCompactHeightfield chf)
        {
            int xSize = chf.width;
            int zSize = chf.height;
            int zStride = xSize;


            int[] distanceToBoundary = new int[chf.spanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    for (int spanIndex = cell.index, maxSpanIndex = (cell.index + cell.count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        //设置默认值
                        distanceToBoundary[spanIndex] = 255;

                        //不可行走设置为边缘
                        if (chf.areas[spanIndex] == AREATYPE.None)
                        {
                            distanceToBoundary[spanIndex] = 0;
                            continue;
                        }

                        CompactSpan span = chf.spans[spanIndex];

                        int neighborCount = 0;
                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection == RecastConfig.RC_NOT_CONNECTED)
                            {
                                break;
                            }

                            int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                            int neighborSpanIndex = chf.cells[neighborX + neighborZ * zStride].index + neighborConnection;

                            if (chf.areas[neighborSpanIndex] == AREATYPE.None)
                            {
                                break;
                            }
                            neighborCount++;
                        }

                        //不是四个位置都存在相邻的可行走，则视为边缘
                        if (neighborCount != 4)
                        {
                            distanceToBoundary[spanIndex] = 0;
                        }
                    }
                }
            }

            int newDistance;

            //第一次从左下到右上遍历，计算出障碍物右上方span的距离，对于CompactSpan本身是左下侧，，，左下最边缘是障碍物,一层层向右上方感染

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    int maxSpanIndex = (cell.index + cell.count);
                    for (int spanIndex = cell.index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = chf.spans[spanIndex];

                        if (RecastUtility.RcGetCon(span, 0) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (-1,0) 左侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(0);
                            int aY = z + RecastUtility.RcGetDirOffsetY(0);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 0);
                            CompactSpan aSpan = chf.spans[aIndex];
                            // 正交的邻居距离+2
                            newDistance = Math.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,-1) 左下
                            if (RecastUtility.RcGetCon(aSpan, 3) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(3);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(3);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 3);
                                // 斜方向的邻居距离+3
                                newDistance = Math.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 3) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (0,-1) 下侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(3);
                            int aY = z + RecastUtility.RcGetDirOffsetY(3);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 3);
                            CompactSpan aSpan = chf.spans[aIndex];
                            newDistance = Math.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,-1) 下右
                            if (RecastUtility.RcGetCon(aSpan, 2) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(2);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(2);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 2);
                                newDistance = Math.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            //第二次从右上到最下遍历，计算出障碍物左下方span的距离
            for (int z = zSize - 1; z > 0; --z)
            {
                for (int x = xSize - 1; x > 0; --x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    int maxSpanIndex = (cell.index + cell.count);
                    for (int spanIndex = cell.index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = chf.spans[spanIndex];

                        if (RecastUtility.RcGetCon(span, 2) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (1,0) 右侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(2);
                            int aY = z + RecastUtility.RcGetDirOffsetY(2);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 2);
                            CompactSpan aSpan = chf.spans[aIndex];
                            // 正交的邻居距离+2
                            newDistance = Math.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,1) 右上
                            if (RecastUtility.RcGetCon(aSpan, 1) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(1);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(1);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 1);
                                // 斜方向的邻居距离+3
                                newDistance = Math.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 1) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (0,1) 上侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(1);
                            int aY = z + RecastUtility.RcGetDirOffsetY(1);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 1);
                            CompactSpan aSpan = chf.spans[aIndex];
                            newDistance = Math.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,1) 上左
                            if (RecastUtility.RcGetCon(aSpan, 0) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(0);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(0);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 0);
                                newDistance = Math.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }



            //大于半径x2 ，，因为算距离的时候本身就是x2
            int minBoundaryDistance = (chf.walkableRadius * 2);
            for (int spanIndex = 0; spanIndex < chf.spanCount; ++spanIndex)
            {
                if (distanceToBoundary[spanIndex] < minBoundaryDistance)
                {
                    chf.areas[spanIndex] = AREATYPE.None;
                }
            }
        }


        public static void RcMarkConvexPolyArea(RcCompactHeightfield chf, float[] vertices, AREATYPE areaId)
        {
            int xSize = chf.width;
            int zSize = chf.height;
            int zStride = xSize;

            RecastUtility.CalcBounds(vertices, out float[] MinBounds, out float[] MaxBounds);

            //计算多边形在高度场内的坐标范围
            int minx = (int)((MinBounds[0] - chf.minBounds[0]) / chf.cellSize);
            int miny = (int)((MinBounds[1] - chf.minBounds[1]) / chf.cellHeight);
            int minz = (int)((MinBounds[2] - chf.minBounds[2]) / chf.cellSize);
            int maxx = (int)((MaxBounds[0] - chf.minBounds[0]) / chf.cellSize);
            int maxy = (int)((MaxBounds[1] - chf.minBounds[1]) / chf.cellHeight);
            int maxz = (int)((MaxBounds[2] - chf.minBounds[2]) / chf.cellSize);


            if (maxx < 0) { return; }
            if (minx >= xSize) { return; }
            if (maxz < 0) { return; }
            if (minz >= zSize) { return; }


            if (minx < 0) { minx = 0; }
            if (maxx >= xSize) { maxx = xSize - 1; }
            if (minz < 0) { minz = 0; }
            if (maxz >= zSize) { maxz = zSize - 1; }


            for (int z = minz; z <= maxz; ++z)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    int maxSpanIndex = cell.index + cell.count;

                    for (int spanIndex = (int)cell.index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = chf.spans[spanIndex];

                        if (chf.areas[spanIndex] == AREATYPE.None)
                        {
                            continue;
                        }

                        if (span.y < miny || span.y > maxy)
                        {
                            continue;
                        }

                        //因为之后是投影至2d平面的运算所以 point[1] = 0
                        float[] point = new float[3];
                        point[0] = chf.minBounds[0] + (x + 0.5f) * chf.cellSize;
                        point[1] = 0;
                        point[2] = chf.minBounds[2] + (z + 0.5f) * chf.cellSize;

                        //投影到2d平面进行运算
                        if (PointInPoly(vertices, point))
                        {
                            chf.areas[spanIndex] = areaId;
                        }
                    }
                }
            }


        }


        private static bool PointInPoly(float[] verts, float[] point)
        {
            bool inPoly = false;
            int nvert = verts.Length;


            for (int i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                int vi = i * 3;
                int vj = j * 3;

                if ((verts[vi + 2] > point[2]) == (verts[vj + 2] > point[2]))
                {
                    continue;
                }

                if (point[0] >= (verts[vj] - verts[vi]) * (point[2] - verts[vi + 2]) / (verts[vj + 2] - verts[vi + 2]) + verts[vi])
                {
                    continue;
                }
                inPoly = !inPoly;
            }
            return inPoly;
        }

        public static void RcBuildDistanceField(RcCompactHeightfield chf)
        {
            //计算距离场
            CalculateDistanceField(chf);

            //平滑
            BoxBlur(chf, 1);

        }


        private static void CalculateDistanceField(RcCompactHeightfield chf)
        {
            int xSize = chf.width;
            int zSize = chf.height;
            int zStride = xSize;


            int[] distanceToBoundary = new int[chf.spanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    for (int spanIndex = cell.index, maxSpanIndex = (cell.index + cell.count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        //设置默认值
                        distanceToBoundary[spanIndex] = 0xffff;

                        CompactSpan span = chf.spans[spanIndex];

                        int neighborCount = 0;
                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection == RecastConfig.RC_NOT_CONNECTED)
                            {
                                break;
                            }

                            int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                            int neighborSpanIndex = chf.cells[neighborX + neighborZ * zStride].index + neighborConnection;

                            if (chf.areas[neighborSpanIndex] != chf.areas[spanIndex])
                            {
                                break;
                            }
                            neighborCount++;
                        }

                        //不是四个位置都存在同类型区域，则视为边缘
                        if (neighborCount != 4)
                        {
                            distanceToBoundary[spanIndex] = 0;
                        }
                    }
                }
            }


            int newDistance;

            //第一次从左下到右上遍历，计算出障碍物右上方span的距离，对于CompactSpan本身是左下侧，，，左下最边缘是障碍物,一层层向右上方感染

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    int maxSpanIndex = (cell.index + cell.count);
                    for (int spanIndex = cell.index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = chf.spans[spanIndex];

                        if (RecastUtility.RcGetCon(span, 0) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (-1,0) 左侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(0);
                            int aY = z + RecastUtility.RcGetDirOffsetY(0);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 0);
                            CompactSpan aSpan = chf.spans[aIndex];
                            // 正交的邻居距离+2
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,-1) 左下
                            if (RecastUtility.RcGetCon(aSpan, 3) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(3);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(3);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 3);
                                // 斜方向的邻居距离+3
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 3) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (0,-1) 下侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(3);
                            int aY = z + RecastUtility.RcGetDirOffsetY(3);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 3);
                            CompactSpan aSpan = chf.spans[aIndex];
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,-1) 下右
                            if (RecastUtility.RcGetCon(aSpan, 2) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(2);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(2);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 2);
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            //第二次从右上到最下遍历，计算出障碍物左下方span的距离
            for (int z = zSize - 1; z > 0; --z)
            {
                for (int x = xSize - 1; x > 0; --x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    int maxSpanIndex = (cell.index + cell.count);
                    for (int spanIndex = cell.index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = chf.spans[spanIndex];

                        if (RecastUtility.RcGetCon(span, 2) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (1,0) 右侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(2);
                            int aY = z + RecastUtility.RcGetDirOffsetY(2);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 2);
                            CompactSpan aSpan = chf.spans[aIndex];
                            // 正交的邻居距离+2
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,1) 右上
                            if (RecastUtility.RcGetCon(aSpan, 1) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(1);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(1);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 1);
                                // 斜方向的邻居距离+3
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 1) != RecastConfig.RC_NOT_CONNECTED)
                        {
                            // (0,1) 上侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(1);
                            int aY = z + RecastUtility.RcGetDirOffsetY(1);
                            int aIndex = chf.cells[aX + aY * xSize].index + RecastUtility.RcGetCon(span, 1);
                            CompactSpan aSpan = chf.spans[aIndex];
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,1) 上左
                            if (RecastUtility.RcGetCon(aSpan, 0) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(0);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(0);
                                int bIndex = chf.cells[bX + bY * xSize].index + RecastUtility.RcGetCon(aSpan, 0);
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }


            int maxDist = 0;
            for (int i = 0; i < distanceToBoundary.Length; i++)
            {
                maxDist = Math.Max(maxDist, distanceToBoundary[i]);
            }

            chf.maxDistance = maxDist;
            chf.distanceToBoundary = distanceToBoundary;
        }


        private static void BoxBlur(RcCompactHeightfield chf, int thr)
        {
            int xSize = chf.width;
            int zSize = chf.height;
            int zStride = xSize;

            thr = thr * 2;

            int[] distanceToBoundary = new int[chf.spanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = chf.cells[x + z * zStride];
                    for (int spanIndex = cell.index, maxSpanIndex = (cell.index + cell.count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        int cd = chf.distanceToBoundary[spanIndex];

                        if (cd <= thr)
                        {
                            distanceToBoundary[spanIndex] = cd;
                            continue;
                        }

                        CompactSpan span = chf.spans[spanIndex];

                        int d = cd;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                                int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                                int neighborSpanIndex = chf.cells[neighborX + neighborZ * zStride].index + neighborConnection;

                                d = d + chf.distanceToBoundary[neighborSpanIndex];

                                CompactSpan neighborSpan = chf.spans[neighborSpanIndex];

                                int direction2 = (direction + 1) & 0x3;
                                int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                                if (neighbor2Connection != RecastConfig.RC_NOT_CONNECTED)
                                {
                                    int neighbor2X = x + RecastUtility.RcGetDirOffsetX(direction2);
                                    int neighbor2Z = z + RecastUtility.RcGetDirOffsetY(direction2);
                                    int neighbor2SpanIndex = chf.cells[neighbor2X + neighbor2Z * zStride].index + neighbor2Connection;
                                    d = d + chf.distanceToBoundary[neighbor2SpanIndex];
                                }
                                else
                                {
                                    d = d + cd;
                                }
                            }
                            else
                            {
                                //cd*2是包括了斜边的部分
                                d = d + cd * 2;
                            }

                            distanceToBoundary[spanIndex] = ((d + 5) / 9);

                        }

                    }

                }
            }

            chf.distanceToBoundary = distanceToBoundary;
        }

    }
}
