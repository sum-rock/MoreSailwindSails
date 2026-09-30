# Deferred architecture cleanup

CLEANUP-1, -2, -3 and -5 are implemented; history is in Git. Current behavior
and validation limits live in [DEVELOPMENT.md](DEVELOPMENT.md).

## CLEANUP-4 — Extract identical shared calculations

**Deferred; do not resume without a new request.** The user reverted this
extraction to keep Flying Sail and staysail development independent. Existing
shared winch controls are outside the deferral.

If requested later, reassess tension and aerodynamic calculations, appearance
setup and text wrapping for exact duplication. Extract only confirmed common
logic; retain family tuning, Harmony scope/order, mounting, reefing and
lifecycle behavior. Do not introduce a generic rig hierarchy.

Preserve finite fallbacks, appearance/text guards, native save layout, plugin
identity and prefab IDs **400/401/402/403**. Use the standard
[automated checks](DEVELOPMENT.md#build-and-automated-checks) and separately
validate both families' propulsion, appearance and affected shipyard behavior
with the [runtime checklist](DEVELOPMENT.md#runtime-validation).
