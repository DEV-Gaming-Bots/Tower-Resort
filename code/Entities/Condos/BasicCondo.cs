using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Condos;

public class BasicCondo : ModelEntity
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

	public override void Spawn()
	{
		base.Spawn();
		Model = WorldModel;
	}
}
