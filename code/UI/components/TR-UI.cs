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
			public class info : BaseMessagePanel
			{
				public info()
				{
					AddClass( "info" );
				}
			}
			public class danger : BaseMessagePanel
			{
				public danger()
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
				footer = Add.Panel( "footer" );
				body = Add.Panel( "body" );
			}
		}
		public class TRButton : Panel
		{
			public Action onClick = null;
			
			public class Primary : TRButton
			{
				public Primary( string Text ) : base( Text )
				{
					AddClass( "btn-prim" );
				}
			}
			public class Danger : TRButton
			{
				public Danger( string Text ) : base( Text )
				{
					AddClass( "btn-danger" );
				}
			}
			

			public TRButton(string Text)
			{
				AddClass( "TRButton btn" );
				Add.Label( Text );

				AddEventListener( "onclick", ( e ) =>
				{
					if (!HasClass("disabled"))
						onClick?.Invoke();
				} );
			}
		}
	}
}

/* ####################################################### */
/* DEBUG AND TESTING */

/*public class TR_UI
{

}*/
	
