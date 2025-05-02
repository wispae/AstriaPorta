using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
	public class BehaviorGateAddressHolder : CollectibleBehavior
	{
        private ICoreAPI api;

		public BehaviorGateAddressHolder(CollectibleObject collObj) : base(collObj)
		{
		}

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            base.GetHeldItemName(sb, itemStack);

            if (itemStack.Attributes.HasAttribute("addresscustomname"))
            {
                sb.AppendLine($"<i>{itemStack.Attributes.GetString("addresscustomname")}</i>");
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			string baseInfo = inSlot.Itemstack.Attributes.GetString("gateAddressS");
			if (inSlot.Itemstack?.Item?.Code.BeginsWith("astriaporta", "coordinatecartouche-") == true)
			{
				if (inSlot.Itemstack.Attributes.HasAttribute("generationfailed"))
				{
					dsc.Append(Lang.Get("astriaporta:astriaporta-cartouche-failed"));
					return;
				}
				if (inSlot.Itemstack.Attributes.GetString("position", "") != "")
				{
					dsc.AppendLine(Lang.Get("astriaporta:astriaporta-cartouche-generating"));
					return;
				}
				dsc.AppendLine($"<i>{baseInfo ?? Lang.Get("astriaporta:astriaporta-cartouche-unlinked")}</i>");
				return;
			}
			dsc.AppendLine($"<i>{baseInfo ?? Lang.Get("astriaporta:astriaporta-no-address-associated")}</i>");
		}

		private byte[] StringAddressToBytes(string s)
		{
			s = s.Replace("-", "");
			string validGlyphs = "0123456789abcdefghijklmnopqrstuvwxyz";
			byte[] glyphs = new byte[s.Length];

			for (byte i = 0; i < s.Length; i++)
			{
				for (byte j = 0; j < validGlyphs.Length; j++)
				{
					if (s[i] == validGlyphs[j])
					{
						glyphs[i] = j;
						break;
					}
				}
			}

			return glyphs;
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
            ItemStack currentStack = slot.Itemstack;

            if (!byEntity.Controls.ShiftKey)
			{
                handHandling = EnumHandHandling.NotHandled;
                handling = EnumHandling.PreventSubsequent;

                if (api.Side == EnumAppSide.Client)
                {
                    TextAreaConfig config = new TextAreaConfig();
                    config.MaxWidth = 200;

                    GuiDialogNoteName noteDialog = new GuiDialogNoteName("Address name", currentStack.Attributes.GetString("addresscustomname", ""), api as ICoreClientAPI, config, slot);

                    noteDialog.OnClosed += () =>
                    {
                        noteDialog.Dispose();
                        noteDialog = null;
                    };

                    noteDialog.TryOpen();
                }

				return;
			}

			if (blockSel != null && blockSel.Block is BlockDialHomeDevice)
			{
				string s = currentStack.Attributes.GetString("gateAddressS", string.Empty);
				bool b = currentStack.TempAttributes.HasAttribute("gatelocatorworking");
				b &= currentStack.Attributes.HasAttribute("generationfinished");
                if (!b && s != string.Empty)
                {
					StargateAddress a = new StargateAddress();
					a.FromGlyphs(StringAddressToBytes(s), api);

					blockSel.Block.GetBlockEntity<BlockEntityDialHomeDevice>(blockSel)?.DialDhd(a);
					handHandling = EnumHandHandling.Handled;
					handling = EnumHandling.PreventSubsequent;
					return;
                }
            }

			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}
	}
}
