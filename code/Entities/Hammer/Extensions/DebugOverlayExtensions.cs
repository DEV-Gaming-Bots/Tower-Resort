using Sandbox.Internal.Globals;

namespace SimpleMirror;

internal static class DebugOverlayExtensions
{
	public static void DrawVector(this DebugOverlay debug, Vector3 start, Vector3 end, Color color, float knobEndGirth = 5.0f )
	{
		debug.Line( start, end, color );
		debug.Sphere( end, knobEndGirth, color );
	}
}
