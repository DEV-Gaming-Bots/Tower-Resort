using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
/*using Components.SBOXTower;*/
using Sandbox;
using Editor;
using TowerResort.Player;

//Interactable NPCs
public partial class InteractNPCBase : BaseNPC, IUse
{
	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
	}

	public virtual void Interact( Entity user )
	{
		//Do stuff
		InteractClient(To.Single(user));
	}

	[ClientRpc]
	public virtual void InteractClient()
	{
		//Do stuff on client
		//THIS MAY NOT BE NEEDED
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

	public bool OnUse( Entity user )
	{
		Interact( user );
		return false;
	}
}

[Library("tr_condo_receptionist")]
[Title("Condo Receptionist"), Category("Condo")]
[EditorModel("models/citizen/citizen.vmdl")]
[HammerEntity]
public partial class CondoReceptionist : InteractNPCBase
{
	/*sboxtowerui.CondoTower condoPanel;*/

	List<Entity> users;

	public override void Spawn()
	{
		base.Spawn();
		users = new List<Entity>();
	}

	public override void Interact( Entity user )
	{
		base.Interact( user );

		if ( user is MainPawn player )
		{
			if ( users.Contains( player ) )
				users.Remove( player );
			else
				users.Add( player );
		}
	}

	[Event.Tick.Server]
	public void Simulate()
	{
		if ( users.Count <= 0 )
			return;

		foreach ( Entity player in users.ToArray() )
		{
			/*if ( Position.Distance( player.Position ) > 90.0f )
			{
				RemoveCondoPanel( To.Single( player.Client ) );
				users.Remove( player );
			}*/
		}
	}

/*	[ClientRpc]
	public override void InteractClient()
	{
		if( condoPanel != null )
		{
			RemoveCondoPanel();
			return;
		}

		condoPanel = new sboxtowerui.CondoTower();
		condoPanel.open = true;
		BaseHud.Current.AddChild( condoPanel );
	}

	[ClientRpc]
	public void RemoveCondoPanel()
	{
		condoPanel?.Delete();
		condoPanel = null;
	}*/
}
