using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.GameComponents;

namespace TowerResort.Entities.Lobby;

public partial class TetrisMachine : ModelEntity, IUse
{
	TetrisGame TetrisGame { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );

		TetrisGame = Components.Create<TetrisGame>();
	}



	public bool IsUsable( Entity user )
	{
		return TetrisGame.Player == null;
	}

	[ClientRpc]
	public void UpdateTetrisUI(int row, int column)
	{
		Log.Info( $"{row}, {column}" );
	}

	public override void Simulate( IClient cl )
	{
		TetrisGame.SimulateTetris( cl );

		if ( TetrisGame.GameOver ) return;

		var block = TetrisGame.CurBlock;
		UpdateTetrisUI( To.Single( cl ), block.GetRow(), block.GetColumn() );

		base.Simulate( cl );
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		var player = user as LobbyPawn;
		if ( player == null ) return false;

		Input.SuppressButton( InputButton.Use );

		player.FocusedEntity = this;
		TetrisGame.StartGame( player );

		return false;
	}
}

