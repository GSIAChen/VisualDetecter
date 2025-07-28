using System;
using System.Runtime.InteropServices;

namespace MyWPF1
{
    public static class SieveDll
    {
    // 对应 typedef int GzsiaHandle;
    public struct GzsiaHandle
    {
        public int Value;
        public static implicit operator int(GzsiaHandle h) => h.Value;
        public static implicit operator GzsiaHandle(int val) => new GzsiaHandle { Value = val };
    }

    // GzsiaImage
    [StructLayout(LayoutKind.Sequential)]
    public struct GzsiaImage
    {
        public short width;
        public short height;
        public short pixelType;
        public short bytePerLine;
    }

    // SieveCaptureEx
    [StructLayout(LayoutKind.Sequential)]
    public struct SieveCaptureEx
    {
        public byte camerId;
        public int detectId;
        public IntPtr image;  // GzsiaImage*
    }

    // CaptureCallbackFunc 类型
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void CaptureCallbackFunc(IntPtr pData, ref SieveCaptureEx captureInfo, IntPtr pUser);

    private const string DllName = "TestEc3224l.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern GzsiaHandle testEc3224l_CreateProjectAgent(string jsFile);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern void testEc3224l_DestroyProjectAgent(GzsiaHandle handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern void testEc3224l_ProjectAgentCommand(GzsiaHandle handle, string cmd);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I1)]  // 对应 C++ 的 bool
    public static extern bool testEc3224l_HandleValid(GzsiaHandle handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern GzsiaHandle testEc3224l_GetHandByName(GzsiaHandle handle, string name);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool testEc3224l_RegisterCaptrueCallBack(GzsiaHandle handle, CaptureCallbackFunc callback, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool testEc3224l_UnRegisterCaptrueCallBack(GzsiaHandle handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern void testEc3224l_Test();
    }
}