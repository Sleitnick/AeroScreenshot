using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using System.IO;
using System.Net;

namespace SimpleTweet {

    /// <summary>
    /// Simple twitter client that allows sending tweets along with media.
    /// </summary>
    public class TwitterClient {
        
        private static readonly string TWEET_URL = "statuses/update.json";
        private static readonly string UPLOAD_MEDIA_URL = "media/upload.json";

        private RestClient twitterApiClient;
        private RestClient twitterUploadClient;

        /// <summary>
        /// Create the TwitterClient object
        /// </summary>
        /// <param name="auth">OAuth1 authentication for the client</param>
        public TwitterClient(OAuth auth) {
            twitterApiClient = new RestClient("https://api.twitter.com/1.1");
            twitterUploadClient = new RestClient("https://upload.twitter.com/1.1");
            IAuthenticator authenticator = OAuth1Authenticator.ForProtectedResource(
                auth.consumerKey,
                auth.consumerSecret,
                auth.accessToken,
                auth.accessTokenSecret
            );
            twitterApiClient.Authenticator = authenticator;
            twitterUploadClient.Authenticator = authenticator;
        }

        private TwitterResponse SendTweetMaster(string message, string mediaIds = null, string replyToId = null) {
            RestRequest tweetRequest = new RestRequest(TWEET_URL, Method.POST);
            tweetRequest.AddQueryParameter("status", message);
            if (mediaIds != null) {
                tweetRequest.AddQueryParameter("media_ids", mediaIds);
            }
            if (replyToId != null) {
                tweetRequest.AddQueryParameter("in_reply_to_status_id", replyToId);
            }
            IRestResponse response = twitterApiClient.Execute(tweetRequest);
            TwitterResponse tweetResponse = new TwitterResponse();
            tweetResponse.StatusCode = response.StatusCode;
            if (response.StatusCode != HttpStatusCode.OK) {
                tweetResponse.Failed = true;
                tweetResponse.FailedMessage = "Failed to post tweet";
            }
            return tweetResponse;
        }

        /// <summary>
        /// Send a tweet.
        /// </summary>
        /// <param name="message">The text to be tweeted.</param>
        /// <param name="replyToId">Optional ID of a tweet in which to reply.</param>
        /// <returns>The response of the request sent.</returns>
        public TwitterResponse SendTweet(string message, string replyToId = null) {
            return SendTweetMaster(message, null, null);
        }

        /// <summary>
        /// Send a tweet with media.
        /// </summary>
        /// <param name="message">The text to be tweeted.</param>
        /// <param name="filePath">The file path in which to send</param>
        /// <param name="replyToId">Optional ID of a tweet in which to reply.</param>
        /// <returns>The response of the request sent.</returns>
        public TwitterResponse SendTweetWithMedia(string message, string filePath, string replyToId = null) {
            TwitterResponse tweetResponse;
            RestRequest uploadMediaRequest = new RestRequest(UPLOAD_MEDIA_URL, Method.POST);
            uploadMediaRequest.AlwaysMultipartFormData = true;
            uploadMediaRequest.AddFile("media", File.ReadAllBytes(filePath), filePath, "appliation/octet-stream");
            IRestResponse uploadMediaResponse = twitterUploadClient.Execute(uploadMediaRequest);
            if (uploadMediaResponse.StatusCode == HttpStatusCode.OK) {
                dynamic content = SimpleJson.DeserializeObject(uploadMediaResponse.Content);
                string mediaId = content.media_id_string;
                tweetResponse = SendTweetMaster(message, mediaId, replyToId);
            } else {
                tweetResponse = new TwitterResponse();
                tweetResponse.Failed = true;
                tweetResponse.FailedMessage = "Failed to upload media";
                tweetResponse.StatusCode = uploadMediaResponse.StatusCode;
            }
            return tweetResponse;
        }

    }

    /// <summary>
    /// OAuth1 object.Be careful if this
    /// object is being stored, since it gives hightened access to the
    /// application and user in reference to the consumer and access properties.
    /// </summary>
    public class OAuth {

        internal string consumerKey, consumerSecret;
        internal string accessToken, accessTokenSecret;

        /// <summary>
        /// ConsumerKey for the application.
        /// </summary>
        public string ConsumerKey {
            set {
                consumerKey = value;
            }
        }

        /// <summary>
        /// ConsumerSecret for the application.
        /// </summary>
        public string ConsumerSecret {
            set {
                consumerSecret = value;
            }
        }

        /// <summary>
        /// AccessToken for the client.
        /// </summary>
        public string AccessToken {
            set {
                accessToken = value;
            }
        }

        /// <summary>
        /// AccessTokenSecret for the client.
        /// </summary>
        public string AccessTokenSecret {
            set {
                accessTokenSecret = value;
            }
        }

        /// <summary>
        /// Creates an OAuth object for OAuth1 authentication. Be careful if this
        /// object is being stored, since it gives hightened access to the
        /// application and user in reference to the consumer and access properties.
        /// </summary>
        /// <param name="consumerKey">Consumer Key.</param>
        /// <param name="consumerSecret">Consumer Secret.</param>
        /// <param name="accessToken">Access Token.</param>
        /// <param name="accessTokenSecret">Access Token Secret.</param>
        public OAuth(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret) {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            AccessToken = accessToken;
            AccessTokenSecret = accessTokenSecret;
        }

    }

    public class TwitterResponse {

        public bool Failed = false;
        public string FailedMessage = String.Empty;
        public HttpStatusCode StatusCode = HttpStatusCode.OK;

        public TwitterResponse() {

        }

    }

}
