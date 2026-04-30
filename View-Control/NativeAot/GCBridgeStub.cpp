// Stub for RhIsGCBridgeActive — runtime pack 10.0.4/10.0.5 không export symbol này
// dù ILC emit unresolved reference từ System.Private.CoreLib (.NET 10 GC Bridge feature
// dành cho Java/Obj-C interop). Trả về 0 = bridge không active, GC chạy bình thường.
// Khi Microsoft fix runtime pack thì xóa file này + node <NativeLibrary> trong .csproj.
//
// Calling convention khớp với RhIsServerGc/RhIsPromoted trong Runtime.WorkstationGC.lib:
// extern "C" Boolean (= int32_t) RhIsXxx().

#include <stdint.h>

extern "C" int32_t RhIsGCBridgeActive()
{
    return 0;
}
