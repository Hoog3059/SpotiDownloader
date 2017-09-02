namespace SpotiDownloader
{
    class WhatToDownload
    {
        public enum Type
        {
            SavedTracks,
            Playlist,
            MyPlaylists
        }

        public Type DownloadType { get; set; }
        public string PlaylistId { get; set; }
        public string PlaylistName { get; set; }
    }
}
