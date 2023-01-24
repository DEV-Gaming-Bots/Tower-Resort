using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace TowerResort.Player;

public partial class MainPawn
{
	public string PlayerName => Client.Name;
	[Net, Local] public int Credits { get; set; }
	public long SteamID => Client.SteamId;
	public List<Vector3> CondoInfoPosition { get; set; }
	public List<Rotation> CondoInfoRotation { get; set; }
	public List<string> CondoInfoAsset { get; set; }

	public PlayerData DataFile;

	public void NewStats()
	{
		Credits = 500;

		CondoInfoPosition = new();
		CondoInfoRotation = new();
		CondoInfoAsset = new List<string>();

		TRGame.Instance.DoSave( Client );
		DataFile = FileSystem.Data.ReadJson<PlayerData>( $"{Client.SteamId}.json" );
	}

	public int GetCredits()
	{
		return Credits;
	}

	public void AddCredits(int amt)
	{
		Credits += amt;
	}

	public void TakeCredits( int amt )
	{
		Credits -= amt;
	}
}

