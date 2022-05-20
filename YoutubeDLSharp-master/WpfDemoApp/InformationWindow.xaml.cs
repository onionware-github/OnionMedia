using System;
using System.Windows;
using YoutubeDLSharp.Metadata;

namespace WpfDemoApp
{
    public partial class InformationWindow : Window
    {
        public InformationWindow(VideoData videoData)
        {
            this.DataContext = videoData;
            InitializeComponent();
        }
    }
}
