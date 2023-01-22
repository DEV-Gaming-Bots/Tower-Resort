using Sandbox;
using System;
using TowerResort.Player;

namespace TowerResort.Achievements;

public class AchBase
{
	public virtual string Name => "Base Achievement";
	public virtual string Description => "A base achievement";
	public virtual bool IsSecret => false;
	public virtual int PerUpdateNotify => 5;
	public virtual int Goal => 1;
	public virtual int RewardCredits => 500;

	public int Progress;

	public bool IsCompleted;

	//The type of pawn the player needs to be for any achievement progress
	public virtual Type PawnType => typeof(MainPawn);

	public virtual void UpdateProgress(int update)
	{
		if ( IsCompleted ) return;

		Progress += update;
		Progress = Progress.Clamp( 0, Goal );

		if ( Progress % PerUpdateNotify == 0 && !IsSecret )
		{
			//TODO: achievement notifications
		}

		if ( Progress == Goal )
			IsCompleted = true;
	}

	public int GetProgress()
	{
		return Progress;
	}
}

