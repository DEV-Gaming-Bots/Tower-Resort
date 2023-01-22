using System;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Sandbox.UI.components
{
	public class CRButton : Panel
	{
		public Button btn;
		public bool disabled;
		public CRButton( string text, string className = null, Action OnClick = null )
		{
			btn = Add.Button( text, className, () =>
			{
				if ( disabled )
					return;

				if ( OnClick != null )
					OnClick();

				if ( OnClick == null )
					Log.Warning( "CRButton: No OnClick action was provided." );
			} );
		}

		public class PrimaryCRButton : Panel
		{
			public Button btn;
			public bool disabled;

			public PrimaryCRButton( string text, string className = null, Action OnClick = null )
			{
				btn = Add.Button( text, className, () =>
				{
					if ( disabled )
						return;

					if ( OnClick != null )
						OnClick();

					if ( OnClick == null )
						Log.Warning( "CRButton: No OnClick action was provided." );
				} );
			}

			public string text
			{
				get { return btn.Text; }
				set { btn.Text = value; }
			}

			public bool isDisabled
			{
				get { return disabled; }
				set
				{
					disabled = value;
					btn.SetClass( "disabled", value );
				}
			}
		}

		public string text
		{
			get { return btn.Text; }
			set { btn.Text = value; }
		}

		public bool isDisabled
		{
			get { return disabled; }
			set
			{
				disabled = value;
				btn.SetClass( "disabled", value );
			}
		}
	}
}
