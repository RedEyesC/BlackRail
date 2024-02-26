
using UnityEngine;
public class RecastDebugComponent : MonoBehaviour
{

    public static float[][] rcContourSet;
    public static float[][] rcCubeList;
    public static Vector3 rcCubeSize;
    public static Color[] colorMap = {Color.green, Color.blue, Color.white, Color.black,
                Color.yellow, Color.cyan, Color.magenta, Color.gray };
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (rcContourSet != null)
        {

            for (int i = 0; i < rcContourSet.Length; i++)
            {

                //int[] showPath = { 153, 133, 123, 145, 115, 100, 65, 41, 104, 9, 105, 81, 53, 131 };

                //if (System.Array.IndexOf(showPath, i) == -1)
                //{
                //    continue;
                //}

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

        if (rcCubeList != null)
        {
            for (int i = 0; i < rcCubeList.Length; i++)
            {

                float[] spanCube = rcCubeList[i];

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
                            Gizmos.color = colorMap[(int)reg % 8];
                        }

                        Gizmos.DrawCube(pos, rcCubeSize);
                    }
                }

            }
        }

    }

    public void SetContour(float[][] val)
    {
        rcContourSet = val;
    }

    public void SetCubeList(float[][] val, Vector3 size)
    {
        rcCubeList = val;
        rcCubeSize = size;
    }
}

