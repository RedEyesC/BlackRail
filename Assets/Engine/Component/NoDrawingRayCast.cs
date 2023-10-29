using UnityEngine.UI;

public class NoDrawingRayCast : MaskableGraphic
{

    protected NoDrawingRayCast()
    {
        useLegacyMeshGeneration = false;
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();
    }
}