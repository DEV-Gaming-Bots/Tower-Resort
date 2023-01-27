using Sandbox;

namespace TowerResort;

public interface IPlayableVideo
{
	void PlayVideo( string id );
	void RequestVideoURL( string url );
}

public interface IPlayableAudio
{
	void PlayAudio( string id );
	void RequestAudioURL( string url );
}
