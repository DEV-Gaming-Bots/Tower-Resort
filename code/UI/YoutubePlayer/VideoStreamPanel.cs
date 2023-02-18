using System.Linq;

namespace TowerResort.UI;

public class VideoStreamPanel : WorldPanel
{
	public static VideoStreamPanel Current;
	public Action<Texture> OnFrameChange { get; set; }
	public List<string> VideoQueue;

	CondoItemBase videoEntity;

	public WebSurface Browser;
	Texture screenTexture;

	double forward;
	double height;

	public VideoStreamPanel()
	{
		Current?.Delete();
		Current = null;

		Current = this;

		VideoQueue = new List<string>();

		Browser = Game.CreateWebSurface();
		Browser.Size = new Vector2( 1024, 1024 );

		Browser.Url = "www.youtube.com";

		Browser.OnTexture = BrowserDataChanged;
	}

	public void AddVideo(string url)
	{
		VideoQueue.Add( url );
	}

	public VideoStreamPanel( CondoItemBase ent, double fPos, double hPos ) : this()
	{
		videoEntity = ent;
		forward = fPos;
		height = hPos;
	}

	public override void OnDeleted()
	{
		Browser?.Dispose();
		Browser = null;
		VideoQueue.Clear();

		base.OnDeleted();
	}

	void BrowserDataChanged( ReadOnlySpan<byte> span, Vector2 size )
	{
		if ( screenTexture == null || screenTexture.Size != size )
		{
			screenTexture?.Dispose();
			screenTexture = null;

			screenTexture = Texture.Create( (int)size.x, (int)size.y, ImageFormat.BGRA8888 )
										.WithName( "WebPanel" )
										.Finish();

			Style.SetBackgroundImage( screenTexture );
		}

		timeToChange = 0.15f;
		screenTexture.Update( span, 0, 0, (int)size.x, (int)size.y );
	}

	TimeUntil timeToChange;

	public override void Tick()
	{
		Browser.TellMouseButton( MouseButtons.Left, true );

		if( timeToChange <= 0.0f && VideoQueue.Count > 0 )
		{
			string newVideo = VideoQueue[0];
			VideoQueue.RemoveAt( 0 );

			Browser.Url = newVideo;
		}

		Position = videoEntity.Position + Vector3.Backward * (float)forward + Vector3.Up * (float)height;
		Rotation = Rotation.LookAt( videoEntity.Rotation.Forward );
	}
}
