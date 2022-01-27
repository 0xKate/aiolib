using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using aiolib;

namespace TestServerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        public bool test = true;
        void App_Startup(object sender, StartupEventArgs e)
        {
            ConsoleAllocator.ShowConsoleWindow();
            Controller c = new();
            c.RunServer();

            // Application is running
            // Process command line args
            bool startMinimized = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/StartMinimized")
                {
                    startMinimized = true;
                }
            }

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = c.StreamServer;
            if (startMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
            mainWindow.Show();
        }


    }
}
