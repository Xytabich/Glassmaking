namespace GlassMaking.Items
{
	internal static class GlassRenderUtil
	{
		public static TemperatureState TemperatureToState(float temperature, float workingTemperature)
		{
			if(temperature < workingTemperature * 0.45f) return TemperatureState.Cold;
			if(temperature < workingTemperature) return TemperatureState.Heated;
			return TemperatureState.Working;
		}

		public static int StateToGlow(TemperatureState state)
		{
			switch(state)
			{
				case TemperatureState.Heated: return 127;
				case TemperatureState.Working: return 255;
				default: return 0;
			}
		}
	}

	internal enum TemperatureState
	{
		Cold,
		Heated,
		Working
	}
}