using System.Runtime.InteropServices;

namespace GeoX.ProjQualification
{
    // Qualification ABI: strings are borrowed for this synchronous call. Native
    // contexts/objects are destroyed inside it; no allocator or handle crosses it.
    public static class ProbeBridge
    {
        [DllImport("geox_proj_probe", CallingConvention = CallingConvention.Cdecl)]
        public static extern int geox_proj_probe_run(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string directory,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string fixtures,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string report);
    }
}
