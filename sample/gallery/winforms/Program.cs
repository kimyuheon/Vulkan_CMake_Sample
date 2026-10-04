using System;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
