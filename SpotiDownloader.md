SpotiDownloader
======
SpotiDownloader is a program for downloading a version of your favorite songs on spotify to your computer. It will try to get the best version it can find, but for some songs there just isn't the right version available. SpotiDownloader is a console program but there is a WPF app available (insert link) [here]().

Usage
------
To run SpotiDownloader, run the command below. Profile.json is a file with settings used for downloading the songs. Such a file can be generated with a different command.
```bash
> SpotiDownloader.exe -f Profile.json
```
Generate a settings file using the command below. The file will be generated in the current working directory. You have to edit it with your own settings.
```bash
> SpotiDownloader.exe -c
```
For more commands run:
```bash
> SpotiDownloader --help
```

Profile
------
A profile json contains the following entries:
 - Username: This is your Spotify username. SpotiDownloader will use it to authorize itself with your account.
 - Password: This is your Spotify password. SpotiDownloader will use it to authorize itself with your account.
 - WhatToDownload: Here you can specify what you want SpotiDownloader to download. You can choose from:
   - MyMusic#SavedTracks : Songs from 'My Music' -> Tracks
   - MyPlaylists#&lt;Playlist Name&gt; : Songs from &lt;PLaylist Name&gt; in 'My Music'. Playlist names are case-sensitive.
   - More comming soon
 - OutputFolder: Songs will be downloaded to: \OutputFolder\&lt;Username&gt;-Downloads

Working
------
SpotiDownloader will use your Spotify username and password to authorize itself with your account. it will have these permissions on your account:
+ playlist-read-private : Read your private playlists
+ playlist-read-collaborative : Read your collaborative playlists
+ user-library-read : Read the 'Your Music' library

SpotiDownloader finds the track's name and artists and makes a YouTube search with the filter 'official audio only'. It will grab the first result and retrieve a download link from [YouTubeInMP3.com](http://www.youtubeinmp3.com/api). It will then download the song to \&lt;Application path&gt;\&lt;Username&gt;-Downloads\

Third Party Libraries used
------
+ [Awesomium](http://www.awesomium.com) : Automation of authorization
+ [CommandLine](https://github.com/gsscoder/commandline) : Command line parsing
+ [Google Apis Youtube Data V3](https://developers.google.com/youtube/v3/) : Youtube search
+ [Newtonsoft.Json](http://www.newtonsoft.com/json) : Json serialization
+ [Spotify-API.Net](https://johnnycrazy.github.io/SpotifyAPI-NET/) : Spotify web api wrapper