using System;
using System.Text;
using System.Threading.Tasks;
using Awesomium.Core;
using System.Timers;

namespace SpotiDownloader
{
    class SpotifyAuthenticationHelper
    {
        public string SpotUsername { get; set; }
        public string SpotPassword { get; set; }
        public string SpotClientID { get; set; }
        public string SpotRedirectUri { get; set; }

        private WebView browser;
        private int errorCode = 0;
        private Timer errortimer = new Timer(5000);

        public int Authorize(SpotiScope scope)
        {
            errortimer.Elapsed += Errortimer_Elapsed;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("https://accounts.spotify.com/nl/authorize?client_id={0}", SpotClientID);
            sb.AppendFormat("&response_type=token&redirect_uri={0}&state=XSS", SpotRedirectUri);
            sb.AppendFormat("&scope={0}&show_dialog=False", scope.Value);
            
            Task webTask = Task.Run( () => {
                browser = WebCore.CreateWebView(640, 480);
                browser.Source = new Uri(sb.ToString());
                browser.AddressChanged += Browser_AddressChanges;
                WebCore.Run();
            });
            webTask.Wait();

            return errorCode;
        }

        private void Errortimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            errorCode = 1;
            WebCore.Shutdown();            
        }

        private bool FirstTimeAuth
        {
            get
            {
                if((int)browser.ExecuteJavascriptWithResult("document.getElementsByClassName('auth-allow').length") > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void Browser_AddressChanges(object sender, UrlEventArgs e)
        {
            if (browser.Source.ToString().Contains("/authorize") && browser.IsDocumentReady)
            {
                if (FirstTimeAuth)
                {
                    browser.ExecuteJavascript("document.getElementsByClassName('auth-allow')[0].click()");
                }
                else
                {
                    browser.ExecuteJavascript("document.getElementsByClassName('col-xs-12')[1].getElementsByTagName('a')[0].click()");
                }                
            }
            else if (browser.Source.ToString().Contains("/login") && browser.IsDocumentReady)
            {
                browser.ExecuteJavascript("function fireEvent(element,event) {	var evt = document.createEvent('HTMLEvents'); evt.initEvent(event, true, false ); element.dispatchEvent(evt); }");
                browser.ExecuteJavascript(String.Format("document.getElementsByTagName('input')[0].value='{0}'", SpotUsername));
                browser.ExecuteJavascript(String.Format("document.getElementsByTagName('input')[1].value='{0}'", SpotPassword));
                browser.ExecuteJavascript("document.getElementsByTagName('input')[2].checked=false");
                browser.ExecuteJavascript("fireEvent(document.getElementsByTagName('input')[0], 'change')");
                browser.ExecuteJavascript("fireEvent(document.getElementsByTagName('input')[1], 'change')");
                browser.ExecuteJavascript("document.getElementsByTagName('button')[0].click()");
                errortimer.Start();
            }
            else if (browser.Source.ToString().StartsWith(SpotRedirectUri))
            {
                errortimer.Stop();
                WebCore.Shutdown();
            }
        }
    }

    public class SpotiScope
    {
        private SpotiScope(string scopeValue) { Value = scopeValue; }
        public string Value { get; }

        public static SpotiScope All { get { return new SpotiScope("playlist-read-private playlist-read-collaborative playlist-modify-public playlist-modify-private streaming ugc-image-upload user-follow-modify user-follow-read user-library-read user-library-modify user-read-private user-read-birthdate user-read-email user-top-read"); } }
        public static SpotiScope None { get { return new SpotiScope(""); } }
        public static SpotiScope ReadAllPlaylist { get { return new SpotiScope("playlist-read-private playlist-read-collaborative"); } }
        public static SpotiScope ReadModifyAllPlaylist { get { return new SpotiScope("playlist-read-private playlist-read-collaborative playlist-modify-public playlist-modify-private"); } }
        public static SpotiScope ReadAllUser { get { return new SpotiScope("user-follow-read user-library-read user-read-private user-read-birthdate user-read-email user-top-read"); } }
        public static SpotiScope ReadModifyAllUser { get { return new SpotiScope("user-follow-modify user-follow-read user-library-read user-library-modify user-read-private user-read-birthdate user-read-email user-top-read"); } }
        public static SpotiScope ReadAllUserAndPlaylist { get { return new SpotiScope("playlist-read-private playlist-read-collaborative user-follow-read user-library-read user-read-private user-read-birthdate user-read-email user-top-read"); } }
        public static SpotiScope ReadUserTracksAndPlaylist{ get { return new SpotiScope("playlist-read-private playlist-read-collaborative user-library-read"); } }
        public static SpotiScope ReadModifyAllUserAndPlaylist { get { return new SpotiScope("playlist-read-private playlist-read-collaborative playlist-modify-public playlist-modify-private user-follow-modify user-follow-read user-library-read user-library-modify user-read-private user-read-birthdate user-read-email user-top-read"); } }
        public static SpotiScope Streaming { get { return new SpotiScope("streaming"); } }
    }
}
