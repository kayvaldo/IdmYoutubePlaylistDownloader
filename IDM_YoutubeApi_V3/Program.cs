using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDM_YoutubeApi_V3
{
    class Program
    {
        public static string playlistURL;
        public static string videoFormat;
        public static int videoResolution;
        public static List<StringBuilder> playlistIDs;
        public static List<StringBuilder> formattedURLs;
        public static List<StringBuilder> videoIds;
        public static List<string> downloadLinks;
        public static List<string> videoTitles;

        static void Main(string[] args)
        {
            //Controller.Presets();
            videoTitles = new List<string>();
            playlistIDs = new List<StringBuilder>();
            string FullUrl = "https://www.youtube.com/playlist?list=PLQl1RunyypKfShqUmuKA72QK1TVQ01VHz";
            string playlistURL = FullUrl.Substring(FullUrl.IndexOf("=") + 1);
            new Controller().Run(playlistURL, playlistIDs, videoTitles).Wait();
            formattedURLs = Controller.reconstructUrls(playlistIDs);
            downloadLinks = Controller.getDownloadLinks(formattedURLs, "mp4", 240, videoTitles);
             Controller.transferToIDM(downloadLinks, playlistURL);

            //ArrayController.Presets();
            //ArrayController.getPlaylistIDs(playlistURL);

            Console.ReadKey();
        }
    }
}
