using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
    public class ItemStargateDebugTablet : Item
    {
        private SkillItem[] modes;
        private ICoreClientAPI capi;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            modes = new SkillItem[]
            {
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-showgateiris"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-showgateiris")
                },
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-showgatevortex"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-showgatevortex")
                },
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-fillgateiris"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-fillgateiris")
                },
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-fillgatevortex"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-fillgatevortex")
                },
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-checkirisstate"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-checkirisstate")
                },
                new SkillItem
                {
                    Code = new AssetLocation("astriaporta:debugtabletmode-showdhdsearchrange"),
                    Name = Lang.Get("astriaporta:debugtabletmodename-showdhdsearchrange")
                }
            };

            capi = api as ICoreClientAPI;
            if (capi != null)
            {
                modes[0].WithLetterIcon(capi, "IS");
                modes[1].WithLetterIcon(capi, "VS");
                modes[2].WithLetterIcon(capi, "IF");
                modes[3].WithLetterIcon(capi, "VF");
                modes[4].WithLetterIcon(capi, "CIS");
                modes[5].WithLetterIcon(capi, "DR");
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.NotHandled;

            if (capi == null)
            {
                return;
            }

            if (byEntity is not EntityPlayer) return;
            EntityPlayer byPlayer = byEntity as EntityPlayer;

            int previousX, previousY, previousZ;
            previousX = slot.Itemstack.TempAttributes.GetInt("posx", -1);
            previousY = slot.Itemstack.TempAttributes.GetInt("posy", -1);
            previousZ = slot.Itemstack.TempAttributes.GetInt("posz", -1);
            if (previousX == blockSel.Position.X && previousY == blockSel.Position.Y && previousZ == blockSel.Position.Z)
            {
                capi.World.HighlightBlocks(byPlayer.Player, 0, new List<BlockPos>());
                slot.Itemstack.TempAttributes.SetInt("posx", -1);
                return;
            }

            BlockEntity targetBE = capi.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (targetBE == null) return;

            switch (slot.Itemstack.Attributes.GetInt("toolMode", 0))
            {
                case 0:
                    DoActionShowIrisArea(byPlayer.Player, targetBE as BlockEntityStargate);
                    break;
                case 1:
                    DoActionShowVortexArea(byPlayer.Player, targetBE as BlockEntityStargate);
                    break;
                case 2:
                    DoActionFillIrisArea(targetBE as BlockEntityStargate);
                    break;
                case 3:
                    DoActionFillVortexArea(targetBE as BlockEntityStargate);
                    break;
                case 4:
                    DoActionCheckIrisState(targetBE as BlockEntityStargate);
                    break;
                case 5:
                    DoActionShowDhdSearchArea(byPlayer.Player, targetBE as BlockEntityDialHomeDevice);
                    break;
            }

            slot.Itemstack.TempAttributes.SetInt("posx", blockSel.Position.X);
            slot.Itemstack.TempAttributes.SetInt("posy", blockSel.Position.Y);
            slot.Itemstack.TempAttributes.SetInt("posz", blockSel.Position.Z);
        }

        protected void DoActionShowIrisArea(IPlayer player, BlockEntityStargate gate)
        {
            if (gate == null) return;
            Cuboidi range = gate.IrisArea;
            if (range == null) return;

            List<BlockPos> positions = CuboidOffsetToBlockPositions(range, gate.Pos);
            capi.World.HighlightBlocks(player, 0, positions);
        }

        protected void DoActionFillIrisArea(BlockEntityStargate gate)
        {
            if (gate == null) return;
            Block filler = capi.World.GetBlock(new AssetLocation("game:soil-medium-none"));
            if (filler == null) return;
            Cuboidi range = gate.IrisArea;
            if (range == null) return;

            List<BlockPos> positions = CuboidOffsetToBlockPositions(range, gate.Pos);
            for (int i = 0; i < positions.Count; i++)
            {
                if (capi.World.BlockAccessor.GetBlock(positions[i]).Id == 0)
                {
                    capi.World.BlockAccessor.SetBlock(filler.Id, positions[i]);
                }
            }
        }

        protected void DoActionShowVortexArea(IPlayer player, BlockEntityStargate gate)
        {
            if (gate == null) return;
            Cuboidi range = gate.VortexArea;
            if (range == null) return;

            List<BlockPos> positions = CuboidOffsetToBlockPositions(range, gate.Pos);
            capi.World.HighlightBlocks(player, 0, positions);
        }

        protected void DoActionFillVortexArea(BlockEntityStargate gate)
        {
            if (gate == null) return;
            Block filler = capi.World.GetBlock(new AssetLocation("game:soil-medium-none"));
            if (filler == null) return;
            Cuboidi range = gate.VortexArea;
            if (range == null) return;

            List<BlockPos> positions = CuboidOffsetToBlockPositions(range, gate.Pos);
            for (int i = 0; i < positions.Count; i++)
            {
                if (capi.World.BlockAccessor.GetBlock(positions[i]).Id == 0)
                {
                    capi.World.BlockAccessor.SetBlock(filler.Id, positions[i]);
                }
            }
        }

        protected void DoActionCheckIrisState(BlockEntityStargate gate)
        {
            if (gate == null) return;
            capi.ShowChatMessage(gate.IsIrisClear() ? "Gate iris space is clear" : "Gate iris space is NOT clear");
        }

        protected void DoActionShowDhdSearchArea(IPlayer player, BlockEntityDialHomeDevice dhd)
        {
            if (dhd == null) return;
            Cuboidi range = dhd.SearchArea;

            List<BlockPos> positions = CuboidOffsetToBlockPositions(range, dhd.Pos);
            capi.World.HighlightBlocks(player, 0, positions);
        }

        protected List<BlockPos> CuboidOffsetToBlockPositions(Cuboidi range, BlockPos fromPos)
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                for (int z = range.MinZ; z <= range.MaxZ; z++)
                {
                    for (int y = range.MinY; y <= range.MaxY; y++)
                    {
                        positions.Add(fromPos.AddCopy(x, y, z));
                    }
                }
            }

            return positions;
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
        {
            return slot.Itemstack.Attributes.GetInt("toolMode", 0);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return modes;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; i < modes.Length; i++)
            {
                modes[i]?.Dispose();
            }
        }
    }
}
