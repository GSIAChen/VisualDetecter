using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using HalconDotNet;

public class NamedPipeImageServer
{
    private const string PipeName = "ImagePipe";

    public async Task StartListeningAsync()
    {
        while (true)
        {
            var server = new NamedPipeServerStream(PipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                                                   PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            Console.WriteLine("[Pipe] Waiting for client...");
            await server.WaitForConnectionAsync();
            Console.WriteLine("[Pipe] Client connected");

            _ = Task.Run(() => HandleClient(server));
        }
    }

    private void HandleClient(NamedPipeServerStream pipe)
    {
        try
        {
            using var reader = new BinaryReader(pipe);

            // 1. 读取头部参数
            int cameraIndex = reader.ReadInt32();
            int objectIndex = reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int channels = reader.ReadInt32();

            int imageBytes = width * height * channels;
            byte[] imageData = reader.ReadBytes(imageBytes);

            Console.WriteLine($"[Pipe] Received: Cam={cameraIndex}, Obj={objectIndex}, {width}x{height}x{channels}");

            // 2. 转换为 HImage（灰度或RGB）
            HImage image;
            if (channels == 1)
                image = new HImage("byte", width, height, imageData[0]);
            else
                image = new HImage();
            image.GenImageInterleaved(imageData[0], "rgb", width, height, -1, "byte", width, height, 0, 0, -1, 0);

            // 3. 处理图像（调用你的Halcon过程）
            //RunHalconProcedure(image, cameraIndex, objectIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Pipe Error] {ex.Message}");
        }
        finally
        {
            pipe.Dispose();
        }
    }

    /**
    private void RunHalconProcedure(HImage image, int cameraIndex, int objectIndex)
    {
        try
        {
            var call = new HDevProcedureCall(program, "YourHalconProcedure"); // 替换为你的 Halcon procedure
            call.SetInputIconicParamObject("Image", image);
            call.SetInputCtrlParamTuple("CameraIndex", new HTuple(cameraIndex));
            call.SetInputCtrlParamTuple("ObjectIndex", new HTuple(objectIndex));
            call.Execute();

            var result = call.GetOutputCtrlParamTuple("IsOk");
            bool ok = result.I == 1;
            Console.WriteLine($"[Result] Cam {cameraIndex}, Obj {objectIndex} => {(ok ? "OK" : "NG")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Halcon Error] {ex.Message}");
        }
    }
    **/
}
