using GlassMaking.Common;
using GlassMaking.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GlassBlowingRecipe : IRecipeBase, IByteSerializable, IRecipeBase<GlassBlowingRecipe>
    {
        private static SmoothRadialShape EmptyShape = new SmoothRadialShape() { segments = 1, outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() { vertices = new float[][] { new float[] { -1.5f, 0 } } } } };

        public int recipeId;

        [JsonProperty]
        public AssetLocation code;

        [JsonProperty]
        public JsonItemStack output;

        [JsonProperty]
        public GlassBlowingRecipeStep[] steps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        AssetLocation IRecipeBase.code => code;

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public int GetStepIndex(ITreeAttribute recipeAttribute)
        {
            int step = recipeAttribute.GetInt("step", 0);
            return step < 0 || step >= steps.Length ? -1 : step;
        }

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
            return true;
        }

        public void GetRecipeInfo(ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine(Lang.Get("glassmaking:Recipe: {0}", Lang.Get(new AssetLocation(recipeAttribute.GetString("code")).WithPathPrefixOnce("glassblowingrecipe-").ToString())));
            int step = recipeAttribute.GetInt("step", 0);
            dsc.AppendLine(Lang.Get("glassmaking:Step {0}/{1}", step + 1, steps.Length));
            dsc.AppendLine(Lang.Get("glassmaking:Tool: {0}", Lang.Get("glassmaking:glassblowingtool-" + steps[step].tool)));
        }

        public bool TryBeginStep(ItemSlot slot, int index)
        {
            int current = slot.Itemstack.TempAttributes.GetInt("glassmaking:blowingStep", 0);
            if(current <= index)
            {
                if(current < index)
                {
                    slot.Itemstack.TempAttributes.SetInt("glassmaking:blowingStep", index);
                    slot.MarkDirty();
                }
                return true;
            }
            return false;
        }

        public bool IsCurrentStep(ItemSlot slot, int index)
        {
            return slot.Itemstack.TempAttributes.GetInt("glassmaking:blowingStep", 0) == index;
        }

        public void OnStepProgress(ItemSlot slot, float progress)
        {
            if(((ItemGlassworkPipe)slot.Itemstack.Collectible).TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
            {
                recipeAttribute.SetFloat("progress", GameMath.Clamp(progress, 0, 1));
                ((ItemGlassworkPipe)slot.Itemstack.Collectible).OnRecipeUpdated(slot, false);
                slot.MarkDirty();
            }
        }

        public void OnStepComplete(ItemSlot slot, EntityAgent byEntity)
        {
            if(byEntity.Api.Side != EnumAppSide.Server) return;
            if(((ItemGlassworkPipe)slot.Itemstack.Collectible).TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
            {
                int step = recipeAttribute.GetInt("step", 0) + 1;
                if(step >= steps.Length)
                {
                    var item = output.ResolvedItemstack.Clone();
                    if(!byEntity.TryGiveItemStack(item))
                    {
                        byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                    }
                    slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:blowingStep");
                    ((ItemGlassworkPipe)slot.Itemstack.Collectible).OnRecipeUpdated(slot, true);
                }
                else
                {
                    recipeAttribute.SetInt("step", step);
                    recipeAttribute.RemoveAttribute("progress");
                }
                slot.MarkDirty();
            }
        }

        public void UpdateMesh(ITreeAttribute recipeAttribute, ItemGlassworkPipe.IMeshContainer container, int glow)
        {
            string code = recipeAttribute.GetString("code");
            int step = recipeAttribute.GetInt("step", 0);
            float t = GameMath.Clamp(recipeAttribute.GetFloat("progress", 0), 0, 1);

            if(container.data == null || !(container.data is MeshInfo data))
            {
                container.data = new MeshInfo(code, step, t);
            }
            else if(!data.Equals(code, step, t))
            {
                data.Set(code, step, t);
            }
            else
            {
                return;
            }

            SmoothRadialShape prevShape = null;
            for(int i = step - 1; i >= 0; i--)
            {
                if(steps[i].shape != null)
                {
                    prevShape = steps[i].shape;
                    break;
                }
            }
            if(steps[step].shape == null)
            {
                container.BeginMeshChange();
                if(prevShape != null)
                {
                    SmoothRadialShape.BuildMesh(container.mesh, prevShape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
                }
                container.EndMeshChange();
                return;
            }

            if(prevShape == null) prevShape = EmptyShape;
            container.BeginMeshChange();
            SmoothRadialShape.BuildLerpedMesh(container.mesh, prevShape, steps[step].shape, t,
                (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
            container.EndMeshChange();
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(code);
            writer.Write(steps.Length);
            for(int i = 0; i < steps.Length; i++)
            {
                steps[i].ToBytes(writer);
            }
            output.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            code = reader.ReadAssetLocation();
            steps = new GlassBlowingRecipeStep[reader.ReadInt32()];
            for(int i = 0; i < steps.Length; i++)
            {
                steps[i] = new GlassBlowingRecipeStep();
                steps[i].FromBytes(reader);
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
                steps = Array.ConvertAll(steps, CloneStep),
                Name = Name.Clone(),
                Enabled = Enabled
            };
        }

        private static GlassBlowingRecipeStep CloneStep(GlassBlowingRecipeStep other)
        {
            return other.Clone();
        }

        private class MeshInfo
        {
            public string code;
            public int step;
            public float progress;

            public MeshInfo(string code, int step, float progress)
            {
                this.code = code;
                this.step = step;
                this.progress = progress;
            }

            public void Set(string code, int step, float progress)
            {
                this.code = code;
                this.step = step;
                this.progress = progress;
            }

            public bool Equals(string code, int step, float progress)
            {
                return this.code == code && this.step == step && this.progress == progress;
            }
        }
    }

    [JsonObject]
    public sealed class GlassBlowingRecipeStep
    {
        [JsonProperty(Required = Required.Always)]
        public string tool;

        [JsonProperty]
        public SmoothRadialShape shape;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject attributes;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(tool);
            writer.Write(shape != null);
            if(shape != null) shape.ToBytes(writer);
            writer.Write(attributes != null);
            if(attributes != null) writer.Write(attributes.Token.ToString());
        }

        public void FromBytes(BinaryReader reader)
        {
            tool = reader.ReadString();
            if(reader.ReadBoolean())
            {
                shape = new SmoothRadialShape();
                shape.FromBytes(reader);
            }
            if(reader.ReadBoolean())
            {
                attributes = new JsonObject(JToken.Parse(reader.ReadString()));
            }
        }

        public GlassBlowingRecipeStep Clone()
        {
            return new GlassBlowingRecipeStep() {
                tool = tool,
                shape = shape.Clone(),
                attributes = attributes?.Clone()
            };
        }
    }
}