using System;
using System.Windows.Forms;

namespace VulkanCadWinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();   // 고DPI(PerMonitorV2)·비주얼 스타일 — csproj 설정을 따른다
            Application.Run(new MainForm());
        }
    }
}
