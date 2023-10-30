
using UnityEngine;
public class RecastComponent : MonoBehaviour
{

    public static float[][] RcContourSet;
    public static float[][] RcCubeList;
    public static Vector3 RcCubeSize;
    public static Color[] ColorMap = {Color.green, Color.blue, Color.white, Color.black,
                Color.yellow, Color.cyan, Color.magenta, Color.gray };
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (RcContourSet != null)
        {

            for (int i = 0; i < RcContourSet.Length; i++)
            {
                float[] contVert = RcContourSet[i];

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

        if (RcCubeList != null)
        {
            for (int i = 0; i < RcCubeList.Length; i++)
            {

                float[] cube = RcCubeList[i];

                if (cube != null)
                {
                    int numVert = cube.Length / 4;

                    for (int j = 0; j < numVert; j++)
                    {
                        Vector3 v = new Vector3(cube[j * 4], cube[j * 4 + 1], cube[j * 4 + 2]);

                        int reg = (int)cube[j * 4 + 3];

                        if (reg == 0)
                        {
                            Gizmos.color = Color.red;
                        }
                        else if (reg < 1)
                        {
                            Gizmos.color = new UnityEngine.Color(reg, reg, reg);
                        }
                        else
                        {
                            Gizmos.color = ColorMap[reg % 7];
                        }

                        Gizmos.DrawCube(v, RcCubeSize);
                    }
                }

            }
        }

    }

    public void SetContour(float[][] val)
    {
        RcContourSet = val;
    }

    public void SetCubeList(float[][] val, Vector3 size)
    {
        RcCubeList = val;
        RcCubeSize = size;
    }
}

