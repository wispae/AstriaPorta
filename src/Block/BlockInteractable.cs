using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.Server;

namespace AstriaPorta.Content
{
	public abstract class BlockInteractable : Block
	{
		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			BlockEntity target = world.BlockAccessor.GetBlockEntity(blockSel.Position);
			if (target != null && target is IBlockEntityInteractable)
			{
				return (target as IBlockEntityInteractable).OnRightClickInteraction(byPlayer);
			}

			return true;
		}
	}
}
