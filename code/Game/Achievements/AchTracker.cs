using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Player;

namespace TowerResort.Achievements;

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

		if ( Entity == null || ach.PawnType != Entity.GetType() ) return;

		ach.UpdateProgress( update );

		if ( ach.Progress % ach.PerUpdateNotify == 0 && !ach.IsSecret )
			Entity.DisplayNotification( To.Single( Entity ), $"{ach.Name} - {ach.Progress}/{ach.Goal}", 7.5f );
	}

	public int GetAchievementProgress(Type type)
	{
		var ach = AchList.FirstOrDefault( x => x.GetType() == type );

		if ( ach != null )
			return ach.GetProgress();

		return -1;
	}

	public void DoCompletion(AchBase ach)
	{
		Entity.AddCredits( ach.RewardCredits );
		CompletedAchList.Add( ach );

		TRGame.ServerAnnouncement($"{Entity.Client.Name} has obtained the achievement: {ach.Name}");
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
