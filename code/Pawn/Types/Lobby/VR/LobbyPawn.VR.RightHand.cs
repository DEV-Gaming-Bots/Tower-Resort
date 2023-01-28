using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Player.VR;

public partial class RightHand : VRHandEntity
{
	protected override string ModelPath => "models/vr/hands/handright.vmdl";
	public override Input.VrHand InputHand => Input.VR.RightHand;

	public override void Spawn()
	{
		base.Spawn();
		Log.Info( "VR Controller Right Spawned" );
		Tags.Add( "vrhand" );
	}
}

