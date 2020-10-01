using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Iot.Device.Ft4222
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DeviceListInfirmation
    {
        public FtFlag Flags;
        public FtDevice Type;
        public uint ID;
        public uint LocId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] SerialNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Description;
        public IntPtr ftHandle;
    }
}
