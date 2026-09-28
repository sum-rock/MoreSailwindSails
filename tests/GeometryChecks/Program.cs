using MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;
using MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

namespace MoreSailwindSails.Tests.GeometryChecks;

internal static class Program
{
    private static void Main()
    {
        Compatibility.TextureCatalogChecks.Run();
        RigChecks.Run();
        ProfileChecks.Run();
        StayChecks.Run();
        Utils.WinchCaptureChecks.Run();
        Controls.NativeSeatProfileChecks.Run();
        Controls.NativeSeatAllocationChecks.Run();
        Controls.NativeSeatPolicyChecks.Run();
        Controls.WinchRefreshChecks.Run();
        OrderTextChecks.Run();
        FlyingSailChecks.Run();
        RopeChecks.Run();
        KnotChecks.Run();
        TravelChecks.Run();
        MastInstallationChecks.Run();
        BillowChecks.Run();
        ShapingChecks.Run();
        AerodynamicChecks.Run();
        MeshChecks.Run();
        FishermansStaysail.MkA.CutChecks.Run();
        FishermansStaysail.MkB.CutChecks.Run();
        FishermansStaysail.MkC.CutChecks.Run();
        FishermansStaysail.CollisionChecks.Run();
        FishermansStaysail.ReefingChecks.Run();
        FishermansStaysail.SheetChecks.Run();
        FishermansStaysail.EdgeFitChecks.Run();
        FishermansStaysail.BillowChecks.Run();
        FishermansStaysail.FixedHeadChecks.Run();
    }
}
