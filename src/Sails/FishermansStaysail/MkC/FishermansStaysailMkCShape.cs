// Selects the Mk.C baked mesh and labels for the shared staysail rig.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkC
{
    internal sealed class FishermansStaysailMkCShape : FishermansStaysailShape
    {
        internal override string ObjectPrefix => "FishermansStaysailMkC";

        internal override FishermansStaysailMeshData Create(float width, float headSlope) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkCBakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
