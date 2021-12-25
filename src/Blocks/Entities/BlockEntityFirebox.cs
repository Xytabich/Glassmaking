using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Blocks
{
    public class BlockEntityFirebox : BlockEntity
    {
        private const float TEMP_INCREASE_PER_HOUR = 1500;
        private const float TEMP_DECREASE_PER_HOUR = 2000;

        protected virtual float fuelTemperatureModifier => 1.1f;
        protected virtual float fuelDurationModifier => 0.8f;
        protected virtual int maxFuelCount => 16;

        private ItemStack contents = null;

        private BlockRendererFirebox renderer = null;

        private bool burning = false;
        private float temperature = 20;
        /// <summary>
        /// How much burning time is left
        /// </summary>
        private float fuelLevel = 0f;
        // Fuel parameters
        private float fuelTemperature = 0f;
        private float fuelBurnDuration = 0f;

        private double lastTickTime;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if(contents != null)
            {
                contents.ResolveBlockOrItem(Api.World);
                ApplyFuelParameters();
            }
            if(api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI coreClientAPI = (ICoreClientAPI)api;
                renderer = new BlockRendererFirebox(Pos, coreClientAPI);
                coreClientAPI.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "glassmaking:firebox");
                UpdateRendererFull();
            }
            RegisterGameTickListener(OnCommonTick, 200);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if(contents != null)
            {
                if(contents.StackSize > 0)
                {
                    dsc.AppendLine("Contents: " + contents.StackSize + "x " + contents.GetName());
                }
                else
                {
                    dsc.AppendLine("Fuel: " + contents.GetName());
                }
            }
            if(temperature > 25)
            {
                dsc.AppendLine("Temperature: " + temperature.ToString("#"));
            }
            if(burning)
            {
                var calendar = Api.World.Calendar;
                dsc.AppendLine("Burn time: " + ((fuelLevel + GetFuelCount() * fuelBurnDuration) * 3600 / (calendar.SpeedOfTime * calendar.CalendarSpeedMul)).ToString("0"));
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            renderer?.Dispose();
            base.OnBlockRemoved();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents = tree.GetItemstack("fuelStack");
            fuelLevel = tree.GetFloat("fuelLevel");
            temperature = tree.GetFloat("temperature", 20);
            burning = tree.GetBool("burning");
            lastTickTime = tree.GetDouble("lastTickTotalHours");
            if(contents != null && Api?.World != null)
            {
                contents.ResolveBlockOrItem(Api.World);
                ApplyFuelParameters();
            }
            UpdateRendererFull();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("fuelStack", contents);
            tree.SetFloat("fuelLevel", fuelLevel);
            tree.SetFloat("temperature", temperature);
            tree.SetBool("burning", burning);
            tree.SetDouble("lastTickTotalHours", lastTickTime);
        }

        public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int count)
        {
            var combustibleProps = slot.Itemstack.Collectible.CombustibleProps;
            if(combustibleProps == null || combustibleProps.BurnTemperature < 100) return false;
            if(burning)
            {
                if(contents == null) return false;
                if(!contents.Equals(Api.World, slot.Itemstack)) return false;
            }
            else
            {
                if(contents != null && !contents.Equals(Api.World, slot.Itemstack)) return false;
            }

            int consume = Math.Min(maxFuelCount - GetFuelCount(), Math.Min(slot.Itemstack.StackSize, count));
            if(consume > 0)
            {
                if(contents == null)
                {
                    contents = slot.Itemstack.Clone();
                    contents.StackSize = consume;
                    if(!burning && temperature >= 300)
                    {
                        burning = true;
                        ApplyFuelParameters();
                        fuelLevel = fuelBurnDuration;
                        contents.StackSize--;
                    }
                    UpdateRendererFull();
                }
                else
                {
                    contents.StackSize += consume;
                }
                if(slot.Itemstack.StackSize > consume)
                {
                    slot.Itemstack.StackSize -= consume;
                }
                else
                {
                    slot.Itemstack = null;
                }
                slot.MarkDirty();
                MarkDirty(true);
            }
            return false;
        }

        public bool CanIgnite()
        {
            return !burning && GetFuelCount() > 0;
        }

        public void TryIgnite()
        {
            burning = true;
            ApplyFuelParameters();
            fuelLevel = fuelBurnDuration;
            if(contents.StackSize > 0) contents.StackSize--;
            lastTickTime = Api.World.Calendar.TotalHours;
            UpdateRendererFull();
            MarkDirty(true);
        }

        public bool IsHeatedUp()
        {
            return temperature > 20;
        }

        /// <summary>
        /// Returns how long in hours the specified temperature was kept until the current World.Calendar.TotalHours
        /// </summary>
        public double GetTempElapsedTime(double startTime, float temperature)
        {
            if(fuelTemperature < temperature) return 0;

            double time = 0;
            float temp = this.temperature;
            double hours = Api.World.Calendar.TotalHours - lastTickTime;
            if(burning && hours > 0)
            {
                double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
                if(temp < temperature)
                {
                    double t = Math.Min((temperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp += (float)(t * TEMP_INCREASE_PER_HOUR);
                    hours -= t;
                    burnTime -= t;
                    startTime -= t;
                }
                if(hours > 0)
                {
                    time = Math.Min(burnTime, hours);
                    hours -= time;
                }
            }
            if(hours > 0)
            {
                time += Math.Min((temp - temperature) / TEMP_DECREASE_PER_HOUR, hours) * TEMP_DECREASE_PER_HOUR;
            }
            return Math.Max(time - Math.Max(startTime, 0), 0);
        }

        public float CalcCurrentTemperature()
        {
            float temp = temperature;
            double hours = Api.World.Calendar.TotalHours - lastTickTime;
            if(burning && hours > 0)
            {
                double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
                if(temp < fuelTemperature)
                {
                    double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp -= (float)(time * TEMP_INCREASE_PER_HOUR);
                    hours -= time;
                    burnTime -= time;
                }
                if(temp < fuelTemperature)
                {
                    double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp += (float)(time * TEMP_INCREASE_PER_HOUR);
                    hours -= time;
                    burnTime -= time;
                }
                if(hours > 0)
                {
                    hours -= Math.Min(burnTime, hours);
                }
            }
            if(hours > 0)
            {
                temp -= (float)(Math.Min((temp - 20) / TEMP_DECREASE_PER_HOUR, hours) * TEMP_DECREASE_PER_HOUR);
            }
            return temp;
        }

        private void OnCommonTick(float dt)
        {
            if(burning)
            {
                double hours = Api.World.Calendar.TotalHours - lastTickTime;
                if(hours > 0)
                {
                    int fuelCount = GetFuelCount();
                    double burnTime = fuelLevel + fuelBurnDuration * fuelCount;
                    if(temperature < fuelTemperature)
                    {
                        double time = Math.Min((fuelTemperature - temperature) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                        temperature += (float)(time * TEMP_INCREASE_PER_HOUR);
                        hours -= time;
                        burnTime -= time;
                        lastTickTime += time;
                    }
                    else if(temperature > fuelTemperature)
                    {
                        temperature = Math.Max(fuelTemperature, temperature - (float)(hours * TEMP_DECREASE_PER_HOUR));
                    }
                    if(hours > 0)
                    {
                        var time = Math.Min(burnTime, hours);
                        burnTime -= time;
                        lastTickTime += time;
                    }
                    if(fuelCount > 0)
                    {
                        int usedFuelCount = fuelCount - (int)Math.Floor(burnTime / fuelBurnDuration);
                        fuelLevel = (float)(burnTime % fuelBurnDuration);
                        if(usedFuelCount > 0)
                        {
                            contents.StackSize -= usedFuelCount;
                            if(contents.StackSize <= 0)
                            {
                                burning = fuelLevel > 0;
                                if(!burning) contents = null;
                            }
                            UpdateRendererFull();
                            MarkDirty(true);
                        }
                    }
                    else
                    {
                        fuelLevel = (float)burnTime;
                        burning = fuelLevel > 0;
                        if(!burning)
                        {
                            contents = null;
                            UpdateRendererFull();
                        }
                    }
                }
            }
            if(!burning && temperature > 20)
            {
                var totalHours = Api.World.Calendar.TotalHours;
                double hours = totalHours - lastTickTime;
                temperature = Math.Max(20, temperature - (float)(hours * TEMP_DECREASE_PER_HOUR));
                lastTickTime = totalHours;
            }
            if(Api.Side == EnumAppSide.Client) UpdateRendererParameters();
        }

        private int GetFuelCount()
        {
            if(contents == null) return 0;
            return contents.StackSize;
        }

        private void ApplyFuelParameters()
        {
            var combustibleProps = contents.Collectible.CombustibleProps;//TODO: smoke level?
            fuelTemperature = combustibleProps.BurnTemperature * fuelTemperatureModifier;
            var calendar = Api.World.Calendar;
            fuelBurnDuration = combustibleProps.BurnDuration * fuelDurationModifier * calendar.SpeedOfTime * calendar.CalendarSpeedMul;
            fuelBurnDuration *= 1f / 3600f;
        }

        private void UpdateRendererFull()
        {
            if(Api != null && Api.Side == EnumAppSide.Client && renderer != null)
            {
                renderer.SetHeight(GetFuelCount() + (burning ? 1 : 0));
                UpdateRendererParameters();
            }
        }

        private void UpdateRendererParameters()
        {
            renderer.SetParameters(burning, Math.Min(128, (int)((temperature / 1500f) * 128)));
        }
    }
}