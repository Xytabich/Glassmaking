using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassSmeltery : BlockEntity, IBlockEntityContainer
    {
        private const float BUBBLING_TEMPERATURE = 1450;
        private const float MELTING_TEMPERATURE = 1300;
        private const float WORKING_TEMPERATURE = 1100;

        private const double PROCESS_HOURS_PER_UNIT = 0.001;
        private const double BUBBLING_PROCESS_MULTIPLIER = 3;

        IInventory IBlockEntityContainer.Inventory => inventory;
        string IBlockEntityContainer.InventoryClassName => inventory.ClassName;

        private BlockEntityFirebox heatSource = null;
        private double processProgress;
        private SmelteryState state;
        private int glassAmount;//TODO: drop glass on broke
        private AssetLocation glassType;

        private GlassSmelteryInventory inventory = new GlassSmelteryInventory(null, null);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("glasswork:glasssmeltery-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            RegisterGameTickListener(OnCommonTick, 200);
            //TODO: на клиенте надо показывать, если стейт containsglass и температура рабочая - contains liquid glass, иначе - contains solidified glass
        }

        private void OnCommonTick(float dt)
        {
            if(state == SmelteryState.Empty || heatSource == null || !heatSource.IsHeatedUp()) return;
            double timeOffset = 0;
            if(state == SmelteryState.ContainsMix)
            {
                if(heatSource.GetTempElapsedTime(timeOffset, MELTING_TEMPERATURE) > 0)
                {
                    state = SmelteryState.Melting;
                }
            }
            if(state == SmelteryState.Melting)
            {
                double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT - processProgress;
                double time = heatSource.GetTempElapsedTime(timeOffset, MELTING_TEMPERATURE);
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
                double timeLeft = glassAmount * BUBBLING_PROCESS_MULTIPLIER - processProgress;
                double time = heatSource.GetTempElapsedTime(timeOffset, BUBBLING_TEMPERATURE);
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

        public void RemoveGlass(int amount)
        {

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