
using UnityEngine;
public class NavDebugComponent : MonoBehaviour
{

    public static float[] parameterList;

    private void OnDrawGizmos()
    {

        if (parameterList != null)
        {

            int index = 0;
            while (index < parameterList.Length)
            {

                int type = (int)parameterList[index];
                int len = (int)parameterList[index + 1];

                switch (type)
                {
                    case 1:
                        int cubeId = (int)parameterList[index + 2];
                        Vector3 pos = new Vector3(parameterList[index + 3], parameterList[index + 4], parameterList[index + 5]);
                        Vector3 size = new Vector3(parameterList[index + 6], parameterList[index + 7], parameterList[index + 8]);
                        Color color = new Color(parameterList[index + 9], parameterList[index + 10], parameterList[index + 11]);
                        DrawCub(cubeId, pos, size, color);
                        break;

                    case 2:
                        int meshId = (int)parameterList[index + 2];
                        int nverts = (int)parameterList[index + 3];

                        int vertIndex = index + 4;
                        for (int j = 0; j < nverts; j++)
                        {
                            int j1 = (j + 1) % nverts;

                            Vector3 v1 = new Vector3(parameterList[vertIndex + j * 3], parameterList[vertIndex + j * 3 + 1], parameterList[vertIndex + j * 3 + 2]);
                            Vector3 v2 = new Vector3(parameterList[vertIndex + j1 * 3], parameterList[vertIndex + j1 * 3 + 1], parameterList[vertIndex + j1 * 3 + 2]);

                            DrawLine(meshId, v1, v2, Color.blue);
                        }
                        break;
                    case 3:
                        int lineId = (int)parameterList[index + 2];
                        Vector3 l1 = new Vector3(parameterList[index + 3], parameterList[index + 4], parameterList[index + 5]);
                        Vector3 l2 = new Vector3(parameterList[index + 6], parameterList[index + 7], parameterList[index + 8]);
                        DrawLine(lineId, l1, l2, Color.blue);
                        break;

                }

                index += len;

            }


        }

    }

    public void DrawCub(int id, Vector3 pos, Vector3 size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawCube(pos, size);
    }


    public void DrawLine(int id, Vector3 v1, Vector3 v2, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(v1, v2);
    }

    public void SetDrawParam(float[] val)
    {
        parameterList = val;
    }

}

