
using UnityEngine;

namespace GameEditor.RecastEditor
{
    internal class RecastComponent:MonoBehaviour
    {

        public static RcContourSet rcContourSet;
        private void OnDrawGizmos()
        {

            for (int i = 0; i < rcContourSet.NumConts; ++i)
            {
                RcContour cont = rcContourSet.ContsList[i];

                for (int j = 0; j < cont.NumVerts; i++)
                {
                    int j1 = (j + 1) % cont.NumVerts;

                    Vector3 v1 = new Vector3(cont.Verts[j * 4], cont.Verts[j * 4 + 1], cont.Verts[j * 4 + 2]);
                    Vector3 v2 = new Vector3(cont.Verts[j1 * 4], cont.Verts[j1 * 4 + 1], cont.Verts[j1 * 4 + 2]);
                    Gizmos.DrawLine(v1, v2);
                }
            }
        }

        public void SetContour(RcContourSet val )
        {
            rcContourSet = val;
        }
    }
}
