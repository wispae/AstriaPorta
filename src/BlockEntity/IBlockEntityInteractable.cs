using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace AstriaPorta.Content
{
	public interface IBlockEntityInteractable
	{
		public bool OnRightClickInteraction(IPlayer player);
	}
}
