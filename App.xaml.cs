using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Planner
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow window = new MainWindow();
            if (e.Args.Length > 0)
            {
                if(window.SmartOpenFile(e.Args[0])<0)
                {
                    Shutdown();
                    return;
                }
            }
            window.ShowDialog();
            Shutdown();
        }


    }
}
