using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Net;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Forms = System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SpotiDownloaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        BackgroundWorker BW;
        string filename;

        public MainWindow()
        {
            InitializeComponent();

            ParseCommandLineArguments();

            txtOutput.Document.Blocks.Clear();

            BW = new BackgroundWorker();
            BW.DoWork += BW_DoWork;
        }

        private void ParseCommandLineArguments()
        {
            var cmdArgs = new CommandArguments();
            CommandLine.Parser.Default.ParseArguments(Environment.GetCommandLineArgs(), cmdArgs);

            if (cmdArgs.OptionFilename != null)
            {
                string input = File.ReadAllText(cmdArgs.OptionFilename);
                SpotiDownloader.OptionsFile optionsFile = JsonConvert.DeserializeObject<SpotiDownloader.OptionsFile>(input);
                txtUsername.Text = optionsFile.Username;
                txtPassword.Password = optionsFile.Password;

                if (optionsFile.WhatToDownload.StartsWith("MyMusic"))
                {
                    comboWhatToDownload.SelectedIndex = 0;
                }
                else if (optionsFile.WhatToDownload.StartsWith("MyPlaylists"))
                {
                    comboWhatToDownload.SelectedIndex = 1;
                    txtPlaylistName.IsReadOnly = false;
                    txtPlaylistName.Text = optionsFile.WhatToDownload.Split('#')[1];
                }

                txtFolder.Text = optionsFile.OutputFolder;
            }
        }

        private void BW_DoWork(object sender, DoWorkEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "SpotiDownloader.exe";
            p.StartInfo.Arguments = "--from-gui -n -f " + filename;

            p.OutputDataReceived += OutputReceived;

            p.Start();
            p.BeginOutputReadLine();

            p.WaitForExit();

            string text = new TextRange(txtOutput.Document.ContentStart, txtOutput.Document.ContentEnd).Text;
            string[] lines = text.Split('\r');
            SortedList<string, string> errors = new SortedList<string, string>();

            foreach (string line in lines)
            {
                if (line.StartsWith("ERR0"))
                {
                    string song = Regex.Match(line, @"\[([^]]*)\]").Groups[1].Value;
                    errors.Add(song, "Download link could not be retreived!");
                }
                else if (line.StartsWith("ERR2"))
                {
                    string song = Regex.Match(line, @"\[([^]]*)\]").Groups[1].Value;
                    errors.Add(song, "Failed to download song!");
                }
                else if (line.StartsWith("ERR-C"))
                {
                    MessageBox.Show("Failed to authorize your account. Are the given credentials correct?");
                }
                else if (line.StartsWith("ERR-P"))
                {
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        MessageBox.Show($"Couldn't find '{txtPlaylistName.Text}' in your playlists. Make sure you typed it corrent. Bare in mind that it is case sensitive.");
                    }));                    
                }
            }

            if (errors.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("There were some errors downloading your songs:\r\n");
                foreach (var item in errors)
                {
                    sb.AppendFormat("{0} | {1}\r\n", item.Key, item.Value);
                }
                MessageBox.Show(sb.ToString(), "Errors", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Document.Blocks.Clear();

            filename = System.IO.Path.GetTempPath() + "spotidownloadertemp";

            SpotiDownloader.OptionsFile of = new SpotiDownloader.OptionsFile()
            {
                Username = txtUsername.Text,
                Password = txtPassword.Password,
                OutputFolder = txtFolder.Text
            };
            if (comboWhatToDownload.SelectedIndex == 1)
            {
                of.WhatToDownload = "MyPlaylists#" + txtPlaylistName.Text;
            }
            else
            {
                of.WhatToDownload = "MyMusic#SavedTracks";
            }

            StreamWriter sw = new StreamWriter(filename);
            sw.Write(JsonConvert.SerializeObject(of));
            sw.Close();
            
            BW.RunWorkerAsync();
        }

        private void OutputReceived(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                txtOutput.AppendText(e.Data + "\r");
                txtOutput.ScrollToEnd();
            }));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboWhatToDownload.SelectedIndex == 1)
            {
                txtPlaylistName.IsReadOnly = false;
            }
            else
            {
                txtPlaylistName.IsReadOnly = true;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Forms.FolderBrowserDialog FBD = new Forms.FolderBrowserDialog()
            {
                Description = "Select output folder.",
                ShowNewFolderButton = true,
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFolder.Text = FBD.SelectedPath;
            }
        }
    }
}

