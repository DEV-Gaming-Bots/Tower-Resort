using TowerResort;
using TowerResort.Entities.CondoItems;
using TowerResort.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase : AnimatedEntity, IUse
{
	public LobbyPawn Sitter { get; private set; }

	TimeSince timeSinceSat;

	public void Sitdown( Entity user )
	{
		if ( timeSinceSat > 2.0f )
		{
			timeSinceSat = 0;

			var player = user as LobbyPawn;

			if ( player == null )
				return;

			if ( Input.Down( InputButton.Duck ) )
				Input.SuppressButton( InputButton.Duck );

			Sitter = player;
			player.SittingChair = this;
			//player.Position = Position;
			player.Rotation = Rotation;
			player.SetViewAngles( Rotation.Angles() );

			player.FreezeMovement = MainPawn.FreezeEnum.MoveAndAnim;
		}
	}

	public void RemoveSittingPlayer( LobbyPawn player )
	{
		if ( !Game.IsServer )
			return;

		Sitter?.SetAnimParameter( "sit", 0 );

		Sitter = null;
		timeSinceSat = 0;

		if ( !player.IsValid() )
			return;

		player.FreezeMovement = MainPawn.FreezeEnum.None;
		
		player.ResetCamera();
		player.Rotation = Rotation.Identity;

		if ( Rotation.Roll() < 0.0f )
			player.Position += Rotation.Up * Asset.SitHeight * Asset.SitHeight + 4;
		else
			player.Position += player.Rotation.Up * Asset.SitHeight;

	}

	[Event.Tick.Server]
	protected void SitTick()
	{
		if ( Sitter is LobbyPawn player && player.LifeState != LifeState.Alive )
		{
			RemoveSittingPlayer( player );
		}
	}
}

