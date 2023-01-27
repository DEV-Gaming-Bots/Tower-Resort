using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Audio;

public class AudioInfoResult
{
	public bool Exists { get; set; }
	public bool TooLong { get; set; }
	public string Title { get; set; }
	public string ChannelTitle { get; set; }
	public DateTime PublishedAt { get; set; }
}
