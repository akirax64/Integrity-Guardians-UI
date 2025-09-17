using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Text;
using static IGAntiRansomwareUI.Ioctl;

namespace IGAntiRansomwareUI
{
    public static class DriverCommunication
    {
        // const para comunicacao driver / aplicacao
        public const string devicePath = "\\\\.\\IGAntiRansomware";

        // Struct para configuração de monitoramento
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORING_CONFIG
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool EnableMonitoring;
            public uint Mode;
            [MarshalAs(UnmanagedType.I1)]
            public bool BackupOnDetection;
        }

        // Constantes para timeout
        private const int IOCTL_TIMEOUT_MS = 10000; // 10 segundos

        // P/Invoke para funções da API do Windows.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            SafeFileHandle device,
            uint ioctl,
            IntPtr inputBuffer,
            uint inputBufferSize,
            IntPtr outputBuffer,
            uint outputBufferSize,
            out uint bytesReturned,
            IntPtr overlapped);

        // Constantes para CreateFile.
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_SHARE_READ = 1;
        public const uint FILE_SHARE_WRITE = 2;

        public static MONITORING_CONFIG GetDriverStatus(SafeFileHandle driverHandle)
        {
            if (driverHandle.IsInvalid)
            {
                throw new InvalidOperationException("O handle do driver é inválido.");
            }

            uint bytesReturned;
            MONITORING_CONFIG driverStatus = new MONITORING_CONFIG();
            IntPtr outputBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(driverStatus));

            try
            {
                bool success = DeviceIoControl(
                    driverHandle,
                    Ioctl.IOCTL_STATUS,
                    IntPtr.Zero,
                    0,
                    outputBuffer,
                    (uint)Marshal.SizeOf(driverStatus),
                    out bytesReturned,
                    IntPtr.Zero);

                if (!success)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                driverStatus = Marshal.PtrToStructure<MONITORING_CONFIG>(outputBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outputBuffer);
            }

            return driverStatus;
        }

        public static void SetDriverMonitoring(SafeFileHandle driverHandle, bool enable, uint mode, bool backupOnDetection)
        {
            if (driverHandle.IsInvalid)
            {
                throw new InvalidOperationException("O handle do driver é inválido.");
            }

            MONITORING_CONFIG config = new MONITORING_CONFIG
            {
                EnableMonitoring = enable,
                Mode = mode,
                BackupOnDetection = backupOnDetection
            };

            IntPtr inputBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(config));
            Marshal.StructureToPtr(config, inputBuffer, false);

            System.Diagnostics.Debug.WriteLine($"Enviando IOCTL_CONFIGURE_MONITORING: 0x{IOCTL_CONFIGURE_MONITORING:X8}");

            uint bytesReturned;
            bool success;

            try
            {
                success = DeviceIoControl(
                    driverHandle,
                    Ioctl.IOCTL_CONFIGURE_MONITORING,
                    inputBuffer,
                    (uint)Marshal.SizeOf(config),
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeHGlobal(inputBuffer);
            }

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Falha na chamada DeviceIoControl para IOCTL_CONFIGURE_MONITORING. Código de erro: {error}");
            }
        }

        public static void LoadRules(SafeFileHandle fileHandle, RuleManager ruleManager)
        {
            if (fileHandle.IsInvalid)
            {
                throw new InvalidOperationException("O handle do driver é inválido.");
            }
            byte[] rulesData = ruleManager.SerializeRulesForDriver();
            // VALIDAÇÃO EXTRA: Verifica se os dados não são nulos ou vazios
            if (rulesData == null || rulesData.Length == 0)
            {
                throw new ArgumentException("Dados das regras não podem ser nulos ou vazios.");
            }

            // VALIDAÇÃO EXTRA: Verifica tamanho mínimo do header
            if (rulesData.Length < 4) // Pelo menos o RULES_DATA_HEADER
            {
                throw new ArgumentException("Dados das regras muito pequenos para conter cabeçalho.");
            }

            uint bytesReturned;
            bool success;

            IntPtr inputBuffer = Marshal.AllocHGlobal(rulesData.Length);
            try
            {
                Marshal.Copy(rulesData, 0, inputBuffer, rulesData.Length);

                success = DeviceIoControl(
                    fileHandle,
                    Ioctl.IOCTL_LOAD_RULES,
                    inputBuffer,
                    (uint)rulesData.Length,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeHGlobal(inputBuffer);
            }

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Falha na chamada DeviceIoControl para IOCTL_LOAD_RULES. Código de erro: {error}");
            }
        }
    }
 }