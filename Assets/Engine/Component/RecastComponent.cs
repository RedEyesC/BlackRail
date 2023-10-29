
using UnityEngine;
public class RecastComponent : MonoBehaviour
{

    public static float[][] rcContourSet;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (rcContourSet != null)
        {

            for (int i = 0; i < rcContourSet.Length; i++)
            {
                float[] contVert = rcContourSet[i];

                int numVert = contVert.Length / 4;

                for (int j = 0; j < numVert; j++)
                {
                    int j1 = (j + 1) % numVert;

                    Vector3 v1 = new Vector3(contVert[j * 4], contVert[j * 4 + 1], contVert[j * 4 + 2]);
                    Vector3 v2 = new Vector3(contVert[j1 * 4], contVert[j1 * 4 + 1], contVert[j1 * 4 + 2]);
                    Gizmos.DrawLine(v1, v2);
                }
            }
        }
        
    }

    public void SetContour(float[][] val)
    {
        rcContourSet = val;
    }
}

