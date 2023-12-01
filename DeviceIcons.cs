using System;
using System.Collections.Generic;
using System.IO;

namespace HeadsetBatteryInfo
{
    internal class DeviceIcons
    {
        public struct HeadsetIcons
        {
            public byte[] headset;
            public byte[] headsetCharging;

            public byte[] leftController;
            public byte[] rightController;
        }

        public static HeadsetIcons Unknown;
        public static HeadsetIcons Pico;
        public static HeadsetIcons Meta;

        private static Dictionary<Company, HeadsetIcons> deviceIcons = new Dictionary<Company, HeadsetIcons>();
        public static void Init()
        {
            InitDefault();
            InitPico();
            InitMeta();

            deviceIcons[Company.Unknown] = Unknown;
            deviceIcons[Company.Pico] = Pico;
            deviceIcons[Company.Meta] = Meta;
        }

        private static void InitPico()
        {
            Pico = new HeadsetIcons();

            Pico.headset = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/pico/headset.png");
            Pico.headsetCharging = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/pico/headset_charging.png");

            Pico.leftController = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/pico/left_controller.png");
            Pico.rightController = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/pico/right_controller.png");
        }

        private static void InitMeta()
        {
            Meta = new HeadsetIcons();

            Meta.headset = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/meta/headset.png");
            Meta.headsetCharging = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/meta/headset_charging.png");

            Meta.leftController = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/meta/left_controller.png");
            Meta.rightController = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/meta/right_controller.png");
        }

        private static void InitDefault()
        {
            Unknown = new HeadsetIcons();

            Unknown.headset = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/unknown/headset.png");
            Unknown.headsetCharging = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/unknown/headset_charging.png");
            Unknown.leftController = Unknown.rightController = File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/unknown/controller.png");
        }

        public static byte[] GetDeviceIcon(DeviceType device, Company company, bool isCharging = false)
        {
            HeadsetIcons icons;
            if (!deviceIcons.TryGetValue(company, out icons))
                icons = deviceIcons[Company.Unknown];

            switch (device)
            {
                case DeviceType.Headset:
                    return isCharging ? icons.headsetCharging : icons.headset;

                case DeviceType.ControllerLeft:
                    return icons.leftController;

                case DeviceType.ControllerRight:
                    return icons.rightController;

                default:
                    return icons.headset;
            }
        }
    }
}
