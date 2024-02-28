namespace GameFramework.Detour
{
    public class DetourConfig
    {
        public static readonly int DT_NODE_PARENT_BITS = 24;
        public static readonly int DT_NODE_STATE_BITS = 2;
        public static readonly int DT_NULL_IDX = -1;
        public static readonly int DT_NULL_LINK = 0xfffffff;
        public static readonly float H_SCALE = 0.999f;


        //保持与 RecastConfig一致
        public static readonly int DT_MESH_NULL_IDX = 0xffff; //空节点标志位
        public static readonly int MaxVertsPerPoly = 6;
    }
}
