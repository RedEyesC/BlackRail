using UnityEngine.UI;

namespace GameFramework.Interface
{
    public class NoDrawingRayCastComponet : MaskableGraphic
    {
        protected NoDrawingRayCastComponet()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            toFill.Clear();
        }
    }
}
