using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace TowerResort.UI;

public class PokerTablePanel : WorldPanel
{
	public Label EntryLabel;

	int entryFee = 0;

	public PokerTablePanel()
	{
		StyleSheet.Load( "UI/Styles/Lobby/PokerTablePanel.scss" );
	}

	public PokerTablePanel(int fee) : this()
	{
		entryFee = fee;

		if ( entryFee <= 0 )
			EntryLabel = Add.Label( $"Entry Fee: FREE" );
		else
			EntryLabel = Add.Label( $"Entry Fee: ${entryFee}" );
	}
}

