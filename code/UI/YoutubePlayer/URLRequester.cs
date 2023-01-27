﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace TowerResort.UI
{
	public class VideoRequestPanel : Panel
	{
		public static VideoRequestPanel Instance { get; set; }

		public Label InfoLabel { get; set; }
		public TextEntry UrlInput { get; set; }
		public Button SubmitButton { get; set; }


		public VideoRequestPanel()
		{
			InfoLabel = Add.Label( "Video URL", "label" );
			UrlInput = Add.TextEntry( "" );
			UrlInput.AddClass( "textentry" );

			SubmitButton = Add.Button( "Play", () =>
			{
				// Playable.RequestVideo(UrlInput.StringValue);
				Style.Display = DisplayMode.None;
				Style.PointerEvents = PointerEvents.None;
				Style.Dirty();

				ConsoleSystem.Run( "request_video", null, UrlInput.Text );
			} );
			SubmitButton.AddClass( "button" );

			StyleSheet.Load( "UI/VideoRequestPanel.scss" );

			Instance = this;
		}

		public void SetPlayer()
		{
			Style.Display = DisplayMode.Flex;
			Style.PointerEvents = PointerEvents.All;
			Style.Dirty();
		}
	}
}
