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

	public void NewStats()
	{
		Credits = 500;
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

