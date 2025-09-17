using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGAntiRansomwareUI
{
    internal class Ioctl
    {
        private static readonly uint FILE_ANY_ACCESS = 0;
        private static readonly uint FILE_READ_ACCESS = 1;
        private static readonly uint METHOD_BUFFERED = 0;
        private static readonly uint FILE_DEVICE_UNKNOWN = 0x800;

        public static uint CTL_CODE(
            uint deviceType,
            uint function,
            uint method,
            uint access)
        {
            return ((deviceType) << 16) | ((access) << 14) | ((function) << 2) | (method);
        }

        // Definições dos IOCTLs
        public static readonly uint IOCTL_LOAD_RULES = CTL_CODE(
            FILE_DEVICE_UNKNOWN,
            0x800,
            METHOD_BUFFERED,
            FILE_ANY_ACCESS);

        public static readonly uint IOCTL_GET_ALERT = CTL_CODE(
            FILE_DEVICE_UNKNOWN,
            0x801,
            METHOD_BUFFERED,
            FILE_READ_ACCESS);

        public static readonly uint IOCTL_CONFIGURE_MONITORING = CTL_CODE(
            FILE_DEVICE_UNKNOWN,
            0x802,
            METHOD_BUFFERED,
            FILE_ANY_ACCESS
            );
        
        public static readonly uint IOCTL_STATUS = CTL_CODE(
            FILE_DEVICE_UNKNOWN, 
            0x803, 
            METHOD_BUFFERED, 
            FILE_READ_ACCESS);
    }
}
