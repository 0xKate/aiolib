using System;
using System.Windows;
using System.Windows.Controls;

namespace TestServerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Console.WriteLine("MainWindow Initialized.");
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine(e.AddedItems.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            aiolib.Crypto.MakeCert("test.crt", "test.key", "gamesys.kfuji.net", "10.0.0.10");
        }
    }
}
