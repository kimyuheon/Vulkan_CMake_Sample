
// VulkanCAD_MFC.cpp: 애플리케이션에 대한 클래스 동작을 정의합니다.
//

#include "pch.h"
#include "framework.h"
#include "VulkanCAD_MFC.h"
#include "VulkanCAD_MFCDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CVulkanCADMFCApp

BEGIN_MESSAGE_MAP(CVulkanCADMFCApp, CWinApp)
	ON_COMMAND(ID_HELP, &CWinApp::OnHelp)
END_MESSAGE_MAP()


// CVulkanCADMFCApp 생성

CVulkanCADMFCApp::CVulkanCADMFCApp()
{
	// TODO: 여기에 생성 코드를 추가합니다.
	// InitInstance에 모든 중요한 초기화 작업을 배치합니다.
}


// 유일한 CVulkanCADMFCApp 개체입니다.

CVulkanCADMFCApp theApp;


// CVulkanCADMFCApp 초기화

BOOL CVulkanCADMFCApp::InitInstance()
{
	CWinApp::InitInstance();

	CVulkanCADMFCDlg dlg;
	m_pMainWnd = &dlg;
	dlg.DoModal();

	// 대화 상자가 닫혔으므로 응용 프로그램의 메시지 펌프를 시작하지 않고 응용 프로그램을 끝낼 수 있도록 FALSE를
	// 반환합니다.
	return FALSE;
}

