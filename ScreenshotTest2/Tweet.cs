using System;
using System.Drawing;
using System.Windows.Forms;

namespace AeroScreenshot {

    public delegate void TweetSubmittedHandler(object sender, TweetEventArgs e);

    public partial class Tweet : Form {

        /// <summary>
        /// Invoked when the user requests to submit the
        /// tweet.
        /// </summary>
        public event TweetSubmittedHandler TweetSubmitted;

        /// <summary>
        /// Open up a dialog to allow the user to type in the
        /// Tweet status message and preview the image to
        /// be sent.
        /// </summary>
        /// <param name="img">The image to preview.</param>
        public Tweet(Image img) {
            InitializeComponent();
            pictureBox1.Image = img;
        }

        private void btnSendTweet_Click(object sender, EventArgs e) {
            TweetSubmitted.Invoke(btnSendTweet, new TweetEventArgs(textBox1.Text, (Image)pictureBox1.Image.Clone()));
            Dispose();
        }

    }

    public class TweetEventArgs : EventArgs {
        private string Tweet;
        private Image Img;
        public TweetEventArgs(string tweet, Image img) {
            Tweet = tweet;
            Img = img;
        }
        public string GetTweet() {
            return Tweet;
        }
        public Image GetImage() {
            return Img;
        }
    }

}
