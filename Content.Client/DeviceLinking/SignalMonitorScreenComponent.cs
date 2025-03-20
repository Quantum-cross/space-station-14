using System.Runtime.InteropServices.JavaScript;
using Robust.Client.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.DeviceLinking;

[RegisterComponent]
public sealed partial class SignalMonitorScreenComponent : Component
{
    public OwnedTexture? ScreenTexture = null;
    public Rgba32[] ScreenBuffer = new Rgba32[32*32];
    public byte[] ValueBuffer = new byte[32];
}
