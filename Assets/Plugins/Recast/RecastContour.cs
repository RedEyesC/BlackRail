
using System;
using System.Collections.Generic;

namespace GameFramework.Recast
{
    public class RecastContour
    {
        public static void RcBuildRegions(RcCompactHeightfield chf)
        {

            const int expandIters = 8;
            const int loglevelsPerStack = 1;//决定了每组的距离
            const int LOG_NB_STACKS = 3;
            const int NB_STACKS = 1 << LOG_NB_STACKS; //决定了每批有8组

            int regionId = 1;

            //使最低一位为0，使得level为2的倍数
            int level = (chf.maxDistance + 1) & (~1);

            int sId = -1;

            Stack<LevelStackEntry>[] lvlStacks = new Stack<LevelStackEntry>[NB_STACKS];
            Stack<LevelStackEntry> stack = new Stack<LevelStackEntry>();

            for (int i = 0; i < NB_STACKS; i++)
            {
                lvlStacks[i] = new Stack<LevelStackEntry>();
            }

            int[] srcReg = new int[chf.spanCount];
            int[] srcDist = new int[chf.spanCount];

            Array.Fill(srcReg, 0);
            Array.Fill(srcDist, 0);


            while (level > 0)
            {
                level = level >= 2 ? level - 2 : 0;
                sId = (sId + 1) & (NB_STACKS - 1);

                //分批划分level层级，ps话说我感觉可以直接一次性划分完啊，为什么要分批去划分
                if (sId == 0)
                    SortCellsByLevel(level, chf, srcReg, NB_STACKS, lvlStacks, loglevelsPerStack);
                else
                    AppendStacks(lvlStacks[sId - 1], lvlStacks[sId], srcReg);

                //从水源进行蔓延 
                ExpandRegions(expandIters, level, chf, srcReg, srcDist, lvlStacks[sId], false);

                //寻找水源
                foreach (LevelStackEntry lvlStackEntry in lvlStacks[sId])
                {
                    int x = lvlStackEntry.x;
                    int y = lvlStackEntry.y;
                    int i = lvlStackEntry.index;
                    if (i >= 0 && srcReg[i] == 0)
                    {
                        if (FloodRegion(x, y, i, level, regionId, chf, srcReg, srcDist, stack))
                        {
                            if (regionId == 0xFFFF)
                            {
                                RecastUtility.LogError("rcBuildRegions: Region ID overflow");
                            }

                            regionId++;
                        }
                    }

                }

            }

            ExpandRegions(expandIters * 8, 0, chf, srcReg, srcDist, stack, false);

            //合并区域
            chf.maxRegions = regionId;

            MergeAndFilterRegions(chf, srcReg);

            for (int i = 0; i < chf.spanCount; ++i)
                chf.spans[i].reg = srcReg[i];

        }

        //loglevelsPerStack 决定距离多少为一个level
        private static void SortCellsByLevel(int startLevel, RcCompactHeightfield chf, int[] srcReg, int nbStacks, Stack<LevelStackEntry>[] stacks, int loglevelsPerStack)
        {
            int w = chf.width;
            int h = chf.height;
            startLevel = startLevel >> loglevelsPerStack;

            for (int i = 0; i < nbStacks; i++)
            {
                stacks[i].Clear();
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = (c.index + c.count); i < ni; ++i)
                    {
                        if (chf.areas[i] == AREATYPE.None || srcReg[i] != 0)
                            continue;

                        int level = chf.distanceToBoundary[i] >> loglevelsPerStack;
                        int sId = startLevel - level;
                        if (sId >= nbStacks)
                            continue;
                        if (sId < 0)
                            sId = 0;

                        stacks[sId].Push(new LevelStackEntry(x, y, i));
                    }
                }
            }
        }

        private static void AppendStacks(Stack<LevelStackEntry> srcStack, Stack<LevelStackEntry> dstStack, int[] srcReg)
        {

            foreach (LevelStackEntry srcEntry in srcStack)
            {
                int i = srcEntry.index;
                if ((i < 0) || (srcReg[i] != 0))
                    continue;
                dstStack.Push(srcEntry);
            }
        }

        private static void ExpandRegions(int maxIter, int level, RcCompactHeightfield chf, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack, bool fillStack)
        {
            int w = chf.width;
            int h = chf.height;

            if (fillStack)
            {
                stack.Clear();

                for (int y = 0; y < h; ++y)
                {
                    for (int x = 0; x < w; ++x)
                    {
                        CompactCell c = chf.cells[x + y * w];
                        for (int i = c.index, ni = (c.index + c.count); i < ni; ++i)
                        {
                            if (chf.distanceToBoundary[i] >= level && srcReg[i] == 0 && chf.areas[i] != AREATYPE.None)
                            {
                                stack.Push(new LevelStackEntry(x, y, i));
                            }
                        }
                    }
                }
            }
            else
            {
                //排除那些已经标记了区域的cell
                foreach (LevelStackEntry entry in stack)
                {
                    int i = entry.index;
                    if (srcReg[i] != 0)
                        entry.index = -1;
                }

            }


            Stack<DirtyEntry> dirtyEntries = new Stack<DirtyEntry>();
            int iter = 0;
            while (stack.Count > 0)
            {

                int failed = 0;
                dirtyEntries.Clear();

                //遍历假如相邻的span已经属于某个regionId，则把自身设置为同个regionId
                foreach (LevelStackEntry entry in stack)
                {
                    int x = entry.x;
                    int z = entry.y;
                    int spanIndex = entry.index;
                    if (spanIndex < 0)
                    {
                        failed++;
                        continue;
                    }

                    int r = srcReg[spanIndex];
                    int d2 = 0xffff;
                    AREATYPE area = chf.areas[spanIndex];
                    CompactSpan span = chf.spans[spanIndex];

                    for (int direction = 0; direction < 4; ++direction)
                    {
                        int neighborConnection = RecastUtility.RcGetCon(span, direction);
                        int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = chf.cells[neighborX + neighborZ * w].index + neighborConnection;

                        if (chf.areas[neighborSpanIndex] != chf.areas[spanIndex])
                        {
                            continue;
                        }

                        if (srcReg[neighborSpanIndex] > 0)
                        {
                            if (srcDist[neighborSpanIndex] + 2 < d2)
                            {
                                r = srcReg[neighborSpanIndex];
                                d2 = srcDist[neighborSpanIndex] + 2;
                            }
                        }
                    }

                    if (r != 0)
                    {
                        entry.index = -1; //标记已经使用了
                        dirtyEntries.Push(new DirtyEntry(spanIndex, r, d2));
                    }
                    else
                    {
                        failed++;
                    }

                }

                foreach (DirtyEntry dirtyEntery in dirtyEntries)
                {
                    int idx = dirtyEntery.index;
                    srcReg[idx] = dirtyEntery.region;
                    srcDist[idx] = dirtyEntery.distance2;

                }

                if (failed == stack.Count)
                    break;

                if (level > 0)
                {
                    ++iter;
                    if (iter >= maxIter)
                        break;
                }

            }

        }

        private static bool FloodRegion(int x, int y, int i, int level, int r, RcCompactHeightfield chf, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack)
        {
            int w = chf.width;
            AREATYPE area = chf.areas[i];

            stack.Clear();
            stack.Push(new LevelStackEntry(x, y, i));
            //标记为当前的regionId
            srcReg[i] = r;
            srcDist[i] = 0;

            int lev = level >= 2 ? level - 2 : 0;
            int count = 0;

            while (stack.Count > 0)
            {
                LevelStackEntry back = stack.Pop();
                int cx = back.x;
                int cy = back.y;
                int ci = back.index;

                CompactSpan span = chf.spans[ci];
                int ar = 0;

                //8向遍历相邻span 如果相邻span属于其他region，则自身region标记为0
                for (int direction = 0; direction < 4; ++direction)
                {

                    int neighborConnection = RecastUtility.RcGetCon(span, direction);
                    if (neighborConnection != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int neighborX = cx + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = cy + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = chf.cells[neighborX + neighborZ * w].index + neighborConnection;

                        //不在同一个area
                        if (chf.areas[neighborSpanIndex] != area)
                            continue;

                        int nr = srcReg[ci];

                        //属于已标记region，且不是当前的regionId
                        if (nr != 0 && nr != r)
                        {
                            ar = nr;
                            break;
                        }


                        int direction2 = (direction + 1) & 0x3;
                        CompactSpan neighborSpan = chf.spans[neighborSpanIndex];
                        int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                        if (neighbor2Connection != RecastConfig.RC_NOT_CONNECTED)
                        {
                            int neighbor2X = neighborX + RecastUtility.RcGetDirOffsetX(direction2);
                            int neighbor2Z = neighborZ + RecastUtility.RcGetDirOffsetY(direction2);
                            int neighbor2SpanIndex = chf.cells[neighbor2X + neighbor2Z * w].index + neighbor2Connection;

                            if (chf.areas[neighbor2SpanIndex] != area)
                                continue;

                            int nr2 = srcReg[neighbor2SpanIndex];

                            if (nr2 != 0 && nr2 != r)
                            {
                                ar = nr2;
                                break;
                            }
                        }

                    }

                }

                if (ar != 0)
                {
                    srcReg[ci] = 0;
                    continue;
                }

                count++;


                //假如相邻的span未标记且距离场大于leve ，则相邻的span标记为当前的regionId
                for (int direction = 0; direction < 4; ++direction)
                {

                    int neighborConnection = RecastUtility.RcGetCon(span, direction);
                    if (neighborConnection != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int neighborX = cx + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = cy + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = chf.cells[neighborX + neighborZ * w].index + neighborConnection;

                        if (chf.areas[neighborSpanIndex] != area)
                            continue;

                        if (chf.distanceToBoundary[neighborSpanIndex] > lev && srcReg[neighborSpanIndex] == 0)
                        {
                            srcReg[neighborSpanIndex] = r;
                            srcDist[neighborSpanIndex] = 0;
                            stack.Push(new LevelStackEntry(neighborX, neighborZ, neighborSpanIndex));
                        }

                    }
                }

            }
            return count > 0;
        }

        private static void MergeAndFilterRegions(RcCompactHeightfield chf, int[] srcReg)
        {

            int minRegionArea =  RecastConfig.MinRegionSize * RecastConfig.MinRegionSize;

            int mergeRegionArea = RecastConfig.MergeRegionSize * RecastConfig.MergeRegionSize;

            int w = chf.width;
            int h = chf.height;

            int nreg = chf.maxRegions + 1;

            RcRegion[] regions = new RcRegion[chf.maxRegions + 1];
            for (int i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion(i);
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = (c.index + c.count); i < ni; ++i)
                    {
                        int r = srcReg[i];
                        if (r == 0 || r >= nreg)
                            continue;

                        RcRegion reg = regions[r];
                        reg.spanCount++;

                        // 寻找相同xz平面位置的上方的span的区域
                        for (int j = c.index; j < ni; ++j)
                        {
                            if (i == j) continue;
                            int floorId = srcReg[j];
                            if (floorId == 0 || floorId >= nreg)
                                continue;
                            if (floorId == r)
                                reg.overlap = true;
                            AddUniqueFloorRegion(reg, floorId);
                        }

                        //区域已经计算完连通区域了，不用再计算了
                        if (reg.connections.Count > 0)
                            continue;

                        reg.areaType = chf.areas[i];


                        int ndir = -1;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                            {
                                ndir = dir;
                                break;
                            }
                        }

                        if (ndir != -1)
                        {
                            WalkContour(chf, srcReg, x, y, i, ndir, reg.connections);
                        }
                    }
                }
            }

            Stack<int> stack = new Stack<int>();
            List<int> trace = new List<int>();

            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.id == 0)
                    continue;
                if (reg.spanCount == 0)
                    continue;
                if (reg.visited)
                    continue;


                int spanCount = 0;

                stack.Clear();
                trace.Clear();

                reg.visited = true;
                stack.Push(i);

                while (stack.Count > 0)
                {
                    int ri = stack.Pop();

                    RcRegion creg = regions[ri];

                    spanCount += creg.spanCount;
                    trace.Add(ri);

                    for (int j = 0; j < creg.connections.Count; ++j)
                    {

                        RcRegion neireg = regions[creg.connections[j]];
                        if (neireg.visited)
                            continue;
                        if (neireg.id == 0)
                            continue;

                        //将相邻区域压入stack ，用于计算spanCount
                        stack.Push(neireg.id);
                        neireg.visited = true;
                    }
                }

                //去掉过于小的区域
                if (spanCount < minRegionArea)
                {
                    for (int j = 0; j < trace.Count; ++j)
                    {
                        regions[trace[j]].spanCount = 0;
                        regions[trace[j]].id = 0;
                    }
                }
            }

            int mergeCount = 0;


            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.id == 0)
                    continue;
                if (reg.overlap)
                    continue;
                if (reg.spanCount == 0)
                    continue;


                if (reg.spanCount > mergeRegionArea)
                    continue;

                int smallest = 0xfffffff;
                int mergeId = reg.id;
                for (int j = 0; j < reg.connections.Count; ++j)
                {

                    RcRegion mreg = regions[reg.connections[j]];
                    if (mreg.id == 0 || mreg.overlap) continue;
                    if (mreg.spanCount < smallest &&
                        CanMergeWithRegion(reg, mreg) &&
                        CanMergeWithRegion(mreg, reg))
                    {
                        smallest = mreg.spanCount;
                        mergeId = mreg.id;
                    }
                }

                //存在可以合并
                if (mergeId != reg.id)
                {
                    int oldId = reg.id;
                    RcRegion target = regions[mergeId];


                    if (MergeRegions(target, reg))
                    {

                        for (int j = 0; j < nreg; ++j)
                        {
                            if (regions[j].id == 0) continue;

                            if (regions[j].id == oldId)
                                regions[j].id = mergeId;

                            ReplaceNeighbour(regions[j], oldId, mergeId);
                        }
                        mergeCount++;
                    }
                }
            }

            for (int i = 0; i < nreg; ++i)
            {
                regions[i].remap = false;
                if (regions[i].id == 0)
                {
                    continue;
                }

                regions[i].remap = true;
            }

            int regIdGen = 0;
            for (int i = 0; i < nreg; ++i)
            {
                if (!regions[i].remap)
                    continue;
                int oldId = regions[i].id;
                int newId = ++regIdGen;
                for (int j = i; j < nreg; ++j)
                {
                    if (regions[j].id == oldId)
                    {
                        regions[j].id = newId;
                        regions[j].remap = false;
                    }
                }
            }
            chf.maxRegions = regIdGen;

            //重新命名区域
            for (int i = 0; i < chf.spanCount; ++i)
            {
                if (srcReg[i] > 0)
                {
                    srcReg[i] = regions[srcReg[i]].id;
                }

            }

            // Return regions that we found to be overlapping.
            //for (int i = 0; i < nreg; ++i)
            //    if (regions[i].overlap)
            //        overlaps.push(regions[i].id);
        }

        private static bool MergeRegions(RcRegion rega, RcRegion regb)
        {

            int aid = rega.id;
            int bid = regb.id;

            List<int> acon = new List<int>(rega.connections.Count);
            for (int i = 0; i < rega.connections.Count; ++i)
                acon.Insert(i, rega.connections[i]);

            List<int> bcon = regb.connections;

            //发现a的插入点
            int insa = -1;
            for (int i = 0; i < acon.Count; ++i)
            {
                if (acon[i] == bid)
                {
                    insa = i;
                    break;
                }
            }
            if (insa == -1)
                return false;

            // 发现b的插入点
            int insb = -1;
            for (int i = 0; i < bcon.Count; ++i)
            {
                if (bcon[i] == aid)
                {
                    insb = i;
                    break;
                }
            }
            if (insb == -1)
                return false;


            rega.connections.Clear();

            for (int i = 0, ni = acon.Count; i < ni - 1; ++i)
                rega.connections.Add(acon[(insa + 1 + i) % ni]);

            for (int i = 0, ni = bcon.Count; i < ni - 1; ++i)
                rega.connections.Add(bcon[(insb + 1 + i) % ni]);

            RemoveAdjacentNeighbours(rega);

            for (int j = 0; j < regb.floors.Count; ++j)
                AddUniqueFloorRegion(rega, regb.floors[j]);

            rega.spanCount += regb.spanCount;
            regb.spanCount = 0;
            regb.connections.Clear();

            return true;

        }

        private static void ReplaceNeighbour(RcRegion reg, int oldId, int newId)
        {
            bool neiChanged = false;
            for (int i = 0; i < reg.connections.Count; ++i)
            {
                if (reg.connections[i] == oldId)
                {
                    reg.connections[i] = newId;
                    neiChanged = true;
                }
            }
            for (int i = 0; i < reg.floors.Count; ++i)
            {
                if (reg.floors[i] == oldId)
                    reg.floors[i] = newId;
            }
            if (neiChanged)
                RemoveAdjacentNeighbours(reg);
        }

        private static void AddUniqueFloorRegion(RcRegion reg, int n)
        {
            for (int i = 0; i < reg.floors.Count; ++i)
                if (reg.floors[i] == n)
                    return;
            reg.floors.Add(n);
        }

        private static void WalkContour(RcCompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir, List<int> cont)
        {
            int startDir = dir;
            int starti = i;

            CompactSpan ss = chf.spans[i];
            int curReg = 0;
            if (RecastUtility.RcGetCon(ss, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(ss, dir);
                curReg = srcReg[ai];
            }
            cont.Add(curReg);

            int iter = 0;

            //从如果dir方向遇到边界就尝试顺时针旋转dir继续走，dir方向可走就走一格，然后逆时针旋转dir继续尝试……，
            while (++iter < 40000)
            {
                CompactSpan s = chf.spans[i];

                if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                {

                    int r = 0;
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dir);
                        r = srcReg[ai];
                    }
                    if (r != curReg)
                    {
                        curReg = r;
                        cont.Add(curReg);
                    }

                    dir = (dir + 1) & 0x3;  //顺指针旋转
                }
                else
                {
                    int ni = -1;
                    int nx = x + RecastUtility.RcGetDirOffsetX(dir);
                    int ny = y + RecastUtility.RcGetDirOffsetY(dir);
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        CompactCell nc = chf.cells[nx + ny * chf.width];
                        ni = nc.index + RecastUtility.RcGetCon(s, dir);
                    }
                    if (ni == -1)
                    {
                        return;
                    }
                    x = nx;
                    y = ny;
                    i = ni; //移动一格
                    dir = (dir + 3) & 0x3;  // 逆时针旋转
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }

            //移除相邻的重复项
            if (cont.Count > 1)
            {
                for (int j = 0; j < cont.Count;)
                {
                    int nj = (j + 1) % cont.Count;
                    if (cont[j] == cont[nj])
                    {
                        for (int k = j; k < cont.Count - 1; ++k)
                            cont[k] = cont[k + 1];
                        cont.RemoveAt(cont.Count - 1);
                    }
                    else
                        ++j;
                }
            }
        }

        private static bool CanMergeWithRegion(RcRegion rega, RcRegion regb)
        {

            if (rega.areaType != regb.areaType)
            {
                return false;
            }

            //两个region之间无多处连接
            int n = 0;
            for (int i = 0; i < rega.connections.Count; ++i)
            {
                if (rega.connections[i] == regb.id)
                    n++;
            }
            if (n > 1)
            {
                return false;
            }

            for (int i = 0; i < rega.floors.Count; ++i)
            {
                if (rega.floors[i] == regb.id)
                    return false;
            }
            return true;
        }


        private static bool IsSolidEdge(RcCompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir)
        {
            //相邻的span与自身是不同区域的，则视为边缘
            CompactSpan s = chf.spans[i];
            int r = 0;
            if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dir);
                r = srcReg[ai];
            }
            if (r == srcReg[i])
                return false;
            return true;
        }

        private static void RemoveAdjacentNeighbours(RcRegion reg)
        {
            //移除相邻的重复项
            for (int i = 0; i < reg.connections.Count && reg.connections.Count > 1;)
            {
                int ni = (i + 1) % reg.connections.Count;
                if (reg.connections[i] == reg.connections[ni])
                {

                    for (int j = i; j < reg.connections.Count - 1; ++j)
                        reg.connections[j] = reg.connections[j + 1];
                    reg.connections.RemoveAt(reg.connections.Count - 1);
                }
                else
                    ++i;
            }
        }

        public static void RcBuildContours(RcCompactHeightfield chf, RcContourSet rcContourSet)
        {
            int w = chf.width;
            int h = chf.height;

            int[] flags = new int[chf.spanCount];

            List<int> verts = new List<int>();
            List<int> simplified = new List<int>();

            rcContourSet.numConts = 0;

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        int res = 0;

                        CompactSpan s = chf.spans[i];

                        if (chf.spans[i].reg == 0)
                        {
                            flags[i] = 0;
                            continue;
                        }

                        //遍历判断每个span与邻居span的关系，如果region相同则记录在该dir方向上连通，不连通则该dir方向为边界
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            int r = 0;
                            if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                                int ai = (int)chf.cells[ax + ay * w].index + RecastUtility.RcGetCon(s, dir);
                                r = chf.spans[ai].reg;
                            }

                            //相同region，则连通，连通标记为1
                            if (r == chf.spans[i].reg)
                                res |= (1 << dir);
                        }
                        //异或，反转标志，连通标记为0，不连通标记为1
                        flags[i] = res ^ 0xf;
                    }
                }
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        //全连通和全不连通的点都无需考虑
                        if (flags[i] == 0 || flags[i] == 0xf)
                        {
                            flags[i] = 0;
                            continue;
                        }
                        int reg = chf.spans[i].reg;
                        if (reg == 0)
                            continue;

                        AREATYPE area = chf.areas[i];

                        verts.Clear();
                        simplified.Clear();

                        //遍历边缘，处理两个区域之间重复的边缘
                        WalkContourPoint(x, y, i, chf, flags, verts);

                        //简化边缘
                        SimplifyContourPoint(verts, simplified);

                        if (simplified.Count / 4 >= 3)
                        {

                            RcContour cont = new RcContour
                            {
                                nverts = simplified.Count / 4,
                                verts = simplified.ToArray(),
                                reg = reg,
                                area = area
                            };

                            rcContourSet.conts.Add(cont);
                            rcContourSet.numConts++;
                        }
                    }
                }
            }

            //打通空洞
            if (rcContourSet.numConts > 0)
            {

                int[] winding = new int[rcContourSet.numConts];

                int nholes = 0;
                for (int i = 0; i < rcContourSet.numConts; ++i)
                {
                    RcContour cont = rcContourSet.conts[i];

                    //只要根据叉乘算出每个轮廓多边形的有向面积，如果结果为小于0，则为轮廓点的顺序为逆时针，这个轮廓就是一个空洞。
                    winding[i] = CalcAreaOfPolygon2D(cont.verts, cont.nverts) < 0 ? -1 : 1;
                    if (winding[i] < 0)
                        nholes++;
                }

                if (nholes > 0)
                {
                    int nregions = chf.maxRegions;

                    RcContourRegion[] regions = new RcContourRegion[nregions];

                    for (int i = 0; i < rcContourSet.numConts; ++i)
                    {
                        RcContour cont = rcContourSet.conts[i];

                        if (winding[i] > 0)
                        {
                            if (regions[cont.reg] != null)
                                RecastUtility.LogErrorFormat("rcBuildContours: Multiple outlines for region %d.", cont.reg);

                            regions[cont.reg] = new RcContourRegion
                            {
                                outline = cont
                            };
                        }
                        else
                        {
                            regions[cont.reg].numHoles++;
                        }
                    }

                    //区域id从1开始
                    for (int i = 1; i < nregions; i++)
                    {
                        if (regions[i] != null && regions[i].numHoles > 0)
                        {
                            regions[i].holes = new RcContourHole[regions[i].numHoles];
                            regions[i].numHoles = 0;
                        }
                    }

                    for (int i = 0; i < rcContourSet.numConts; ++i)
                    {
                        RcContour cont = rcContourSet.conts[i];

                        RcContourRegion reg = regions[cont.reg];

                        if (winding[i] < 0)
                        {
                            reg.holes[reg.numHoles] = new RcContourHole
                            {
                                contour = cont
                            };
                            reg.numHoles++;
                        }

                    }

                    //合并空洞
                    for (int i = 1; i < nregions; i++)
                    {
                        RcContourRegion reg = regions[i];
                        if (reg.numHoles == 0) continue;

                        if (reg.outline != null)
                        {
                            MergeRegionHoles(reg);
                        }
                        else
                        {
                            RecastUtility.LogErrorFormat("rcBuildContours: Bad outline for region %d, contour simplification is likely too aggressive.", i);
                        }
                    }
                }

            }
        }


        private static void MergeRegionHoles(RcContourRegion region)
        {
            for (int i = 0; i < region.numHoles; i++)
            {
                FindLeftMostVertex(region.holes[i].contour, out region.holes[i].minX, out region.holes[i].minZ, out region.holes[i].leftMost);
            }

            System.Array.Sort(region.holes, CompareHoles);

            int maxVerts = region.outline.nverts;

            for (int i = 0; i < region.numHoles; i++)
            {
                maxVerts += region.holes[i].contour.nverts;
            }

            RcContour outline = region.outline;

            List<RcPotentialDiagonal> diags = new List<RcPotentialDiagonal>();

            for (int i = 0; i < region.numHoles; i++)
            {
                RcContour hole = region.holes[i].contour;


                int index = -1;
                int bestVertex = region.holes[i].leftMost;
                for (int iter = 0; iter < hole.nverts; iter++)
                {
                    //找到空洞的bestVertex（bestVetex为最左边的点，如果有多个最左点，取其中最下的点），并把N个空洞按照bestVertex排序，按照排序好的空洞顺序遍历。
                    int ndiags = 0;
                    int[] corner = { hole.verts[bestVertex * 4], hole.verts[bestVertex * 4 + 1], hole.verts[bestVertex * 4 + 2], hole.verts[bestVertex * 4 + 3] };
                    
                    for (int j = 0; j < outline.nverts; j++)
                    {
                        int piIndex = j * 4;
                        int[] pi = { outline.verts[piIndex], outline.verts[piIndex + 1], outline.verts[piIndex + 2] };

                        int pi1Index =RecastUtility.Next(j, outline.nverts);
                        int[] pi1 = { outline.verts[pi1Index], outline.verts[pi1Index + 1], outline.verts[pi1Index + 2] };

                        int pin1Index = RecastUtility.Prev(j, outline.nverts);
                        int[] pin1 = { outline.verts[pin1Index], outline.verts[pin1Index + 1], outline.verts[pin1Index + 2] };

                        if (RecastUtility.InCone(pi, pi1, pin1, corner))
                        {
                            int dx = outline.verts[j * 4 + 0] - corner[0];
                            int dz = outline.verts[j * 4 + 2] - corner[2];
                            diags.Add(new RcPotentialDiagonal(j, dx * dx + dz * dz));
                            ndiags++;
                        }
                    }

                    diags.Sort(CompareDiagDist);

                    index = -1;
                    for (int j = 0; j < ndiags; j++)
                    {
                        int pt = diags[j].vert * 4;
                        //是否和与外轮廓相交
                        bool intersect = IntersectSegContour(pt, corner, diags[i].vert, outline.nverts, outline.verts);

                        //是否和其他空洞相交
                        for (int k = i; k < region.numHoles && !intersect; k++)
                            intersect |= IntersectSegContour(pt, corner, -1, region.holes[k].contour.nverts, region.holes[k].contour.verts);
                        if (!intersect)
                        {
                            index = diags[j].vert;
                            break;
                        }
                    }

                    //只有与外轮廓和其他空洞都不相交，才会对index赋值,
                    //选择距离最短并且两点连线与外轮廓和所有空洞没有交点的,把此时的bestVertex和外轮廓点的索引作为开口位置
                    if (index != -1)
                        break;

                    // 尝试下一个点
                    bestVertex = (bestVertex + 1) % hole.nverts;
                }

                if (index == -1)
                {
                    RecastUtility.LogError("mergeHoles: Failed to find merge points");
                    continue;
                }
                if (!MergeContours(region.outline, hole, index, bestVertex))
                {
                    RecastUtility.LogError("mergeHoles: Failed to merge contours %p and %p.");
                    continue;
                }

            }
        }

        private static bool MergeContours(RcContour ca, RcContour cb, int ia, int ib)
        {
            int maxVerts = ca.nverts + cb.nverts + 2;
            int[] verts = new int[maxVerts * 4];

            int nv = 0;

            for (int i = 0; i <= ca.nverts; ++i)
            {
                int dstIndex = nv * 4;
                int srcIndex = ((ia + i) % ca.nverts) * 4;
                verts[dstIndex] = ca.verts[srcIndex];
                verts[dstIndex + 1] = ca.verts[srcIndex + 1];
                verts[dstIndex + 2] = ca.verts[srcIndex + 2];
                verts[dstIndex + 3] = ca.verts[srcIndex + 3];
                nv++;
            }

            for (int i = 0; i <= cb.nverts; ++i)
            {
                int dstIndex = nv * 4;
                int srcIndex = ((ib + i) % cb.nverts) * 4;
                verts[dstIndex] = cb.verts[srcIndex];
                verts[dstIndex + 1] = cb.verts[srcIndex + 1];
                verts[dstIndex + 2] = cb.verts[srcIndex + 2];
                verts[dstIndex + 3] = cb.verts[srcIndex + 3];
                nv++;
            }

            ca.verts = verts;
            ca.nverts = nv;

            cb.verts = new int[0];
            cb.nverts = 0;

            return true;
        }


        private static void FindLeftMostVertex(RcContour contour, out int minx, out int minz, out int leftmost)
        {
            minx = contour.verts[0];
            minz = contour.verts[2];
            leftmost = 0;
            for (int i = 1; i < contour.nverts; i++)
            {
                int x = contour.verts[i * 4 + 0];
                int z = contour.verts[i * 4 + 2];
                if (x < minx || (x == minx && z < minz))
                {
                    minx = x;
                    minz = z;
                    leftmost = i;
                }
            }
        }

        private static int CompareHoles(RcContourHole va, RcContourHole vb)
        {

            if (va.minX == vb.minX)
            {
                if (va.minZ < vb.minZ)
                    return -1;
                if (va.minZ > vb.minZ)
                    return 1;
            }
            else
            {
                if (va.minX < vb.minX)
                    return -1;
                if (va.minX > vb.minX)
                    return 1;
            }
            return 0;
        }


        private static int CompareDiagDist(RcPotentialDiagonal a, RcPotentialDiagonal b)
        {

            if (a.dist < b.dist)
                return -1;
            if (a.dist > b.dist)
                return 1;
            return 0;
        }


        private static bool Vequal(int[] a, int[] b)
        {
            return a[0] == b[0] && a[2] == b[2];
        }


        //是否于边缘相交
        public static bool IntersectSegContour(int d0Index, int[] d1, int i, int n, int[] verts)
        {
            int[] d0 = { verts[d0Index], verts[d0Index + 1], verts[d0Index + 2], verts[d0Index + 3] };

            //遍历 （k，k+1)
            for (int k = 0; k < n; k++)
            {
                int k1 = RecastUtility.Next(k, n);
                //跳过当前点所在边缘
                if (i == k || i == k1)
                    continue;

                int[] p0 = { verts[k], verts[k + 1], verts[k + 2], verts[k + 3] };
                int[] p1 = { verts[k1], verts[k1 + 1], verts[k1 + 2], verts[k1 + 3] };

                if (Vequal(d0, p0) || Vequal(d1, p0) || Vequal(d0, p1) || Vequal(d1, p1))
                    continue;

                if (RecastUtility.Intersect(d0, d1, p0, p1))
                    return true;
            }
            return false;
        }

        private static void WalkContourPoint(int x, int y, int i, RcCompactHeightfield chf, int[] flags, List<int> points)
        {
            int dir = 0;
            //找第一个边界
            while ((flags[i] & (1 << dir)) == 0)
                dir++;

            int startDir = dir;
            int starti = i;

            AREATYPE area = chf.areas[i];

            int iter = 0;
            while (++iter < 40000)
            {
                // dir方向是边界，则保存轮廓点后，顺时针旋转后再循环尝试
                if ((flags[i] & (1 << dir)) != 0)
                {

                    bool isAreaBorder = false;
                    int px = x;
                    int py = GetCornerHeight(x, y, i, dir, chf);
                    int pz = y;

                    //span左方是边界，轮廓点取其上方span坐标；
                    //span上方是边界，轮廓点取其右上方span坐标；
                    //span右方是边界，轮廓点取其右方span坐标；
                    //span下方是边界，轮廓点取其自身span坐标。
                    switch (dir)
                    {
                        case 0: pz++; break;
                        case 1: px++; pz++; break;
                        case 2: px++; break;
                    }
                    int r = 0;

                    CompactSpan s = chf.spans[i];
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dir);
                        r = chf.spans[ai].reg;
                        if (area != chf.areas[ai])
                            isAreaBorder = true;
                    }

                    if (isAreaBorder)
                        r |= RecastConfig.RC_AREA_BORDER;


                    points.Add(px);
                    points.Add(py);
                    points.Add(pz);
                    points.Add(r);

                    // 去掉该dir上的边界标记
                    flags[i] &= ~(1 << dir);
                    // 顺时针旋转dir
                    dir = (dir + 1) & 0x3;
                }
                // 如果不是边界，则移动x y至这个方向，并将dir逆时针旋转
                else
                {
                    int ni = -1;

                    int nx = x + RecastUtility.RcGetDirOffsetX(dir);
                    int ny = y + RecastUtility.RcGetDirOffsetY(dir);
                    CompactSpan s = chf.spans[i];
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        CompactCell nc = chf.cells[nx + ny * chf.width];
                        ni = nc.index + RecastUtility.RcGetCon(s, dir);
                    }
                    if (ni == -1)
                    {
                        return;
                    }
                    x = nx;
                    y = ny;
                    i = ni;
                    // 逆时针旋转di
                    dir = (dir + 3) & 0x3;
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }
        }

        private static void SimplifyContourPoint(List<int> points, List<int> simplified)
        {
            bool hasConnections = false;
            for (int i = 0; i < points.Count; i += 4)
            {
                if ((points[i + 3] & RecastConfig.RC_CONTOUR_REG_MASK) != 0)
                {
                    hasConnections = true;
                    break;
                }
            }

            if (hasConnections)
            {
                //初始简化点条件
                //当前轮廓点邻近一个region，下一个轮廓点邻接另一个region
                //当前轮廓点邻接一个region，下一个轮廓点邻接不可行走的边界
                for (int i = 0, ni = points.Count / 4; i < ni; ++i)
                {
                    int ii = (i + 1) % ni;
                    bool differentRegs = (points[i * 4 + 3] & RecastConfig.RC_CONTOUR_REG_MASK) != (points[ii * 4 + 3] & RecastConfig.RC_CONTOUR_REG_MASK);
                    bool areaBorders = (points[i * 4 + 3] & RecastConfig.RC_AREA_BORDER) != (points[ii * 4 + 3] & RecastConfig.RC_AREA_BORDER);
                    if (differentRegs || areaBorders)
                    {
                        simplified.Add(points[i * 4 + 0]);
                        simplified.Add(points[i * 4 + 1]);
                        simplified.Add(points[i * 4 + 2]);
                        simplified.Add(i);
                    }
                }
            }

            if (simplified.Count == 0)
            {
                //没有满足条件的点，此时从轮廓点中选择最左下和最右上的两个点作为初始简化点。
                int llx = points[0];
                int lly = points[1];
                int llz = points[2];
                int lli = 0;
                int urx = points[0];
                int ury = points[1];
                int urz = points[2];
                int uri = 0;
                for (int i = 0; i < points.Count; i += 4)
                {
                    int x = points[i + 0];
                    int y = points[i + 1];
                    int z = points[i + 2];
                    if (x < llx || (x == llx && z < llz))
                    {
                        llx = x;
                        lly = y;
                        llz = z;
                        lli = i / 4;
                    }
                    if (x > urx || (x == urx && z > urz))
                    {
                        urx = x;
                        ury = y;
                        urz = z;
                        uri = i / 4;
                    }
                }
                simplified.Add(llx);
                simplified.Add(lly);
                simplified.Add(llz);
                simplified.Add(lli);

                simplified.Add(urx);
                simplified.Add(ury);
                simplified.Add(urz);
                simplified.Add(uri);
            }


            int pn = points.Count / 4;
            for (int i = 0; i < simplified.Count / 4;)
            {
                int ii = (i + 1) % (simplified.Count / 4);

                int ax = simplified[i * 4 + 0];
                int az = simplified[i * 4 + 2];
                int ai = simplified[i * 4 + 3];

                int bx = simplified[ii * 4 + 0];
                int bz = simplified[ii * 4 + 2];
                int bi = simplified[ii * 4 + 3];

                float maxd = 0;
                int maxi = -1;
                int ci, cinc, endi;


                if (bx > ax || (bx == ax && bz > az))
                {
                    cinc = 1;
                    ci = (ai + cinc) % pn;
                    endi = bi;
                }
                else
                {
                    cinc = pn - 1;
                    ci = (bi + cinc) % pn;
                    endi = ai;
                    RecastUtility.RcSwap(ref ax, ref bx);
                    RecastUtility.RcSwap(ref az, ref bz);
                }

                // 不可行走边界或者region边界
                if ((points[ci * 4 + 3] & RecastConfig.RC_CONTOUR_REG_MASK) == 0 ||
                    (points[ci * 4 + 3] & RecastConfig.RC_AREA_BORDER) != 0)
                {
                    while (ci != endi)
                    {
                        float d = RecastUtility.DistancePtSeg2D(points[ci * 4 + 0], points[ci * 4 + 2], ax, az, bx, bz);
                        if (d > maxd)
                        {
                            maxd = d;
                            maxi = ci;
                        }
                        ci = (ci + cinc) % pn;
                    }
                }

                if (maxi != -1 && maxd > (RecastConfig.MaxSimplificationError * RecastConfig.MaxSimplificationError))
                {

                    // simplified i索引后面的point后移
                    simplified.Insert((i + 1) * 4 + 0, points[maxi * 4 + 0]);
                    simplified.Insert((i + 1) * 4 + 1, points[maxi * 4 + 1]);
                    simplified.Insert((i + 1) * 4 + 2, points[maxi * 4 + 2]);
                    simplified.Insert((i + 1) * 4 + 3, maxi);

                    // i不++，下次遍历还是从i开始，ii为新插入的point
                }
                else
                {
                    ++i;
                }
            }

            //切割过长的边缘
            if (RecastConfig.MaxEdgeLen > 0 && (RecastConfig.TessellateWallEdges | RecastConfig.TessellateAreaEdges))
            {
                for (int i = 0; i < simplified.Count / 4;)
                {
                    int ii = (i + 1) % (simplified.Count / 4);

                    int ax = simplified[i * 4 + 0];
                    int az = simplified[i * 4 + 2];
                    int ai = simplified[i * 4 + 3];

                    int bx = simplified[ii * 4 + 0];
                    int bz = simplified[ii * 4 + 2];
                    int bi = simplified[ii * 4 + 3];

                    int maxi = -1;
                    int ci = (ai + 1) % pn;

                    bool tess = false;
                    //不可行走的边缘
                    if (RecastConfig.TessellateWallEdges && (points[ci * 4 + 3] & RecastConfig.RC_CONTOUR_REG_MASK) == 0)
                        tess = true;
                    // 区域之间的边缘
                    if (RecastConfig.TessellateAreaEdges && ((points[ci * 4 + 3] & RecastConfig.RC_AREA_BORDER) != 0))
                        tess = true;

                    if (tess)
                    {
                        int dx = bx - ax;
                        int dz = bz - az;
                        if (dx * dx + dz * dz > RecastConfig.MaxEdgeLen * RecastConfig.MaxEdgeLen)
                        {

                            //选取a，b的中点
                            int n = bi < ai ? (bi + pn - ai) : (bi - ai);
                            if (n > 1)
                            {
                                if (bx > ax || (bx == ax && bz > az))
                                    maxi = (ai + n / 2) % pn;
                                else
                                    maxi = (ai + (n + 1) / 2) % pn;
                            }
                        }
                    }


                    //插入新的边缘点
                    if (maxi != -1)
                    {

                        simplified.Insert((i + 1) * 4 + 0, points[maxi * 4 + 0]);
                        simplified.Insert((i + 1) * 4 + 1, points[maxi * 4 + 1]);
                        simplified.Insert((i + 1) * 4 + 2, points[maxi * 4 + 2]);
                        simplified.Insert((i + 1) * 4 + 3, maxi);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            for (int i = 0; i < simplified.Count / 4; ++i)
            {
                //不可行走边缘顶点标志取自当前原始点，
                //相邻区域边缘顶点标志取自下一个原始点。
                int ai = (simplified[i * 4 + 3] + 1) % pn;
                int bi = simplified[i * 4 + 3];
                simplified[i * 4 + 3] = (points[ai * 4 + 3] & (RecastConfig.RC_CONTOUR_REG_MASK | RecastConfig.RC_AREA_BORDER));
            }

            //去除重复的点
            int npts = simplified.Count / 4;
            for (int i = 0; i < npts; ++i)
            {
                int ni = RecastUtility.Next(i, npts);

                bool vequal = simplified[i * 4] == simplified[ni * 4] &&
                    simplified[i * 4 + 1] == simplified[ni * 4 + 1] &&
                    simplified[i * 4 + 2] == simplified[ni * 4 + 2] &&
                     simplified[i * 4 + 3] == simplified[ni * 4 + 3];

                if (vequal)
                {
                    for (int j = i; j < simplified.Count / 4 - 1; ++j)
                    {
                        simplified[j * 4 + 0] = simplified[(j + 1) * 4 + 0];
                        simplified[j * 4 + 1] = simplified[(j + 1) * 4 + 1];
                        simplified[j * 4 + 2] = simplified[(j + 1) * 4 + 2];
                        simplified[j * 4 + 3] = simplified[(j + 1) * 4 + 3];
                    }
                    npts--;
                }
            }

        }
        private static int GetCornerHeight(int x, int y, int i, int dir, RcCompactHeightfield chf)
        {

            CompactSpan s = chf.spans[i];
            int ch = s.y;
            // 逆时针旋转di
            int dirp = (dir + 1) & 0x3;

            int[] regs = { 0, 0, 0, 0 };

            regs[0] = chf.spans[i].reg | ((int)chf.areas[i] << 16);

            //取周围4个span的最大y作为边界的y
            if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dir);
                CompactSpan as1 = chf.spans[ai];
                ch = Math.Max(ch, as1.y);
                regs[1] = chf.spans[ai].reg | ((int)chf.areas[ai] << 16);

                if (RecastUtility.RcGetCon(as1, dirp) != RecastConfig.RC_NOT_CONNECTED)
                {
                    int ax2 = ax + RecastUtility.RcGetDirOffsetX(dirp);
                    int ay2 = ay + RecastUtility.RcGetDirOffsetY(dirp);
                    int ai2 = (int)chf.cells[ax2 + ay2 * chf.width].index + RecastUtility.RcGetCon(as1, dirp);
                    CompactSpan as2 = chf.spans[ai2];
                    ch = Math.Max(ch, as2.y);
                    regs[2] = chf.spans[ai2].reg | ((int)chf.areas[ai2] << 16);
                }
            }

            if (RecastUtility.RcGetCon(s, dirp) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dirp);
                int ay = y + RecastUtility.RcGetDirOffsetY(dirp);
                int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dirp);
                CompactSpan as1 = chf.spans[ai];
                ch = Math.Max(ch, as1.y);
                regs[3] = chf.spans[ai].reg | ((int)chf.areas[ai] << 16);

                if (RecastUtility.RcGetCon(as1, dir) != RecastConfig.RC_NOT_CONNECTED)
                {
                    int ax2 = ax + RecastUtility.RcGetDirOffsetX(dir);
                    int ay2 = ay + RecastUtility.RcGetDirOffsetY(dir);
                    int ai2 = (int)chf.cells[ax2 + ay2 * chf.width].index + RecastUtility.RcGetCon(as1, dir);
                    CompactSpan as2 = chf.spans[ai2];
                    ch = Math.Max(ch, as2.y);
                    regs[2] = chf.spans[ai2].reg | ((int)chf.areas[ai2] << 16);
                }
            }

            return ch;
        }

        public static int CalcAreaOfPolygon2D(int[] verts, int numVerts)
        {
            int area = 0;
            for (int i = 0, j = numVerts - 1; i < numVerts; j = i++)
            {
                area += verts[i * 4] * verts[j * 4 + 2] - verts[j * 4] * verts[i * 4 + 2];
            }
            return (area + 1) / 2;
        }

    }


}
