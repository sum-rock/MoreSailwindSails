// Selects the Mk.B baked mesh and labels for the shared staysail rig.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkB
{
    internal sealed class FishermansStaysailMkBShape : FishermansStaysailShape
    {
        internal override string ObjectPrefix => "FishermansStaysailMkB";

        internal override FishermansStaysailMeshData Create(float width, float headSlope) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkBBakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
