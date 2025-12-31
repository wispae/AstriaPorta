using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
    public enum EnumGateSoundLocation
    {
        Active,
        Break,
        Lock,
        Release,
        Rotate,
        Vortex,
        Warning
    }

    public class StargateSoundLocationConfiguration
    {
        [JsonProperty("activeSound")]
        public AssetLocation ActiveSoundLocation;
        [JsonProperty("breakSound")]
        public AssetLocation BreakSoundLocation;
        [JsonProperty("lockSound")]
        public AssetLocation LockSoundLocation;
        [JsonProperty("releaseSound")]
        public AssetLocation ReleaseSoundLocation;
        [JsonProperty("rotateSound")]
        public AssetLocation RotateSoundLocation;
        [JsonProperty("vortexSound")]
        public AssetLocation VortexSoundLocation;
        [JsonProperty("warningSound")]
        public AssetLocation WarningSoundLocation;
    }

    public class StargateSoundManager
    {
        protected static readonly Dictionary<EnumStargateType, StargateSoundLocationConfiguration> SoundLocations = new();

        private readonly ICoreClientAPI _capi;
        private readonly BlockPos _gatePos;
        private readonly EnumStargateType _gateType;

        private ILoadedSound _activeSound;
        private ILoadedSound _rotateSound;

        public StargateSoundManager(ICoreClientAPI capi, EnumStargateType gateType, BlockPos gatePos)
        {
            _capi = capi;
            _gateType = gateType;
            _gatePos = gatePos;
        }

        public virtual void Dispose()
        {
            StopAllSounds();
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

        public void PauseActiveSound()
        {
            if (_activeSound == null) return;
            _activeSound.Pause();
        }

        public void PauseAllSounds()
        {
            PauseActiveSound();
            PauseRotateSound();
        }

        public void PauseRotateSound()
        {
            if (_rotateSound == null) return;
            _rotateSound.Pause();
        }

        public virtual void Play(EnumGateSoundLocation gateSound)
        {
            StargateSoundLocationConfiguration currentConfig = SoundLocations[_gateType];

            AssetLocation soundLocation = gateSound switch
            {
                EnumGateSoundLocation.Active => currentConfig.ActiveSoundLocation,
                EnumGateSoundLocation.Break => currentConfig.BreakSoundLocation,
                EnumGateSoundLocation.Lock => currentConfig.LockSoundLocation,
                EnumGateSoundLocation.Release => currentConfig.ReleaseSoundLocation,
                EnumGateSoundLocation.Rotate => currentConfig.RotateSoundLocation,
                EnumGateSoundLocation.Warning => currentConfig.WarningSoundLocation,
                EnumGateSoundLocation.Vortex => currentConfig.VortexSoundLocation,
                _ => null
            };

            if (soundLocation == null) return;

            _capi.World.PlaySoundAt(soundLocation, _gatePos, 1, null, false);
        }

        public void StartActiveSound()
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

        public void StartRotateSound()
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

        public void StopActiveSound()
        {
            _activeSound?.Stop();
            _activeSound?.Dispose();
            _activeSound = null;
        }

        public void StopAllSounds()
        {
            StopActiveSound();
            StopRotateSound();
        }

        public void StopRotateSound()
        {
            _rotateSound?.Stop();
            _rotateSound?.Dispose();
            _rotateSound = null;
        }
    }
}
