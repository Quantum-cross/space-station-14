using Content.Shared.DeviceLinking.Components;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.DeviceLinking;

public sealed partial class SignalMonitorSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalMonitorComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<SignalMonitorComponent> ent, ref ComponentInit evt)
    {
        EnsureComp(ent.Owner, out SignalMonitorScreenComponent screen);

        for (var i = 0; i < screen.ScreenBuffer.Length; i++)
        {
            screen.ScreenBuffer[i] = SixLabors.ImageSharp.Color.Black;
        }
        var tex = _clyde.CreateBlankTexture<Rgba32>((32, 32));
        tex.SetSubImage<Rgba32>((0, 0), (32, 32), screen.ScreenBuffer);
    }
}
