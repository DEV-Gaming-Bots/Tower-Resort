using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Player.VR;

public partial class LeftHand : VRHandEntity
{
	protected override string ModelPath => "models/hands/handleft.vmdl";
	public override Input.VrHand InputHand => Input.VR.LeftHand;

	public override void Spawn()
	{
		base.Spawn();
		Log.Info( "VR Controller Right Spawned" );
		Tags.Add( "vrhand" );
	}
}

