// Selects the Mk.A baked mesh and labels for the shared staysail rig.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkA
{
    internal sealed class FishermansStaysailMkAShape : FishermansStaysailShape
    {
        internal override string ObjectPrefix => "FishermansStaysailMkA";

        internal override FishermansStaysailMeshData Create(float width, float headSlope) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkABakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
