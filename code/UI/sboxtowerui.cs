using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using SBOXTower.UI.components;

/* TEST FOR SBOX TOWER */
namespace Components.SBOXTower
{
	public class sboxtowerui : Panel
	{
		public class Window : Panel
		{
			public Panel Titlebar;
			public Panel WindowContent;
			public bool isMoving;
			public Vector2 DragOffset;
			public Vector2 _pos;
			public bool Dragable = true;
			public bool ShowTitlebar = true;

			public Window(string Title, Panel content, Action OnCLose = null)
			{
				Titlebar = Add.Panel( "Window Window-titlebar" );
				if ( !ShowTitlebar )
				{
					Titlebar.Style.Set( "display: none" );
				}

				Titlebar.AddEventListener( "OnMouseDown", () =>
				{
					if(Dragable ){
						isMoving = true;
						DragOffset = Titlebar.MousePosition + Vector2.Up + 1 + Vector2.Left + 1;
						_pos = DragOffset;
					}
				} );
				Titlebar.AddEventListener( "OnMouseUp", () => {
					isMoving = false;
				} );

				Panel TitleBarLeft = Titlebar.Add.Panel( "tl" );
				Panel TitleBarRight = Titlebar.Add.Panel( "tr" );
				TitleBarLeft.Add.Label( Title, "title" );
				Label closeBtn = TitleBarRight.Add.Label( "close", "icon closeBtn" );
				closeBtn.AddEventListener( "onClick", () => { 
					if ( OnCLose != null)
					{
						OnCLose();
					}

					Delete();
				} );
				WindowContent = Add.Panel( "content" );
				WindowContent.AddChild( content );
			}

			public void Move( Vector2 vec )
			=> Move( vec.x, vec.y );

			public void Move( float x, float y )
			{
				if ( Style.Left.HasValue )
					x += Style.Left.Value.Value;

				if ( Style.Top.HasValue )
					y += Style.Top.Value.Value;

				Style.Left = x;
				_pos.x = x;
				Style.Top = y;
				_pos.y = y;
				Style.Dirty();
			}


			[Event.Client.Frame]
			void Frame()
			{
				if ( isMoving )
				{
					var pos = MousePosition - DragOffset;
					Move( pos );

					if ( _pos.x <= 0 )
					{
						_pos.x = 0;
						Style.Left = 0;
					}
					if ( _pos.y <= 0 )
					{
						_pos.y = 0;
						Style.Top = 0;
					}
				}
			}
		}
		public class CondoTower : Panel
		{
			public bool open = false;
			public Panel CondoTowerMenu;
			public Panel Window;
			public Panel Titlebar;
			/* THIS WILL BE REWORKED IN THE FUTURE, COMPONENTS ARE GOING TO BE
			 * MORE BETTER, HOPEFULY.
			 */

			public CondoTower()
			{
				StyleSheet.Load( "ui/scss/Condos.scss" );
				StyleSheet.Load( "ui/scss/CondoTower.scss" );
				StyleSheet.Load( "ui/Styles/Lobby/CSUIContruct.scss" );
				StyleSheet.Load( "ui/Styles/Lobby/CondoRecept.scss" );

				CondoTowerMenu = Add.Panel("CondoTowerMenu");

				Titlebar = CondoTowerMenu.Add.Panel( "Window Window-titlebar Window-titlebar-condomenu" );
				Titlebar.Add.Label( "Tower Condo", "title" );

				Window = CondoTowerMenu.Add.Panel( "Window Window-menu-condos" );

				Panel Btn_condos = Window.Add.Panel( "menubutton op1" );
				Panel Btn_checkIn = Window.Add.Panel( "menubutton op2" );
				Panel Btn_condoInfo = Window.Add.Panel( "menubutton op3" );
				Btn_condos.Add.Label( "Condos", "title" );
				Btn_checkIn.Add.Label( "Check in", "title" );
				Btn_checkIn.Add.Label( "You dont own one yet.", "footer" );
				Btn_condoInfo.Add.Label( "Condo Info", "title" );

				Btn_condos.AddEventListener("onClick", () =>
				{
					Panel WindowContent = Add.Panel();
					WindowContent.AddChild( new Condos.CondosReceptionists() );
					Window w = new( "Condos", WindowContent, () => open = true );
					w.Dragable = false;
					w.Style.Set( "width: 90%; height: 100%; left: 5%;" );
					AddChild( w );
					open = false;
				} );
				Btn_checkIn.AddEventListener( "onClick", () =>
				{
					Panel WindowContent = Add.Panel();

					WindowContent.AddChild( new TRButton( "Get Condo", "", () =>
					{
						// Get Condo button code goes here
						// CONDO_GET_SCRIPT
					} ) );

					Window w = new( "Check in", WindowContent, () => open = true );
					w.Dragable = false;
					w.Style.Set( "width: 90%; height: 20%; left: 5%; top: 50px;" );
					w.WindowContent.Style.Set( "flex-direction: column; padding: 10px;" );
					AddChild( w );
					open = false;
				} );
				Btn_condoInfo.AddEventListener( "onClick", () =>
				{
					Panel WindowContent = Add.Panel();
					ClientSuitePanel suite = new();
					WindowContent.AddChild( suite ) ;
					Window w = new( "Condo info", WindowContent, () => open = true );
					w.Dragable = false;
					w.Style.Set( "width: 90%; height: 100%; left: 5%;" );
					AddChild( w );
					open = false;
				} );
			}

			public override void Tick()
			{
				base.Tick();
				CondoTowerMenu.SetClass( "open", open );
			}
		}
	}
}
