using AstriaPorta.Config;
using AstriaPorta.Content;
using AstriaPorta.Systems;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AstriaPorta.Content
{
    public abstract class StargateStateManagerBase : IStargateStateManager
    {
        protected ICoreAPI Api;
        protected StargateBase Gate;

        public StargateStateManagerBase()
        {
        }

        protected bool AwaitingChevronAnimation;

        protected long CallbackId = -1;
        protected long TickListenerId = -1;

        protected int CurrentAddressIndex;
        protected EnumDialSpeed CurrentDialSpeed;
        protected byte CurrentGlyph;
        protected float GlyphAngle;
        internal byte NextGlyph;
        protected float PreviousAngle;
        protected bool RotateCW;
        internal float TimeOpen;

        public byte ActiveChevrons { get; protected set; }
        public float CurrentAngle { get; protected set; }
        public IStargateAddress DialingAddress { get; set; }

        public bool IsCallbackRegistered => CallbackId != -1;
        public bool IsForceLoaded { get; set; } = false;
        public bool IsRegisteredToGateManager { get; set; } = false;
        public bool IsTickListenerRegistered => TickListenerId != -1;

        public float MaxConnectionDuration => Gate.Type switch
        {
            EnumStargateType.Milkyway => StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway,
            EnumStargateType.Pegasus => StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway,
            EnumStargateType.Destiny => StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway,
            _ => StargateConfig.Loaded.MaxConnectionDurationSecondsMilkyway
        };

        public float RemoteLoadTimeout { get; set; }
        public float RotationDegPerSecond { get; set; }
        public bool UseQuickDial { get; set; } = false;
        public EnumStargateState State { get; protected set; }


        public abstract void AcceptConnection(byte activeChevrons);
        public abstract void AcceptIncomingConnection(IStargate caller);
        public abstract void ProcessStatePacket(EnumStargatePacketType packetType, byte[] data);
        protected abstract void OnGlyphReached();
        protected abstract void OnTick(float delta);
        public abstract bool TryDial(IStargateAddress address, EnumDialSpeed speed);

        public virtual void Initialize(StargateBase gate)
        {
            Api = gate.Api;
            Gate = gate;
            RemoteLoadTimeout = StargateConfig.Loaded.MaxTimeoutSeconds;
            GlyphAngle = 360 / Gate.GlyphLength;
        }

        public virtual void Dispose()
        {
            if (IsRegisteredToGateManager)
            {
                StargateManagerSystem.GetInstance(Api).UnregisterLoadedGate(Gate);
                IsRegisteredToGateManager = false;
            }

            // TODO: add to worldgatemanager to release when gate unregisters itself
            if (IsForceLoaded)
            {
                StargateManagerSystem.GetInstance(Api).ReleaseChunk(Gate.Pos);
                IsForceLoaded = false;
            }

            UnregisterTickListener();
        }

        public virtual void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessorForResolve)
        {
            if (tree.GetBool("hasAddress", false))
            {
                DialingAddress = new StargateAddress();
                DialingAddress.FromTreeAttributes(tree);
            }

            TimeOpen = tree.GetFloat("timeOpen", 0f);
            CurrentAngle = tree.GetFloat("currentAngle", 0f);
            CurrentGlyph = (byte)tree.GetInt("currentGlyph", 0);
            CurrentAddressIndex = (byte)tree.GetInt("currentAddressIndex", 0);
            ActiveChevrons = (byte)tree.GetInt("activeChevrons", 0);
            State = (EnumStargateState)tree.GetInt("gateState", 0);
            CurrentDialSpeed = (EnumDialSpeed)tree.GetInt("dialType", 0);
            RotateCW = tree.GetBool("rotateCW", false);
            UseQuickDial = tree.GetBool("quickdialMode", false);
        }

        public virtual void ForceDisconnect(bool notifyRemote = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the next angle of the inner ring given
        /// a time since the last gametick in seconds
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        protected float NextAngle(float delta)
        {
            byte targetGlyph = DialingAddress.AddressCoordinates.Glyphs[CurrentAddressIndex];
            PreviousAngle = CurrentAngle;

            if (CurrentGlyph != targetGlyph)
            {
                CurrentAngle += delta * RotationDegPerSecond * (RotateCW ? -1 : 1);
                if (CurrentAngle < 0) CurrentAngle += 360;
                else if (CurrentAngle >= 360) CurrentAngle %= 360;

                NextGlyph = (byte)(CurrentGlyph + (RotateCW ? -1 : 1));
                if (NextGlyph > 250) NextGlyph = (byte)(Gate.GlyphLength - 1);
                else if (NextGlyph >= Gate.GlyphLength) NextGlyph = 0;

                float nextAngle = (NextGlyph * GlyphAngle + 360f) % 360;
                float tPreviousAngle;
                float tCurrentAngle;
                float tNextAngle;

                if (RotateCW)
                {
                    tPreviousAngle = 360f;
                    tCurrentAngle = (CurrentAngle - PreviousAngle + 360) % 360;
                    tNextAngle = (nextAngle - PreviousAngle + 360) % 360;
                }
                else
                {
                    tPreviousAngle = 0f;
                    tCurrentAngle = (CurrentAngle + (360 - PreviousAngle)) % 360;
                    tNextAngle = (nextAngle + (360 - PreviousAngle)) % 360;
                }

                if ((tCurrentAngle >= tNextAngle && tNextAngle > tPreviousAngle) || (tCurrentAngle <= tNextAngle && tNextAngle < tPreviousAngle))
                {
                    CurrentGlyph = NextGlyph;

                    if (CurrentGlyph == targetGlyph)
                    {
                        CurrentAngle = NextGlyph * GlyphAngle;
                        OnGlyphReached();
                    }
                }
            }
            else
            {
                OnGlyphReached();
            }

            return CurrentAngle;
        }

        public virtual void ToTreeAttributes(ITreeAttribute tree)
        {
            if (DialingAddress != null)
            {
                DialingAddress.ToTreeAttributes(tree);
                tree.SetBool("hasAddress", true);
            }
            else
            {
                tree.SetBool("hasAddress", false);
            }

            tree.SetFloat("timeOpen", TimeOpen);
            tree.SetFloat("currentAngle", CurrentAngle);
            tree.SetInt("currentGlyph", CurrentGlyph);
            tree.SetInt("currentAddressIndex", CurrentAddressIndex);
            tree.SetInt("activeChevrons", ActiveChevrons);
            tree.SetInt("gateState", (int)State);
            tree.SetInt("dialType", (int)CurrentDialSpeed);
            tree.SetBool("rotateCW", RotateCW);
            tree.SetBool("quickdialMode", UseQuickDial);
        }

        public bool TryRegisterDelayedCallback(Action<float> onDelayedCallback, int millisecondInterval)
        {
            if (CallbackId != -1) return false;

            CallbackId = Gate.RegisterDelayedCallback((t) =>
            {
                onDelayedCallback(t);
                CallbackId = -1;
            }, millisecondInterval);
            return true;
        }

        public bool TryRegisterTickListener(Action<float> onTick, int millisecondInterval)
        {
            if (TickListenerId != -1) return false;

            TickListenerId = Gate.RegisterGameTickListener(onTick, millisecondInterval);
            return true;
        }

        public void UnregisterDelayedCallback()
        {
            if (CallbackId != -1)
            {
                Gate.UnregisterDelayedCallback(CallbackId);
                TickListenerId = -1;
            }
        }

        public void UnregisterTickListener()
        {
            if (TickListenerId != -1)
            {
                Gate.UnregisterGameTickListener(TickListenerId);
                TickListenerId = -1;
            }
        }

        public bool WillDialSucceed(IStargateAddress address)
        {
            if (address.AddressBits == Gate.Address.AddressBits) return false;
            int distanceChunks = Gate.Address.GetDistanceTo(address);
            if (distanceChunks < StargateConfig.Loaded.MinRangeChunksMilkyway) return false;
            if (distanceChunks > StargateConfig.Loaded.MaxRangeChunksMilkyway) return false;

            return true;
        }

        internal Vec3f GetTpOffsetStart()
        {
            return (Gate.Block.Shape.rotateY == 180 || Gate.Block.Shape.rotateY == 0) ? new(-2f, 0.5f, 0) : new(0, 0.5f, -2f);
        }

        internal Vec3f GetTpOffsetEnd()
        {
            return (Gate.Block.Shape.rotateY == 180 || Gate.Block.Shape.rotateY == 0) ? new(3f, 6f, 1f) : new(1f, 6f, 3f);
        }
    }
}
