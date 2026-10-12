// Selects the baked Mk.B cut for the shared staysail fitter.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkB
{
    internal static class FishermansStaysailMkBGeometry
    {
        internal const float FixedUpperHeadAngle = 14f;

        internal static FishermansStaysailMeshData Create(float width, float headSlope = 20f) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkBBakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
