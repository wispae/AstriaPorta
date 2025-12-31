using AstriaPorta.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content;

public abstract class StargateVisualManager
{
    protected readonly ICoreClientAPI Capi;
    protected readonly StargateBase Gate;

    public StargateVisualManager(StargateBase gate)
    {
        Capi = gate.Api as ICoreClientAPI;
        Gate = gate;
    }

    protected EventHorizonRenderer EventHorizonRenderer;
    protected GateRenderer GateRenderer;
    protected bool HorizonRendererRegistered = false;
    protected bool HorizonRendererInitialized = false;
    protected bool GateRendererRegistered = false;
    protected bool GateRendererInitialized = false;

    protected LightiningPointLight EventHorizonLight;
    protected bool HorizonLightAdded = false;

    public BlockEntityAnimationUtil AnimUtil => Gate.GetBehavior<BEBehaviorAnimatable>().animUtil;

    public abstract void Initialize();

    /// <summary>
    /// Initializes the gate renderer.
    /// </summary>
    protected abstract void InitializeGateRenderer();

    public virtual void Dispose()
    {
        DisposeRenderers();
    }

    /// <summary>
    /// Activates and registers the renderer for
    /// the gate event horizon
    /// </summary>
    /// <param name="isActivating"></param>
    public void ActivateHorizon(bool isActivating = true)
    {
        if (EventHorizonRenderer == null) return;

        Gate.SoundManager.StopRotateSound();
        Gate.SoundManager.StartActiveSound();

        EventHorizonRenderer.t = 0;
        EventHorizonRenderer.activating = isActivating;
        EventHorizonRenderer.shouldRender = true;
        if (!HorizonRendererRegistered)
        {
            Capi.Event.RegisterRenderer(EventHorizonRenderer, EnumRenderStage.Opaque);
            HorizonRendererRegistered = true;
        }

        if (!HorizonLightAdded)
        {
            Capi.Render.AddPointLight(EventHorizonLight);
            HorizonLightAdded = true;
        }

        if (isActivating)
        {
            SpawnActivationParticles();
        }
    }

    public virtual void ActivateLockChevron()
    {
        if (GateRenderer == null) return;

        GateRenderer.chevronGlow[8] = 200;

        AnimationMetaData metaData = new()
        {
            Animation = "chevron_activte",
            Code = "chevron_activate",
            AnimationSpeed = 1,
            EaseInSpeed = 1,
            EaseOutSpeed = 1,
            Weight = 1
        };

        AnimUtil.StartAnimation(metaData);
    }

    /// <summary>
    /// Disables and unregisters the event horizon renderer
    /// </summary>
    public void DeactivateHorizon()
    {
        if (EventHorizonRenderer == null) return;

        Gate.SoundManager.StopActiveSound();

        EventHorizonRenderer.shouldRender = false;
        Capi.Event.UnregisterRenderer(EventHorizonRenderer, EnumRenderStage.Opaque);
        HorizonRendererRegistered = false;
        Capi.Render.RemovePointLight(EventHorizonLight);
        HorizonLightAdded = false;
    }

    /// <summary>
    /// Disposes of the main and event horizon renderers
    /// </summary>
    protected virtual void DisposeRenderers()
    {
        if (GateRenderer != null)
        {
            if (GateRendererRegistered)
                Capi.Event.UnregisterRenderer(GateRenderer, EnumRenderStage.Opaque);
            GateRenderer.Dispose();
            GateRenderer = null;
            GateRendererRegistered = false;
            GateRendererInitialized = false;
        }

        if (EventHorizonRenderer != null)
        {
            if (HorizonRendererRegistered)
                Capi.Event.UnregisterRenderer(EventHorizonRenderer, EnumRenderStage.Opaque);
            EventHorizonRenderer.Dispose();
            EventHorizonRenderer = null;
            HorizonRendererRegistered = false;
            GateRendererInitialized = false;
        }
    }

    public void DeactivateLockChevron()
    {
        if (GateRenderer == null) return;

        GateRenderer.chevronGlow[8] = 0;
    }

    /// <summary>
    /// Initializes the event horizon renderer. Registers the renderer
    /// if the gate is connected
    /// </summary>
    protected void InitializeHorizonRenderer()
    {
        if (HorizonRendererInitialized) return;

        TextureAtlasPosition texPos = Capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonTexPos;

        EventHorizonRenderer = new EventHorizonRenderer(Capi, Gate.Pos, texPos, false);
        EventHorizonRenderer.shouldRender = false;
        EventHorizonRenderer.Orientation = Gate.Block.Shape.rotateY;

        HorizonRendererInitialized = true;

        if (Gate.State == EnumStargateState.ConnectedIncoming || Gate.State == EnumStargateState.ConnectedOutgoing)
        {
            ActivateHorizon(false);
        }
    }

    public void OnYawPacket(int activeChevrons, float currentAngle)
    {
        if (EventHorizonRenderer != null && !HorizonRendererRegistered)
        {
            ActivateHorizon();
            UpdateChevronGlow(activeChevrons);
            UpdateRendererState(currentAngle);
        }
    }

    public abstract void StartDialing();

    /// <summary>
    /// Updates which chevrons should glow, depending on the
    /// active chevrons and the length of the address being dialed
    /// </summary>
    /// <param name="activeChevrons"></param>
    public void UpdateChevronGlow(int activeChevrons)
    {
        if (Gate.DialingAddress == null)
        {
            UpdateChevronGlow(activeChevrons, EnumAddressLength.Short);
        }
        else
        {
            UpdateChevronGlow(activeChevrons, Gate.DialingAddress.AddressLength);
        }
    }

    /// <summary>
    /// Updates which chevrons should glow, depending on the
    /// active chevrons and the length of the address being dialed
    /// </summary>
    /// <param name="activeChevrons"></param>
    /// <param name="length"></param>
    protected virtual void UpdateChevronGlow(int activeChevrons, EnumAddressLength length)
    {
        if (GateRenderer == null) return;

        GateRenderer.chevronGlow = [0, 0, 0, 0, 0, 0, 0, 0, 0];
        byte padding = 0;

        for (int i = 1; i < 10; i++)
        {
            switch (length)
            {
                case EnumAddressLength.Short:
                    if (i == 4 || i == 5)
                    {
                        padding++;
                        continue;
                    }

                    if (i <= (activeChevrons + padding))
                    {
                        GateRenderer.chevronGlow[i - 1] = 200;
                    }
                    break;

                case EnumAddressLength.Medium:
                    if (i == 5)
                    {
                        padding++;
                        continue;
                    }

                    if (i <= (activeChevrons + padding))
                    {
                        GateRenderer.chevronGlow[i - 1] = 200;
                    }
                    break;
                case EnumAddressLength.Long:
                    if (i <= activeChevrons + padding) GateRenderer.chevronGlow[i - 1] = 200;
                    break;
            }

        }
    }

    /// <summary>
    /// Updates the renderer state.<br/>
    /// Currently updates only the inner ring rotation
    /// </summary>
    public virtual void UpdateRendererState(float currentAngle)
    {
        if (GateRenderer == null) return;

        GateRenderer.ringRotation = currentAngle;
    }

    public virtual void SpawnActivationParticles()
    {
        int particleColor = ColorUtil.ColorFromRgba(new Vec4f(0.8f, 0.2f, 0.2f, 0.8f));
        float offsetXMin = 0;
        float offsetXMax = 0;
        float offsetZMin = 0;
        float offsetZMax = 0;
        Vec3f minV = new Vec3f();
        Vec3f maxV = new Vec3f();

        switch (Gate.Block.Shape.rotateY)
        {
            case 0f:
                offsetXMin = -1f;
                offsetXMax = 2f;
                offsetZMin = 0.5f;
                offsetZMax = 0.5f;

                minV.Z = 1f;
                maxV.Z = 2f;

                break;

            case 90f:
                offsetXMin = 0.5f;
                offsetXMax = 0.5f;
                offsetZMin = -1f;
                offsetZMax = 2f;

                minV.X = 1f;
                maxV.X = 2f;

                break;

            case 180f:
                offsetXMin = -1f;
                offsetXMax = 2f;
                offsetZMin = 0.5f;
                offsetZMax = 0.5f;

                minV.Z = -1f;
                maxV.Z = -2f;

                break;

            case 270f:
                offsetXMin = 0.5f;
                offsetXMax = 0.5f;
                offsetZMin = -1f;
                offsetZMax = 2f;

                minV.X = -1f;
                maxV.X = -2f;

                break;
        }

        Gate.RegisterDelayedCallback((float d) =>
        {
            Capi.World.SpawnParticles(new SimpleParticleProperties()
            {
                MinQuantity = 64f,
                AddQuantity = 64f,
                Color = particleColor,
                MinPos = new Vec3d(Gate.Pos.X + offsetXMin, Gate.Pos.Y + 2, Gate.Pos.Z + offsetZMin),
                AddPos = new Vec3d(offsetXMax - offsetXMin, 3, offsetZMax - offsetZMin),
                MinVelocity = minV,
                AddVelocity = maxV - minV,
                ParticleModel = EnumParticleModel.Cube,
                OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f),
                Async = true,
                GravityEffect = 0,
                WithTerrainCollision = false,
                MinSize = 0.5f,
                MaxSize = 1f,
                LifeLength = 0.25f,
                addLifeLength = 0.25f,
                LightEmission = particleColor
            });
        }, 500);
    }

    public void SpawnDeactivationParticles()
    {
        int particleColor = ColorUtil.ColorFromRgba(new Vec4f(0.9f, 0.5f, 0.5f, 0.75f));
        float offsetXMin = 0;
        float offsetXMax = 0;
        float offsetZMin = 0;
        float offsetZMax = 0;

        switch (Gate.Block.Shape.rotateY)
        {
            case 0f:
                offsetXMin = -1f;
                offsetXMax = 2f;
                offsetZMin = 0.5f;
                offsetZMax = 0.5f;
                break;

            case 90f:
                offsetXMin = 0.5f;
                offsetXMax = 0.5f;
                offsetZMin = -1f;
                offsetZMax = 2f;
                break;

            case 180f:
                offsetXMin = -1f;
                offsetXMax = 2f;
                offsetZMin = 0.5f;
                offsetZMax = 0.5f;
                break;

            case 270f:
                offsetXMin = 0.5f;
                offsetXMax = 0.5f;
                offsetZMin = -1f;
                offsetZMax = 2f;
                break;
        }

        Gate.RegisterDelayedCallback((float d) =>
        {
            Capi.World.SpawnParticles(new SimpleParticleProperties()
            {
                MinQuantity = 64f,
                AddQuantity = 64f,
                Color = particleColor,
                MinPos = new Vec3d(Gate.Pos.X + offsetXMin, Gate.Pos.Y + 2, Gate.Pos.Z + offsetZMin),
                AddPos = new Vec3d(offsetXMax - offsetXMin, 3, offsetZMax - offsetZMin),
                MinVelocity = new Vec3f(0, 0, 0),
                AddVelocity = new Vec3f(0, 0, 0),
                ParticleModel = EnumParticleModel.Cube,
                OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f),
                Async = true,
                GravityEffect = 0,
                WithTerrainCollision = false,
                MinSize = 0.5f,
                MaxSize = 1f,
                LifeLength = 0.1f,
                addLifeLength = 0.1f,
                LightEmission = particleColor
            });
        }, 500);
    }
}
