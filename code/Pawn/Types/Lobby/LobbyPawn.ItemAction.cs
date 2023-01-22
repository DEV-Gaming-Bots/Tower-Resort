using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using TowerResort.Entities.CondoItems;

namespace TowerResort.Player;

public partial class LobbyPawn : MainPawn
{
	public CondoItemBase SittingChair;

	public void SimulateSittingOnObject()
	{
		if ( Game.IsServer )
		{
			if ( Input.Pressed( InputButton.Use ) || Input.Pressed( InputButton.Jump ) )
			{
				Input.SuppressButton( InputButton.Jump );

				SittingChair.RemoveSittingPlayer( this );
				SittingChair = null;
				SetAnimParameter( "sit", 0 );
				return;
			}

			Position = SittingChair.Position + Vector3.Up * SittingChair.Asset.SitHeight;

			var chairEyes = Camera.Position;

			//Its stupid but it'll do for now
			if ( SittingChair.Rotation.z < -0.60f )
				chairEyes += Camera.Rotation.Left * 999;
			else if ( SittingChair.Rotation.z > -0.60f && SittingChair.Rotation.z <= 0.0f )
				chairEyes += Camera.Rotation.Forward * 999;
			else if( SittingChair.Rotation.z > 0.0f && SittingChair.Rotation.z < 0.70f )
				chairEyes += Camera.Rotation.Right * 999;
			else if ( SittingChair.Rotation.z > 0.70f)
				chairEyes += Camera.Rotation.Backward * 999;

			var tr = Controller.TraceBBox(Position, Position + Rotation.Down * 64);
			Controller?.UpdateGroundEntity( tr );

			SetAnimParameter( "aim_head", Vector3.Zero );
			SetAnimParameter( "aim_body", Vector3.Zero );
			SetAnimParameter( "aim_eyes", Vector3.Zero );

			SetAnimParameter( "duck", 0.25f );
			SetAnimParameter( "b_grounded", true );
			
			SetAnimParameter( "aim_head", chairEyes );
			SetAnimParameter( "aim_body", chairEyes );
			SetAnimParameter( "aim_eyes", chairEyes );
			SetAnimParameter( "sit", 1 );
		}
	}
}

