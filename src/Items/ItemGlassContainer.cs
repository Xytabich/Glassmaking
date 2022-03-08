using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Items
{
	public abstract class ItemGlassContainer : Item
	{
		public virtual float GetGlassTemperature(IWorldAccessor world, ItemStack itemstack)
		{
			if(itemstack == null || itemstack.Attributes == null || itemstack.Attributes["glassTemperature"] == null || !(itemstack.Attributes["glassTemperature"] is ITreeAttribute))
			{
				return 20f;
			}
			ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
			double totalHours = world.Calendar.TotalHours;
			double lastUpdate = attr.GetDouble("temperatureLastUpdate");
			if(totalHours - lastUpdate > 1.0 / 85)
			{
				float temperature = Math.Max(20f, attr.GetFloat("temperature", 20f) - Math.Max(0f, (float)(totalHours - lastUpdate) * 180f));
				SetGlassTemperature(world, itemstack, temperature);
				return temperature;
			}
			return attr.GetFloat("temperature", 20f);
		}

		public virtual void SetGlassTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = false)
		{
			if(itemstack == null) return;

			ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
			if(attr == null)
			{
				attr = new TreeAttribute();
				itemstack.Attributes["glassTemperature"] = attr;
			}
			double totalHours = world.Calendar.TotalHours;
			float prevTemperature = attr.GetFloat("temperature");
			if(delayCooldown && prevTemperature < temperature)
			{
				totalHours += 0.5;
			}

			attr.SetDouble("temperatureLastUpdate", totalHours);
			attr.SetFloat("temperature", temperature);
		}

		protected float GetGlassTemperatureWithoutCheck(ItemStack itemstack)
		{
			ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
			if(attr == null) return 20;
			return attr.GetFloat("temperature");
		}
	}
}