using System.Collections.Generic;
using System;
using OVRToolkit.Modules;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

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
            Print("Initializing HBI...");
            InitModule("Headset Battery Info", File.ReadAllBytes("Plugins/HeadsetBatteryInfo/Images/batteryIcon.png"));

            DeviceIcons.Init();

            SetupDeviceDefaults();
            SetupContents();

            InitUdp();
            ListenForData();

            RequestRefresh();

            Print("Initialized HBI");
        }

        private void SetupDeviceDefaults()
        {
            deviceData[DeviceType.Headset] = new DevicePacket
            {
                device = DeviceType.Headset,
                isCharging = false,
                batteryLevel = -1,
            };

            deviceData[DeviceType.ControllerLeft] = new DevicePacket
            {
                device = DeviceType.ControllerLeft,
                isCharging = false,
                batteryLevel = -1,
            };

            deviceData[DeviceType.ControllerRight] = new DevicePacket
            {
                device = DeviceType.ControllerRight,
                isCharging = false,
                batteryLevel = -1,
            };
        }

        private void SetupContents()
        {
            contents.Clear();

            SetContents(contents.Values.ToArray());
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
                Print("Failed to create UDP client for HBI!");
                SendNotification("Headset Battery Info", "Failed to create UDP client for HBI!");

                udpCreated = false;
            }
        }

        private void RequestRefresh()
        {
            var buffer = Encoding.ASCII.GetBytes("/hbi/requestUpdate");
            udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 28092));
        }

        private async void ListenForData()
        {
            while (true)
            {
                var data = await udp.ReceiveAsync();

                if (data != null && data.Buffer.Length > 0)
                {
                    if (data.Buffer[0] == 47)
                    {
                        ProceedNotificationPacket(data.Buffer);
                    }
                    else
                    {
                        ProceedBatteryInfo(data.Buffer);
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private void ProceedNotificationPacket(byte[] buffer)
        {
            string msg = "";
            bool readMsg = false;
            for (int i = 1; i < buffer.Length; i++)
            {
                char c = (char)buffer[i];

                if (c == 47)
                {
                    readMsg = true;
                }
                else if (readMsg)
                {
                    msg += c;
                }
            }

            SendNotification("Headset Battery Info", msg);
        }

        private void ProceedBatteryInfo(byte[] buffer)
        {
            var packet = GetPacketFromBytes(buffer);

            lock (this)
            {
                if (deviceData[packet.device].batteryLevel == -1)
                {
                    var icon = DeviceIcons.GetDeviceIcon(packet.device, packet.company, false);
                    AddCustomBattery($"HBI_{packet.device}", packet.batteryLevel, DeviceBatteryStates.Connected, DeviceBatteryIcons.Custom, icon);
                }

                deviceData[packet.device] = packet;
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
                    if (packet.batteryLevel == -1)
                        continue;

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
