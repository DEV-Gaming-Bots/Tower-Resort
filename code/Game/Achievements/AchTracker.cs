using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheHub.Player;

namespace TheHub.Achievements;

public class AchTracker : EntityComponent<MainPawn>, ISingletonComponent
{
	public IList<AchBase> AchList = new List<AchBase>();
	public IList<AchBase> CompletedAchList = new List<AchBase>();

	protected override async void OnActivate()
	{
		await SetUp();

		var achTypes = TypeLibrary.GetTypes( typeof( AchBase ) );

		foreach ( var achType in achTypes )
		{
			AchBase ach = (AchBase)TypeLibrary.Create( achType.ClassName, typeof( AchBase ) );

			AchList.Add( ach );
		}

		base.OnActivate();
	}

	async Task SetUp()
	{
		await Task.Delay( 500 );
	}

	public void UpdateAchievement( Type type, int update = 1 )
	{
		var ach = AchList.FirstOrDefault( x => x.GetType() == type );

		if ( ach.PawnType != Entity.GetType() ) return;

		ach.UpdateProgress( update );
	}

	public void DoCompletion(AchBase ach)
	{
		Entity.AddCredits( ach.RewardCredits );
		CompletedAchList.Add( ach );

		MainGame.ServerAnnouncement($"{Entity.Client.Name} has obtained the achievement: {ach.Name}");
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	public void Simulate()
	{
		foreach ( var ach in AchList )
		{
			if ( Entity.GetType() != ach.PawnType )
				continue;

			if ( ach.IsCompleted && !CompletedAchList.Contains( ach ) )
				DoCompletion(ach);
		}
	}
}
