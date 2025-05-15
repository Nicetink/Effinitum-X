using System;
using System.Reflection;
using System.Windows;

namespace SystemOptimizer
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            
            // Display application version
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                tbVersion.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                tbVersion.Text = "Version 1.0.0";
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 