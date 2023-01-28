using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace TowerResort.UI
{
	/*
	 * PUBLIC DOMAIN
	 * CREATED BY ALEXVEEBEE 
	 */
	public class TRHubTracker : BaseHud
	{
		public TRHubTracker()
		{
			StyleSheet.Load( "/UI/Styles/TRHubTracker.scss" );
		}
	}

	public class TRBaseUIComponents
	{
		public class ThemePanel : Panel
		{
			private bool _darktheme;
			public bool darktheme
			{
				set
				{
					Log.Info( "[ThemeTest]: Switching Themes" );
					_darktheme = value;
					switch ( _darktheme )
					{
						case true:
							AddClass( "theme-dark" );
							RemoveClass( "theme-light" );
							break;
						case false:
							AddClass( "theme-light" );
							RemoveClass( "theme-dark" );
							break;
					}
				}
				get
				{
					return _darktheme;
				}
			}
			public ThemePanel()
			{
				StyleSheet.Load( "/UI/Styles/TRHud.blub.global.scss" );
				StyleSheet.Load( "/UI/Styles/TRHud.blub.light.scss" );
				StyleSheet.Load( "/UI/Styles/TRHud.blub.dark.scss" );
				StyleSheet.Load( "/UI/Styles/vars.scss" );
			}
		}
		public class EmptyBasePanel : Panel
		{
			public string Theme = "theme-blub";
			public bool inclideThemes = true;

			public EmptyBasePanel()
			{
				AddClass( "panel " + Theme );
			}
		}
		public class BaseMessagePanel : EmptyBasePanel
		{
			/* styles: info and dnager */
			public class Info : BaseMessagePanel
			{
				public Info()
				{
					AddClass( "info" );
				}
			}
			public class Danger : BaseMessagePanel
			{
				public Danger()
				{
					AddClass( "danger" );
				}
			}


			public Panel header;
			public Panel body;
			public Panel footer;
			public BaseMessagePanel()
			{
				AddClass( "themeSet" );
				header = Add.Panel( "header" );
				body = Add.Panel( "body" );
				footer = Add.Panel( "footer" );
			}
		}
		public class TRButton : Panel
		{
			public Action onClick = null;
			
			public class Primary : TRButton
			{
				public Primary( string Text, Action onClick = null ) : base( Text , onClick )
				{
					AddClass( "btn-prim" );
				}
			}
			public class Danger : TRButton
			{
				public Danger( string Text, Action onClick = null ) : base( Text, onClick )
				{
					AddClass( "btn-danger" );
				}
			}
			

			public TRButton(string Text, Action onClick = null)
			{
				AddClass( "TRButton btn" );
				Add.Label( Text );

				if ( onClick != null )
				{
					this.onClick = onClick;
				}

				AddEventListener( "onclick", ( e ) =>
				{
					if (!HasClass("disabled"))
						this.onClick?.Invoke();
				} );
			}
		}
	
	}
	public class TRTextEntry : TextEntry
	{
		public string placeholder;
		public override void Tick()
		{
			base.Tick();
			/*Input.Placeholder = string.IsNullOrEmpty( Input.Text ) ? "Enter your message..." : string.Empty;*/
			/* if not empty, hide placeholder */
			Placeholder = string.IsNullOrEmpty( Text ) ? placeholder : string.Empty;
		}
	}
}

/* ####################################################### */
/* DEBUG AND TESTING */

/*public class TR_UI
{

}*/

