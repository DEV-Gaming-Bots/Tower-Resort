
using Sandbox;

namespace TowerResort.Player;

public class Unstuck
{
	public StandardController Controller;

	public bool IsActive; // replicate

	internal int StuckTries = 0;

	public Unstuck( StandardController controller )
	{
		Controller = controller;
	}

	public virtual bool TestAndFix()
	{
		var result = Controller.TraceBBox( Controller.Owner.Position, Controller.Owner.Position );

		// Not stuck, we cool
		if ( !result.StartedSolid )
		{
			StuckTries = 0;
			return false;
		}

		if ( result.StartedSolid )
		{
			if ( StandardController.Debug )
			{
				DebugOverlay.Text( $"[stuck in {result.Entity}]", Controller.Owner.Position, Color.Red );
				DebugOverlay.Box( result.Entity, Color.Red );
			}
		}

		//
		// Client can't jiggle its way out, needs to wait for
		// server correction to come
		//
		if ( Game.IsClient )
			return true;

		int AttemptsPerTick = 20;

		for ( int i = 0; i < AttemptsPerTick; i++ )
		{
			var pos = Controller.Owner.Position + Vector3.Random.Normal * (((float)StuckTries) / 2.0f);

			// First try the up direction for moving platforms
			if ( i == 0 )
			{
				pos = Controller.Owner.Position + Vector3.Up * 5;
			}

			result = Controller.TraceBBox( pos, pos );

			if ( !result.StartedSolid )
			{
				if ( StandardController.Debug )
				{
					DebugOverlay.Text( $"unstuck after {StuckTries} tries ({StuckTries * AttemptsPerTick} tests)", Controller.Owner.Position, Color.Green, 5.0f );
					DebugOverlay.Line( pos, Controller.Owner.Position, Color.Green, 5.0f, false );
				}

				Controller.Owner.Position = pos;
				return false;
			}
			else
			{
				if ( StandardController.Debug )
				{
					DebugOverlay.Line( pos, Controller.Owner.Position, Color.Yellow, 0.5f, false );
				}
			}
		}

		StuckTries++;

		return true;
	}
}
