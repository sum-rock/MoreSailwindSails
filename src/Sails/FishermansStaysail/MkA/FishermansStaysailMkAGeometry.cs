// Selects the baked Mk.A cut for the shared staysail fitter.
namespace MoreSailwindSails.Sails.FishermansStaysail.MkA
{
    internal static class FishermansStaysailMkAGeometry
    {
        internal const float NominalSlope = 20f;
        internal const float FixedUpperHeadAngle = 14f;

        internal static FishermansStaysailMeshData Create(float width, float headSlope = 20f) =>
            FishermansStaysailMeshFitting.Fit(
                data: FishermansStaysailMkABakedMesh.Create(),
                width: width,
                headSlope: headSlope
            );
    }
}
