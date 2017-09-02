using CommandLine;
using CommandLine.Text;

namespace SpotiDownloaderGUI
{
    public class CommandArguments
    {
        [Option('f', "file", Required = false, DefaultValue = null, HelpText = "Option for downloading are stored in this .json file. It loads the values into the window.")]
        public string OptionFilename { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("SpotiDownloaderGUI", "0.1.1 Beta"),
                Copyright = new CopyrightInfo("Hoog3059 (Timo Hoogenbosch)", 2017),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            help.AddOptions(this);
            return help.ToString();
        }
    }
}

