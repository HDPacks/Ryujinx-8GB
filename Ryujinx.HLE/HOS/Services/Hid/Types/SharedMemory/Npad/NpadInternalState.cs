﻿using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Common;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Npad
{
    struct NpadInternalState
    {
        public NpadStyleTag StyleSet;
        public NpadJoyAssignmentMode JoyAssignmentMode;
        public NpadFullKeyColorState FullKeyColor;
        public NpadJoyColorState JoyColor;
        public RingLifo<NpadCommonState> FullKey;
        public RingLifo<NpadCommonState> Handheld;
        public RingLifo<NpadCommonState> JoyDual;
        public RingLifo<NpadCommonState> JoyLeft;
        public RingLifo<NpadCommonState> JoyRight;
        public RingLifo<NpadCommonState> Palma;
        public RingLifo<NpadCommonState> SystemExt;
        public RingLifo<SixAxisSensorState> FullKeySixAxisSensor;
        public RingLifo<SixAxisSensorState> HandheldSixAxisSensor;
        public RingLifo<SixAxisSensorState> JoyDualSixAxisSensor;
        public RingLifo<SixAxisSensorState> JoyDualRightSixAxisSensor;
        public RingLifo<SixAxisSensorState> JoyLeftSixAxisSensor;
        public RingLifo<SixAxisSensorState> JoyRightSixAxisSensor;
        public DeviceType DeviceType;
        private uint _reserved1;
        public NpadSystemProperties SystemProperties;
        public NpadSystemButtonProperties SystemButtonProperties;
        public NpadBatteryLevel BatteryLevelJoyDual;
        public NpadBatteryLevel BatteryLevelJoyLeft;
        public NpadBatteryLevel BatteryLevelJoyRight;
        public uint AppletFooterUiAttributes;
        public AppletFooterUiType AppletFooterUiType;
        private Reserved2Struct _reserved2;
        public RingLifo<NpadGcTriggerState> GcTrigger;
        public NpadLarkType LarkTypeLeftAndMain;
        public NpadLarkType LarkTypeRight;
        public NpadLuciaType LuciaType;
        public uint Unknown43EC;

        [StructLayout(LayoutKind.Sequential, Size = 123, Pack = 1)]
        private struct Reserved2Struct {}

        public static NpadInternalState Create()
        {
            return new NpadInternalState
            {
                FullKey = RingLifo<NpadCommonState>.Create(),
                Handheld = RingLifo<NpadCommonState>.Create(),
                JoyDual = RingLifo<NpadCommonState>.Create(),
                JoyLeft = RingLifo<NpadCommonState>.Create(),
                JoyRight = RingLifo<NpadCommonState>.Create(),
                Palma = RingLifo<NpadCommonState>.Create(),
                SystemExt = RingLifo<NpadCommonState>.Create(),
                FullKeySixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                HandheldSixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                JoyDualSixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                JoyDualRightSixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                JoyLeftSixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                JoyRightSixAxisSensor = RingLifo<SixAxisSensorState>.Create(),
                GcTrigger = RingLifo<NpadGcTriggerState>.Create(),
            };
        }
    }
}