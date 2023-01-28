using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_effect_reflective")]
[Title("Reflective Surface"), Category("Effect")]
[Solid, HammerEntity, AutoApplyMaterial( "materials/hammer/mirror.vmat" )]
public partial class ReflectivePlane : ModelEntity
{
	[Property, Net, DefaultValue( "materials/hammer/mirror.vmat" )]
	public Material MirrorMaterial { get; set; } = Material.Load( "materials/hammer/mirror.vmat" );
	protected ScenePortal so;

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Static );
		Transmit = TransmitType.Always;

		EnableAllCollisions = true;
		EnableSolidCollisions = false;
		EnableTouch = true;

		Predictable = false;
	}
	public override void ClientSpawn()
	{
		base.ClientSpawn();
		CreateScenePortalClient();
	}

	public override void OnNewModel( Model model )
	{
		if ( !Game.IsClient || so.IsValid() )
			return;
		base.OnNewModel( model );
		so = new ScenePortal( Sandbox.Game.SceneWorld, GeneratePortalModel(), Transform, true, (int)Screen.Width )
		{
			Transform = this.Transform,
			Position = this.Position,
			RenderShadows = true,
			RenderingEnabled = true
		};
		SceneObject.RenderingEnabled = false;
	}

	protected void CreateScenePortalClient()
	{
		if ( so.IsValid() )
		{
			so.Delete();
		}

		DisableOriginal();
	}

	private async void DisableOriginal()
	{
		await GameTask.DelaySeconds( 0.1f );
		SceneObject.RenderingEnabled = false;
		SetModel( "" );
	}

	[Event.PreRender]
	public void UpdatePortalView()
	{

		if ( !Sandbox.Game.IsClient || !so.IsValid() )
			return;
		so.RenderingEnabled = true;
		so.Rotation = Rotation;
		so.Position = Position;

		//float zNear = 1.0f;
		Plane p = new( Position, Rotation.Up );
		// Reflect
		Matrix viewMatrix = Matrix.CreateWorld( Camera.Position, Camera.Rotation.Forward, Camera.Rotation.Up );
		Matrix reflectMatrix = ReflectMatrix( viewMatrix, p );

		// Apply Rotation
		Vector3 reflectionPosition = reflectMatrix.Transform( Camera.Position );
		Rotation reflectionRotation = ReflectRotation( Camera.Rotation, Rotation.Up );

		so.ViewPosition = reflectionPosition;
		so.ViewRotation = reflectionRotation;

		//DebugOverlay.Sphere( so.ViewPosition, 10, Color.Red );
		//so.ZNear = zNear;


		//so.Aspect = Render.Viewport.Size.x / Render.Viewport.Size.y;

		so.Aspect = Screen.Width / Screen.Height;

		so.FieldOfView = MathF.Atan( MathF.Tan( Camera.FieldOfView.DegreeToRadian() * 0.41f ) * (so.Aspect * 0.75f) ).RadianToDegree() * 2.0f;

		Plane clippingPlane = new Plane( Position - so.ViewPosition, Rotation.Backward );
		// small tolerance to prevent seam
		clippingPlane.Distance -= 1.0f;

		so.SetClipPlane( clippingPlane );
	}

	private Rotation ReflectRotation( Rotation source, Vector3 normal )
	{
		return Rotation.LookAt( Vector3.Reflect( source * Vector3.Forward, normal ), Vector3.Reflect( source * Vector3.Up, normal ) );
	}

	public Matrix ReflectMatrix( Matrix m, Plane plane )
	{
		m.Numerics.M11 = (1.0f - 2.0f * plane.Normal.x * plane.Normal.x);
		m.Numerics.M21 = (-2.0f * plane.Normal.x * plane.Normal.y);
		m.Numerics.M31 = (-2.0f * plane.Normal.x * plane.Normal.z);
		m.Numerics.M41 = (-2.0f * -plane.Distance * plane.Normal.x);

		m.Numerics.M12 = (-2.0f * plane.Normal.y * plane.Normal.x);
		m.Numerics.M22 = (1.0f - 2.0f * plane.Normal.y * plane.Normal.y);
		m.Numerics.M32 = (-2.0f * plane.Normal.y * plane.Normal.z);
		m.Numerics.M42 = (-2.0f * -plane.Distance * plane.Normal.y);

		m.Numerics.M13 = (-2.0f * plane.Normal.z * plane.Normal.x);
		m.Numerics.M23 = (-2.0f * plane.Normal.z * plane.Normal.y);
		m.Numerics.M33 = (1.0f - 2.0f * plane.Normal.z * plane.Normal.z);
		m.Numerics.M43 = (-2.0f * -plane.Distance * plane.Normal.z);

		m.Numerics.M14 = 0.0f;
		m.Numerics.M24 = 0.0f;
		m.Numerics.M34 = 0.0f;
		m.Numerics.M44 = 1.0f;

		return m;
	}

	public Model GeneratePortalModel()
	{
		Log.Info( MirrorMaterial.Name );
		Mesh portalMesh = new( MirrorMaterial );

		VertexBuffer buf = new();
		buf.Init( true );

		var Depth = CollisionBounds.Size.x / 2;
		var Width = CollisionBounds.Size.y / 2;
		var Height = CollisionBounds.Size.x / 2;
		//Make a Box that is the size of the mirror
		var v1 = new Vertex( new Vector3( -Height, -Width, 0 ), Vector3.Up, Vector3.Right, new Vector2( 0, 1 ) );
		var v2 = new Vertex( new Vector3( Height, -Width, 0 ), Vector3.Up, Vector3.Right, new Vector2( -1, 1 ) );
		var v3 = new Vertex( new Vector3( Height, Width, 0 ), Vector3.Up, Vector3.Right, new Vector2( -1, 0 ) );
		var v4 = new Vertex( new Vector3( -Height, Width, 0 ), Vector3.Up, Vector3.Right, new Vector2( 0, 0 ) );
		buf.AddQuad( v1, v2, v3, v4 );


		portalMesh.CreateBuffers( buf );

		return Model.Builder.AddMesh( portalMesh )
			.Create();
	}
}


