namespace TowerResort.Player;

public partial class LobbyPawn
{
	public BBox PreviewHull;
	public CondoItemBase ItemPreview;
	[Net] public bool IsPreviewing { get; set; }
	public CondoAssetBase ServerPreviewAsset;

	float yawRot = 0.0f;

	public TraceResult GetEyePlacement()
	{
		return Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 256 )
			.Ignore( this )
			.Size( PreviewHull )
			.Run();
	}

	public void SimulatePlacement()
	{
		if ( !IsPreviewing || Game.IsClient ) return;

		yawRot += Input.MouseWheel * 10;

		if ( yawRot > 360 )
			yawRot = 0;
		else if ( yawRot < 0 )
			yawRot = 360;

		UpdatePreview( To.Single( this ), GetEyePlacement().EndPosition, yawRot );

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
			PlaceItem();

		if ( Input.Pressed( InputButton.SecondaryAttack ) )
		{
			DestroyPreview( To.Single( this ) );
			IsPreviewing = false;
		}
	}

	public void PlaceItem()
	{
		DestroyPreview( To.Single( this ) );

		var placed = new CondoItemBase();
		placed.SpawnFromAsset( ServerPreviewAsset );

		placed.Position = GetEyePlacement().EndPosition;
		placed.Rotation = Rotation.FromYaw( yawRot );

		IsPreviewing = false;
	}

	public void StartPlacing( CondoAssetBase asset )
	{
		PreviewHull = Model.Load( asset.ModelPath ).Bounds;

		CreatePreview( asset.ModelPath );

		yawRot = 0.0f;

		ServerPreviewAsset = asset;
		IsPreviewing = true;
	}

	[ClientRpc]
	public void CreatePreview( string modelPath )
	{
		ItemPreview?.Delete();
		ItemPreview = null;

		ItemPreview = new CondoItemBase()
		{
			Model = Model.Load( modelPath )
		};
	}

	[ClientRpc]
	public void UpdatePreview(Vector3 end, float yaw)
	{
		if ( ItemPreview == null ) return;

		ItemPreview.Position = end;
		ItemPreview.Rotation = Rotation.FromYaw( yaw );
	}

	[ClientRpc]
	public void DestroyPreview()
	{
		ItemPreview?.Delete();
		ItemPreview = null;
	}
}
