using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using TowerResort.Entities.Hammer;

namespace TowerResort.Player;

public partial class BallPawn
{
	public TraceResult TraceCheck()
	{
		var tr = Trace.Ray( Ball.Position, Ball.Position + EyeRotation.Backward * 105 )
			.WithTag( "solid" )
			.Ignore( this )
			.Size( 26.0f )
			.Run();

		return tr;
	}

	[ClientRpc]
	public void ResetCamera()
	{
		Camera.Rotation = EyeRotation;
	}

	public void FrameCamera()
	{
		if ( Ball == null ) return;

		Camera.Position = TraceCheck().EndPosition;
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FirstPersonViewer = null;
	}

}
