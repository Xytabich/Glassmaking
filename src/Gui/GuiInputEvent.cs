using Vintagestory.API.Client;

namespace GlassMaking.Gui
{
	public readonly struct GuiInputEvent
	{
		/// <summary>
		/// Event type.
		/// The fields may have different values depending on the type.
		/// </summary>
		public readonly EventType Type;
		/// <summary>
		/// Keyboard event data.
		/// Only have value if the <see cref="Type"/> is <see cref="EventType.KeyDown"/> or <see cref="EventType.KeyUp"/>.
		/// </summary>
		public readonly KeyEvent? Key;
		/// <summary>
		/// Mouse button event data.
		/// Only have value if the <see cref="Type"/> is <see cref="EventType.MouseButtonDown"/> or <see cref="EventType.MouseButtonUp"/>.
		/// </summary>
		public readonly MouseEvent? MouseButton;
		/// <summary>
		/// Mouse wheel event data.
		/// Only have value if the <see cref="Type"/> is <see cref="EventType.MouseWheel"/>.
		/// </summary>
		public readonly MouseWheelEventArgs? MouseWheel;

		public GuiInputEvent(KeyEvent key, bool isDown)
		{
			Key = key;
			Type = isDown ? EventType.KeyDown : EventType.KeyUp;
		}

		public GuiInputEvent(MouseEvent mouseButton, bool isDown)
		{
			MouseButton = mouseButton;
			Type = isDown ? EventType.MouseButtonDown : EventType.MouseButtonUp;
		}

		public GuiInputEvent(MouseWheelEventArgs mouseWheel)
		{
			MouseWheel = mouseWheel;
			Type = EventType.MouseWheel;
		}

		public enum EventType
		{
			KeyDown,
			KeyUp,
			MouseButtonDown,
			MouseButtonUp,
			MouseWheel
		}
	}
}