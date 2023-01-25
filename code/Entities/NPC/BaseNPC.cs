using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

//Base NPC, this can be a shop keepers or hostile
public class BaseNPC : AnimatedEntity
{
	public virtual string BaseModel => "models/citizen/citizen.vmdl";

	public override void Spawn()
	{
		base.Spawn();

		SetModel( BaseModel );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}
