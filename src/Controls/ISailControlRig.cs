// Supplies sail-specific requirements without coupling placement to a sail family.
namespace MoreSailwindSails.Controls
{
    internal interface ISailControlRig
    {
        bool TryGetControlRequest(out SailControlRequest request);
        void OnControlsChanged(SailControlSet controls);
    }
}
