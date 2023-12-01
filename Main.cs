using System.Collections.Generic;
using System;
using OVRToolkit.Modules;
using System.Linq;
using System.Net;
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

        Dictionary<string, object> contents = new Dictionary<string, object>();
        Dictionary<DeviceType, DevicePacket> packets = new Dictionary<DeviceType, DevicePacket>();
        private static Company company = Company.Unknown;

        public override void Start()
        {
            InitModule("Headset Battery Info", null);

            DeviceIcons.Init();
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

            SetContents(contents.Values.ToArray());
            ListenForData();
        }

        private async void ListenForData()
        {
            while (true)
            {
                var data = await udp.ReceiveAsync();

                if (data != null && data.Buffer.Length > 0)
                {
                    var packet = GetPacketFromBytes(data.Buffer);

                    packets[packet.device] = packet;
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
                contents.Clear();

                foreach (var kv in packets)
                {
                    var packet = kv.Value;

                    company = packet.company;
                    DrawDevice(packet.device, packet.batteryLevel, packet.isCharging);
                }

                SetContents(contents.Values.ToArray());
            }
        }

        private void DrawDevice(DeviceType device, int batteryLevel, bool isCharging)
        {
            contents.Add("HBI_DeviceBtn" + device, new Button("" + device, DeviceIcons.GetDeviceIcon(device, company, isCharging), new Action(() =>
            {

            })));

            contents.Add("HBI_DeviceText" + device, new Header(batteryLevel + "%"));
        }
    }
}
