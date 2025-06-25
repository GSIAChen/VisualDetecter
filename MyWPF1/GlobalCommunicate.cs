using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using HalconDotNet;

class GlobalCommunicate
{
    // 约定同上
    private const string MmfName = "Global\\SharedImageBuffer";
    private const string WriteDoneEvt = "Global\\SharedImage_WriteDone";
    private const int Width = 640;
    private const int Height = 480;
    private const int Chans = 3;
    private const long ImageSize = (long)Width * Height * Chans;

    // 偏移常量
    private const long OffsetCameraIndex = 0;
    private const long OffsetObjectId = 4;
    private const long OffsetImageData = 12;

    static void Main()
    {
        // 1) 打开同名内存映射
        using var mmf = MemoryMappedFile.OpenExisting(MmfName, MemoryMappedFileRights.Read);

        // 2) 打开写完成事件
        var writeDone = EventWaitHandle.OpenExisting(WriteDoneEvt);

        // 3) 等待写入完成
        Console.WriteLine("Waiting for image...");
        writeDone.WaitOne();

        // 4) 读取元数据 + 图像
        using var accessor = mmf.CreateViewAccessor(0, OffsetImageData + ImageSize, MemoryMappedFileAccess.Read);

        int cameraIndex = accessor.ReadInt32(OffsetCameraIndex);
        ulong objectId = accessor.ReadUInt64(OffsetObjectId);
        byte[] buffer = new byte[ImageSize];
        accessor.ReadArray(OffsetImageData, buffer, 0, buffer.Length);

        Console.WriteLine($"接收到机位: {cameraIndex}, 物件号: {objectId}");

        // 5) （可选）将字节数组转换为 HImage 并调用你的 HALCON 脚本
        //    假设你的图像是连续的 R,G,B byte 一维数组：
        HImage img = new HImage("byte", Width, Height);
    }
}
