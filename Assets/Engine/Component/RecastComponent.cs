
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

                float[] spanCube = RcCubeList[i];

                if (spanCube != null)
                {
                    int num = spanCube.Length / 4;

                    for (int j = 0; j < num; j++)
                    {
                        Vector3 pos = new Vector3(spanCube[j * 4], spanCube[j * 4 + 1], spanCube[j * 4 + 2]);

                        float reg = spanCube[j * 4 + 3];

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
                            Gizmos.color = ColorMap[(int)reg % 8];
                        }

                        Gizmos.DrawCube(pos, RcCubeSize);
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

