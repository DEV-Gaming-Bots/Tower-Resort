using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace SBOXTower.UI.components
{
	public class HeaderPanel : Panel
	{
		public Label textLabel;
		public HeaderPanel( string title, bool haveMarginTop = true )
		{
			Panel MainHeaderPanel = Add.Panel( "HeaderPanel" );
			textLabel = MainHeaderPanel.Add.Label( title, "headerTitle" );
			MainHeaderPanel.Add.Panel( "headerSeparator" );

			if ( haveMarginTop )
			{
				AddClass( "addMarginTop" );
			}
		}
		public string Title
		{
			get { return textLabel.Text; }
			set { textLabel.Text = value; }
		}
	}
	public class ClientSuitePanel : Panel
	{
		public Label SuiteTitle;
		public Panel SuiteBackground;
		public Label suiteName;
		public Button CheckoutBtn;
		public ClientSuitePanel()
		{
			Panel RootPanel = Add.Panel( "ClientSuitePanel" );
			SuiteBackground = RootPanel.Add.Panel( "SuiteBackground" );
			RootPanel.AddChild( new HeaderPanel( "Your Condo." ) );
			Panel SuiteInfo = RootPanel.Add.Panel( "SuiteInfo" );
			Panel SuiteInfoImage = SuiteInfo.Add.Panel( "SuiteInfoImage" );

			Panel SuiteInfoStatus = SuiteInfo.Add.Panel( "SuiteInfoStatus" );
			suiteName = SuiteInfoStatus.Add.Label( "No Name", "suiteText SuiteTitle" );
			CheckoutBtn = SuiteInfoStatus.Add.Button( "Check out", "checkoutBtn" );
		}
	}
	public class TRButton : Panel
	{
		public Button btn;
		public bool disabled;
		public TRButton( string text, string className, Action OnClick )
		{
			btn = Add.Button( text, className, () =>
			{
				if ( disabled )
					return;

				OnClick();
			} );
		}

		public class PrimaryTRButton : Panel
		{
			public Button btn;
			public bool disabled;

			public PrimaryTRButton( string text, string className, Action OnClick )
			{
				btn = Add.Button( text, className, () =>
				{
					if ( disabled )
						return;

					OnClick();
				} );
			}

			public string text
			{
				get { return btn.Text; }
				set { btn.Text = value; }
			}

			public bool isDisabled
			{
				get { return disabled; }
				set
				{
					disabled = value;
					btn.SetClass( "disabled", value );
				}
			}
		}

		public string text
		{
			get { return btn.Text; }
			set { btn.Text = value; }
		}

		public bool isDisabled
		{
			get { return disabled; }
			set
			{
				disabled = value;
				btn.SetClass( "disabled", value );
			}
		}

	}
	public class Condos
	{
		public class CondoItem : Panel
		{
			public Label itemName;
			public Label itemDescription;
			public Panel itemImage;
			public CondoItem(Action onbuy = null, Action onreturn = null, Action onBodyCLick = null)
			{
				itemName  = Add.Label( "title", "title" );
				itemImage = Add.Label( "description", "description" );
				Panel Actions = Add.Panel();
				if ( onbuy != null )
				{
					Actions.AddChild( new TRButton.PrimaryTRButton( "Get", "get-condo" ,onbuy ) );
				}
				if ( onreturn != null)
				{
					Actions.AddChild( new TRButton( "Return", "get-condo" , onreturn ) );
				}
				if ( onBodyCLick != null)
				{
					AddEventListener( "onClick", () => onBodyCLick() );
				}

 			}
		}
		public class CondoFullInfo : Panel
		{
			public Label itemName;
			public Label itemDescription;
			public Panel itemImage;
			public Panel itemContent;
			public Panel footer;
			public CondoFullInfo()
			{
				StyleSheet.Load( "" );
				itemImage = Add.Panel( "itemImage" );
				itemContent = Add.Panel( "itemContent" );
				Panel itemInfo = itemContent.Add.Panel( "iteminfo" );
				itemName = itemInfo.Add.Label( "", "title" );
				itemDescription = itemInfo.Add.Label( "descrition", "description" );
				itemInfo.Add.Label( "Descrition by Jeff", "jeff" );

				footer = itemContent.Add.Panel("footer");
			}
		}
		public class CondosReceptionists : Panel
		{
			public Label itemName;
			public Label itemDescription;
			public Panel itemImage;
			public Panel itemContent;
			public Panel footer;

			public Panel shop;

			public CondosReceptionists(Panel Buttons = null)
			{
				shop = Add.Panel( "shop" );
				CondoFullInfo fi = new();
				shop.AddChild( new CondoItem( null, null, () =>
				{
					fi.footer.DeleteChildren( );
					fi.itemName.SetText( "item 1" );
					fi.footer.AddChild( new TRButton.PrimaryTRButton( "Primary Button", "get-condo", () => { } ) );
					fi.footer.AddChild( new TRButton( "Normal Button", "get-condo", () => { } ) );
				} ) );
				shop.AddChild( new CondoItem(null,null, () =>
				{
					fi.footer.DeleteChildren( );
					fi.itemName.SetText( "item 2" );
					Panel FunnyButtons = Add.Panel();
					fi.footer.AddChild( new TRButton.PrimaryTRButton( "Get Condo", "get-condo", () => { } ) );
				} ));
				shop.AddChild( new CondoItem( null, null, () =>
				{
					fi.footer.DeleteChildren( );
					fi.itemName.SetText( "item 3" );
				} ) );
				AddChild( fi );
			}

			public override void Tick()
			{
				base.Tick();
			}
		}

	}
}
