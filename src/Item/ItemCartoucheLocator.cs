using AstriaPorta.Config;
using AstriaPorta.Systems;
using AstriaPorta.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
    public class StargateLocatorProps
    {
        public LocatorSchematicList[] Schematics;
    }

    public class LocatorSchematicList
    {
        public string Schematic;
        public int Weight;
    }

    public class ItemCartoucheLocator : Item
    {
        ModSystemStructureLocator strucLocSys;
        ICoreServerAPI sapi;
        ItemStack currentStack;
        long tickId = -1;
        int attempts = 0;

        public bool findNextChunk = true;

        public int MinTeleporterRangeInBlocks = 1000;
        public int MaxTeleporterRangeInBlocks = 8000;
        public BlockPos StartPosition;

        private WorldInteraction[] interactions;
        private WorldInteraction[] emptyInteractions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);


            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            emptyInteractions = new WorldInteraction[] { };
            interactions = ObjectCacheUtil.GetOrCreate(api, "astriaporta:cartoucheinteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "astriaporta:itemhelp-cartouche-generate",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            strucLocSys = api.ModLoader.GetModSystem<ModSystemStructureLocator>();
        }

        public string GetRandomSchematicCode(StargateLocatorProps props)
        {
            if (props.Schematics.Length == 0) return "";

            int chance = 0;
            for (int i = 0; i < props.Schematics.Length; i++)
            {
                chance += props.Schematics[i].Weight;
            }

            int roll = api.World.Rand.Next(1, chance + 1);
            chance = 0;
            for (int i = 0; i < props.Schematics.Length; i++)
            {
                chance += props.Schematics[i].Weight;
                if (chance >= roll)
                {
                    return props.Schematics[i].Schematic;
                }
            }

            return props.Schematics[0].Schematic;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (inSlot.Itemstack?.Item?.Code.BeginsWith("astriaporta", "coordinatecartouche-") == true)
            {
                if (!inSlot.Itemstack.Attributes.HasAttribute("generationfinished") && inSlot.Itemstack.Attributes.HasAttribute("gatelocatorworking"))
                {
                    return interactions;
                }
            }

            return emptyInteractions;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefaultAction;
            if (byEntity.World.Side == EnumAppSide.Client) return;

            if (!StargateConfig.Loaded.EnableCartoucheGates)
            {
                if (byEntity is EntityPlayer)
                {
                    ((byEntity as EntityPlayer).Player as IServerPlayer).SendIngameError("cartouchedisabled");
                }
            }

            if (slot.Itemstack?.Attributes.HasAttribute("generationfinished") == true) return;

            StargateLocatorModSystem slms = api.ModLoader.GetModSystem<StargateLocatorModSystem>();
            if (slms == null) return;
            BlockPos startPos = byEntity.Pos.AsBlockPos;
            StargateLocatorProps props = Attributes["stargateLocatorProps"].AsObject<StargateLocatorProps>();
            string schematicCode = GetRandomSchematicCode(props);
            slot.Itemstack.Attributes.SetString("schematic", schematicCode);
            slms.AddGenerationRequest(slot, startPos.X, startPos.Z, schematicCode);
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForResolve, inSlot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
        }
    }
}
