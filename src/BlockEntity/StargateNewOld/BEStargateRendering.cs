using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using AstriaPorta.Util;

namespace AstriaPorta.Content
{
    public abstract partial class BEStargate
	{
		protected GateRenderer renderer;
		private EventHorizonRenderer eventHorizonRenderer;

		protected bool rendererInitialized = false;
		protected bool horizonInitialized = false;
		protected bool horizonRegistered = false;

		/// <summary>
		/// Activates and registers the renderer for the gate
		/// event horizon
		/// </summary>
		/// <param name="isActivating">Whether the splash should occur</param>
		public void ActivateHorizon(bool isActivating = true)
		{
			if (eventHorizonRenderer != null)
			{
				eventHorizonRenderer.t = 0;
				eventHorizonRenderer.activating = isActivating;
				eventHorizonRenderer.shouldRender = true;
				if (!horizonRegistered)
				{
					((ICoreClientAPI)Api).Event.RegisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
					horizonRegistered = true;
				}
			}
		}

		/// <summary>
		/// Disables and unregisters the event horizon renderer
		/// </summary>
		public void DeactivateHorizon()
		{
			if (eventHorizonRenderer != null)
			{
				eventHorizonRenderer.shouldRender = false;
				((ICoreClientAPI)Api).Event.UnregisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
				horizonRegistered = false;
			}
		}

		/// <summary>
		/// Updates the renderer state.
		/// Currently updates only the inner ring rotation
		/// </summary>
		protected void UpdateRendererState()
		{
			if (renderer == null) return;

			renderer.ringRotation = currentAngle;
		}

		/// <summary>
		/// Updates which chevrons should glow, depending on the
		/// active chevrons and the length of the address being dialed
		/// </summary>
		protected void UpdateChevronGlow()
		{
			if (renderer == null) return;
			renderer.chevronGlow = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			byte padding = 0;

			for (int i = 1; i < 10; i++)
			{
				switch (dialingAddress.AddressLength)
				{
					case EnumAddressLength.Short:
						{
							if (i == 4 || i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}
							break;
						}
					case EnumAddressLength.Medium:
						{
							if (i == 5)
							{
								padding++;
								continue;
							}

							if (i <= (activeChevrons + padding))
							{
								renderer.chevronGlow[i - 1] = 200;
							}
							break;
						}
					case EnumAddressLength.Long:
						{
							if (i <= activeChevrons) renderer.chevronGlow[i - 1] = 200;
							break;
						}
				}
			}
		}

		// ===================
		// Initialization
		// ===================

		/// <summary>
		/// Initializes the event horizon renderer. Registers the renderer
		/// if the gate is connected
		/// </summary>
		/// <param name="api"></param>
		[Obsolete]
		public void InitializeHorizonRenderer(ICoreAPI api)
		{
			AssetLocation horizonLoc = new AssetLocation("astriaporta", "gates/kawoosh2");
			IShaderProgram horizonProgram = Api.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonShaderProgram;

			MeshData horizonMesh = GenRoundHorizonMesh(1.3f, 39, 16, .5f + 1.3f + 0.065f, .5f + 1.3f + 0.025f);

			eventHorizonRenderer = new EventHorizonRenderer((ICoreClientAPI)api, Pos, horizonMesh, horizonProgram, horizonLoc, false);
			eventHorizonRenderer.shouldRender = false;
			eventHorizonRenderer.Orientation = Block.Shape.rotateY;
			// ((ICoreClientAPI)api).Event.RegisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
			horizonInitialized = true;

			if (stargateState == EnumStargateState.ConnectedIncoming || stargateState == EnumStargateState.ConnectedOutgoing)
			{
				ActivateHorizon(false);
			}
		}


		// ===================
		// Disposal
		// ===================

		/// <summary>
		/// Disposes of the main and event horizon renderers
		/// </summary>
		private void DisposeRenderers()
		{
			if (Api.Side == EnumAppSide.Server) return;

			if (renderer != null)
			{
				((ICoreClientAPI)Api).Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
			}
			if (eventHorizonRenderer != null && horizonRegistered)
			{
				((ICoreClientAPI)Api).Event.UnregisterRenderer(eventHorizonRenderer, EnumRenderStage.Opaque);
				horizonRegistered = false;
			}
			renderer?.Dispose();
			renderer = null;
			eventHorizonRenderer?.Dispose();
			eventHorizonRenderer = null;
			rendererInitialized = false;
			horizonInitialized = false;
		}
	}
}
