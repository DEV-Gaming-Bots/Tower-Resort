using System;
using Editor;
using Sandbox;

namespace SimpleMirror;

[Library( "func_reflective" )]
[HammerEntity]
[Title( "Mirror" ), Category( "Effects" ), Icon( "monitor" )]
[Solid, AutoApplyMaterial( "materials/mirror.vmat" )]
public partial class Mirror : ModelEntity
{

	[Property, Net, DefaultValue( "materials/mirror.vmat" )]
	public Material MirrorMaterial { get; set; } = Material.Load( "materials/mirror.vmat" );
	private static bool _debugDraw { get; set; } = false;
	protected ScenePortal so;

	public override void Spawn()
	{
		Tags.Add( "mirror" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );
		Transmit = TransmitType.Always;

		EnableAllCollisions = true;
		EnableSolidCollisions = true;
		EnableTouch = true;
		EnableDrawing = false;

		Predictable = false;
	}

	[ConCmd.Client( "cl_debug_overlay_mirror" )]
	public static void MirrorDebugDraw( bool enable )
	{
		_debugDraw = enable;
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
		so = new ScenePortal( Game.SceneWorld, model, Transform, true, (int)Screen.Width )
		{
			Transform = this.Transform,
			Position = this.Position,
			Rotation = this.Rotation,
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
	}

	[Event.PreRender]
	public void UpdatePortalView()
	{
		if ( !Sandbox.Game.IsClient || !so.IsValid() )
			return;

		so.RenderingEnabled = true;
		so.Rotation = Rotation;
		so.Position = Position;

		Vector3 planeNormal = GetPlaneNormal();

		Plane p = new( Position, planeNormal );
		// Reflect
		Matrix viewMatrix = Matrix.CreateWorld( Camera.Position, Camera.Rotation.Forward, Camera.Rotation.Up );
		Matrix reflectMatrix = ReflectMatrix( viewMatrix, p );

		// Apply Rotation
		Vector3 reflectionPosition = reflectMatrix.Transform( Camera.Position );
		Rotation reflectionRotation = ReflectRotation( Camera.Rotation, planeNormal );

		so.ViewPosition = reflectionPosition;
		so.ViewRotation = reflectionRotation;

		if ( _debugDraw )
			DebugOverlay.Sphere( so.ViewPosition, 10, Color.Red );

		so.Aspect = Screen.Width / Screen.Height;

		so.FieldOfView = MathF.Atan( MathF.Tan( Camera.FieldOfView.DegreeToRadian() * 0.41f ) * (so.Aspect * 0.75f) ).RadianToDegree() * 2.0f;

		Vector3 planePosition = Position;
		Plane clippingPlane = new Plane( Position - so.ViewPosition, planeNormal );
		if ( _debugDraw )
		{
			// Draw clipping plane normal
			Vector3 lineEnd = planePosition + planeNormal * 100f;
			DebugOverlay.DrawVector( planePosition, lineEnd, Color.Green );
		}
		// small tolerance to prevent seam
		clippingPlane.Distance -= 1.0f;

		so.SetClipPlane( clippingPlane );
	}

	public Vector3 GetPlaneNormal()
	{
		TraceResult tr = Trace.Sphere( 10f, Camera.Position, Position )
			.WithTag( "mirror" )
			.EntitiesOnly()
			.IncludeClientside()
			.Run();

		// It should be impossible for the trace to not hit.
		if ( !tr.Hit )
		{
			Log.Error( "Nothing hit!" );
		}
		if ( _debugDraw )
		{
			DebugOverlay.Line( tr.StartPosition, tr.EndPosition, Color.Yellow );
		}
		return tr.Normal;
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
}
