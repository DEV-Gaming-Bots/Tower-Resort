using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Audio;

public class AudioProgress
{
	public bool IsDownloading { get; set; }
	public string DownloadText { get; set; }
	public bool IsConverting { get; set; }

	public string VideoTitle { get; set; }
	public double DownloadProgress { get; set; }
	public string VideoSize { get; set; }
	public string Eta { get; set; }
	public string DownloadRate { get; set; }

	public bool IsProbing { get; set; }
	public AudioInfoResult VideoInfo { get; set; }
}
