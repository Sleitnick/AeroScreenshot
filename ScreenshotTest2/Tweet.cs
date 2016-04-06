using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AeroScreenshot {

    public delegate void TweetSubmittedHandler(object sender, TweetEventArgs e);

    public partial class Tweet : Form {

        public event TweetSubmittedHandler TweetSubmitted;

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
