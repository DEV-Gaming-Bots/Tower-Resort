using Sandbox;
using TheHub.Entities.Base;

public partial class WeaponViewModel : AnimatedEntity
{
	/// <summary>
	/// All active view models.
	/// </summary>
	public static WeaponViewModel Current;

	protected WeaponBase WeaponBase { get; init; }

	public WeaponViewModel( WeaponBase weapon )
	{
		if ( Current.IsValid() )
		{
			Current.Delete();
		}

		Current = this;
		EnableShadowCasting = false;
		EnableViewmodelRendering = true;
		WeaponBase = weapon;
	}

	protected override void OnDestroy()
	{
		Current = null;
	}

	[Event.Client.PostCamera]
	public void PlaceViewmodel()
	{
		if ( Game.IsRunningInVR )
			return;

		Camera.Main.SetViewModelCamera( 80f, 1, 500 );
		Current.Position = Camera.Position;
		Current.Rotation = Camera.Rotation;
	}

	public override Sound PlaySound( string soundName, string attachment )
	{
		if ( Owner.IsValid() )
			return Owner.PlaySound( soundName, attachment );

		return base.PlaySound( soundName, attachment );
	}
}
