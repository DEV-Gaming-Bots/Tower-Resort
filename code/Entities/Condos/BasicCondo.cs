using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Condos;

//Since we can't have nice things... like prefab loading
//MAYBE???
public class CondoBase : ModelEntity
{
	public Vector3[] LightPositions = new Vector3[]
	{
		new Vector3(-57, -16, 24),
		new Vector3(-60, 162, 24),
		new Vector3(151, 122, 24),
		new Vector3(-25, -172, 12),
	};

	public float[] LightRadius = new float[]
	{
		256.0f,
		148.0f,
		128.0f,
		112.0f,
	};

	public Color[] LightColor = new Color[]
	{
		new Color(1.0f, 0.89f, 0.7f),
		new Color(1.0f, 0.89f, 0.7f),
		new Color(0.45f, 1.0f, 1.0f),
		new Color(1.0f, 0.89f, 0.7f),
	};

	public Model WorldModel => Model.Load( "models/condo/basic_condo.vmdl" );
	public Vector3 DoorPos => new Vector3( -23.75f, -226, -60 );
	public Vector3 TPPos => new Vector3( -24, -186, -60 );
	public Angles TPAngles => new Angles( 0, 90, 0 );

	public Vector3[] BuyDoorPos = new Vector3[]
	{
		//Bedroom
		new Vector3(-36, 82, -52),

		//Bathroom
		new Vector3(128, 70, -52)
	};

	public Angles[] BuyDoorAngles = new Angles[]
	{
		new Angles(0, 90, 0),
		new Angles(0, 90, 0)
	};

	public Angles[] BuyDoorOpenAngles = new Angles[]
	{
		new Angles(0, 90, 0),
		new Angles(0, 90, 0)
	};

	public string[] BuyDoorModel = new string[]
	{
		"models/condo/basic_condo_door.vmdl",
		"models/condo/basic_condo_door.vmdl"
	};

	public int[] BuyDoorCosts = new int[]
	{
		1000,
		750,
	};

	public override void Spawn()
	{
		base.Spawn();
		Model = WorldModel;
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

}
