using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;

namespace Content.Client.Anomaly;

public sealed partial class AnomalySystem
{
    private void InitializeScanner()
    {
        SubscribeNetworkEvent<AnomalyChangedEvent>(OnScannerAnomalyChanged);
    }

    private void OnScannerAnomalyChanged(AnomalyChangedEvent args)
    {
        var uid = GetEntity(args.Entity);

        var screen = EnsureComp<AnomalyScannerScreenComponent>(uid);

        var tex = screen.ScreenTexture ?? _clyde
    }
}
