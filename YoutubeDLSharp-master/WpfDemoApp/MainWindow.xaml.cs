using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace WpfDemoApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool isNotDownloading = true;
        private bool audioOnly = false;

        private IProgress<DownloadProgress> progress;
        private IProgress<string> output;

        public MainWindow()
        {
            this.YoutubeDL = new YoutubeDL() { YoutubeDLPath = "yt-dlp.exe" };
            this.DataContext = this;
            InitializeComponent();
            progress = new Progress<DownloadProgress>((p) => showProgress(p));
            output = new Progress<string>((s) => txtOutput.AppendText(s + Environment.NewLine));
        }

        public YoutubeDL YoutubeDL { get; }

        public bool IsNotDownloading
        {
            get => isNotDownloading;
            set
            {
                isNotDownloading = value;
                propertyChanged();
            }
        }

        public bool AudioOnly
        {
            get => audioOnly;
            set
            {
                audioOnly = value;
                propertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void propertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text;
            IsNotDownloading = false;
            txtOutput.Clear();
            // Parse custom arguments
            OptionSet custom = OptionSet.FromString(txtOptions.Text.Split('\n'));
            RunResult<string> result;
            if (AudioOnly)
            {
                result = await YoutubeDL.RunAudioDownload(
                    url, AudioConversionFormat.Mp3, progress: progress,
                    output: output, overrideOptions: custom
                );
            }
            else
            {
                result = await YoutubeDL.RunVideoDownload(url, progress: progress, output: output, overrideOptions: custom);
            }
            if (result.Success)
            {
                MessageBox.Show($"Successfully downloaded \"{url}\" to:\n\"{result.Data}\".", "YoutubeDLSharp");
            }
            else showErrorMessage(url, String.Join("\n", result.ErrorOutput));
            IsNotDownloading = true;
        }

        private void showProgress(DownloadProgress p)
        {
            txtState.Text = p.State.ToString();
            progDownload.Value = p.Progress;
            txtProgress.Text = $"speed: {p.DownloadSpeed} | left: {p.ETA}";
        }

        private async void InformationButton_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text;
            RunResult<VideoData> result = await YoutubeDL.RunVideoDataFetch(url);
            if (result.Success)
            {
                var infoWindow = new InformationWindow(result.Data);
                infoWindow.ShowDialog();
            }
            else showErrorMessage(url, String.Join("\n", result.ErrorOutput));
        }

        private void showErrorMessage(string url, string error)
            => MessageBox.Show($"Failed to process '{url}'. Output:\n\n{error}", "YoutubeDLSharp - ERROR",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
