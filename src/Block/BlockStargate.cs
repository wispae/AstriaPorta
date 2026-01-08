using AstriaPorta.Config;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace AstriaPorta.Content
{
	public class BlockStargate : BlockInteractable
	{
		WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			if (api.Side != EnumAppSide.Client) return;
			ICoreClientAPI capi = api as ICoreClientAPI;

			interactions = ObjectCacheUtil.GetOrCreate(capi, "astriaporta:stargateInteractions", () =>
			{
				List<ItemStack> writeableStacklist = new List<ItemStack>();

				foreach (CollectibleObject obj in capi.World.Collectibles)
				{
					if (obj.FirstCodePart() == "paper")
					{
						List<ItemStack> stacks = obj.GetHandBookStacks(capi);
						if (stacks != null) writeableStacklist.AddRange(stacks);
					}
				}

				return new WorldInteraction[]
				{
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-stargate-open-camouflage",
						MouseButton = EnumMouseButton.Right
					},
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-stargate-copy-address",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = writeableStacklist.ToArray(),
						GetMatchingStacks = (wi, bs, es) =>
						{
							if (wi.Itemstacks.Length == 0) return null;
							return wi.Itemstacks;
						}
					}
				};
			});
		}

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
			if (world.BlockAccessor.GetBlockEntity<StargateBase>(pos) == null)
			{
				return Lang.Get("astriaporta:error-gate-blockentity-broken", Lang.Get("Right mouse button"));
			}

            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity target = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (target != null && target is IBlockEntityInteractable)
            {
                return (target as IBlockEntityInteractable).OnRightClickInteraction(byPlayer);
            }
			else
			{
                FixGateEntity(world.BlockAccessor, blockSel.Position);
            }

			return true;
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
		{
			return true;
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions;
		}

		public override void AddMiningTierInfo(StringBuilder sb)
		{
			if (!StargateConfig.Loaded.StargateDestructable) return;
			RequiredMiningTier = StargateConfig.Loaded.StargateMiningTier;
			base.AddMiningTierInfo(sb);
		}

		public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
		{
			if (!StargateConfig.Loaded.StargateDestructable) return EnumBlockMaterial.Mantle;
			if (pos == null)
			{
				GateLogger.LogError(LogLevel.Error, "gate block position was null?");
                return EnumBlockMaterial.Mantle;
            }
			IStargate gate = GetBlockEntity<StargateBase>(pos);
			if (gate == null)
			{
				// gates breaking safety feature
				FixGateEntity(blockAccessor, pos);
                return EnumBlockMaterial.Soil;
            }

			if (gate.CanBreak) return EnumBlockMaterial.Metal;
			return EnumBlockMaterial.Mantle;
		}

        public override float GetResistance(IBlockAccessor blockAccessor, BlockPos pos)
        {
			if (GetBlockEntity<StargateBase>(pos) == null)
			{
				FixGateEntity(blockAccessor, pos);
				return 1f;
			}

            return base.GetResistance(blockAccessor, pos);
        }

		private void FixGateEntity(IBlockAccessor accessor, BlockPos pos)
		{
			GateLogger.LogWarning(LogLevel.Warning, $"Stargate at {pos} somehow lost its BlockEntity, attempting to fix...");
			accessor.SpawnBlockEntity(EntityClass, pos);
		}
	}
}
