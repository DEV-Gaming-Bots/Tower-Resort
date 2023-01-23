using Components.NotificationManager;
using Editor;
using Sandbox;
using System.Threading.Tasks;
using TowerResort.Player;

namespace TowerResort.Entities.Lobby;

[Library( "tr_lobby_casino_slotmachine" ), HammerEntity]
[Title( "Slot Machine" ), Category( "Lobby" )]
[EditorModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" )]
public partial class SlotMachine : ModelEntity, IUse
{
	[Property, Title( "Machine Variant" ), MinMax( 0, 1 )]
	public int MachineVariant { get; set; } = 0;

	public enum DefaultSlotEnum
	{
		None,
		SingleBar,
		DoubleBar,
		TripleBar,
		LuckySeven,
		Diamond
	}

	DefaultSlotEnum FirstSlot;
	DefaultSlotEnum SecondSlot;
	DefaultSlotEnum ThirdSlot;

	AnimatedEntity SlotOne;
	AnimatedEntity SlotTwo;
	AnimatedEntity SlotThree;

	bool IsActive;

	Sound LoopSound;

	LobbyPawn curPlayer;
	TimeSince timeLastSpin;
	const float timePawnExpiry = 45.0f;

	public bool CanPlaySlots()
	{
		if ( curPlayer == null ) return false;

		//Player doesn't have enough credits
		if ( curPlayer.Credits < 2 ) return false;

		//Machine is spinning
		if( IsActive ) return false;

		return true;
	}

	public async void PlaySlots()
	{
		if ( !CanPlaySlots() )
			return;

		timeLastSpin = 0;

		LoopSound = PlaySound( "slotmachine_spin" );

		FirstSlot = DetermineChance();
		SecondSlot = DetermineChance();
		ThirdSlot = DetermineChance();

		//SlotOne.SetMaterialGroup( 1 );
		//SlotTwo.SetMaterialGroup( 1 );
		//SlotThree.SetMaterialGroup( 1 );

		IsActive = true;
		await SpinSlots();
		IsActive = false;

		LoopSound.Stop();

		if ( GetWinnings() > 0 )
		{
			if ( curPlayer == null ) return;

			int reward = GetWinnings() * 2;

			PlaySound( "slotmachine_win" );
			DisplayMessage( To.Single( curPlayer ), $"You won {reward} credits", 12.5f );
			curPlayer.AddCredits( reward );
		}
	}
	[ClientRpc]
	public void DisplayMessage( string msg, float time = 5.0f )
	{
		Log.Warning( $"SlotMachine - {msg}" );

		BaseHud.Current.NotificationManager.AddNotification( msg, NotificationType.Info, time );
	}

	protected override void OnDestroy()
	{
		FreeUpSlot();

		base.OnDestroy();
	}

	public override void Simulate( IClient cl )
	{
		//We have no current player
		if ( curPlayer == null ) return;

		if ( curPlayer.Position.Distance( Position ) > 64.0f )
			FreeUpSlot();

		//Player took too long to do anything
		if ( timeLastSpin >= timePawnExpiry || Input.Pressed( InputButton.SecondaryAttack ) || Input.Pressed(InputButton.Jump) )
			FreeUpSlot();

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
			PlaySlots();


		base.Simulate( cl );
	}

	public void TakeSlot(LobbyPawn pawn)
	{
		if ( !CanTakeSlot( pawn ) ) return;

		Input.SuppressButton( InputButton.Use );
		timeLastSpin = 0;

		curPlayer = pawn;
		curPlayer.FocusedEntity = this;
		curPlayer.FreezeMovement = MainPawn.FreezeEnum.Movement;
	}

	public void FreeUpSlot()
	{
		if ( curPlayer == null ) return;

		if ( IsActive ) return;

		curPlayer.FreezeMovement = MainPawn.FreezeEnum.None;
		curPlayer.FocusedEntity = null;
		curPlayer = null;
	}

	//Async task to play animations
	public async Task SpinSlots()
	{
		await Task.DelayRealtimeSeconds( 3.0f );

		//SlotOne.SetMaterialGroup( ShowWinningGroup( FirstSlot ) );
		PlaySound( "slotmachine_stop" );

		await Task.DelayRealtimeSeconds( 0.5f + Game.Random.Float( -0.08f, 0.24f ) );

		//SlotTwo.SetMaterialGroup( ShowWinningGroup( SecondSlot ) );
		PlaySound( "slotmachine_stop" );

		await Task.DelayRealtimeSeconds( 0.5f + Game.Random.Float( -0.16f, 0.50f ) );

		//SlotThree.SetMaterialGroup( ShowWinningGroup( ThirdSlot ) );
		PlaySound( "slotmachine_stop" );
	}

	//Shows the winning (if none) materials on that slot
	public int ShowWinningGroup( DefaultSlotEnum slotWon )
	{
		int[] noneSlots = new int[] { 3, 5, 7, 9, 11 };

		switch ( slotWon )
		{
			case DefaultSlotEnum.None: return noneSlots[Game.Random.Int( 0, noneSlots.Length - 1 )];
			case DefaultSlotEnum.Diamond: return 2;
			case DefaultSlotEnum.SingleBar: return 4;
			case DefaultSlotEnum.DoubleBar: return 6;
			case DefaultSlotEnum.TripleBar: return 8;
			case DefaultSlotEnum.LuckySeven: return 10;
		}

		return 0;
	}

	//Determines before reveal the chance of getting one of either or none
	public DefaultSlotEnum DetermineChance()
	{
		var i = Game.Random.Int( 1, 10 );

		switch ( i )
		{
			case 1:
				return DefaultSlotEnum.SingleBar;
			case 2:
				return DefaultSlotEnum.None;
			case 3:
				return DefaultSlotEnum.DoubleBar;
			case 4:
				return DefaultSlotEnum.None;
			case 5:
				return DefaultSlotEnum.TripleBar;
			case 6:
				return DefaultSlotEnum.None;
			case 7:
				return DefaultSlotEnum.LuckySeven;
			case 8:
				return DefaultSlotEnum.None;
			case 9:
				return DefaultSlotEnum.Diamond;
			case 10:
				return DefaultSlotEnum.None;
		}

		return DefaultSlotEnum.None;
	}

	public bool CanTakeSlot( LobbyPawn pawn )
	{
		if ( Game.IsClient ) return false;

		//slots are already spinning
		if ( IsActive ) return false;

		//The player isn't sitting down
		if ( pawn.GroundEntity == null ) return false;

		return true;
	}

	//Get any winnings and give to the current player
	public int GetWinnings()
	{
		//Any diamonds earned in the line
		int diamondShown = 0;

		if ( FirstSlot == DefaultSlotEnum.Diamond ) diamondShown++;
		if ( SecondSlot == DefaultSlotEnum.Diamond ) diamondShown++;
		if ( ThirdSlot == DefaultSlotEnum.Diamond ) diamondShown++;

		switch ( diamondShown )
		{
			case 1: return 2;
			case 2: return 10;
			case 3: return 1000;
		}


		if ( CheckLineups( DefaultSlotEnum.SingleBar ) ) return 10;
		if ( CheckLineups( DefaultSlotEnum.DoubleBar ) ) return 20;
		if ( CheckLineups( DefaultSlotEnum.TripleBar ) ) return 40;
		if ( CheckLineups( DefaultSlotEnum.LuckySeven ) ) return 100;

		if ( CheckAndGetAnyBars() > 0 ) return CheckAndGetAnyBars();

		return 0;
	}

	public int CheckAndGetAnyBars()
	{
		int barReward = 0;

		switch ( FirstSlot )
		{
			case DefaultSlotEnum.SingleBar: barReward++; break;
			case DefaultSlotEnum.DoubleBar: barReward++; break;
			case DefaultSlotEnum.TripleBar: barReward++; break;
		}

		switch ( SecondSlot )
		{
			case DefaultSlotEnum.SingleBar: barReward++; break;
			case DefaultSlotEnum.DoubleBar: barReward++; break;
			case DefaultSlotEnum.TripleBar: barReward++; break;
		}

		switch ( ThirdSlot )
		{
			case DefaultSlotEnum.SingleBar: barReward++; break;
			case DefaultSlotEnum.DoubleBar: barReward++; break;
			case DefaultSlotEnum.TripleBar: barReward++; break;
		}

		if ( barReward == 3 ) return 5;

		return 0;
	}

	public bool CheckLineups( DefaultSlotEnum slotType )
	{
		if ( FirstSlot == slotType && SecondSlot == slotType && ThirdSlot == slotType ) return true;

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return user is LobbyPawn;
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		TakeSlot( user as LobbyPawn );

		//TODO: Player Casino Chips

		return false;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		/*SlotOne = new AnimatedEntity( "" );
		SlotTwo = new AnimatedEntity( "" );
		SlotThree = new AnimatedEntity( "" );

		SlotOne.Position = Position;
		SlotTwo.Position = Position;
		SlotThree.Position = Position;

		SlotOne.Rotation = Rotation;
		SlotTwo.Rotation = Rotation;
		SlotThree.Rotation = Rotation;

		SlotOne.SetParent( this );
		SlotTwo.SetParent( this );
		SlotThree.SetParent( this );

		SlotOne.LocalPosition += new Vector3( 0, -5, 0 );
		SlotThree.LocalPosition += new Vector3( 0, 5, 0 );*/

		IsActive = false;

		/*switch(MachineVariant)
		{
			TODO: Slot machine skin variants
		}*/

	}
}

