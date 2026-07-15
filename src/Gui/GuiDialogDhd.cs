using AstriaPorta.Content;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Gui
{
	public class GuiDialogDhd : GuiDialogGeneric
	{
		private GuiElementTextInput addressInputElement;
		private GuiElementDynamicText connectedGateElement;
		private GuiElementTextButton dialbutton;
		private bool isDialing = false;

		public BlockPos bePosition;

		public Action<IStargateAddress> OnAddressChanged;
		public Action<IStargateAddress> OnAddressConfirmed;

		public GuiDialogDhd(string dialogTitle, ICoreClientAPI capi, BlockEntityDialHomeDevice owner, bool isDialing) : base(dialogTitle, capi)
		{
			bePosition = owner.Pos.Copy();
			this.isDialing = isDialing;

			ElementBounds line = ElementBounds.Fixed(0, 0, 150, 20);
			ElementBounds input = ElementBounds.Fixed(0, 20, 150, 25);

			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;

			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

			float width = 250;

			SingleComposer = capi.Gui
				.CreateCompo("blockentitydhdaddressdialog", dialogBounds)
				.AddShadedDialogBG(bgBounds)
				.AddDialogTitleBar(DialogTitle, OnTitleBarClose)
				.BeginChildElements(bgBounds)
					.AddStaticText(Lang.Get("astriaporta:DhdGuiTitleText"), CairoFont.WhiteMediumText(), line = line.BelowCopy(0, 0).WithFixedWidth(width))
					.AddButton("Dial", OnClickGateControlButton, line = line.BelowCopy(0, 40), EnumButtonStyle.Small)
					.AddTextInput(line = line.BelowCopy(0, 40), (s) => { }, CairoFont.SmallTextInput(), "addressInput")
					.AddButton("Close connection", OnClickDisconnect, line = line.BelowCopy(0, 40), EnumButtonStyle.Small)
					.AddButton("Relink", OnClickRefresh, line = line.BelowCopy(0, 40), EnumButtonStyle.Small)
					.AddDynamicText(owner.LinkedAddress, CairoFont.WhiteSmallishText(), line = line.BelowCopy(0, 40), "linkedGate")
				.EndChildElements()
				.Compose();

			addressInputElement = SingleComposer.GetTextInput("addressInput");
			addressInputElement.OnTryTextChangeText = OnTryChangeAddressText;
			connectedGateElement = SingleComposer.GetDynamicText("linkedGate");
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
		}

		protected void OnTitleBarClose()
		{
			OnButtonCancel();
		}

		protected bool OnButtonCancel()
		{
			TryClose();
			return true;
		}

		private bool OnTryChangeAddressText(List<string> sl)
		{
			if (sl.Count == 0) return false;

			string s = sl[0];
			s = AddressUtils.SanitizeAddressString(s);
			addressInputElement.Text = s;

			sl.Clear();
			sl.Add(s);

			return true;
		}

		private bool OnClickConnect()
		{
			string s = AddressUtils.SanitizeAddressString(addressInputElement.Text);
			if (s.Length < 7) return false;

			StargateAddress a = new StargateAddress();
			a.FromGlyphs(AddressUtils.StringAddressToBytes(s), capi);

			BlockEntityDialHomeDevice dhd;
			dhd = capi.World.BlockAccessor.GetBlockEntity<BlockEntityDialHomeDevice>(bePosition);
			if (dhd != null && dhd is BlockEntityDialHomeDevice)
			{
				dhd.DialDhd(a);
			}

			return true;
		}

		private bool OnClickDisconnect()
		{
			BlockEntityDialHomeDevice dhd;

			dhd = capi.World.BlockAccessor.GetBlockEntity<BlockEntityDialHomeDevice>(bePosition);
			if (dhd != null && dhd is BlockEntityDialHomeDevice)
			{
				dhd.TryCloseGate();
			}

			return true;
		}

		private bool OnClickRefresh()
		{
			BlockEntityDialHomeDevice dhd;

			dhd = capi.World.BlockAccessor.GetBlockEntity<BlockEntityDialHomeDevice>(bePosition);
			if (dhd != null && dhd is BlockEntityDialHomeDevice)
			{
				connectedGateElement.SetNewText(dhd.CoupleDhd());
			}

			return true;
		}

		private bool OnClickGateControlButton()
		{
			if (isDialing)
			{
				return OnClickDisconnect();
			}

			return OnClickConnect();
		}
	}
}
