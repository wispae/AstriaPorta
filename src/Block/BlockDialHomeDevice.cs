using AstriaPorta.Config;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AstriaPorta.Content
{
	public class BlockDialHomeDevice : BlockInteractable
	{
		WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if (api.Side != EnumAppSide.Client) return;

			ICoreClientAPI capi = api as ICoreClientAPI;

			if (capi != null) interactions = ObjectCacheUtil.GetOrCreate(api, "astriaporta:dhdInteractions", () =>
			{
				List<ItemStack> writeableStacklist = new List<ItemStack>();

				foreach (CollectibleObject obj in capi.World.Collectibles)
				{
					if (obj.Attributes?.IsTrue("holdsgateaddress") == true)
					{
						List<ItemStack> stacks = obj.GetHandBookStacks(capi);
						if (stacks != null) writeableStacklist.AddRange(stacks);
					}
				}

				return new WorldInteraction[]
				{
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-dhd-open-ui",
						RequireFreeHand = false,
						MouseButton = EnumMouseButton.Right
					},
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-dhd-close-gate",
						HotKeyCode = "shift",
						RequireFreeHand = true,
						MouseButton = EnumMouseButton.Right,
						ShouldApply = (wi, bs, es) =>
						{
							return bs.Block.GetBlockEntity<BlockEntityDialHomeDevice>(bs)?.IsGateOpen ?? false;
						}
					},
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-dhd-dial",
						HotKeyCode = "shift",
						RequireFreeHand = true,
						MouseButton = EnumMouseButton.Right,
						ShouldApply = (wi, bs, es) =>
						{
							return (!bs.Block.GetBlockEntity<BlockEntityDialHomeDevice>(bs)?.IsGateOpen) ?? false;
						}
					},
					new WorldInteraction
					{
						ActionLangCode = "astriaporta:blockhelp-dhd-notedial",
						HotKeyCode = "shift",
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

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}

		public override void AddMiningTierInfo(StringBuilder sb)
		{
			if (!StargateConfig.Loaded.DhdDestructable) return;
			RequiredMiningTier = StargateConfig.Loaded.DhdMiningTier;
			base.AddMiningTierInfo(sb);
		}

		public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
		{
			if (!StargateConfig.Loaded.DhdDestructable) return EnumBlockMaterial.Mantle;
			if (pos == null) return EnumBlockMaterial.Mantle;
			BlockEntityDialHomeDevice dhd = GetBlockEntity<BlockEntityDialHomeDevice>(pos);
			if (dhd == null) return EnumBlockMaterial.Mantle;

			if (dhd.CanBreak) return EnumBlockMaterial.Metal;

			return EnumBlockMaterial.Mantle;
		}
	}
}
