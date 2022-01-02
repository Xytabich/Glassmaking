using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public class GlassBlowingRecipe : IByteSerializable, IRecipeBase<GlassBlowingRecipe>
    {
        public int recipeId;

        public AssetLocation code;

        public JsonItemStack output;

        public JsonGlassBlowingToolStep[] steps;

        public GlassBlowingToolStep[] resolvedSteps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if(steps == null || steps.Length == 0 || output == null)
            {
                world.Logger.Error("Glassblowing recipe with output {0} has no steps or missing output. Ignoring recipe.", Output);
                return false;
            }
            if(!output.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }
            var system = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
            resolvedSteps = new GlassBlowingToolStep[steps.Length];
            for(int i = 0; i < steps.Length; i++)
            {
                var tool = system.GetGlassBlowingTool(steps[i].tool);
                if(tool == null)
                {
                    world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}", steps[i].tool, sourceForErrorLogging);
                    return false;
                }
                var step = tool.GetStepInstance();
                step.tool = steps[i].tool;
                step.shape = steps[i].GenRadii();
                if(!step.Resolve(steps[i].attributes, world, sourceForErrorLogging))
                {
                    return false;
                }
                resolvedSteps[i] = step;
            }
            return true;
        }

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public void GetRecipeInfo(ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine("Recipe: " + recipeAttribute.GetString("code"));
            int step = recipeAttribute.GetInt("step");
            dsc.AppendLine("Step: " + (step + 1));
            resolvedSteps[step].GetStepInfo(recipeAttribute.GetTreeAttribute("data"), dsc, world, withDebugInfo);
        }

        public WorldInteraction[] GetHeldInteractionHelp(ITreeAttribute recipeAttribute)
        {
            int step = recipeAttribute.GetInt("step");
            return resolvedSteps[step].GetHeldInteractionHelp(recipeAttribute.GetTreeAttribute("data"));
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(code);
            writer.Write(resolvedSteps.Length);
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                writer.Write(resolvedSteps[i].tool);
                resolvedSteps[i].ToBytes(writer);
            }
            output.ToBytes(writer);
        }

        public void OnHeldInteractStart(ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            resolvedSteps[step].OnHeldInteractStart(slot, ref data, byEntity, blockSel, entitySel, firstEvent, ref handling, out float progress);
            if(progress >= 1f)
            {
                //TODO: изменить меш
                step++;
                if(step >= steps.Length)
                {
                    recipeAttribute = null;
                    //TODO: give output
                }
                else
                {
                    if(prevData != null) recipeAttribute.RemoveAttribute("data");
                    recipeAttribute.SetInt("step", step);
                    slot.MarkDirty();
                }
            }
            else
            {
                //TODO: изменить меш в соответствии с прогрессом
                if(prevData != data)
                {
                    if(data == null) recipeAttribute.RemoveAttribute("data");
                    else recipeAttribute["data"] = data;
                }
            }
        }

        public bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            bool result = resolvedSteps[step].OnHeldInteractStep(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, out float progress);
            if(progress >= 1f)
            {
                //TODO: изменить меш
                step++;
                if(step >= steps.Length)
                {
                    recipeAttribute = null;
                    //TODO: give output
                }
                else
                {
                    if(prevData != null) recipeAttribute.RemoveAttribute("data");
                    recipeAttribute.SetInt("step", step);
                    slot.MarkDirty();
                }
                return false;
            }
            else
            {
                //TODO: изменить меш в соответствии с прогрессом
                if(prevData != data)
                {
                    if(data == null) recipeAttribute.RemoveAttribute("data");
                    else recipeAttribute["data"] = data;
                }
            }
            return result;
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            code = reader.ReadAssetLocation();
            resolvedSteps = new GlassBlowingToolStep[reader.ReadInt32()];
            var system = resolver.Api.ModLoader.GetModSystem<GlassMakingMod>();
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                var code = reader.ReadString();
                var tool = system.GetGlassBlowingTool(code);
                resolvedSteps[i] = tool.GetStepInstance();
                resolvedSteps[i].tool = code;
                resolvedSteps[i].FromBytes(reader, resolver);
            }
            output = new JsonItemStack();
            output.FromBytes(reader, resolver.ClassRegistry);
            output.Resolve(resolver, "[FromBytes]");
        }

        public GlassBlowingRecipe Clone()
        {
            return new GlassBlowingRecipe() {
                recipeId = recipeId,
                code = code.Clone(),
                output = output.Clone(),
                resolvedSteps = Array.ConvertAll(resolvedSteps, CloneStep),
                Name = Name.Clone(),
                Enabled = Enabled
            };
        }

        private static GlassBlowingToolStep CloneStep(GlassBlowingToolStep other)
        {
            return other.Clone();
        }
    }

    public sealed class JsonGlassBlowingToolStep
    {
        public string tool;

        public string[] shape;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject attributes;

        public int[,] GenRadii()
        {
            if(shape != null && shape.Length > 0)
            {
                var shapeRadii = new int[shape.Length, 2];
                for(int i = 0; i < shape.Length; i++)
                {
                    string str = shape[i];
                    int innerRadius = 0;
                    int index = 0;
                    for(; index < str.Length; index++)
                    {
                        if(str[index] == '_') innerRadius++;
                        if(str[index] == '#') break;
                    }
                    innerRadius = GameMath.Clamp(innerRadius, 0, 15);
                    int outerRadius = 0;
                    for(; index < str.Length; index++)
                    {
                        if(str[index] != '#') break;
                        outerRadius++;
                    }
                    outerRadius = GameMath.Clamp(innerRadius + outerRadius, innerRadius + 1, 16);
                    shapeRadii[i, 0] = innerRadius;
                    shapeRadii[i, 1] = outerRadius;
                }
                return shapeRadii;
            }
            else
            {
                return null;
            }
        }
    }

    public abstract class GlassBlowingToolStep
    {
        public string tool;

        public int[,] shape;

        public bool hasShape => shape != null && shape.GetLength(0) > 0;

        public abstract bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging);

        public virtual void ToBytes(BinaryWriter writer)
        {
            if(shape == null) writer.Write(0);
            else
            {
                int length = shape.GetLength(0);
                writer.Write(length);
                for(int i = 0; i < length; i++)
                {
                    writer.Write((byte)((shape[i, 0] & 15) | ((shape[i, 1] << 4) & 15)));
                }
            }
        }

        public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            int length = reader.ReadInt32();
            shape = new int[length, 2];
            for(int i = 0; i < length; i++)
            {
                var data = reader.ReadByte();
                shape[i, 0] = data & 15;
                shape[i, 1] = (data >> 4) & 15;
            }
        }

        public abstract GlassBlowingToolStep Clone();

        public virtual void GetStepInfo(ITreeAttribute treeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { }

        public abstract WorldInteraction[] GetHeldInteractionHelp(ITreeAttribute treeAttribute);

        public void OnHeldInteractStart(ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out float progress)
        {
            progress = 1;
        }

        public bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out float progress)
        {
            progress = 1;
            return true;
        }
    }
}