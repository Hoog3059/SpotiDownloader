using CommandLine;
using CommandLine.Text;

namespace SpotiDownloader
{
    public class CommandArguments
    {
        [Option('f', "file", Required = false, DefaultValue = null, HelpText ="Option for downloading are stored in this .json file.")]
        public string OptionFilename { get; set; }

        [Option('n', "no-interaction", DefaultValue = false, HelpText = "Enable this if you are not able to interact with the application.")]
        public bool OptionNoInteraction { get; set; }

        [Option('l', "log", DefaultValue = false, HelpText = "Enable this if you want the console output to be saved in SpotiDownloader-<time>-<date>.log")]
        public bool OptionLogging { get; set; }

        [Option('c', "create-options-file", DefaultValue = false, HelpText = "Run this if you want the application to create a profile.json file for settings.")]
        public bool OptionCreateOptionsFile { get; set; }

        [Option("from-gui", HelpText = "Is only used by SpotiDownloaderGUI.exe. If you run under this command, no unnecessary information will be shown.")]
        public bool OptionFromGUI { get; set; }

        [Option('u', "username", HelpText = "Sets the username used for authorizing your account.")]
        public string OptionUsername { get; set; }

        [Option('h', "help")]
        public bool OptionShowHelp { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("SpotiDownloader", "0.1.1 Beta"),
                Copyright = new CopyrightInfo("Hoog3059 (Timo Hoogenbosch)", 2017),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Usage: SpotiDownloader.exe -f profile.json");
            help.AddPreOptionsLine("Profile.json WhatToDownload property:\r\n  MyMusic#SavedTracks: Music in 'My Music'\r\n  Playlist#<id>: Music from playlist with id\r\n  MyPlaylists#<playlist name>: The name of one of the playlists you are following or have made.");
            help.AddOptions(this);
            return help.ToString();
        }
    }
}
