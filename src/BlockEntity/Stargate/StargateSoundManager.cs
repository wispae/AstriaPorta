using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Content
{
    public enum EnumGateSoundLocation
    {
        Active,
        Break,
        Enter,
        Fail,
        Lock,
        Release,
        Rotate,
        RotateStart,
        Vortex,
        Warning
    }

    public class StargateSoundLocationConfiguration
    {
        [JsonProperty("activeSound")]
        public AssetLocation ActiveSoundLocation;
        [JsonProperty("breakSound")]
        public AssetLocation BreakSoundLocation;
        [JsonProperty("enterSound")]
        public AssetLocation EnterSoundLocation;
        [JsonProperty("failSound")]
        public AssetLocation FailSoundLocation;
        [JsonProperty("lockSound")]
        public AssetLocation LockSoundLocation;
        [JsonProperty("releaseSound")]
        public AssetLocation ReleaseSoundLocation;
        [JsonProperty("rotateSound")]
        public AssetLocation RotateSoundLocation;
        [JsonProperty("rotateStartSound")]
        public AssetLocation RotateStartSoundLocation;
        [JsonProperty("vortexSound")]
        public AssetLocation VortexSoundLocation;
        [JsonProperty("warningSound")]
        public AssetLocation WarningSoundLocation;

        [JsonProperty("vortexSoundDelay")]
        public int? VortexSoundDelay = 750;
    }

    public abstract class StargateSoundManagerBase
    {
        protected static readonly Dictionary<EnumStargateType, StargateSoundLocationConfiguration> SoundLocations = new();

        protected readonly ICoreAPI _api;
        protected readonly BlockPos _gatePos;
        protected readonly EnumStargateType _gateType;

        public int VortexSoundDelay => SoundLocations[_gateType].VortexSoundDelay ?? 750;

        public StargateSoundManagerBase(ICoreAPI api, EnumStargateType gateType, BlockPos gatePos)
        {
            _api = api;
            _gateType = gateType;
            _gatePos = gatePos;
        }

        /// <summary>
        /// Initializes the global sound locations for the given gate type
        /// </summary>
        /// <param name="gateType"></param>
        /// <param name="config"></param>
        public static void InitializeLocations(EnumStargateType gateType, StargateSoundLocationConfiguration config)
        {
            SoundLocations[gateType] = config;
        }

        public virtual void Play(EnumGateSoundLocation gateSound, float volume = 1f)
        {
            StargateSoundLocationConfiguration currentConfig = SoundLocations[_gateType];

            AssetLocation soundLocation = gateSound switch
            {
                EnumGateSoundLocation.Active => currentConfig.ActiveSoundLocation,
                EnumGateSoundLocation.Break => currentConfig.BreakSoundLocation,
                EnumGateSoundLocation.Enter => currentConfig.EnterSoundLocation,
                EnumGateSoundLocation.Fail => currentConfig.FailSoundLocation,
                EnumGateSoundLocation.Lock => currentConfig.LockSoundLocation,
                EnumGateSoundLocation.Release => currentConfig.ReleaseSoundLocation,
                EnumGateSoundLocation.Rotate => currentConfig.RotateSoundLocation,
                EnumGateSoundLocation.RotateStart => currentConfig.RotateStartSoundLocation,
                EnumGateSoundLocation.Warning => currentConfig.WarningSoundLocation,
                EnumGateSoundLocation.Vortex => currentConfig.VortexSoundLocation,
                _ => null
            };

            if (soundLocation == null) return;

            _api.World.PlaySoundAt(soundLocation, _gatePos, 1, null, false);
        }

        public abstract void Dispose();
        public abstract void PauseActiveSound();
        public abstract void PauseAllSounds();
        public abstract void PauseRotateSound();
        public abstract void StartActiveSound();
        public abstract void StartRotateSound();
        public abstract void StopActiveSound();
        public abstract void StopAllSounds();
        public abstract void StopRotateSound();
    }

    public class StargateSoundManagerServer : StargateSoundManagerBase
    {
        private readonly ICoreServerAPI _sapi;

        public StargateSoundManagerServer(ICoreServerAPI sapi, EnumStargateType gateType, BlockPos gatePos) : base(sapi, gateType, gatePos)
        {
            _sapi = sapi;
        }

        public override void Dispose()
        {
        }

        public override void PauseActiveSound()
        {
            throw new System.NotImplementedException();
        }

        public override void PauseAllSounds()
        {
            throw new System.NotImplementedException();
        }

        public override void PauseRotateSound()
        {
            throw new System.NotImplementedException();
        }

        public override void StartActiveSound()
        {
            throw new System.NotImplementedException();
        }

        public override void StartRotateSound()
        {
            throw new System.NotImplementedException();
        }

        public override void StopActiveSound()
        {
            throw new System.NotImplementedException();
        }

        public override void StopAllSounds()
        {
            throw new System.NotImplementedException();
        }

        public override void StopRotateSound()
        {
            throw new System.NotImplementedException();
        }
    }

    public class StargateSoundManagerClient : StargateSoundManagerBase
    {
        private readonly ICoreClientAPI _capi;

        private ILoadedSound _activeSound;
        private ILoadedSound _rotateSound;

        public StargateSoundManagerClient(ICoreClientAPI capi, EnumStargateType gateType, BlockPos gatePos) : base(capi, gateType, gatePos)
        {
            _capi = capi;
        }

        public override void Dispose()
        {
            StopAllSounds();
        }

        public override void PauseActiveSound()
        {
            if (_activeSound == null) return;
            _activeSound.Pause();
        }

        public override void PauseAllSounds()
        {
            PauseActiveSound();
            PauseRotateSound();
        }

        public override void PauseRotateSound()
        {
            if (_rotateSound == null) return;
            _rotateSound.Pause();
        }

        public override void StartActiveSound()
        {
            if (_activeSound == null)
            {
                _activeSound = _capi.World.LoadSound(new SoundParams()
                {
                    Location = SoundLocations[_gateType].ActiveSoundLocation,
                    ShouldLoop = true,
                    Position = _gatePos.ToVec3f().Add(0.5f, 2f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.05f
                });
                _activeSound.Start();
            }

            _activeSound.Start();
        }

        public override void StartRotateSound()
        {
            if (_rotateSound == null)
            {
                _rotateSound = _capi.World.LoadSound(new SoundParams()
                {
                    Location = SoundLocations[_gateType].RotateSoundLocation,
                    ShouldLoop = true,
                    Position = _gatePos.ToVec3f().Add(0.5f, 2f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f
                });
                _rotateSound.Start();
            }

            if (_rotateSound.IsPaused)
            {
                _rotateSound.Start();
            }
        }

        public override void StopActiveSound()
        {
            _activeSound?.Stop();
            _activeSound?.Dispose();
            _activeSound = null;
        }

        public override void StopAllSounds()
        {
            StopActiveSound();
            StopRotateSound();
        }

        public override void StopRotateSound()
        {
            _rotateSound?.Stop();
            _rotateSound?.Dispose();
            _rotateSound = null;
        }
    }
}
