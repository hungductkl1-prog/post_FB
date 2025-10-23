#define HTTPREQUESTLIB_EXPORTS
#include "HttpRequestLib.h"
#include <winhttp.h>
#include <string>
#include <vector>
#include <fstream>
#include <filesystem>
#include <chrono>
#include <ctime>

#pragma comment(lib, "winhttp.lib")

static std::string WideToUtf8(const std::wstring& w) {
    if (w.empty()) return {};
    int len = WideCharToMultiByte(CP_UTF8, 0, w.data(), (int)w.size(), nullptr, 0, nullptr, nullptr);
    std::string u(len, '\0');
    WideCharToMultiByte(CP_UTF8, 0, w.data(), (int)w.size(), &u[0], len, nullptr, nullptr);
    return u;
}

static std::wstring Utf8ToWide(const std::string& u8) {
    if (u8.empty()) return {};
    int len = MultiByteToWideChar(CP_UTF8, 0, u8.data(), (int)u8.size(), nullptr, 0);
    std::wstring w(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, u8.data(), (int)u8.size(), &w[0], len);
    return w;
}

static void LogError(const std::wstring& message) {
    try {
        auto now = std::chrono::system_clock::now();
        std::time_t now_c = std::chrono::system_clock::to_time_t(now);
        struct tm timeinfo;
        localtime_s(&timeinfo, &now_c);

        wchar_t dateFolder[100];
        wcsftime(dateFolder, 100, L"%d-%m-%Y", &timeinfo);
        std::wstring folder = std::wstring(L"logs\\") + dateFolder;
        std::filesystem::create_directories(folder);
        std::wstring filepath = folder + L"\\HttpRequestLib.txt";

        std::wofstream logFile(filepath, std::ios::app);
        if (!logFile.is_open()) return;

        wchar_t timeStr[100];
        wcsftime(timeStr, 100, L"%d-%m-%Y %H:%M:%S", &timeinfo);
        logFile << L"[" << timeStr << L"] " << message << std::endl;
    }
    catch (...) {}
}

extern "C" HTTP_API
LPWSTR __stdcall HttpRequestW(
    const wchar_t* method,
    const wchar_t* url,
    const wchar_t* extraHeader,
    const wchar_t* body,
    const wchar_t* proxy,
    const wchar_t* proxyUser,
    const wchar_t* proxyPass)
{
    LPWSTR result = nullptr;

    HINTERNET hSession = WinHttpOpen(L"C++AutoHttp/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY,
        WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSession) {
        LogError(L"WinHttpOpen (init) failed.");
        return nullptr;
    }

    WINHTTP_AUTOPROXY_OPTIONS apo{};
    WINHTTP_PROXY_INFO proxyInfo{};
    bool useAutoProxy = (!proxy || !*proxy);

    if (useAutoProxy) {
        apo.dwFlags = WINHTTP_AUTOPROXY_AUTO_DETECT;
        apo.dwAutoDetectFlags = WINHTTP_AUTO_DETECT_TYPE_DHCP | WINHTTP_AUTO_DETECT_TYPE_DNS_A;
        apo.fAutoLogonIfChallenged = TRUE;

        wchar_t urlBuf[2048];
        wcsncpy_s(urlBuf, url, _TRUNCATE);

        if (WinHttpGetProxyForUrl(hSession, urlBuf, &apo, &proxyInfo)) {
            WinHttpCloseHandle(hSession);
            hSession = WinHttpOpen(L"C++AutoHttp/1.0",
                WINHTTP_ACCESS_TYPE_NAMED_PROXY,
                proxyInfo.lpszProxy,
                proxyInfo.lpszProxyBypass,
                0);
            GlobalFree(proxyInfo.lpszProxy);
            GlobalFree(proxyInfo.lpszProxyBypass);
        }
        else {
            WinHttpCloseHandle(hSession);
            hSession = WinHttpOpen(L"C++AutoHttp/1.0",
                WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
                WINHTTP_NO_PROXY_NAME,
                WINHTTP_NO_PROXY_BYPASS,
                0);
        }
    }
    else {
        WinHttpCloseHandle(hSession);
        hSession = WinHttpOpen(L"C++AutoHttp/1.0",
            WINHTTP_ACCESS_TYPE_NAMED_PROXY,
            proxy,
            WINHTTP_NO_PROXY_BYPASS,
            0);
    }

    if (!hSession) {
        LogError(L"WinHttpOpen (after proxy config) failed.");
        return nullptr;
    }

    URL_COMPONENTS uc{ sizeof(uc) };
    wchar_t host[256]{}, path[2048]{};
    uc.lpszHostName = host; uc.dwHostNameLength = _countof(host);
    uc.lpszUrlPath = path; uc.dwUrlPathLength = _countof(path);
    if (!WinHttpCrackUrl(url, 0, 0, &uc)) {
        LogError(std::wstring(L"WinHttpCrackUrl failed for URL: ") + url);
        WinHttpCloseHandle(hSession);
        return nullptr;
    }

    HINTERNET hConnect = WinHttpConnect(hSession, host, uc.nPort, 0);
    if (!hConnect) {
        LogError(std::wstring(L"WinHttpConnect failed for host: ") + host);
        WinHttpCloseHandle(hSession);
        return nullptr;
    }

    DWORD flags = (uc.nScheme == INTERNET_SCHEME_HTTPS) ? WINHTTP_FLAG_SECURE : 0;
    HINTERNET hRequest = WinHttpOpenRequest(hConnect, method, path, nullptr,
        WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, flags);
    if (!hRequest) {
        LogError(std::wstring(L"WinHttpOpenRequest failed for path: ") + path);
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        return nullptr;
    }

    if (proxy && *proxy && proxyUser && *proxyUser)
        WinHttpSetCredentials(hRequest, WINHTTP_AUTH_TARGET_PROXY,
            WINHTTP_AUTH_SCHEME_BASIC, proxyUser, proxyPass, nullptr);

    std::string bodyUtf8 = WideToUtf8(body ? body : L"");
    DWORD bodyLen = (DWORD)bodyUtf8.size();

    BOOL ok = WinHttpSendRequest(
        hRequest,
        (extraHeader && *extraHeader) ? extraHeader : WINHTTP_NO_ADDITIONAL_HEADERS,
        (extraHeader && *extraHeader) ? -1L : 0,
        bodyLen ? (LPVOID)bodyUtf8.data() : WINHTTP_NO_REQUEST_DATA,
        bodyLen, bodyLen, 0);

    if (!ok) {
        LogError(std::wstring(L"WinHttpSendRequest failed for: ") + method + L" " + url);
        WinHttpCloseHandle(hRequest); WinHttpCloseHandle(hConnect); WinHttpCloseHandle(hSession);
        return nullptr;
    }

    if (!WinHttpReceiveResponse(hRequest, nullptr)) {
        LogError(std::wstring(L"WinHttpReceiveResponse failed for: ") + url);
        WinHttpCloseHandle(hRequest); WinHttpCloseHandle(hConnect); WinHttpCloseHandle(hSession);
        return nullptr;
    }

    std::vector<char> buf;
    DWORD sz = 0;
    do {
        if (!WinHttpQueryDataAvailable(hRequest, &sz) || !sz) break;
        size_t cur = buf.size(); buf.resize(cur + sz);
        DWORD rd = 0;
        if (!WinHttpReadData(hRequest, buf.data() + cur, sz, &rd)) break;
        buf.resize(cur + rd);
    } while (sz);

    WinHttpCloseHandle(hRequest);
    WinHttpCloseHandle(hConnect);
    WinHttpCloseHandle(hSession);

    std::wstring wide = Utf8ToWide(std::string(buf.begin(), buf.end()));
    size_t bytes = (wide.size() + 1) * sizeof(wchar_t);
    result = (LPWSTR)CoTaskMemAlloc(bytes);
    if (result) memcpy(result, wide.c_str(), bytes);
    else LogError(L"CoTaskMemAlloc failed.");

    return result;
}

extern "C" HTTP_API
void __stdcall FreeResponse(LPWSTR p) {
    if (p) CoTaskMemFree(p);
}
