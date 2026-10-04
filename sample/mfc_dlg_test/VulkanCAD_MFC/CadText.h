#pragma once
// 문자열 도우미 — CAD_* 는 UTF-8, MFC 유니코드 빌드는 UTF-16(wchar_t). 여러 파일이 같이 쓴다.
//
// ⚠️ 한글이 든 문자열을 CAD_* 에 바로 줄 때는 u8"…" 로 쓴다. 그냥 "…" 는 MSVC 가 시스템 코드 페이지(CP949)로
//    바꿔 넣어 엔진(UTF-8)에서 글자가 깨진다. (프로젝트에 /utf-8 을 켜도 되지만 샘플은 기본 설정 그대로 둔다.)

#include "../../../sdk/include/VulkanCAD_API.h"

#include <string>

inline std::wstring Utf8ToWide(const char* s)
{
	if (!s) return L"";
	const int n = MultiByteToWideChar(CP_UTF8, 0, s, -1, nullptr, 0);
	std::wstring out(n > 0 ? n - 1 : 0, L'\0');
	if (n > 1) MultiByteToWideChar(CP_UTF8, 0, s, -1, &out[0], n);
	return out;
}

inline std::string WideToUtf8(const std::wstring& s)
{
	const int n = WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, nullptr, 0, nullptr, nullptr);
	std::string out(n > 0 ? n - 1 : 0, '\0');
	if (n > 1) WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, &out[0], n, nullptr, nullptr);
	return out;
}

// 직전 CAD_* 호출이 실패한 이유 (없으면 빈 문자열)
inline std::wstring CadLastError()
{
	char buf[512] = {};
	CAD_GetLastError(buf, sizeof(buf));
	return Utf8ToWide(buf);
}

// "2회 호출 규약" JSON 읽기 — 먼저 길이를 묻고, 그만큼 버퍼를 잡아 다시 부른다.
//   std::string json = CadReadJson(CAD_GetSectionStateJson);
template <class Fn>
std::string CadReadJson(Fn fn)
{
	const int n = fn(nullptr, 0);
	if (n <= 0) return {};
	std::string out(n + 1, '\0');
	fn(&out[0], n + 1);
	out.resize(strlen(out.c_str()));
	return out;
}
