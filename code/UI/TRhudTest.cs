using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using TowerResort.UI;

public class TRHudTest : TRBaseUIComponents.ThemePanel
{
	public bool open = false;
	public TRHudTest() {
		darktheme = true;
		Style.Set( "display", open ? "flex" : "none" );
		Style.Set( "position", "absolute" );
		Style.Set( "pointer-events", "all" );

		Panel EnabledButtonsTest = Add.Panel("enabled-buttons-test");
		TRBaseUIComponents.BaseMessagePanel infopanel = new();
		infopanel.header.Add.Label( "Test - Info Panel", "title" );
		/* ENABLED BUTTONS */

		infopanel.Style.Set("pointer-events", "all");
		TRBaseUIComponents.TRButton testbutton = new("Change Theme")
		{
			onClick = () =>
			{
				darktheme = !darktheme;
			}
		};
		TRBaseUIComponents.TRButton.Primary testbuttonPrimary = new("Hello World");
		TRBaseUIComponents.TRButton.Danger testbuttonDanger = new("Hello World");
		EnabledButtonsTest.AddChild(testbutton);
		EnabledButtonsTest.AddChild(testbuttonPrimary);
		EnabledButtonsTest.AddChild(testbuttonDanger);
		/* DISABLED BUTTONS */

		Panel DisabledButtonsTest = Add.Panel("enabled-buttons-test");
		TRBaseUIComponents.TRButton dtestbutton = new("Change Theme")
		{
			onClick = () =>
			{
				darktheme = !darktheme;
			}
		};
		TRBaseUIComponents.TRButton.Primary dtestbuttonPrimary = new("Hello World");
		TRBaseUIComponents.TRButton.Danger dtestbuttonDanger = new("Hello World");
		DisabledButtonsTest.AddChild(dtestbutton);
		DisabledButtonsTest.AddChild(dtestbuttonPrimary);
		DisabledButtonsTest.AddChild(dtestbuttonDanger);
		dtestbutton.AddClass("disabled");
		dtestbuttonPrimary.AddClass("disabled");
		dtestbuttonDanger.AddClass("disabled");
		/*  */
		infopanel.body.Style.Set( "flex-direction", "column" );
		infopanel.body.AddChild(EnabledButtonsTest);
		infopanel.body.AddChild(DisabledButtonsTest);

		DropDown dr = new();
		dr.Options.Add( new Option()
		{
			Icon = "home",
			Title = "Normal",
			Value = "0",
			Tooltip = "Normal",
			Subtitle = "Normal",
		} );
		dr.Options.Add( new Option()
		{
			Icon = "add",
			Title = "Primary",
			Value = "1"
		} );
		dr.Options.Add( new Option()
		{
			Icon = "warning",
			Title = "Danger",
			Value = "2"
		} );
		dr.Value = "1";


		/* OTHER BASEMESSAGEPANEL TYPES */
		TRBaseUIComponents.BaseMessagePanel normalinfopanel = new();
		normalinfopanel.header.Add.Label( "Info Panel" );

		TRBaseUIComponents.BaseMessagePanel.info infoinfopanel = new();
		infoinfopanel.header.Add.Label( "Info Panel" );

		TRBaseUIComponents.BaseMessagePanel.danger dangerinfopanel = new();
		dangerinfopanel.header.Add.Label( "danger Panel" );


		infopanel.body.AddChild( dr );
		infopanel.body.AddChild( normalinfopanel );
		infopanel.body.AddChild( infoinfopanel );
		infopanel.body.AddChild( dangerinfopanel );


		AddChild( infopanel);
	}

	public override void Tick()
	{
		base.Tick();

		/* style display  */
		if ( Input.Pressed( InputButton.Menu ) )
		{
			open = !open;
			Style.Set( "display", open ? "flex" : "none" );
		}

	}
}
