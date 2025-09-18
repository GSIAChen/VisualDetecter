using System;
using System.IO;
using System.Runtime.InteropServices;

public class DllArchitectureChecker
{
    [DllImport("MCDLL_NET.dll", CharSet = CharSet.Auto)]
    private static extern bool MapAndLoad(string imageName, string dllPath, out LOADED_IMAGE loadedImage, bool dotDll, bool readOnly);

    [DllImport("MCDLL_NET.dll", CharSet = CharSet.Auto)]
    private static extern bool UnMapAndLoad(ref LOADED_IMAGE loadedImage);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct LOADED_IMAGE
    {
        public IntPtr ModuleName;
        public IntPtr hFile;
        public IntPtr MappedAddress;
        public IntPtr FileHeader;
        // 其他字段...
    }

    public static string GetDllArchitecture(string dllPath)
    {
        if (!File.Exists(dllPath))
            return "文件不存在";

        try
        {
            LOADED_IMAGE loadedImage;
            if (MapAndLoad(Path.GetFileName(dllPath), Path.GetDirectoryName(dllPath), out loadedImage, false, true))
            {
                // 读取PE头信息
                // 这里简化处理，实际需要读取更多信息
                UnMapAndLoad(ref loadedImage);

                // 使用更简单的方法
                return CheckArchitectureSimple(dllPath);
            }
            return "无法读取";
        }
        catch
        {
            return CheckArchitectureSimple(dllPath);
        }
    }

    private static string CheckArchitectureSimple(string dllPath)
    {
        try
        {
            // 读取文件的前几个字节来检测PE头
            byte[] data = new byte[1024];
            using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(data, 0, 1024);
            }

            // 检查PE签名 (PE\0\0)
            if (data[0] == 0x4D && data[1] == 0x5A) // MZ头
            {
                int peHeaderOffset = BitConverter.ToInt32(data, 0x3C);

                if (peHeaderOffset + 4 < data.Length &&
                    data[peHeaderOffset] == 0x50 &&
                    data[peHeaderOffset + 1] == 0x45 &&
                    data[peHeaderOffset + 2] == 0x00 &&
                    data[peHeaderOffset + 3] == 0x00)
                {
                    // 读取机器类型
                    ushort machineType = BitConverter.ToUInt16(data, peHeaderOffset + 4);

                    switch (machineType)
                    {
                        case 0x014C: return "32位 (x86)";
                        case 0x8664: return "64位 (x64)";
                        case 0x0200: return "64位 (IA64)";
                        case 0x01C4: return "ARM";
                        case 0xAA64: return "ARM64";
                        default: return $"未知架构: 0x{machineType:X4}";
                    }
                }
            }
            return "不是有效的PE文件";
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }

    
}


