using MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;
using MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

namespace MoreSailwindSails.Tests.GeometryChecks;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (System.Array.IndexOf(array: args, value: "--benchmark-mount") >= 0)
        {
            Spritsail.MountOptimizationChecks.Benchmark();
            return;
        }
        Visuals.RoutedRopeChecks.Run();
        Visuals.SailVisualRevisionChecks.Run();
        Spritsail.CategoryChecks.Run();
        Spritsail.BoomedSpritsail.GeometryChecks.Run();
        Spritsail.CollisionChecks.Run();
        Spritsail.TravelChecks.Run();
        Spritsail.MastAlignmentChecks.Run();
        Spritsail.NativeControlChecks.Run();
        Spritsail.GallusChecks.Run();
        Spritsail.SpritVisualChecks.Run();
        Spritsail.SnotterChecks.Run();
        Spritsail.MountChecks.Run();
        Spritsail.MountFrameChecks.Run();
        Spritsail.DeploymentChecks.Run();
        Spritsail.ObstructionChecks.Run();
        Spritsail.LooseFootedSpritsail.FlexChecks.Run();
        Spritsail.LooseFootedSpritsail.MkA.PrototypeChecks.Run();
        Spritsail.LooseFootedSpritsail.MkB.CutChecks.Run();
        RigChecks.Run();
        ProfileChecks.Run();
        StayChecks.Run();
        CollarChecks.Run();
        Utils.WinchCaptureChecks.Run();
        Utils.WinchMountOverlayChecks.Run();
        Controls.NativeSeatProfileChecks.Run();
        Controls.HalyardProfileChecks.Run();
        Controls.KakamWinchChecks.Run();
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
