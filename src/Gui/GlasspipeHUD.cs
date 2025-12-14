// using Vintagestory.API.Client;
// using Vintagestory.Client;

// namespace GlassMaking.Gui
// {
// 	public class GlasspipeHUD : GuiDialog
// 	{
// 		public override double InputOrder => 0.3f;

// 		public override string ToggleKeyCombinationCode => null;

// 		private AxisKeycomb vertical;
// 		private AxisKeycomb horizontal;
// 		private AxisKeycomb multiplier;
// 		private long tickListenerId;

// 		public GlasspipeHUD(ICoreClientAPI capi) : base(capi)
// 		{
// 		}

// 		public override bool CaptureAllInputs()
// 		{
// 			return IsOpened();
// 		}

// 		public override bool ShouldReceiveKeyboardEvents()
// 		{
// 			return IsOpened();
// 		}

// 		public override bool ShouldReceiveMouseEvents()
// 		{
// 			return IsOpened();
// 		}

// 		public override bool TryOpen()
// 		{
// 			if(base.TryOpen())
// 			{
// 				capi.Gui.RequestFocus(this);
// 				tickListenerId = capi.Event.RegisterGameTickListener(OnUpdate, 20);
// 				LoadKeyCodes();
// 				return true;
// 			}
// 			return false;
// 		}

// 		public override bool TryClose()
// 		{
// 			if(base.TryClose())
// 			{
// 				capi.Event.UnregisterGameTickListener(tickListenerId);
// 				return true;
// 			}
// 			return false;
// 		}

// 		public override void UnFocus()
// 		{
// 			base.UnFocus();
// 			if(IsOpened()) TryClose();
// 		}

// 		public override void OnKeyDown(KeyEvent args)
// 		{
// 			base.OnKeyDown(args);
// 			args.Handled = true;
// 		}

// 		public override void OnKeyUp(KeyEvent args)
// 		{
// 			base.OnKeyUp(args);
// 			args.Handled = true;
// 		}

// 		public override void OnKeyPress(KeyEvent args)
// 		{
// 			base.OnKeyPress(args);
// 			args.Handled = true;
// 		}

// 		private void OnUpdate(float dt)
// 		{
// 			var keyboardState = capi.Input.KeyboardKeyState;
// 			vertical.UpdateValue(keyboardState);
// 			horizontal.UpdateValue(keyboardState);
// 			multiplier.UpdateValue(keyboardState);
// 		}

// 		private void LoadKeyCodes()
// 		{
// 			vertical = AxisKeycomb.FromHotkey("walkforward", "walkbackward", 1, -1);
// 			horizontal = AxisKeycomb.FromHotkey("walkright", "walkleft", 1, -1);
// 			multiplier = AxisKeycomb.FromHotkey("sprint", "sneak", 4, 0.25f);
// 		}

// 		private struct AxisKeycomb
// 		{
// 			public float value;

// 			private readonly int positiveKey, negativeKey;
// 			private readonly float positiveValue, negativeValue;

// 			public AxisKeycomb(int positiveKey, int negativeKey, float positiveValue, float negativeValue)
// 			{
// 				this.positiveKey = positiveKey;
// 				this.negativeKey = negativeKey;
// 				this.positiveValue = positiveValue;
// 				this.negativeValue = negativeValue;
// 				value = 0f;
// 			}

// 			public void UpdateValue(bool[] keyboardState)
// 			{
// 				value = (keyboardState[positiveKey] ^ keyboardState[negativeKey]) ? (keyboardState[positiveKey] ? positiveValue : negativeValue) : 0f;
// 			}

// 			public static AxisKeycomb FromHotkey(string positiveKey, string negativeKey, float positiveValue, float negativeValue)
// 			{
// 				var hotkeys = ScreenManager.hotkeyManager.HotKeys;
// 				return new AxisKeycomb(hotkeys[positiveKey].CurrentMapping.KeyCode, hotkeys[negativeKey].CurrentMapping.KeyCode, positiveValue, negativeValue);
// 			}
// 		}
// 	}
// }