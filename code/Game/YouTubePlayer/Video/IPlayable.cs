using Sandbox;

namespace TowerResort.Video;

public interface IPlayableVideo
{
	void PlayVideo(string id);
	void RequestVideoURL( string url );
}

