using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Condos;

[Prefab]
public partial class CondoLight : Entity
{
	[Prefab] public float LightRadius { get; set; } = 2f;

	[Prefab] public Color LightColor { get; set; } = Color.White;
}
