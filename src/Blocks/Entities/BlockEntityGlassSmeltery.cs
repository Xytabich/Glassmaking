using GlassMaking.Common;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassSmeltery : BlockEntity, IBlockEntityContainer, ITimeBasedHeatReceiver
    {
        private const float BUBBLING_TEMPERATURE = 1450;
        private const float MELTING_TEMPERATURE = 1300;
        private const float WORKING_TEMPERATURE = 1100;

        private const double PROCESS_HOURS_PER_UNIT = 0.001;
        private const double BUBBLING_PROCESS_MULTIPLIER = 3;

        protected virtual int maxGlassAmount => 200;

        IInventory IBlockEntityContainer.Inventory => inventory;
        string IBlockEntityContainer.InventoryClassName => inventory.ClassName;

        private ITimeBasedHeatSource heatSource = null;

        private SmelteryState state;
        private int glassAmount;
        private AssetLocation glassCode;
        private double processProgress;

        private GlassSmelteryInventory inventory = new GlassSmelteryInventory(null, null);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("glasswork:glasssmeltery-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if(heatSource != null)
            {
                switch(state)
                {
                    case SmelteryState.ContainsMix:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        break;
                    case SmelteryState.Melting:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Melting progress: " + (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT) * 100).ToString("0") + "%");
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                    case SmelteryState.Bubbling:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Bubbling progress: " + (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER) * 100).ToString("0") + "%");
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                    case SmelteryState.ContainsGlass:
                        dsc.AppendLine("Contains melt: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("state", (int)state);
            if(state != SmelteryState.Empty)
            {
                tree.SetInt("glassamount", glassAmount);
                tree.SetString("glasscode", glassCode.ToShortString());
                if(state == SmelteryState.ContainsMix)
                {
                    inventory.ToTreeAttributes(tree.GetOrAddTreeAttribute("inventory"));
                }
                else if(state != SmelteryState.ContainsGlass)
                {
                    tree.SetDouble("progress", processProgress);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            state = (SmelteryState)tree.GetInt("state");
            if(state != SmelteryState.Empty)
            {
                glassAmount = tree.GetInt("glassamount");
                glassCode = new AssetLocation(tree.GetString("glasscode"));
                if(state == SmelteryState.ContainsMix)
                {
                    inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
                }
                else if(state != SmelteryState.ContainsGlass)
                {
                    processProgress = tree.GetDouble("progress");
                }
            }
        }

        public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int count)
        {
            if(glassAmount >= maxGlassAmount || heatSource == null) return false;

            GlassBlend blend = slot.Itemstack.ItemAttributes?[GlassBlend.PROPERTY_NAME]?.AsObject<GlassBlend>(null, slot.Itemstack.Collectible.Code.Domain);
            if(blend == null) blend = GlassBlend.FromTreeAttributes(slot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
            if(blend != null && blend.amount > 0 && (glassCode == null || glassCode.Equals(blend.code)) && (blend.amount + glassAmount) <= maxGlassAmount)
            {
                if(glassCode == null) glassCode = blend.code.Clone();
                if(state == SmelteryState.Bubbling || state == SmelteryState.ContainsGlass)
                {
                    if(heatSource.CalcCurrentTemperature() >= MELTING_TEMPERATURE)
                    {
                        state = SmelteryState.Melting;
                        processProgress = PROCESS_HOURS_PER_UNIT * glassAmount;
                    }
                    else
                    {
                        state = SmelteryState.ContainsMix;
                    }
                }
                int consume = Math.Min(Math.Min(count, slot.Itemstack.StackSize), (maxGlassAmount - glassAmount) / blend.amount);
                var item = slot.TakeOut(consume);
                if(state == SmelteryState.Empty || state == SmelteryState.ContainsMix)
                {
                    inventory.AddItem(item);
                    state = SmelteryState.ContainsMix;
                }
                glassAmount += blend.amount * consume;
                MarkDirty(true);
                return true;
            }
            return false;
        }

        public ItemStack[] GetDropItems()
        {
            var items = inventory.GetItems();
            if(glassAmount >= 5)
            {
                var shards = new ItemStack(Api.World.GetItem(new AssetLocation("glassmaking", "glassshards")), glassAmount / 5);
                new GlassBlend(glassCode, 5).ToTreeAttributes(shards.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
                items.Add(shards);
            }
            return items.ToArray();
        }

        public int GetGlassAmount()
        {
            if(heatSource != null)
            {
                if(state == SmelteryState.ContainsGlass && heatSource.CalcCurrentTemperature() >= WORKING_TEMPERATURE)
                {
                    return glassAmount;
                }
            }
            return 0;
        }

        public AssetLocation GetGlassCode()
        {
            return glassCode;
        }

        public void RemoveGlass(int amount)
        {
            glassAmount -= amount;
            if(glassAmount <= 0)
            {
                state = SmelteryState.Empty;
            }
        }

        public void SpawnGlassUseParticles(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer)
        {
            // Smoke on the mold
            Vec3d blockpos = blockSel.Position.ToVec3d().Add(0.5, 0, 0.5);
            world.SpawnParticles(
                1,
                ColorUtil.ToRgba(50, 220, 220, 220),
                blockpos.AddCopy(-0.25, -0.1, -0.25),
                blockpos.Add(0.25, 0.1, 0.25),
                new Vec3f(-0.5f, 0f, -0.5f),
                new Vec3f(0.5f, 0f, 0.5f),
                0.25f,
                -0.05f,
                0.5f,
                EnumParticleModel.Quad,
                byPlayer
            );
        }

        void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSource heatSource)
        {
            this.heatSource = heatSource;
        }

        void ITimeBasedHeatReceiver.OnHeatSourceTick(float dt)
        {
            if(state == SmelteryState.Empty || !heatSource.IsHeatedUp()) return;
            double timeOffset = 0;
            if(state == SmelteryState.ContainsMix)
            {
                if(heatSource.CalcTempElapsedTime(timeOffset, MELTING_TEMPERATURE) > 0)
                {
                    state = SmelteryState.Melting;
                    processProgress = 0;
                    inventory.Clear();
                    MarkDirty();
                }
            }
            if(state == SmelteryState.Melting)
            {
                double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT - processProgress;
                double time = heatSource.CalcTempElapsedTime(timeOffset, MELTING_TEMPERATURE);
                if(time >= timeLeft)
                {
                    timeOffset += timeLeft;
                    processProgress = 0;
                    state = SmelteryState.Bubbling;
                    MarkDirty();
                }
                else
                {
                    processProgress += time;
                }
            }
            if(state == SmelteryState.Bubbling)
            {
                double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER - processProgress;
                double time = heatSource.CalcTempElapsedTime(timeOffset, BUBBLING_TEMPERATURE);
                if(time >= timeLeft)
                {
                    state = SmelteryState.ContainsGlass;
                    MarkDirty();
                }
                else
                {
                    processProgress += time;
                }
            }
        }

        private enum SmelteryState
        {
            Empty,
            ContainsMix,
            Melting,//TODO: при переходе на этот процесс, т.е. когда считается количество стекла которое было помещено, нужно проверять на макс. кол-во, и выкидывать лишние предметы..? либо надо как-то сделать функцию, типа canputitem или что-то вроде того
            Bubbling,
            ContainsGlass
        }
    }
}