using AstriaPorta.Content;
using AstriaPorta.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Config
{
    [ProtoContract]
    public class StargateConfig
    {
        [JsonIgnore]
        private int _minRangeChunksMilkyway = 10;
        [JsonIgnore]
        private int _minRangeChunksPegasus = 10;
        [JsonIgnore]
        private int _maxRangeChunksMilkyway = 262144;
        [JsonIgnore]
        private int _maxRangeChunksPegasus = 262144;
        [JsonIgnore]
        private float _maxConnectionDurationSecondsMilkyway = 60f;
        [JsonIgnore]
        private float _maxConnectionDurationSecondsPegasus = 60f;
        [JsonIgnore]
        private float _dialSpeedDegreesPerSecondMilkyway = 80f;
        [JsonIgnore]
        private float _dialSpeedDegreesPerSecondPegasus = 120f;
        [JsonIgnore]
        private int _minDistanceSurfaceGates = 1000;
        [JsonIgnore]
        private int _minDistanceUndergroundGates = 1000;

        [JsonIgnore]
        private string[] _supportedLoggingLevels = [
            "None",
            "Low",
            "Medium",
            "High"
        ];

        public int GetMinRangeChunks(EnumStargateType type)
        {
            switch (type)
            {
                case EnumStargateType.Milkyway:
                    return _minRangeChunksMilkyway;
                case EnumStargateType.Pegasus:
                    return _minRangeChunksPegasus;
                default:
                    return _minRangeChunksMilkyway;
            }
        }

        public int GetMaxRangeChunks(EnumStargateType type)
        {
            switch (type)
            {
                case EnumStargateType.Milkyway:
                    return _maxRangeChunksMilkyway;
                case EnumStargateType.Pegasus:
                    return _maxRangeChunksPegasus;
                default:
                    return _maxRangeChunksMilkyway;
            }
        }

        public float GetMaxConnectionDuration(EnumStargateType type)
        {
            switch (type)
            {
                case EnumStargateType.Milkyway:
                    return _maxConnectionDurationSecondsMilkyway;
                case EnumStargateType.Pegasus:
                    return _maxConnectionDurationSecondsPegasus;
                default:
                    return _maxConnectionDurationSecondsMilkyway;
            }
        }

        public float GetDialSpeedDegreesPerSecond(EnumStargateType type)
        {
            switch (type)
            {
                case EnumStargateType.Milkyway:
                    return _dialSpeedDegreesPerSecondMilkyway;
                case EnumStargateType.Pegasus:
                    return _dialSpeedDegreesPerSecondPegasus;
                default:
                    return _dialSpeedDegreesPerSecondMilkyway;
            }
        }

        public static StargateConfig Loaded { get; set; } = new StargateConfig();

        [ProtoMember(1), DefaultValue(true)]
        public bool DhdDestructable { get; set; } = true;
        [ProtoMember(2), DefaultValue(4)]
        public int DhdMiningTier { get; set; } = 4;
        [ProtoMember(3), DefaultValue(true)]
        public bool StargateDestructable { get; set; } = true;
        [ProtoMember(4), DefaultValue(5)]
        public int StargateMiningTier { get; set; } = 5;
        [ProtoMember(5), DefaultValue(true)]
        public bool VortexDestroys { get; set; } = true;
        [ProtoMember(6), DefaultValue(true)]
        public bool VortexKills { get; set; } = true;
        [ProtoMember(7), DefaultValue(5f)]
        public float MaxTimeoutSeconds { get; set; } = 5f;
        [ProtoMember(8), DefaultValue(10)]
        public int MinRangeChunksMilkyway
        {
            get => _minRangeChunksMilkyway;
            set
            {
                if (value > _maxRangeChunksMilkyway) value = _maxRangeChunksMilkyway - 1;
                if (value < 0) value = 0;
                if (value > 262144) value = 262144;
                _minRangeChunksMilkyway = value;
            }
        }
        [ProtoMember(9), DefaultValue(10)]
        public int MinRangeChunksPegasus
        {
            get => _minRangeChunksPegasus;
            set
            {
                if (value < _minRangeChunksPegasus) value = _minRangeChunksPegasus - 1;
                if (value < 0) value = 0;
                if (value > 262144) value = 262144;
                _maxRangeChunksPegasus = value;
            }
        }
        [ProtoMember(10), DefaultValue(262144)]
        public int MaxRangeChunksMilkyway
        {
            get => _maxRangeChunksMilkyway;
            set
            {
                if (value < _minRangeChunksMilkyway) value = _minRangeChunksMilkyway + 1;
                if (value < 0) value = 0;
                if (value > 262144) value = 262144;
                _maxRangeChunksMilkyway = value;
            }
        }
        [ProtoMember(11), DefaultValue(262144)]
        public int MaxRangeChunksPegasus
        {
            get => _maxRangeChunksPegasus;
            set
            {
                if (value < _minRangeChunksPegasus) value = _minRangeChunksPegasus + 1;
                if (value < 0) value = 0;
                if (value > 262144) value = 262144;
                _maxRangeChunksPegasus = value;
            }
        }
        [ProtoMember(12), DefaultValue(60f)]
        public float MaxConnectionDurationSecondsMilkyway
        {
            get => _maxConnectionDurationSecondsMilkyway;
            set
            {
                if (value < 10f) value = 10f;
                if (value > 180f) value = 180f;
                _maxConnectionDurationSecondsMilkyway = value;
            }
        }
        [ProtoMember(13), DefaultValue(60f)]
        public float MaxConnectionDurationSecondsPegasus
        {
            get => _maxConnectionDurationSecondsPegasus;
            set
            {
                if (value < 10f) value = 10f;
                if (value > 180f) value = 180f;
                _maxConnectionDurationSecondsPegasus = value;
            }
        }
        [ProtoMember(14), DefaultValue(80f)]
        public float DialSpeedDegreesPerSecondMilkyway
        {
            get => _dialSpeedDegreesPerSecondMilkyway;
            set
            {
                if (value < 20f) value = 20f;
                if (value > 180f) value = 180f;
                _dialSpeedDegreesPerSecondMilkyway = value;
            }
        }
        [ProtoMember(15), DefaultValue(120f)]
        public float DialSpeedDegreesPerSecondPegasus
        {
            get => _dialSpeedDegreesPerSecondPegasus;
            set
            {
                if (value < 20f) value = 20f;
                if (value > 180f) value = 180f;
                _dialSpeedDegreesPerSecondPegasus = value;
            }
        }
        [ProtoMember(16), DefaultValue(true)]
        public bool AllowQuickDial { get; set; } = true;

        [ProtoMember(17)]
        public int MinDistanceSurfaceGates
        {
            get => _minDistanceSurfaceGates;
            set
            {
                if (value < 100) value = 100;
                _minDistanceSurfaceGates = value;
            }
        }

        [ProtoMember(18)]
        public int MinDistanceUndergroundGates
        {
            get => _minDistanceUndergroundGates;
            set
            {
                if (value < 100) value = 100;
                _minDistanceUndergroundGates = value;
            }
        }

        [ProtoMember(19), DefaultValue(true)]
        public bool EnableWorldGenGates { get; set; } = true;

        [ProtoMember(20), DefaultValue(true)]
        public bool EnableCartoucheGates { get; set; } = true;

        [ProtoMember(21), JsonConverter(typeof(StringEnumConverter))]
        public LogLevel DebugLogLevel { get; set; } = LogLevel.None;

        [ProtoMember(22)]
        public string[] AvailableLoggingLevels
        {
            get
            {
                return _supportedLoggingLevels;
            }
            set { }
        }
    }
}
