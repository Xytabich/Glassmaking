using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public class GuiDialogItemRecipeSelector : GenericItemAction.GuiHeldItemActionDialog
    {
        private const double floatyDialogPosition = 0.5;

        private const double floatyDialogAlign = 0.75;

        public override string ToggleKeyCombinationCode => null;

        public override bool PrefersUngrabbedMouse => false;

        private int prevSlotOver = -1;

        private CollectibleObject item;
        private KeyValuePair<IAttribute, ItemStack>[] recipeOutputs;
        private List<SkillItem> skillItems;

        private BlockSelection blockPos;

        public GuiDialogItemRecipeSelector(ICoreClientAPI capi) : base(capi) { }

        public override void OnBlockTexturesLoaded()
        {
            base.OnBlockTexturesLoaded();
            capi.Input.SetHotKeyHandler("itemrecipeselect", OnKeyCombination);
        }

        private bool OnKeyCombination(KeyCombination viaKeyComb)
        {
            var player = capi.World.Player;
            var itemstack = player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if(itemstack == null) return false;
            this.item = itemstack.Collectible;

            bool sourceSelected = false;
            IItemCrafter source;
            if((source = itemstack.Collectible as IItemCrafter) != null)
            {
                if(source.PreventRecipeAssignment(player, itemstack)) return false;
                if(source.TryGetRecipeOutputs(player, itemstack, out recipeOutputs))
                {
                    sourceSelected = true;
                }
            }
            if(!sourceSelected)
            {
                foreach(var behavior in itemstack.Collectible.CollectibleBehaviors)
                {
                    if((source = behavior as IItemCrafter) != null)
                    {
                        if(source.PreventRecipeAssignment(player, itemstack)) return false;
                        if(source.TryGetRecipeOutputs(player, itemstack, out recipeOutputs))
                        {
                            sourceSelected = true;
                            break;
                        }
                    }
                }
            }
            if(!sourceSelected) return false;

            blockPos = capi.World.Player.CurrentBlockSelection?.Clone();
            Toggle();

            return true;
        }

        public override void OnGuiOpened()
        {
            skillItems = new List<SkillItem>();
            double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
            foreach(var pair in recipeOutputs)
            {
                ItemStack stack = pair.Value;
                ItemSlot dummySlot = new DummySlot(stack);
                string key = GetCraftDescKey(stack);
                string desc = Lang.GetMatching(key);
                if(desc == key)
                {
                    desc = "";
                }
                skillItems.Add(new SkillItem {
                    Code = stack.Collectible.Code.Clone(),
                    Name = stack.GetName(),
                    Description = desc,
                    RenderHandler = delegate (AssetLocation code, float dt, double posX, double posY) {
                        double num = GuiElement.scaled(size - 5.0);
                        capi.Render.RenderItemstackToGui(dummySlot, posX + num / 2.0, posY + num / 2.0, 100.0, (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize), -1);
                    }
                });
            }

            prevSlotOver = -1;

            int cnt = Math.Max(1, skillItems.Count);

            int cols = Math.Min(cnt, 7);

            int rows = (int)Math.Ceiling(cnt / (float)cols);

            double innerWidth = Math.Max(300, cols * size);
            ElementBounds skillGridBounds = ElementBounds.Fixed(0, 30, innerWidth, rows * size);

            ElementBounds textBounds = ElementBounds.Fixed(0, rows * size + 50, innerWidth, 33);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("toolmodeselect", ElementStdBounds.AutosizedMainDialog)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(Lang.Get("Select Recipe"), OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, skillGridBounds, "skillitemgrid")
                    .AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "name")
                    .AddDynamicText("", CairoFont.WhiteDetailText(), textBounds.BelowCopy(0, 10, 0, 0), "desc")
                .EndChildElements()
                .Compose();

            SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver;
        }

        public override void OnRenderGUI(float deltaTime)
        {
            if(capi.Settings.Bool["immersiveMouseMode"] && blockPos?.Position != null)
            {
                Vec3d vec3d = MatrixToolsd.Project(new Vec3d(blockPos.Position.X + 0.5, blockPos.Position.Y + floatyDialogPosition, blockPos.Position.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
                if(vec3d.Z < 0.0)
                {
                    return;
                }
                SingleComposer.Bounds.Alignment = EnumDialogArea.None;
                SingleComposer.Bounds.fixedOffsetX = 0.0;
                SingleComposer.Bounds.fixedOffsetY = 0.0;
                SingleComposer.Bounds.absFixedX = vec3d.X - SingleComposer.Bounds.OuterWidth / 2.0;
                SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - vec3d.Y - SingleComposer.Bounds.OuterHeight * floatyDialogAlign;
                SingleComposer.Bounds.absMarginX = 0.0;
                SingleComposer.Bounds.absMarginY = 0.0;
            }
            base.OnRenderGUI(deltaTime);
        }

        private string GetCraftDescKey(ItemStack stack)
        {
            string type = stack.Class.Name();
            return stack.Collectible.Code?.Domain + AssetLocation.LocationSeparator + type + "craftdesc-" + stack.Collectible.Code?.Path;
        }

        private void OnSlotOver(int num)
        {
            if(num >= skillItems.Count) return;

            if(num != prevSlotOver)
            {
                prevSlotOver = num;
                SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
                SingleComposer.GetDynamicText("desc").SetNewText(skillItems[num].Description);
            }
        }

        private void OnSlotClick(int index)
        {
            var attributes = new TreeAttribute();
            attributes["key"] = recipeOutputs[index].Key;
            DoItemAction(capi.World.Player, item, "recipe", attributes);

            TryClose();
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }

        public override bool TryClose()
        {
            item = null;
            recipeOutputs = null;
            return base.TryClose();
        }
    }
}