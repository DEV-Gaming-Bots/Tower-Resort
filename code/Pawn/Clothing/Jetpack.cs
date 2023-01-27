using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Player.UsableClothing;

public interface IClothingUsable
{
	string Name { get; }
	string Description { get; }
	Model WorldModel { get; }
}

public class Jetpack : ModelEntity, IClothingUsable
{
	public virtual string Description => "A simple jetpack that you can fly in the air";
	public virtual Model WorldModel => Model.Load( "models/jetpack/jetpack/jetpack.vmdl" );
	public virtual MainPawn User => Owner as MainPawn; 

	float jetFuel;
	TimeSince timeJetUsed;
	
	public override void Spawn()
	{
		base.Spawn();
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Model = WorldModel;
		SetParent( User, true );

		jetFuel = 100;
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( User.Controller is NoclipControl ) return;

		if( jetFuel < 100.0f && timeJetUsed > 4.0f )
		{
			jetFuel += 12.5f * Time.Delta;
			jetFuel = jetFuel.Clamp( 0.0f, 100.0f );
		}

		if(Input.Down(InputButton.Jump))
			Fly();
	}

	public void Fly()
	{
		if ( jetFuel <= 0.0f ) return;

		timeJetUsed = 0;
		jetFuel -= 50.0f * Time.Delta;
		jetFuel = jetFuel.Clamp( 0.0f, 100.0f );

		User.Velocity += Vector3.Up * 20;
	}
}
