using System;
using Sandbox;
using TowerResort.Entities.Weapons;

namespace TowerResort.Player;

public partial class LobbyPawn
{
	[Net] public float ThirdCamOffset { get; set; } = 25;
	float scrollSpeed => 5.0f;
	[Net] public bool InThird { get; set; } = false;

	float zoom = 0;
	float lastZoom;

	public void DoCameraActions()
	{
		if ( FreezeMovement == FreezeEnum.Movement )
			return;

		if ( InThird )
		{
			if( Input.Down( InputButton.SecondaryAttack ) )
				FreezeMovement = FreezeEnum.Animation;
			else
				FreezeMovement = FreezeEnum.None;
		}
					
		if ( LastActiveWeapon is PhysGun && IsValid )
		{
			ThirdCamOffset = 25.0f;
			InThird = false;
			return;
		}

		if ( Input.MouseWheel > 0 ) 
			ThirdCamOffset -= scrollSpeed;
		else if ( Input.MouseWheel < 0 ) 
			ThirdCamOffset += scrollSpeed;

		ThirdCamOffset = ThirdCamOffset.Clamp( 25.0f, 75.0f );

		if ( ThirdCamOffset > 25.0f )
			InThird = true;
		else
			InThird = false;
	}

	[ClientRpc]
	public void ResetCamera()
	{
		Camera.Rotation = Rotation.Identity;
	}

	float speed = 175;

	public void DoZoomingCamera()
	{
		if ( InThird )
		{
			zoom = 0;
			lastZoom = 0;
			return;
		}

		if(Input.Down(InputButton.Zoom))
			zoom += speed * Time.Delta;
		else
			zoom -= speed * Time.Delta;

		zoom = zoom.Clamp( 0, 60 );

		lastZoom = MathX.Lerp( lastZoom, zoom, Time.Delta * ( speed/16 ) );
		//lastZoom = zoom;
	}

	public void FrameCamera()
	{
		if ( InThird )
		{
			Camera.Position = EyePosition + EyeRotation.Backward * ThirdCamOffset;
			Camera.FirstPersonViewer = null;
		} 
		else
		{
			Camera.FirstPersonViewer = this;
			Camera.Position = EyePosition;
		}

		Camera.Rotation = ViewAngles.ToRotation();

		DoZoomingCamera();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView - lastZoom );
	}
}
