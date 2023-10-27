
using System.Collections.Generic;
using UnityEngine;

namespace GameEditor.RecastEditor
{
    internal class RecastContour
    {
        public static void RcBuildRegions(CompactHeightfield compactHeightfield)
        {

            const int expandIters = 8;
            const int loglevelsPerStack = 1;//决定了每组的距离
            const int LOG_NB_STACKS = 3;
            const int NB_STACKS = 1 << LOG_NB_STACKS; //决定了每批有8组

            int regionId = 1;

            //使最低一位为0，使得level为2的倍数
            int level = (compactHeightfield.MaxDistance + 1) & (~1);

            int sId = -1;

            Stack<LevelStackEntry>[] lvlStacks = new Stack<LevelStackEntry>[NB_STACKS];
            Stack<LevelStackEntry> stack = new Stack<LevelStackEntry>();

            for (int i = 0; i < NB_STACKS; i++)
            {
                lvlStacks[i] = new Stack<LevelStackEntry>();
            }

            int[] srcReg = new int[compactHeightfield.SpanCount];
            int[] srcDist = new int[compactHeightfield.SpanCount];

            for (int i = 0; i < compactHeightfield.SpanCount; i++)
            {
                srcReg[i] = 0;
                srcDist[i] = 0;
            }

            while (level > 0)
            {
                level = level >= 2 ? level - 2 : 0;
                sId = (sId + 1) & (NB_STACKS - 1);

                //分批划分level层级，ps话说我感觉可以直接一次性划分完啊，为什么要分批去划分
                if (sId == 0)
                    SortCellsByLevel(level, compactHeightfield, srcReg, NB_STACKS, lvlStacks, loglevelsPerStack);
                else
                    AppendStacks(lvlStacks[sId - 1], lvlStacks[sId], srcReg);

                //从水源进行蔓延 
                ExpandRegions(expandIters, level, compactHeightfield, srcReg, srcDist, lvlStacks[sId], false);

                //寻找水源
                foreach (LevelStackEntry lvlStackEntry in lvlStacks[sId])
                {
                    int x = lvlStackEntry.X;
                    int y = lvlStackEntry.Y;
                    int i = lvlStackEntry.Index;
                    if (i >= 0 && srcReg[i] == 0)
                    {
                        if (FloodRegion(x, y, i, level, regionId, compactHeightfield, srcReg, srcDist, stack))
                        {
                            if (regionId == 0xFFFF)
                            {
                                Debug.LogError("rcBuildRegions: Region ID overflow");
                            }

                            regionId++;
                        }
                    }

                }

            }

            ExpandRegions(expandIters * 8, 0, compactHeightfield, srcReg, srcDist, stack, false);

            //合并区域
            compactHeightfield.MaxRegions = regionId;

            MergeAndFilterRegions(compactHeightfield, srcReg);

            for (int i = 0; i < compactHeightfield.SpanCount; ++i)
                compactHeightfield.SpanList[i].Reg = srcReg[i];

        }

        //loglevelsPerStack 决定距离多少为一个level
        private static void SortCellsByLevel(int startLevel, CompactHeightfield compactHeightfield, int[] srcReg, int nbStacks, Stack<LevelStackEntry>[] stacks, int loglevelsPerStack)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;
            startLevel = startLevel >> loglevelsPerStack;

            for (int i = 0; i < nbStacks; i++)
            {
                stacks[i].Clear();
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {
                        if (compactHeightfield.AreaList[i] == AREATYPE.None || srcReg[i] != 0)
                            continue;

                        int level = compactHeightfield.DistanceToBoundary[i] >> loglevelsPerStack;
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
                int i = srcEntry.Index;
                if ((i < 0) || (srcReg[i] != 0))
                    continue;
                dstStack.Push(srcEntry);
            }
        }

        private static void ExpandRegions(int maxIter, int level, CompactHeightfield compactHeightfield, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack, bool fillStack)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;

            if (fillStack)
            {
                stack.Clear();

                for (int y = 0; y < h; ++y)
                {
                    for (int x = 0; x < w; ++x)
                    {
                        CompactCell c = compactHeightfield.CellList[x + y * w];
                        for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                        {
                            if (compactHeightfield.DistanceToBoundary[i] >= level && srcReg[i] == 0 && compactHeightfield.AreaList[i] != AREATYPE.None)
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
                    int i = entry.Index;
                    if (srcReg[i] != 0)
                        entry.Index = -1;
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
                    int x = entry.X;
                    int z = entry.Y;
                    int spanIndex = entry.Index;
                    if (spanIndex < 0)
                    {
                        failed++;
                        continue;
                    }

                    int r = srcReg[spanIndex];
                    int d2 = 0xffff;
                    AREATYPE area = compactHeightfield.AreaList[spanIndex];
                    CompactSpan span = compactHeightfield.SpanList[spanIndex];

                    for (int direction = 0; direction < 4; ++direction)
                    {
                        int neighborConnection = RecastUtility.RcGetCon(span, direction);
                        int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        if (compactHeightfield.AreaList[neighborSpanIndex] != compactHeightfield.AreaList[spanIndex])
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
                        entry.Index = -1; //标记已经使用了
                        dirtyEntries.Push(new DirtyEntry(spanIndex, r, d2));
                    }
                    else
                    {
                        failed++;
                    }

                }

                foreach (DirtyEntry dirtyEntery in dirtyEntries)
                {
                    int idx = dirtyEntery.Index;
                    srcReg[idx] = dirtyEntery.Region;
                    srcDist[idx] = dirtyEntery.Distance2;

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

        private static bool FloodRegion(int x, int y, int i, int level, int r, CompactHeightfield compactHeightfield, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack)
        {
            int w = compactHeightfield.Width;
            AREATYPE area = compactHeightfield.AreaList[i];

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
                int cx = back.X;
                int cy = back.Y;
                int ci = back.Index;

                CompactSpan span = compactHeightfield.SpanList[ci];
                int ar = 0;

                //8向遍历相邻span 如果相邻span属于其他region，则自身region标记为0
                for (int direction = 0; direction < 4; ++direction)
                {

                    int neighborConnection = RecastUtility.RcGetCon(span, direction);
                    if (neighborConnection != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int neighborX = cx + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = cy + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        //不在同一个area
                        if (compactHeightfield.AreaList[neighborSpanIndex] != area)
                            continue;

                        int nr = srcReg[ci];

                        //属于已标记region，且不是当前的regionId
                        if (nr != 0 && nr != r)
                        {
                            ar = nr;
                            break;
                        }


                        int direction2 = (direction + 1) & 0x3;
                        CompactSpan neighborSpan = compactHeightfield.SpanList[neighborSpanIndex];
                        int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                        if (neighbor2Connection != RecastConfig.RC_NOT_CONNECTED)
                        {
                            int neighbor2X = neighborX + RecastUtility.RcGetDirOffsetX(direction2);
                            int neighbor2Z = neighborZ + RecastUtility.RcGetDirOffsetY(direction2);
                            int neighbor2SpanIndex = compactHeightfield.CellList[neighbor2X + neighbor2Z * w].Index + neighbor2Connection;

                            if (compactHeightfield.AreaList[neighbor2SpanIndex] != area)
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
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        if (compactHeightfield.AreaList[neighborSpanIndex] != area)
                            continue;

                        if (compactHeightfield.DistanceToBoundary[neighborSpanIndex] > lev && srcReg[neighborSpanIndex] == 0)
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

        private static void MergeAndFilterRegions(CompactHeightfield compactHeightfield, int[] srcReg)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;

            int nreg = compactHeightfield.MaxRegions + 1;

            RcRegion[] regions = new RcRegion[compactHeightfield.MaxRegions + 1];
            for (int i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion(i);
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {
                        int r = srcReg[i];
                        if (r == 0 || r >= nreg)
                            continue;

                        RcRegion reg = regions[r];
                        reg.SpanCount++;

                        // 寻找相同xz平面位置的上方的span的区域
                        for (int j = c.Index; j < ni; ++j)
                        {
                            if (i == j) continue;
                            int floorId = srcReg[j];
                            if (floorId == 0 || floorId >= nreg)
                                continue;
                            if (floorId == r)
                                reg.Overlap = true;
                            AddUniqueFloorRegion(reg, floorId);
                        }

                        //区域已经计算完连通区域了，不用再计算了
                        if (reg.Connections.Count > 0)
                            continue;

                        reg.AreaType = compactHeightfield.AreaList[i];


                        int ndir = -1;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (IsSolidEdge(compactHeightfield, srcReg, x, y, i, dir))
                            {
                                ndir = dir;
                                break;
                            }
                        }

                        if (ndir != -1)
                        {
                            WalkContour(compactHeightfield, srcReg, x, y, i, ndir, reg.Connections);
                        }
                    }
                }
            }

            Stack<int> stack = new Stack<int>();
            List<int> trace = new List<int>();

            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.Id == 0)
                    continue;
                if (reg.SpanCount == 0)
                    continue;
                if (reg.Visited)
                    continue;


                int spanCount = 0;

                stack.Clear();
                trace.Clear();

                reg.Visited = true;
                stack.Push(i);

                while (stack.Count > 0)
                {
                    int ri = stack.Pop();

                    RcRegion creg = regions[ri];

                    spanCount += creg.SpanCount;
                    trace.Add(ri);

                    for (int j = 0; j < creg.Connections.Count; ++j)
                    {

                        RcRegion neireg = regions[creg.Connections[j]];
                        if (neireg.Visited)
                            continue;
                        if (neireg.Id == 0)
                            continue;

                        //将相邻区域压入stack ，用于计算spanCount
                        stack.Push(neireg.Id);
                        neireg.Visited = true;
                    }
                }

                //去掉过于小的区域
                if (spanCount < RecastConfig.MinRegionArea)
                {
                    for (int j = 0; j < trace.Count; ++j)
                    {
                        regions[trace[j]].SpanCount = 0;
                        regions[trace[j]].Id = 0;
                    }
                }
            }

            int mergeCount = 0;


            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.Id == 0)
                    continue;
                if (reg.Overlap)
                    continue;
                if (reg.SpanCount == 0)
                    continue;


                if (reg.SpanCount > RecastConfig.MergeRegionArea)
                    continue;

                int smallest = 0xfffffff;
                int mergeId = reg.Id;
                for (int j = 0; j < reg.Connections.Count; ++j)
                {

                    RcRegion mreg = regions[reg.Connections[j]];
                    if (mreg.Id == 0 || mreg.Overlap) continue;
                    if (mreg.SpanCount < smallest &&
                        CanMergeWithRegion(reg, mreg) &&
                        CanMergeWithRegion(mreg, reg))
                    {
                        smallest = mreg.SpanCount;
                        mergeId = mreg.Id;
                    }
                }

                //存在可以合并
                if (mergeId != reg.Id)
                {
                    int oldId = reg.Id;
                    RcRegion target = regions[mergeId];


                    if (MergeRegions(target, reg))
                    {

                        for (int j = 0; j < nreg; ++j)
                        {
                            if (regions[j].Id == 0) continue;

                            if (regions[j].Id == oldId)
                                regions[j].Id = mergeId;

                            ReplaceNeighbour(regions[j], oldId, mergeId);
                        }
                        mergeCount++;
                    }
                }
            }

            for (int i = 0; i < nreg; ++i)
            {
                regions[i].Remap = false;
                if (regions[i].Id == 0)
                {
                    continue;
                }

                regions[i].Remap = true;
            }

            int regIdGen = 0;
            for (int i = 0; i < nreg; ++i)
            {
                if (!regions[i].Remap)
                    continue;
                int oldId = regions[i].Id;
                int newId = ++regIdGen;
                for (int j = i; j < nreg; ++j)
                {
                    if (regions[j].Id == oldId)
                    {
                        regions[j].Id = newId;
                        regions[j].Remap = false;
                    }
                }
            }
            compactHeightfield.MaxRegions = regIdGen;

            //重新命名区域
            for (int i = 0; i < compactHeightfield.SpanCount; ++i)
            {
                if (srcReg[i] > 0)
                {
                    srcReg[i] = regions[srcReg[i]].Id;
                }

            }

            // Return regions that we found to be overlapping.
            //for (int i = 0; i < nreg; ++i)
            //    if (regions[i].Overlap)
            //        overlaps.push(regions[i].id);
        }

        private static bool MergeRegions(RcRegion rega, RcRegion regb)
        {

            int aid = rega.Id;
            int bid = regb.Id;

            List<int> acon = new List<int>(rega.Connections.Count);
            for (int i = 0; i < rega.Connections.Count; ++i)
                acon.Insert(i, rega.Connections[i]);

            List<int> bcon = regb.Connections;

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


            rega.Connections.Clear();

            for (int i = 0, ni = acon.Count; i < ni - 1; ++i)
                rega.Connections.Add(acon[(insa + 1 + i) % ni]);

            for (int i = 0, ni = bcon.Count; i < ni - 1; ++i)
                rega.Connections.Add(bcon[(insb + 1 + i) % ni]);

            RemoveAdjacentNeighbours(rega);

            for (int j = 0; j < regb.Floors.Count; ++j)
                AddUniqueFloorRegion(rega, regb.Floors[j]);

            rega.SpanCount += regb.SpanCount;
            regb.SpanCount = 0;
            regb.Connections.Clear();

            return true;

        }

        private static void ReplaceNeighbour(RcRegion reg, int oldId, int newId)
        {
            bool neiChanged = false;
            for (int i = 0; i < reg.Connections.Count; ++i)
            {
                if (reg.Connections[i] == oldId)
                {
                    reg.Connections[i] = newId;
                    neiChanged = true;
                }
            }
            for (int i = 0; i < reg.Floors.Count; ++i)
            {
                if (reg.Floors[i] == oldId)
                    reg.Floors[i] = newId;
            }
            if (neiChanged)
                RemoveAdjacentNeighbours(reg);
        }

        private static void AddUniqueFloorRegion(RcRegion reg, int n)
        {
            for (int i = 0; i < reg.Floors.Count; ++i)
                if (reg.Floors[i] == n)
                    return;
            reg.Floors.Add(n);
        }

        private static void WalkContour(CompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir, List<int> cont)
        {
            int startDir = dir;
            int starti = i;

            CompactSpan ss = chf.SpanList[i];
            int curReg = 0;
            if (RecastUtility.RcGetCon(ss, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(ss, dir);
                curReg = srcReg[ai];
            }
            cont.Add(curReg);

            int iter = 0;

            //从如果dir方向遇到边界就尝试顺时针旋转dir继续走，dir方向可走就走一格，然后逆时针旋转dir继续尝试……，
            while (++iter < 40000)
            {
                CompactSpan s = chf.SpanList[i];

                if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                {

                    int r = 0;
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                        int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(s, dir);
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
                        CompactCell nc = chf.CellList[nx + ny * chf.Width];
                        ni = nc.Index + RecastUtility.RcGetCon(s, dir);
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

            if (rega.AreaType != regb.AreaType)
            {
                return false;
            }

            //两个region之间无多处连接
            int n = 0;
            for (int i = 0; i < rega.Connections.Count; ++i)
            {
                if (rega.Connections[i] == regb.Id)
                    n++;
            }
            if (n > 1)
            {
                return false;
            }

            for (int i = 0; i < rega.Floors.Count; ++i)
            {
                if (rega.Floors[i] == regb.Id)
                    return false;
            }
            return true;
        }


        private static bool IsSolidEdge(CompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir)
        {
            //相邻的span与自身是不同区域的，则视为边缘
            CompactSpan s = chf.SpanList[i];
            int r = 0;
            if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(s, dir);
                r = srcReg[ai];
            }
            if (r == srcReg[i])
                return false;
            return true;
        }

        private static void RemoveAdjacentNeighbours(RcRegion reg)
        {
            //移除相邻的重复项
            for (int i = 0; i < reg.Connections.Count && reg.Connections.Count > 1;)
            {
                int ni = (i + 1) % reg.Connections.Count;
                if (reg.Connections[i] == reg.Connections[ni])
                {

                    for (int j = i; j < reg.Connections.Count - 1; ++j)
                        reg.Connections[j] = reg.Connections[j + 1];
                    reg.Connections.RemoveAt(reg.Connections.Count - 1);
                }
                else
                    ++i;
            }
        }

        public static void RcBuildContours(CompactHeightfield compactHeightfield, RcContourSet rcContourSet)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;

            int[] flags = new int[compactHeightfield.SpanCount];

            List<int> verts = new List<int>();
            List<int> simplified = new List<int>();

            rcContourSet.NumConts = 0;

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = c.Index + c.Count; i < ni; ++i)
                    {
                        int res = 0;

                        CompactSpan s = compactHeightfield.SpanList[i];

                        if (compactHeightfield.SpanList[i].Reg == 0)
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
                                int ai = (int)compactHeightfield.CellList[ax + ay * w].Index + RecastUtility.RcGetCon(s, dir);
                                r = compactHeightfield.SpanList[ai].Reg;
                            }

                            //相同region，则连通，连通标记为1
                            if (r == compactHeightfield.SpanList[i].Reg)
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
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = c.Index + c.Count; i < ni; ++i)
                    {
                        //全连通和全不连通的点都无需考虑
                        if (flags[i] == 0 || flags[i] == 0xf)
                        {
                            flags[i] = 0;
                            continue;
                        }
                        int reg = compactHeightfield.SpanList[i].Reg;
                        if (reg == 0)
                            continue;

                        AREATYPE area = compactHeightfield.AreaList[i];

                        verts.Clear();
                        simplified.Clear();

                        //遍历边缘，处理两个区域之间重复的边缘
                        WalkContourPoint(x, y, i, compactHeightfield, flags, verts);

                        //简化边缘
                        //SimplifyContourPoint(verts, simplified);

                        simplified = verts;

                        if (simplified.Count / 4 >= 3)
                        {

                            RcContour cont = new RcContour
                            {
                                NumVerts = simplified.Count / 4,
                                Verts = simplified.ToArray(),
                                Reg = reg,
                                Area = area
                            };

                            rcContourSet.ContsList.Add(cont);
                            rcContourSet.NumConts++;
                        }


                        RecastEditor.DrawFieldContour(rcContourSet);
                        return;
                    }
                }
            }

            //打通空洞
            if (rcContourSet.NumConts > 0)
            {

                int[] winding = new int[rcContourSet.NumConts];

                int nholes = 0;
                for (int i = 0; i < rcContourSet.NumConts; ++i)
                {
                    RcContour cont = rcContourSet.ContsList[i];

                    //只要根据叉乘算出每个轮廓多边形的有向面积，如果结果为小于0，则为轮廓点的顺序为逆时针，这个轮廓就是一个空洞。
                    winding[i] = CalcAreaOfPolygon2D(cont.Verts, cont.NumVerts) < 0 ? -1 : 1;
                    if (winding[i] < 0)
                        nholes++;
                }

                if (nholes > 0)
                {
                    int nregions = compactHeightfield.MaxRegions + 1;

                    RcContourRegion[] regions = new RcContourRegion[nregions];
                    RcContourHole[] holes = new RcContourHole[rcContourSet.NumConts];

                    for (int i = 0; i < rcContourSet.NumConts; ++i)
                    {
                        RcContour cont = rcContourSet.ContsList[i];

                        if (winding[i] > 0)
                        {
                            if (regions[cont.Reg].Outline != null)
                                Debug.LogErrorFormat("rcBuildContours: Multiple outlines for region %d.", cont.Reg);
                            regions[cont.Reg].Outline = cont;
                        }
                        else
                        {
                            regions[cont.Reg].NumHoles++;
                        }
                    }

                    for (int i = 0; i < nregions; i++)
                    {
                        if (regions[i].NumHoles > 0)
                        {
                            regions[i].Holes = new RcContourHole[regions[i].NumHoles];
                            regions[i].NumHoles = 0;
                        }
                    }

                    for (int i = 0; i < rcContourSet.NumConts; ++i)
                    {
                        RcContour cont = rcContourSet.ContsList[i];

                        RcContourRegion reg = regions[cont.Reg];

                        if (winding[i] < 0)
                            reg.Holes[reg.NumHoles++].Contour = cont;
                    }


                    //合并空洞
                    for (int i = 0; i < nregions; i++)
                    {
                        RcContourRegion reg = regions[i];
                        if (reg.NumHoles == 0) continue;

                        if (reg.Outline != null)
                        {
                            MergeRegionHoles(reg);
                        }
                        else
                        {
                            Debug.LogErrorFormat("rcBuildContours: Bad outline for region %d, contour simplification is likely too aggressive.", i);
                        }
                    }
                }

            }
        }


        private static void MergeRegionHoles(RcContourRegion region)
        {
            for (int i = 0; i < region.NumHoles; i++)
            {
                FindLeftMostVertex(region.Holes[i].Contour, out region.Holes[i].MinX, out region.Holes[i].MinZ, out region.Holes[i].LeftMost);
            }

            System.Array.Sort(region.Holes, CompareHoles);

            int maxVerts = region.Outline.NumVerts;

            for (int i = 0; i < region.NumHoles; i++)
            {
                maxVerts += region.Holes[i].Contour.NumVerts;
            }

            RcContour outline = region.Outline;

            RcPotentialDiagonal[] diags = new RcPotentialDiagonal[maxVerts];
            for (int i = 0; i < region.NumHoles; i++)
            {
                RcContour hole = region.Holes[i].Contour;


                int index = -1;
                int bestVertex = region.Holes[i].LeftMost;
                for (int iter = 0; iter < hole.NumVerts; iter++)
                {
                    //找到空洞的bestVertex（bestVetex为最左边的点，如果有多个最左点，取其中最下的点），并把N个空洞按照bestVertex排序，按照排序好的空洞顺序遍历。
                    int ndiags = 0;
                    int[] corner = { hole.Verts[bestVertex * 4], hole.Verts[bestVertex * 4 + 1], hole.Verts[bestVertex * 4 + 2], hole.Verts[bestVertex * 4 + 3] };
                    for (int j = 0; j < outline.NumVerts; j++)
                    {
                        if (InCone(j, outline.NumVerts, outline.Verts, corner))
                        {
                            int dx = outline.Verts[j * 4 + 0] - corner[0];
                            int dz = outline.Verts[j * 4 + 2] - corner[2];
                            diags[ndiags].Vert = j;
                            diags[ndiags].Dist = dx * dx + dz * dz;
                            ndiags++;
                        }
                    }

                    System.Array.Sort(diags, CompareDiagDist);

                    index = -1;
                    for (int j = 0; j < ndiags; j++)
                    {
                        int pt = diags[j].Vert * 4;
                        //是否和与外轮廓相交
                        bool intersect = IntersectSegContour(pt, corner, diags[i].Vert, outline.NumVerts, outline.Verts);

                        //是否和其他空洞相交
                        for (int k = i; k < region.NumHoles && !intersect; k++)
                            intersect |= IntersectSegContour(pt, corner, -1, region.Holes[k].Contour.NumVerts, region.Holes[k].Contour.Verts);
                        if (!intersect)
                        {
                            index = diags[j].Vert;
                            break;
                        }
                    }

                    //只有与外轮廓和其他空洞都不相交，才会对index赋值,
                    //选择距离最短并且两点连线与外轮廓和所有空洞没有交点的,把此时的bestVertex和外轮廓点的索引作为开口位置
                    if (index != -1)
                        break;

                    // 尝试下一个点
                    bestVertex = (bestVertex + 1) % hole.NumVerts;
                }

                if (index == -1)
                {
                    Debug.LogError("mergeHoles: Failed to find merge points");
                    continue;
                }
                if (!MergeContours(region.Outline, hole, index, bestVertex))
                {
                    Debug.LogError("mergeHoles: Failed to merge contours %p and %p.");
                    continue;
                }

            }
        }

        private static bool MergeContours(RcContour ca, RcContour cb, int ia, int ib)
        {
            int maxVerts = ca.NumVerts + cb.NumVerts + 2;
            int[] verts = new int[maxVerts * 4];

            int nv = 0;

            for (int i = 0; i <= ca.NumVerts; ++i)
            {
                int dstIndex = nv * 4;
                int srcIndex = ((ia + i) % ca.NumVerts);
                verts[dstIndex] = ca.Verts[srcIndex];
                verts[dstIndex + 1] = ca.Verts[srcIndex + 1];
                verts[dstIndex + 2] = ca.Verts[srcIndex + 2];
                verts[dstIndex + 3] = ca.Verts[srcIndex + 3];
                nv++;
            }

            for (int i = 0; i <= cb.NumVerts; ++i)
            {
                int dstIndex = nv * 4;
                int srcIndex = ((ib + i) % cb.NumVerts);
                verts[dstIndex] = cb.Verts[srcIndex];
                verts[dstIndex + 1] = cb.Verts[srcIndex + 1];
                verts[dstIndex + 2] = cb.Verts[srcIndex + 2];
                verts[dstIndex + 3] = cb.Verts[srcIndex + 3];
                nv++;
            }

            ca.Verts = verts;
            ca.NumVerts = nv;

            cb.Verts = new int[0];
            cb.NumVerts = 0;

            return true;
        }


        private static void FindLeftMostVertex(RcContour contour, out int minx, out int minz, out int leftmost)
        {
            minx = contour.Verts[0];
            minz = contour.Verts[2];
            leftmost = 0;
            for (int i = 1; i < contour.NumVerts; i++)
            {
                int x = contour.Verts[i * 4 + 0];
                int z = contour.Verts[i * 4 + 2];
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

            if (va.MinX == vb.MinX)
            {
                if (va.MinZ < vb.MinZ)
                    return -1;
                if (va.MinZ > vb.MinZ)
                    return 1;
            }
            else
            {
                if (va.MinX < vb.MinX)
                    return -1;
                if (va.MinX > vb.MinX)
                    return 1;
            }
            return 0;
        }


        private static int CompareDiagDist(RcPotentialDiagonal a, RcPotentialDiagonal b)
        {

            if (a.Dist < b.Dist)
                return -1;
            if (a.Dist > b.Dist)
                return 1;
            return 0;
        }

        private static int Prev(int i, int n) { return i - 1 >= 0 ? i - 1 : n - 1; }
        private static int Next(int i, int n) { return i + 1 < n ? i + 1 : 0; }

        private static bool Left(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) < 0;
        }

        private static bool LeftOn(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) <= 0;
        }

        private static bool Collinear(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) == 0;
        }

        private static int Area2(int[] a, int[] b, int[] c)
        {
            return (b[0] - a[0]) * (c[2] - a[2]) - (c[0] - a[0]) * (b[2] - a[2]);
        }


        private static bool Vequal(int[] a, int[] b)
        {
            return a[0] == b[0] && a[2] == b[2];
        }
        private static bool xorb(bool x, bool y)
        {
            return !x ^ !y;
        }

        static bool IntersectProp(int[] a, int[] b, int[] c, int[] d)
        {

            if (Collinear(a, b, c) || Collinear(a, b, d) ||
                Collinear(c, d, a) || Collinear(c, d, b))
                return false;

            return xorb(Left(a, b, c), Left(a, b, d)) && xorb(Left(c, d, a), Left(c, d, b));
        }

        //当a.b.c 共线，且c在ab线段上时返回ture
        static bool Between(int[] a, int[] b, int[] c)
        {
            if (!Collinear(a, b, c))
                return false;

            if (a[0] != b[0])
                return ((a[0] <= c[0]) && (c[0] <= b[0])) || ((a[0] >= c[0]) && (c[0] >= b[0]));
            else
                return ((a[2] <= c[2]) && (c[2] <= b[2])) || ((a[2] >= c[2]) && (c[2] >= b[2]));
        }

        //当线段ab和cd 相交 ，返回true。
        public static bool Intersect(int[] a, int[] b, int[] c, int[] d)
        {
            if (IntersectProp(a, b, c, d))
                return true;
            else if (Between(a, b, c) || Between(a, b, d) ||
                     Between(c, d, a) || Between(c, d, b))
                return true;
            else
                return false;
        }

        private static bool InCone(int i, int n, int[] verts, int[] pj)
        {
            int piIndex = i * 4;
            int[] pi = { verts[piIndex], verts[piIndex + 1], verts[piIndex + 2] };

            int pi1Index = Next(i, n);
            int[] pi1 = { verts[pi1Index], verts[pi1Index + 1], verts[pi1Index + 2] };

            int pin1Index = Prev(i, n);
            int[] pin1 = { verts[pin1Index], verts[pin1Index + 1], verts[pin1Index + 2] };

            //判断pj是否在以pi为顶点 以pi->pin1 、pi->pi1边组成的锥形范围内
            if (LeftOn(pin1, pi, pi1))
                return Left(pi, pj, pin1) && Left(pj, pi, pi1);

            return !(LeftOn(pi, pj, pi1) && LeftOn(pj, pi, pin1));
        }


        //是否于边缘相交
        public static bool IntersectSegContour(int d0Index, int[] d1, int i, int n, int[] verts)
        {
            int[] d0 = { verts[d0Index], verts[d0Index + 1], verts[d0Index + 2], verts[d0Index + 3] };

            // For each edge (k,k+1) of P
            for (int k = 0; k < n; k++)
            {
                int k1 = Next(k, n);
                // Skip edges incident to i.
                if (i == k || i == k1)
                    continue;

                int[] p0 = { verts[k], verts[k + 1], verts[k + 2], verts[k + 3] };
                int[] p1 = { verts[k1], verts[k1 + 1], verts[k1 + 2], verts[k1 + 3] };

                if (Vequal(d0, p0) || Vequal(d1, p0) || Vequal(d0, p1) || Vequal(d1, p1))
                    continue;

                if (Intersect(d0, d1, p0, p1))
                    return true;
            }
            return false;
        }

        private static void WalkContourPoint(int x, int y, int i, CompactHeightfield compactHeightField, int[] flags, List<int> points)
        {
            int dir = 0;
            //找第一个边界
            while ((flags[i] & (1 << dir)) == 0)
                dir++;

            int startDir = dir;
            int starti = i;

            AREATYPE area = compactHeightField.AreaList[i];

            int iter = 0;
            while (++iter < 40000)
            {
                // dir方向是边界，则保存轮廓点后，顺时针旋转后再循环尝试
                if ((flags[i] & (1 << dir)) != 0)
                {

                    bool isAreaBorder = false;
                    int px = x;
                    int py = GetCornerHeight(x, y, i, dir, compactHeightField);
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

                    CompactSpan s = compactHeightField.SpanList[i];
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                        int ai = compactHeightField.CellList[ax + ay * compactHeightField.Width].Index + RecastUtility.RcGetCon(s, dir);
                        r = compactHeightField.SpanList[ai].Reg;
                        if (area != compactHeightField.AreaList[ai])
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
                    CompactSpan s = compactHeightField.SpanList[i];
                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                    {
                        CompactCell nc = compactHeightField.CellList[nx + ny * compactHeightField.Width];
                        ni = nc.Index + RecastUtility.RcGetCon(s, dir);
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
                    RecastUtility.RcSwap(ax, bx);
                    RecastUtility.RcSwap(az, bz);
                }

                // 不可行走边界或者region边界
                if ((points[ci * 4 + 3] & RecastConfig.RC_CONTOUR_REG_MASK) == 0 ||
                    (points[ci * 4 + 3] & RecastConfig.RC_AREA_BORDER) != 0)
                {
                    while (ci != endi)
                    {
                        float d = DistancePtSeg(points[ci * 4 + 0], points[ci * 4 + 2], ax, az, bx, bz);
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
                    int n = simplified.Count / 4;
                    for (int j = n - 1; j > i; --j)
                    {
                        simplified[j * 4 + 0] = simplified[(j - 1) * 4 + 0];
                        simplified[j * 4 + 1] = simplified[(j - 1) * 4 + 1];
                        simplified[j * 4 + 2] = simplified[(j - 1) * 4 + 2];
                        simplified[j * 4 + 3] = simplified[(j - 1) * 4 + 3];
                    }
                    // 添加到i索引后面
                    simplified.Insert((i + 1) * 4 + 0,points[maxi * 4 + 0]);
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

                        int n = simplified.Count / 4;
                        for (int j = n - 1; j > i; --j)
                        {
                            simplified[j * 4 + 0] = simplified[(j - 1) * 4 + 0];
                            simplified[j * 4 + 1] = simplified[(j - 1) * 4 + 1];
                            simplified[j * 4 + 2] = simplified[(j - 1) * 4 + 2];
                            simplified[j * 4 + 3] = simplified[(j - 1) * 4 + 3];
                        }

                        simplified.Insert((i + 1) * 4 + 0,points[maxi * 4 + 0]);
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
                simplified[i * 4 + 3] = (points[ai * 4 + 3] & (RecastConfig.RC_CONTOUR_REG_MASK | RecastConfig.RC_AREA_BORDER)) | (points[bi * 4 + 3] & RecastConfig.RC_BORDER_VERTEX);
            }

            //去除重复的点
            int npts = simplified.Count / 4;
            for (int i = 0; i < npts; ++i)
            {
                int ni = Next(i, npts);

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
        private static int GetCornerHeight(int x, int y, int i, int dir, CompactHeightfield compactHeightField)
        {

            CompactSpan s = compactHeightField.SpanList[i];
            int ch = s.Y;
            // 逆时针旋转di
            int dirp = (dir + 1) & 0x3;

            int[] regs = { 0, 0, 0, 0 };

            regs[0] = compactHeightField.SpanList[i].Reg | ((int)compactHeightField.AreaList[i] << 16);

            //取周围4个span的最大y作为边界的y
            if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = compactHeightField.CellList[ax + ay * compactHeightField.Width].Index + RecastUtility.RcGetCon(s, dir);
                CompactSpan as1 = compactHeightField.SpanList[ai];
                ch = Mathf.Max(ch, as1.Y);
                regs[1] = compactHeightField.SpanList[ai].Reg | ((int)compactHeightField.AreaList[ai] << 16);

                if (RecastUtility.RcGetCon(as1, dirp) != RecastConfig.RC_NOT_CONNECTED)
                {
                    int ax2 = ax + RecastUtility.RcGetDirOffsetX(dirp);
                    int ay2 = ay + RecastUtility.RcGetDirOffsetY(dirp);
                    int ai2 = (int)compactHeightField.CellList[ax2 + ay2 * compactHeightField.Width].Index + RecastUtility.RcGetCon(as1, dirp);
                    CompactSpan as2 = compactHeightField.SpanList[ai2];
                    ch = Mathf.Max(ch, as2.Y);
                    regs[2] = compactHeightField.SpanList[ai2].Reg | ((int)compactHeightField.AreaList[ai2] << 16);
                }
            }

            if (RecastUtility.RcGetCon(s, dirp) != RecastConfig.RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dirp);
                int ay = y + RecastUtility.RcGetDirOffsetY(dirp);
                int ai = compactHeightField.CellList[ax + ay * compactHeightField.Width].Index + RecastUtility.RcGetCon(s, dirp);
                CompactSpan as1 = compactHeightField.SpanList[ai];
                ch = Mathf.Max(ch, as1.Y);
                regs[3] = compactHeightField.SpanList[ai].Reg | ((int)compactHeightField.AreaList[ai] << 16);

                if (RecastUtility.RcGetCon(as1, dir) != RecastConfig.RC_NOT_CONNECTED)
                {
                    int ax2 = ax + RecastUtility.RcGetDirOffsetX(dir);
                    int ay2 = ay + RecastUtility.RcGetDirOffsetY(dir);
                    int ai2 = (int)compactHeightField.CellList[ax2 + ay2 * compactHeightField.Width].Index + RecastUtility.RcGetCon(as1, dir);
                    CompactSpan as2 = compactHeightField.SpanList[ai2];
                    ch = Mathf.Max(ch, as2.Y);
                    regs[2] = compactHeightField.SpanList[ai2].Reg | ((int)compactHeightField.AreaList[ai2] << 16);
                }
            }

            return ch;
        }

        private static float DistancePtSeg(int x, int z, int px, int pz, int qx, int qz)
        {

            float pqx = (float)(qx - px);
            float pqz = (float)(qz - pz);
            float dx = (float)(x - px);
            float dz = (float)(z - pz);
            float d = pqx * pqx + pqz * pqz;
            float t = pqx * dx + pqz * dz;
            if (d > 0)
                t /= d;
            if (t < 0)

                t = 0;
            else if (t > 1)
                t = 1;

            dx = px + t * pqx - x;
            dz = pz + t * pqz - z;

            return dx * dx + dz * dz;
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
