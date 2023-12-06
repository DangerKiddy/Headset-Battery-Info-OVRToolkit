using System.Collections.Generic;
using System;
using OVRToolkit.Modules;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace HeadsetBatteryInfo
{
    public enum Company
    {
        Unknown = -1,

        Pico,
        Meta
    }
    public enum DeviceType
    {
        Headset,
        ControllerLeft,
        ControllerRight,
    }
    public struct DevicePacket
    {
        public DeviceType device;
        public bool isCharging;
        public int batteryLevel;
        public Company company;
    }

    public class OVRToolkitModule : Module
    {

        private const int port = 28093;

        private UdpClient udp;
        private bool udpCreated = false;

        private Dictionary<string, object> contents = new Dictionary<string, object>();
        private static Dictionary<DeviceType, DevicePacket> deviceData = new Dictionary<DeviceType, DevicePacket>();

        public override void Start()
        {
            InitModule("Headset Battery Info", File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/batteryIcon.png"));

            DeviceIcons.Init();

            SetupDeviceDefaults();
            InitUdp();
            ListenForData();

            AddCustomBattery($"HBI_{DeviceType.Headset}", 1f, DeviceBatteryStates.Disconnected, DeviceBatteryIcons.HMD, null);
            AddCustomBattery($"HBI_{DeviceType.ControllerLeft}", 1f, DeviceBatteryStates.Disconnected, DeviceBatteryIcons.LeftController, null);
            AddCustomBattery($"HBI_{DeviceType.ControllerRight}", 1f, DeviceBatteryStates.Disconnected, DeviceBatteryIcons.RightController, null);
            //SetContents(contents.Values.ToArray());
        }

        private void SetupDeviceDefaults()
        {
            deviceData[DeviceType.Headset] = new DevicePacket
            {
                device = DeviceType.Headset,
                isCharging = false,
                batteryLevel = 0,
            };

            deviceData[DeviceType.ControllerLeft] = new DevicePacket
            {
                device = DeviceType.ControllerLeft,
                isCharging = false,
                batteryLevel = 0,
            };

            deviceData[DeviceType.ControllerRight] = new DevicePacket
            {
                device = DeviceType.ControllerRight,
                isCharging = false,
                batteryLevel = 0,
            };
        }

        private void InitUdp()
        {
            try
            {
                if (udp == null)
                    udp = new UdpClient(port);

                udpCreated = true;
            }
            catch
            {
                contents.Add("HBI_Error", new Header("Failed to create listen socket!"));

                udpCreated = false;
            }
        }

        private async void ListenForData()
        {
            while (true)
            {
                var data = await udp.ReceiveAsync();

                if (data != null && data.Buffer.Length > 0)
                {
                    var packet = GetPacketFromBytes(data.Buffer);

                    lock (this)
                    {
                        deviceData[packet.device] = packet;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private DevicePacket GetPacketFromBytes(byte[] bytes)
        {
            DevicePacket str = new DevicePacket();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(bytes, 0, ptr, size);

                str = (DevicePacket)Marshal.PtrToStructure(ptr, str.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return str;
        }

        public override void Update()
        {
            if (!udpCreated)
                return;

            lock(this)
            {
                foreach (var kv in deviceData)
                {
                    var packet = kv.Value;

                    string key = $"HBI_{packet.device}";

                    float percent = packet.batteryLevel / 100f;

                    bool isConnected = packet.batteryLevel > 0;
                    if (isConnected)
                        UpdateCustomBattery(key, percent, packet.isCharging ? DeviceBatteryStates.Charging : DeviceBatteryStates.Connected);
                    else
                        UpdateCustomBattery(key, percent, DeviceBatteryStates.Disconnected);
                }
            }
        }
    }
}
