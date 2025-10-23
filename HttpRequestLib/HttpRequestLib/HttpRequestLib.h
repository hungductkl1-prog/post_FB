#ifndef HTTP_REQUEST_LIB_H
#define HTTP_REQUEST_LIB_H

#include <windows.h>

#ifdef HTTPREQUESTLIB_EXPORTS
#  define HTTP_API __declspec(dllexport)
#else
#  define HTTP_API __declspec(dllimport)
#endif

extern "C" {

    // Gửi HTTP(S) GET/POST hỗ trợ:
    // - Header tùy chỉnh
    // - Body UTF-8
    // - Proxy thủ công hoặc tự động (PAC/WPAD)
    // - Proxy auth (Basic)
    // Trả về: chuỗi UTF-16 cấp phát bằng CoTaskMemAlloc
    HTTP_API LPWSTR __stdcall HttpRequestW(
        const wchar_t* method,
        const wchar_t* url,
        const wchar_t* extraHeader,
        const wchar_t* body,
        const wchar_t* proxy,        // "" để tự động lấy hệ thống (PAC/WPAD)
        const wchar_t* proxyUser,
        const wchar_t* proxyPass);

    // Giải phóng kết quả
    HTTP_API void __stdcall FreeResponse(LPWSTR p);
}

#endif
