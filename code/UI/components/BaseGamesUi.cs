using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Components.BaseGamesUi;

public class BaseGamesUi
{
	public class BaseTabletopLayout : Panel
	{
		public Panel top;
		public Panel middle;
		public Panel bottom;

		public BaseTabletopLayout()
		{
			StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BaseTabletopLayout.scss" );
			top = Add.Panel( "mainbaselayout top" );
			middle = Add.Panel( "mainbaselayout middle" );
			bottom = Add.Panel( "mainbaselayout bottom" );
		}
	}

	public class BaseGameValueEntry : Panel
	{
		public Label value;
		public Label name;

		public BaseGameValueEntry( string name, string value )
		{
			StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BaseGameValueEntry.scss" );
			this.name = Add.Label( name, "name" );
			this.name.AddClass( "background" );
			this.value = Add.Label( value, "value" );
		}
	}
	public class BaseGameValues : Panel
	{
		public Label value;

		public BaseGameValues( string maxwidth = "100%" )
		{
			StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BaseGameValues.scss" );
			Style.Set( "max-width", maxwidth );
		}

		public BaseGameValueEntry AddValue( string name, string value )
		{
			BaseGameValueEntry item = new( name, value );
			AddChild( item );
			return item;
		}

		public BaseGameValueEntry AddValue( BaseGameValueEntry panel )
		{
			AddChild( panel );
			return panel;
		}
	}

	public class PlayerEntry : Panel
	{
		public Panel bottom;
		public string playerName;
		public PlayerEntry( string plrname )
		{
			playerName = plrname;
			Add.Label( playerName );
			bottom = Add.Panel( "bottom" );
		}
	}

	public class PlayerItem : IEquatable<PlayerItem>
	{
		public Panel panel;

		public bool Equals( PlayerItem other )
		{
			return panel == other.panel;
		}
	}

	public class PlayersPanel : Panel
	{
		/* players list */
		public List<PlayerEntry> playerslist = new List<PlayerEntry>();

		public Panel players;
		public PlayersPanel( bool disableStyleSheet = false )
		{
			if ( !disableStyleSheet )
			{
				StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BasePlayersPanel.scss" );
			}
		}

		public PlayerItem AddPlayer( Panel panel )
		{
			PlayerItem item = new() { panel = panel, };
			AddChild( item.panel );

			return item;
		}
		public void RemovePlayer( PlayerEntry item )
		{
			//item.panel.Delete();
			playerslist.Remove( item );
		}

		public void RemovePlayer( string name )
		{
			foreach ( var panel in Children.ToArray() )
			{
				if ( panel is PlayerEntry entry && entry.playerName == name )
					RemovePlayer( entry );
			}
		}

		public void RemoveAllPlayers()
		{
			foreach ( var item in playerslist )
			{
				item.Delete();
			}

			playerslist.Clear();
			/*players.DeleteChildren();*/
		}
	}

	public class ButtonActions : Panel
	{
		public class ButtonAction : Panel
		{
			public string actionName;
			public Label actiontext;
			public Action action;
			/// <summary>
			/// ⚠ WARNING ⚠: This is a hacky way to do a button for ButtonActions. It's not recommended to use this. ⚠ WARNING ⚠. Internal use only.
			/// </summary>
			/// <param name="text"></param>
			public ButtonAction( string text )
			{
				actionName = text;
				actiontext = Add.Label("[ NO TEXT ]");
				AddEventListener( "onclick", ( e ) =>
				{
					action?.Invoke();
				} );
			}
		}

		public Panel actions;
		public ButtonActions()
		{
			StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BaseGameButtonsActions.scss" );
			actions = Add.Panel( "Buttons" );
		}

		public ButtonAction AddButton( string name, string classnames, string text )
		{
			ButtonAction item = new( name ) { actionName = name };
			item.actiontext.SetText( text );
			item.AddClass( classnames );
			actions.AddChild( item );

			return item;
		}

		public ButtonAction AddButton( string name, string text )
		{
			ButtonAction item = AddButton( name, null, text );
			return item;
		}
	}

	public class CardEntry : Panel
	{
		public Label cardlabel;
		public Action onclick = null;

		public CardEntry()
		{
			if ( onclick != null )
			{
				AddEventListener( "onclick", () =>
				{
					onclick();
				} );
			}

			cardlabel = Add.Label( "", "" );
		}
	}

	public class DeckCardsPanel : Panel
	{
		public Panel cards;
		public bool clickable {
			get { return cards.HasClass( "clickable" ); }
			set 
			{
				if ( value )
				{
					AddClass( "clickable" );
				}
				else
				{
					RemoveClass( "clickable" );
				}
			} 
		}
		public DeckCardsPanel( string cardsstylesheet = "" )
		{
			StyleSheet.Load( "UI/Components/BaseGamesUIStyles/BaseCardsPanel.scss" );
			cards = Add.Panel( "Cards" );
			cards.Style.Set( cardsstylesheet );
		}

		/// <summary>
		/// Add a card to the panel, and provide a path for the image
		/// </summary>
		/// <param name="cardtext">d</param>
		/// <param name="className"></param>
		/// <param name="OnClick"></param>
		/// <returns></returns>
		public CardEntry AddCard( string cardtext, string className )
		{
			CardEntry card = new();
			cards.AddChild( card );
			card.AddClass( className );
			card.cardlabel.SetText( cardtext );
			return card;
		}


		public CardEntry AddCard( string cardtext, string className , string texturepath )
		{
			CardEntry card = new();
			cards.AddChild( card );
			card.AddClass( className );
			card.cardlabel.SetText( cardtext );
			card.Style.Set( "background-image", $"url({texturepath})" );
			return card;
		}

		public void ClearCardsPanel()
		{
			cards.DeleteChildren();
		}
	}
}
