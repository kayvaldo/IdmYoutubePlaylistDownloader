using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YoutubeExtractor;
using IDManLib;
using System.Net;

namespace IDM_YoutubeApi_V3
{
    public class Controller
    {
        internal static void Presets()
        {
            //Project ID: idmyoutubedownloader-999 Project Number: 763181940381
            var projectId = "idmyoutubedownloader-999";
            var projectNumber = 763181940381;
            var apiKey = "AIzaSyCAUxoRnvkKrQfsOtZZuojXzbwcgta0RGU";
        }

        internal static List<StringBuilder> getPlaylistIDs(string playlistUrl, List<string> videoTitles)
        {
            List<StringBuilder> videoIds = new List<StringBuilder>();
            return videoIds;
        }

        internal static List<StringBuilder> reconstructUrls(List<StringBuilder> playlistIDs)
        {
            StringBuilder reconstructedUrl;
            List<StringBuilder> restructuredUrls = new List<StringBuilder>();
            foreach (var item in playlistIDs)
            {
                reconstructedUrl = new StringBuilder("http://www.youtube.com/watch?v=" + item.ToString());
                restructuredUrls.Add(reconstructedUrl);
                Console.WriteLine(reconstructedUrl);
            }

            return restructuredUrls;
        }

        internal static List<string> getDownloadLinks(List<StringBuilder> reconstructedUrls, string videoFormat, int resolution, List<string> videoTitles)
        {
            List<string> downloadUrls = new List<string>();
            videoTitles.Capacity = 100;
            VideoType videotype = getVideoType(videoFormat);

            for (int i = 0; i < reconstructedUrls.Count; i++)
            {
                try
                {
                    var url = reconstructedUrls[i].ToString();
                    if (videoFormat.Equals("mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        // http://www.youtubeinmp3.com/fetch/?format=text&video=http://www.youtube.com/watch?v=i62Zjga8JOM
                        var requestUrl = " http://www.youtubeinmp3.com/fetch/?format=text&video=" + url;
                        WebRequest wrGETURL;
                        wrGETURL = WebRequest.Create(requestUrl);

                        //WebProxy myProxy = new WebProxy("myproxy", 80);
                        //myProxy.BypassProxyOnLocal = true;

                        //wrGETURL.Proxy = WebProxy.GetDefaultProxy();

                        Stream objStream;
                        objStream = wrGETURL.GetResponse().GetResponseStream();

                        StreamReader objReader = new StreamReader(objStream);

                        string sLine = "";

                        string response = objReader.ReadToEnd();
                        var mp3Url = response.Substring(response.IndexOf("=http://www.youtube.com"));


                        downloadUrls.Add(mp3Url);
                        Console.WriteLine(mp3Url);
                        //Select only one entry that matches the video type and resolution
                        break;

                    }
                    else
                    {
                        IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);

                        foreach (var video in videoInfos)
                        {
                            if ((video.Resolution == resolution) && (video.VideoType == videotype))
                            {
                                downloadUrls.Add(video.DownloadUrl + "&title=" + videoTitles[i]);
                                Console.WriteLine(video.DownloadUrl);
                                //Select only one entry that matches the video type and resolution
                                break; 
                            }

                        }
                    }


                }
                catch (Exception)
                {
                    continue;
                }


            }

            Console.WriteLine("--------------------------------------Download Urls--------------------------------------------");

            foreach (var item in downloadUrls)
            {
                Console.WriteLine(item.ToString());
            }

            return downloadUrls;
        }

        internal static void transferToIDM(List<string> downloadLinks, string playlistUrl)
        {
            string[,] downloadMatrix = new string[downloadLinks.Count, 4];
            for (int i = 0; i < downloadLinks.Count; i++)
            {
                downloadMatrix[i, 0] = downloadLinks[i];
            }

            CIDMLinkTransmitter cidm = new CIDMLinkTransmitter();
            for (int i = 0; i < 3; i++)
            {
                cidm.SendLinksArray(playlistUrl, downloadMatrix);                
            }
        }


        public async Task Run(string playlistUrl, List<StringBuilder> videoIds, List<String> videoTitles)
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for read-only access to the authenticated 
                    // user's account, but not other types of account access.
                    new[] { YouTubeService.Scope.YoutubeReadonly },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            var channelsListRequest = youtubeService.Channels.List("contentDetails");
            //channelsListRequest.Mine = true;
            channelsListRequest.ForUsername = "Drzakirchannel";
            

            // Retrieve the contentDetails part of the channel resource for the authenticated user's channel.
            var channelsListResponse = await channelsListRequest.ExecuteAsync();

            foreach (var channel in channelsListResponse.Items)
            {
                // From the API response, extract the playlist ID that identifies the list
                // of videos uploaded to the authenticated user's channel.
                var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;
                //var uploadsListId = playlistUrl;

                Console.WriteLine("Videos in list {0}", uploadsListId);

                var nextPageToken = "";
                while (nextPageToken != null)
                {
                    var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                    playlistItemsListRequest.PlaylistId = uploadsListId;
                    playlistItemsListRequest.MaxResults = 5;
                    playlistItemsListRequest.PageToken = nextPageToken;
                   


                    // Retrieve the list of videos uploaded to the authenticated user's channel.
                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        // Print information about each video.
                        Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
                        videoIds.Add(new StringBuilder(playlistItem.Snippet.ResourceId.VideoId));
                        videoTitles.Add(playlistItem.Snippet.Title);
                        var x = youtubeService.Videos.List("snippet, contentDetails");
                        x.Id = playlistItem.Snippet.ResourceId.VideoId;
                        var y = await x.ExecuteAsync(); 
                    }

                    nextPageToken = playlistItemsListResponse.NextPageToken;
                    nextPageToken = null;
                }
            }

        }

        public static VideoType getVideoType(string videoFormat)
        {
            VideoType type;
            switch (videoFormat.ToLower())
            {
                case ("flv"):
                    type = VideoType.Flash;
                    break;

                case ("mp4"):
                    type = VideoType.Mp4;
                    break;

                case ("webm"):
                    type = VideoType.WebM;
                    break;

                case ("mobile"):
                    type = VideoType.Mobile;
                    break;

                default:
                    type = VideoType.Flash;
                    break;

            }
            return type;
        }

    }
}
