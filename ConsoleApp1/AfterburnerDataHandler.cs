using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AfterburnerDataHandler.SharedMemory.RivaTunerStatisticsServer
{
    public class RTSSSM
    {
        public string MapName = "RTSSSharedMemoryV2";
        public RTSS_SHARED_MEMORY Header;
        public List<RTSS_SHARED_MEMORY_OSD_ENTRY> OSDEntries = new List<RTSS_SHARED_MEMORY_OSD_ENTRY>();
        public List<RTSS_SHARED_MEMORY_APP_ENTRY> APPEntries = new List<RTSS_SHARED_MEMORY_APP_ENTRY>();

        private MemoryMappedFile rtssMappedFile;
        private MemoryMappedViewStream rtssStream;
        private bool serverState = false;

        public bool Start()
        {
            if (serverState)
                Stop();

            try
            {
                // Open the shared memory region created by RTSS/MSI Afterburner.
                serverState = true;
                rtssMappedFile = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.ReadWrite);
                rtssStream = rtssMappedFile.CreateViewStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening RTSS memory: " + ex.Message);
                serverState = false;
            }
            return serverState;
        }

        public RTSSSM Update()
        {
            if (!serverState)
                Start();

            Header = new RTSS_SHARED_MEMORY();

            if (OSDEntries == null)
                OSDEntries = new List<RTSS_SHARED_MEMORY_OSD_ENTRY>();
            else
                OSDEntries.Clear();

            if (APPEntries == null)
                APPEntries = new List<RTSS_SHARED_MEMORY_APP_ENTRY>();
            else
                APPEntries.Clear();

            try
            {
                int headerBufferSize, osdEntryBufferSize, appEntryBufferSize;
                unsafe
                {
                    headerBufferSize = sizeof(RTSS_SHARED_MEMORY_UNSAFE);
                    osdEntryBufferSize = sizeof(RTSS_SHARED_MEMORY_OSD_ENTRY_UNSAFE);
                    appEntryBufferSize = sizeof(RTSS_SHARED_MEMORY_APP_ENTRY_UNSAFE);
                }

                var headerBuffer = new byte[headerBufferSize];
                var osdEntryBuffer = new byte[osdEntryBufferSize];
                var appEntryBuffer = new byte[appEntryBufferSize];

                if (rtssStream.CanRead && rtssStream.Capacity >= headerBufferSize)
                {
                    unsafe
                    {
                        fixed (void* bufferPointer = headerBuffer)
                        {
                            rtssStream.Position = 0;
                            rtssStream.Read(headerBuffer, 0, headerBufferSize);
                            var headerPointer = (RTSS_SHARED_MEMORY_UNSAFE*)bufferPointer;

                            // Check signature (should be 'RTSS' in ASCII: 0x52545353)
                            if (headerPointer->dwSignature == 0x52545353)
                            {
                                Header = new RTSS_SHARED_MEMORY
                                {
                                    dwSignature = headerPointer->dwSignature,
                                    dwVersion = headerPointer->dwVersion,
                                    dwAppEntrySize = headerPointer->dwAppEntrySize,
                                    dwAppArrOffset = headerPointer->dwAppArrOffset,
                                    dwAppArrSize = headerPointer->dwAppArrSize,
                                    dwOSDEntrySize = headerPointer->dwOSDEntrySize,
                                    dwOSDArrOffset = headerPointer->dwOSDArrOffset,
                                    dwOSDArrSize = headerPointer->dwOSDArrSize,
                                    dwOSDFrame = headerPointer->dwOSDFrame
                                };
                            }
                        }

                        // Read OSD entries.
                        if (rtssStream.Capacity > Header.dwOSDArrOffset + osdEntryBufferSize * Header.dwOSDArrSize)
                        {
                            fixed (void* osdBufferPtr = osdEntryBuffer)
                            {
                                for (int i = 0; i < Header.dwOSDArrSize; i++)
                                {
                                    rtssStream.Position = Header.dwOSDArrOffset + Header.dwOSDEntrySize * i;
                                    rtssStream.Read(osdEntryBuffer, 0, osdEntryBufferSize);
                                    var osdPointer = (RTSS_SHARED_MEMORY_OSD_ENTRY_UNSAFE*)osdBufferPtr;
                                    OSDEntries.Add(new RTSS_SHARED_MEMORY_OSD_ENTRY
                                    {
                                        szOSD = new string(osdPointer->szOSD, 0, 256, Encoding.Default).Trim('\0'),
                                        szOSDOwner = new string(osdPointer->szOSDOwner, 0, 256, Encoding.Default).Trim('\0')
                                    });
                                }
                            }
                        }

                        // Read application entries.
                        if (rtssStream.Capacity > Header.dwAppArrOffset + appEntryBufferSize * Header.dwAppArrSize)
                        {
                            fixed (void* appBufferPtr = appEntryBuffer)
                            {
                                for (int i = 0; i < Header.dwAppArrSize; i++)
                                {
                                    rtssStream.Position = Header.dwAppArrOffset + Header.dwAppEntrySize * i;
                                    rtssStream.Read(appEntryBuffer, 0, appEntryBufferSize);
                                    var appPointer = (RTSS_SHARED_MEMORY_APP_ENTRY_UNSAFE*)appBufferPtr;
                                    APPEntries.Add(new RTSS_SHARED_MEMORY_APP_ENTRY
                                    {
                                        dwProcessID = appPointer->dwProcessID,
                                        szName = new string(appPointer->szName, 0, 260, Encoding.Default).Trim('\0'),
                                        dwFlags = appPointer->dwFlags,
                                        dwTime0 = appPointer->dwTime0,
                                        dwTime1 = appPointer->dwTime1,
                                        dwFrames = appPointer->dwFrames,
                                        dwFrameTime = appPointer->dwFrameTime,
                                        dwStatFlags = appPointer->dwStatFlags,
                                        dwStatTime0 = appPointer->dwStatTime0,
                                        dwStatTime1 = appPointer->dwStatTime1,
                                        dwStatFrames = appPointer->dwStatFrames,
                                        dwStatCount = appPointer->dwStatCount,
                                        dwStatFramerateMin = appPointer->dwStatFramerateMin,
                                        dwStatFramerateAvg = appPointer->dwStatFramerateAvg,
                                        dwStatFramerateMax = appPointer->dwStatFramerateMax,
                                        dwOSDX = appPointer->dwOSDX,
                                        dwOSDY = appPointer->dwOSDY,
                                        dwOSDPixel = appPointer->dwOSDPixel,
                                        dwOSDColor = appPointer->dwOSDColor,
                                        dwOSDFrame = appPointer->dwOSDFrame,
                                        dwScreenCaptureFlags = appPointer->dwScreenCaptureFlags,
                                        szScreenCapturePath = new string(appPointer->szScreenCapturePath, 0, 260, Encoding.Default).Trim('\0')
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading RTSS memory: " + ex.Message);
            }

            return this;
        }

        public RTSSSM UpdateOnce()
        {
            Update();
            Stop();
            return this;
        }

        public void Stop()
        {
            try
            {
                serverState = false;
                rtssStream?.Dispose();
                rtssMappedFile?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error stopping RTSSSM: " + ex.Message);
            }
        }

        // Example: Static helper methods to get a specific OSD or App entry.
        public static RTSS_SHARED_MEMORY_OSD_ENTRY GetOSDEntry(uint osdID, string mapName = "RTSSSharedMemoryV2")
        {
            RTSS_SHARED_MEMORY header = new RTSS_SHARED_MEMORY();
            RTSS_SHARED_MEMORY_OSD_ENTRY osd = new RTSS_SHARED_MEMORY_OSD_ENTRY();

            try
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite))
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    int headerBufferSize, osdEntryBufferSize;
                    unsafe
                    {
                        headerBufferSize = sizeof(RTSS_SHARED_MEMORY_UNSAFE);
                        osdEntryBufferSize = sizeof(RTSS_SHARED_MEMORY_OSD_ENTRY_UNSAFE);
                    }

                    var headerBuffer = new byte[headerBufferSize];
                    var osdEntryBuffer = new byte[osdEntryBufferSize];

                    unsafe
                    {
                        fixed (void* bufferPointer = headerBuffer)
                        {
                            stream.Position = 0;
                            stream.Read(headerBuffer, 0, headerBufferSize);
                            var headerPointer = (RTSS_SHARED_MEMORY_UNSAFE*)bufferPointer;
                            if (headerPointer->dwSignature == 0x52545353)
                            {
                                header = new RTSS_SHARED_MEMORY
                                {
                                    dwSignature = headerPointer->dwSignature,
                                    dwVersion = headerPointer->dwVersion,
                                    dwAppEntrySize = headerPointer->dwAppEntrySize,
                                    dwAppArrOffset = headerPointer->dwAppArrOffset,
                                    dwAppArrSize = headerPointer->dwAppArrSize,
                                    dwOSDEntrySize = headerPointer->dwOSDEntrySize,
                                    dwOSDArrOffset = headerPointer->dwOSDArrOffset,
                                    dwOSDArrSize = headerPointer->dwOSDArrSize,
                                    dwOSDFrame = headerPointer->dwOSDFrame
                                };
                            }
                        }

                        int entrySize = (int)header.dwOSDEntrySize;
                        int osdOffset = (int)(header.dwOSDArrOffset + entrySize * osdID);

                        if (stream.Capacity > osdOffset + entrySize)
                        {
                            fixed (void* bufferPointer = osdEntryBuffer)
                            {
                                stream.Position = osdOffset;
                                stream.Read(osdEntryBuffer, 0, osdEntryBufferSize);
                                var osdPointer = (RTSS_SHARED_MEMORY_OSD_ENTRY_UNSAFE*)bufferPointer;
                                osd = new RTSS_SHARED_MEMORY_OSD_ENTRY
                                {
                                    szOSD = new string(osdPointer->szOSD, 0, 256, Encoding.Default).Trim('\0'),
                                    szOSDOwner = new string(osdPointer->szOSDOwner, 0, 256, Encoding.Default).Trim('\0')
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting OSD entry: " + ex.Message);
            }

            return osd;
        }

        public static RTSS_SHARED_MEMORY_APP_ENTRY GetAppEntry(uint appID, string mapName = "RTSSSharedMemoryV2")
        {
            RTSS_SHARED_MEMORY header = new RTSS_SHARED_MEMORY();
            RTSS_SHARED_MEMORY_APP_ENTRY app = new RTSS_SHARED_MEMORY_APP_ENTRY();

            try
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite))
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    int headerBufferSize, appEntryBufferSize;
                    unsafe
                    {
                        headerBufferSize = sizeof(RTSS_SHARED_MEMORY_UNSAFE);
                        appEntryBufferSize = sizeof(RTSS_SHARED_MEMORY_APP_ENTRY_UNSAFE);
                    }

                    var headerBuffer = new byte[headerBufferSize];
                    var appEntryBuffer = new byte[appEntryBufferSize];

                    unsafe
                    {
                        fixed (void* bufferPointer = headerBuffer)
                        {
                            stream.Position = 0;
                            stream.Read(headerBuffer, 0, headerBufferSize);
                            var headerPointer = (RTSS_SHARED_MEMORY_UNSAFE*)bufferPointer;
                            if (headerPointer->dwSignature == 0x52545353)
                            {
                                header = new RTSS_SHARED_MEMORY
                                {
                                    dwSignature = headerPointer->dwSignature,
                                    dwVersion = headerPointer->dwVersion,
                                    dwAppEntrySize = headerPointer->dwAppEntrySize,
                                    dwAppArrOffset = headerPointer->dwAppArrOffset,
                                    dwAppArrSize = headerPointer->dwAppArrSize,
                                    dwOSDEntrySize = headerPointer->dwOSDEntrySize,
                                    dwOSDArrOffset = headerPointer->dwOSDArrOffset,
                                    dwOSDArrSize = headerPointer->dwOSDArrSize,
                                    dwOSDFrame = headerPointer->dwOSDFrame
                                };
                            }
                        }

                        int entrySize = (int)header.dwAppEntrySize;
                        int appOffset = (int)(header.dwAppArrOffset + entrySize * appID);

                        if (stream.Capacity > appOffset + entrySize)
                        {
                            fixed (void* bufferPointer = appEntryBuffer)
                            {
                                stream.Position = appOffset;
                                stream.Read(appEntryBuffer, 0, appEntryBufferSize);
                                var appPointer = (RTSS_SHARED_MEMORY_APP_ENTRY_UNSAFE*)bufferPointer;
                                app = new RTSS_SHARED_MEMORY_APP_ENTRY
                                {
                                    dwProcessID = appPointer->dwProcessID,
                                    szName = new string(appPointer->szName, 0, 260, Encoding.Default).Trim('\0'),
                                    dwFlags = appPointer->dwFlags,
                                    dwTime0 = appPointer->dwTime0,
                                    dwTime1 = appPointer->dwTime1,
                                    dwFrames = appPointer->dwFrames,
                                    dwFrameTime = appPointer->dwFrameTime,
                                    dwStatFlags = appPointer->dwStatFlags,
                                    dwStatTime0 = appPointer->dwStatTime0,
                                    dwStatTime1 = appPointer->dwStatTime1,
                                    dwStatFrames = appPointer->dwStatFrames,
                                    dwStatCount = appPointer->dwStatCount,
                                    dwStatFramerateMin = appPointer->dwStatFramerateMin,
                                    dwStatFramerateAvg = appPointer->dwStatFramerateAvg,
                                    dwStatFramerateMax = appPointer->dwStatFramerateMax,
                                    dwOSDX = appPointer->dwOSDX,
                                    dwOSDY = appPointer->dwOSDY,
                                    dwOSDPixel = appPointer->dwOSDPixel,
                                    dwOSDColor = appPointer->dwOSDColor,
                                    dwOSDFrame = appPointer->dwOSDFrame,
                                    dwScreenCaptureFlags = appPointer->dwScreenCaptureFlags,
                                    szScreenCapturePath = new string(appPointer->szScreenCapturePath, 0, 260, Encoding.Default).Trim('\0')
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting App entry: " + ex.Message);
            }

            return app;
        }
    }

    // Data structures representing the shared memory
    public struct RTSS_SHARED_MEMORY
    {
        public UInt32 dwSignature;
        public UInt32 dwVersion;
        public UInt32 dwAppEntrySize;
        public UInt32 dwAppArrOffset;
        public UInt32 dwAppArrSize;
        public UInt32 dwOSDEntrySize;
        public UInt32 dwOSDArrOffset;
        public UInt32 dwOSDArrSize;
        public UInt32 dwOSDFrame;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("RTSS Shared Memory V2\n");
            sb.AppendLine(String.Format("{0,-30}{1}", "Signature", dwSignature));
            sb.AppendLine(String.Format("{0,-30}{1}.{2}", "Version", dwVersion >> 16, dwVersion & 0xffff));
            sb.AppendLine(String.Format("{0,-30}{1}", "App Entry Size", dwAppEntrySize));
            sb.AppendLine(String.Format("{0,-30}{1}", "App Array Offset", dwAppArrOffset));
            sb.AppendLine(String.Format("{0,-30}{1}", "App Array Size", dwAppArrSize));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Entry Size", dwOSDEntrySize));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Array Offset", dwOSDArrOffset));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Array Size", dwOSDArrSize));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Frame", dwOSDFrame));
            return sb.ToString();
        }
    }

    public struct RTSS_SHARED_MEMORY_OSD_ENTRY
    {
        public string szOSD;
        public string szOSDOwner;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("RTSS Shared Memory V2 OSD Entry\n");
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD", szOSD));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Owner", szOSDOwner));
            return sb.ToString();
        }
    }

    public struct RTSS_SHARED_MEMORY_APP_ENTRY
    {
        public UInt32 dwProcessID;
        public string szName;
        public UInt32 dwFlags;
        public UInt32 dwTime0;
        public UInt32 dwTime1;
        public UInt32 dwFrames;
        public UInt32 dwFrameTime;
        public UInt32 dwStatFlags;
        public UInt32 dwStatTime0;
        public UInt32 dwStatTime1;
        public UInt32 dwStatFrames;
        public UInt32 dwStatCount;
        public UInt32 dwStatFramerateMin;
        public UInt32 dwStatFramerateAvg;
        public UInt32 dwStatFramerateMax;
        public UInt32 dwOSDX;
        public UInt32 dwOSDY;
        public UInt32 dwOSDPixel;
        public UInt32 dwOSDColor;
        public UInt32 dwOSDFrame;
        public UInt32 dwScreenCaptureFlags;
        public string szScreenCapturePath;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("RTSS Shared Memory V2 APP Entry\n");
            sb.AppendLine(String.Format("{0,-30}{1}", "Frames", dwFrames));
            sb.AppendLine(String.Format("{0,-30}{1}", "Frame Time", dwFrameTime));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Flags", dwStatFlags));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Time 0", dwStatTime0));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Time 1", dwStatTime1));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Frames", dwStatFrames));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Count", dwStatCount));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Framerate Min", dwStatFramerateMin));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Framerate Avg", dwStatFramerateAvg));
            sb.AppendLine(String.Format("{0,-30}{1}", "Stat Framerate Max", dwStatFramerateMax));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD X", dwOSDX));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Y", dwOSDY));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Pixel", dwOSDPixel));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Color", dwOSDColor));
            sb.AppendLine(String.Format("{0,-30}{1}", "OSD Frame", dwOSDFrame));
            sb.AppendLine(String.Format("{0,-30}{1}", "Screen Capture Flags", dwScreenCaptureFlags));
            sb.AppendLine(String.Format("{0,-30}{1}", "Screen Capture Path", szScreenCapturePath));
            return sb.ToString();
        }

        public uint FPS()
        {
            return dwStatFrames;
        }
    }

    // Unsafe definitions to match the layout of the shared memory
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct RTSS_SHARED_MEMORY_UNSAFE
    {
        [FieldOffset(0)] public UInt32 dwSignature;
        [FieldOffset(4)] public UInt32 dwVersion;
        [FieldOffset(8)] public UInt32 dwAppEntrySize;
        [FieldOffset(12)] public UInt32 dwAppArrOffset;
        [FieldOffset(16)] public UInt32 dwAppArrSize;
        [FieldOffset(20)] public UInt32 dwOSDEntrySize;
        [FieldOffset(24)] public UInt32 dwOSDArrOffset;
        [FieldOffset(28)] public UInt32 dwOSDArrSize;
        [FieldOffset(32)] public UInt32 dwOSDFrame;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct RTSS_SHARED_MEMORY_OSD_ENTRY_UNSAFE
    {
        [FieldOffset(0)] public fixed sbyte szOSD[256];
        [FieldOffset(256)] public fixed sbyte szOSDOwner[256];
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct RTSS_SHARED_MEMORY_APP_ENTRY_UNSAFE
    {
        [FieldOffset(0)] public UInt32 dwProcessID;
        [FieldOffset(4)] public fixed sbyte szName[260];
        [FieldOffset(264)] public UInt32 dwFlags;
        [FieldOffset(268)] public UInt32 dwTime0;
        [FieldOffset(272)] public UInt32 dwTime1;
        [FieldOffset(276)] public UInt32 dwFrames;
        [FieldOffset(280)] public UInt32 dwFrameTime;
        [FieldOffset(284)] public UInt32 dwStatFlags;
        [FieldOffset(288)] public UInt32 dwStatTime0;
        [FieldOffset(292)] public UInt32 dwStatTime1;
        [FieldOffset(296)] public UInt32 dwStatFrames;
        [FieldOffset(300)] public UInt32 dwStatCount;
        [FieldOffset(304)] public UInt32 dwStatFramerateMin;
        [FieldOffset(308)] public UInt32 dwStatFramerateAvg;
        [FieldOffset(312)] public UInt32 dwStatFramerateMax;
        [FieldOffset(316)] public UInt32 dwOSDX;
        [FieldOffset(320)] public UInt32 dwOSDY;
        [FieldOffset(324)] public UInt32 dwOSDPixel;
        [FieldOffset(328)] public UInt32 dwOSDColor;
        [FieldOffset(332)] public UInt32 dwOSDFrame;
        [FieldOffset(336)] public UInt32 dwScreenCaptureFlags;
        [FieldOffset(340)] public fixed sbyte szScreenCapturePath[260];
    }
}
