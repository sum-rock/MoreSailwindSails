// Selects the baked Mk.C cut for the shared staysail fitter.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkC
{
    internal static class FishermansStaysailMkCGeometry
    {
        internal const float LuffLengthRatio = 1.5f;
        internal const float FootSlope = 40f;
        internal const float FixedUpperHeadAngle = 14f;

        internal static FishermansStaysailMeshData Create(float width, float headSlope = 20f) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkCBakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
