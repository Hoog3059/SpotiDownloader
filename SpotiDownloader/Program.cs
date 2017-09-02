using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.IO;
#region CommandLine
#endregion
#region SpotifyApi
using SpotifyAPI.Web; //Base Namespace
using SpotifyAPI.Web.Auth; //All Authentication-related classes
using SpotifyAPI.Web.Enums; //Enums
using SpotifyAPI.Web.Models; //Models for the JSON-responses
#endregion
#region GoogleApis
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
#endregion

namespace SpotiDownloader
{
    class Program
    {
        #region Variables
        private static SpotifyWebAPI _spotify;
        private static List<SongInfo> songList = new List<SongInfo>();
        private static List<SongInfo> failedDownloads = new List<SongInfo>();
        private static string optionsFileFilename;
        static bool logging = false;
        static bool noInteraction = false;
        static OptionsFile optionsFile;
        #endregion

        static void Main(string[] args)
        {
            var cmdArgs = new CommandArguments();
            ParseCMDRArgs(args, cmdArgs);

            if (!cmdArgs.OptionFromGUI) { WriteStart(); }

            optionsFile = LoadOptionsFile();

            WhatToDownload WTD = new WhatToDownload();
            CheckOptionsFileErrors(optionsFile, WTD);

            GetSpotifyApi(optionsFile.Username, optionsFile.Password);

            Console.WriteLine("\r\nListing found tracks:");
            Console.WriteLine("|Name|Artist|Youtube Link|");
            
            List<FullTrack> tracks = new List<FullTrack>();
            LoadTracksIntoListForUse(optionsFile, WTD, tracks);

            ListTracksGetYoutubeLink(tracks);

            Console.WriteLine("\r\nFinding download Links:");
            Console.WriteLine("|Name|Download Link|");
            RetrieveDownloadLinks();

            Console.WriteLine("\r\nDownloading songs to {0}\\{1}-Downloads", optionsFile.OutputFolder, optionsFile.Username);
            DownloadListedTracks();

            Console.WriteLine("\r\nRetrying failed downloads.");
            RetryFailedDownloads();

            if (!noInteraction)
            {
                Console.WriteLine("\r\nFinished! Press enter to continue!");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("\r\nFinished! Exiting in 3 seconds!");
                Thread.Sleep(3000);
            }
        }

        #region File and Command Arguments handling
        private static void CheckOptionsFileErrors(OptionsFile optionsFile, WhatToDownload WTD)
        {
            bool error = false;
            if (optionsFile.WhatToDownload.StartsWith("MyMusic"))
            {
                if (optionsFile.WhatToDownload.Split('#')[1] == "SavedTracks")
                {
                    WTD.DownloadType = WhatToDownload.Type.SavedTracks;
                }
                else
                {
                    error = true;
                }
            }
            else if (optionsFile.WhatToDownload.StartsWith("Playlist"))
            {
                WTD.DownloadType = WhatToDownload.Type.Playlist;
                WTD.PlaylistId = optionsFile.WhatToDownload.Split('#')[1];
            }
            else if (optionsFile.WhatToDownload.StartsWith("MyPlaylists"))
            {
                WTD.DownloadType = WhatToDownload.Type.MyPlaylists;
                WTD.PlaylistName = optionsFile.WhatToDownload.Split('#')[1];
            }
            else
            {
                error = true;
            }
            if (error)
            {
                Console.WriteLine(String.Format("There seems to be an error in {0}. Start SpotiDownloader.exe -h or generate a new profile by running SpotiDownloader.exe -c", optionsFileFilename));
            }
        }

        private static void ParseCMDRArgs(string[] args, CommandArguments cmdArgs)
        {
            if (CommandLine.Parser.Default.ParseArguments(args, cmdArgs))
            {                
                if (cmdArgs.OptionShowHelp)
                {
                    Console.WriteLine(cmdArgs.GetUsage());
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                if (cmdArgs.OptionCreateOptionsFile)
                {
                    OptionsFile exampleFile = new OptionsFile()
                    {
                        Username = "JohnnyAppleseed",
                        Password = "1234",
                        WhatToDownload = "MyMusic#SavedTracks",
                        OutputFolder = Environment.CurrentDirectory
                    };
                    string output = JsonConvert.SerializeObject(exampleFile);
                    StreamWriter sw = new StreamWriter("Profile.json");
                    sw.Write(output);
                    sw.Close();
                    Environment.Exit(0);
                }
                if (cmdArgs.OptionLogging)
                {
                    Console.WriteLine("Logging is currently not implemented!");
                    logging = true;
                }
                if (cmdArgs.OptionNoInteraction)
                {
                    Console.WriteLine("No Interaction is enabled!");
                    noInteraction = true;
                }

                if(cmdArgs.OptionFilename == null)
                {
                    Console.WriteLine(cmdArgs.GetUsage());
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    optionsFileFilename = cmdArgs.OptionFilename;
                }
            }
            else
            {
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        private static OptionsFile LoadOptionsFile()
        {
            string input = File.ReadAllText(Program.optionsFileFilename);
            OptionsFile optionsFile = JsonConvert.DeserializeObject<OptionsFile>(input);
            return optionsFile;
        }
        #endregion

        #region Downloading
        private static void LoadTracksIntoListForUse(OptionsFile optionsFile, WhatToDownload WTD, List<FullTrack> tracks)
        {
            if (WTD.DownloadType == WhatToDownload.Type.SavedTracks)
            {
                _spotify.GetSavedTracks(50).Items.ForEach(track => tracks.Add(track.Track));
            }
            else if (WTD.DownloadType == WhatToDownload.Type.MyPlaylists)
            {
                Paging<SimplePlaylist> playlists = _spotify.GetUserPlaylists(optionsFile.Username);
                bool found = false;
                foreach (SimplePlaylist playlist in playlists.Items)
                {
                    if (WTD.PlaylistName == playlist.Name)
                    {
                        found = true;
                        _spotify.GetPlaylistTracks(optionsFile.Username, playlist.Id).Items.ForEach(track => tracks.Add(track.Track));
                    }
                }
                if (!found)
                {
                    Console.WriteLine("ERR-P: Playlist not found!");
                    if (!noInteraction)
                    {
                        Console.WriteLine("\r\nFinished! Press enter to continue!");
                        Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("\r\nFinished! Exiting in 3 seconds!");
                        Thread.Sleep(3000);
                    }
                    Environment.Exit(0);
                }
            }
            else if (WTD.DownloadType == WhatToDownload.Type.Playlist)
            {
                throw new NotImplementedException();
            }
        }

        private static void RetryFailedDownloads()
        {
            foreach (var songListItem in failedDownloads)
            {
                if (songListItem.DownloadLink != null)
                {
                    if (!Directory.Exists(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\")) { Directory.CreateDirectory(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\"); }

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(songListItem.DownloadLink);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        var responseStream = response.GetResponseStream();
                        using (var fileStream = File.Create(Path.Combine(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\", songListItem.Filename + ".mp3")))
                        {
                            responseStream.CopyTo(fileStream);
                        }

                        Console.WriteLine("Succesfully downloaded: {0}", songListItem.Filename);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("ERR2: An error has occurred downloading [{0}]\r\nTry running the application again or reporting a bug to the developer.", songListItem.Filename);
                    }
                }
            }
        }

        private static void DownloadListedTracks()
        {
            foreach (var songListItem in songList)
            {
                if (songListItem.DownloadLink != null && !File.Exists(Path.Combine(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\", songListItem.Filename + ".mp3")))
                {
                    if (!Directory.Exists(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\")) { Directory.CreateDirectory(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\"); }

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(songListItem.DownloadLink);
                        request.Timeout = 20000;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        var responseStream = response.GetResponseStream();
                        using (var fileStream = File.Create(Path.Combine(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\", songListItem.Filename + ".mp3")))
                        {
                            responseStream.CopyTo(fileStream);
                        }

                        Console.WriteLine("Succesfully downloaded: {0}", songListItem.Filename);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("ERR1: An error has occurred downloading {0}", songListItem.Filename);
                        failedDownloads.Add(songListItem);
                    }
                }
                else if (File.Exists(Path.Combine(optionsFile.OutputFolder + "\\" + optionsFile.Username + "-Downloads\\", songListItem.Filename + ".mp3")))
                {
                    Console.WriteLine("Song {0} was already downloaded. Skipping...", songListItem.Filename);
                }
            }
        }

        private static void RetrieveDownloadLinks()
        {
            for (int i = 0; i < songList.Count; i++)
            {
                try
                {
                    YoutubeInMp3 yimp3 = JsonConvert.DeserializeObject<YoutubeInMp3>(GetYouTubeInMp3ApiResult(songList[i].YoutubeLink));
                    songList[i].DownloadLink = yimp3.Link;

                    Console.WriteLine("{0} | {1}", songList[i].Filename, yimp3.Link);
                }
                catch (Exception)
                {
                    songList[i].DownloadLink = null;
                    Console.WriteLine("ERR0: [{0}] | download link could not be retreived!", songList[i].Filename);
                }
            }
        }

        private static void ListTracksGetYoutubeLink(List<FullTrack> tracks)
        {
            foreach (var track in tracks)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}", track.Name);
                sb.AppendFormat(" - {0}", track.Artists[0].Name);
                sb.Append(" official audio only");
                Task<string> temp = CallYoutubeApi(sb.ToString());
                songList.Add(new SongInfo { YoutubeLink = temp.Result.ToString(), Filename = sb.ToString().Replace(" official audio only", "") });

                Console.WriteLine("{0} | {1}", sb.ToString().Replace(" official audio only", ""), temp.Result.ToString());
            }
        }
        #endregion
        
        #region API's
        private static void GetSpotifyApi(string Username, string Password)
        {
            SpotifyAuthenticationHelper SAH = new SpotifyAuthenticationHelper()
            {
                SpotClientID = "c2ff7ca2d8b648489bf2d5916e2c0395",
                SpotUsername = Username,
                SpotPassword = Password,
                SpotRedirectUri = "http://localhost"
            };

            var auth = new ImplicitGrantAuth()
            {
                ClientId = SAH.SpotClientID,
                RedirectUri = SAH.SpotRedirectUri,
                Scope = Scope.None
            };
            auth.StartHttpServer();

            auth.OnResponseReceivedEvent += (Token token, string state) =>
            {
                auth.StopHttpServer();

                _spotify = new SpotifyWebAPI()
                {
                    TokenType = token.TokenType,
                    AccessToken = token.AccessToken
                };
            };

            Console.WriteLine("Authorization in progress. Please wait. The Login process is fully automated.");
            Thread.Sleep(3000);
            if(SAH.Authorize(SpotiScope.ReadUserTracksAndPlaylist) != 0)
            {
                Console.WriteLine("ERR-C: Authorization timed out. Are the login credentials correct?");
                if (!noInteraction)
                {
                    Console.WriteLine("\r\nFinished! Press enter to continue!");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("\r\nFinished! Exiting in 3 seconds!");
                    Thread.Sleep(3000);
                }
                Environment.Exit(0);
            }
        }

        private async static Task<string> CallYoutubeApi(string searchTerm) 
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer() { ApiKey = "AIzaSyD9SpuKEFp3r5Oi2tHBBqP4rAi440dn4Zs", ApplicationName = "SpotiDownloader" });

            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = searchTerm;
            searchRequest.MaxResults = 5;

            var searchResult = await searchRequest.ExecuteAsync();

            List<string> videos = new List<string>();

            foreach(var result in searchResult.Items)
            {
                switch (result.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(result.Id.VideoId.ToString());
                        break;

                    default:
                        break;
                }
            }

            return videos[0];
        }

        private static string GetYouTubeInMp3ApiResult(string youtubeLink)
        {
            using(var client = new HttpClient())
            {
                Uri link = new Uri("http://www.youtubeinmp3.com/fetch/?format=JSON&video=https://www.youtube.com/watch?v=" + youtubeLink);
                var response = client.GetStringAsync(link);
                return response.Result.ToString();
            }
        }
        #endregion

        static void WriteStart()
        {
            Console.WriteLine("  _____             _   _ _____                      _                 _           \r\n / ____|           | | (_)  __ \\                    | |               | |          \r\n| (___  _ __   ___ | |_ _| |  | | _____      ___ __ | | ___   __ _  __| | ___ _ __ \r\n \\___ \\| '_ \\ / _ \\| __| | |  | |/ _ \\ \\ /\\ / / '_ \\| |/ _ \\ / _` |/ _` |/ _ \\ '__|\r\n ____) | |_) | (_) | |_| | |__| | (_) \\ V  V /| | | | | (_) | (_| | (_| |  __/ |   \r\n|_____/| .__/ \\___/ \\__|_|_____/ \\___/ \\_/\\_/ |_| |_|_|\\___/ \\__,_|\\__,_|\\___|_|   \r\n       | |                                                                         \r\n       |_|                                                                          ");
            Console.WriteLine("____________________________________________________________________________________");
            Console.WriteLine("SpotiDownloader finds tracks on your Spotify account and tries to find the track on YouTube. It will then download the YouTube video to your computer as .mp3");
            var sb = new StringBuilder();
            sb.Append("DISCLAIMER: When using this program, you agree that it may use the given login credentials to automatically login and");
            sb.Append(" authorize 'SpotiDownloader' on your account. SpotiDownloader will NOT use your credentials for anything else than retrieving");
            sb.Append(" songs from your account. By hitting enter on the keyboard you agree to these terms. For information about licensing, see License.txt.\r\n");
            Console.WriteLine(sb.ToString());
            if(!noInteraction)
            {
                Console.WriteLine("Starting after any key is pressed...");
                Console.ReadKey();
            }            
        }
    }
}
