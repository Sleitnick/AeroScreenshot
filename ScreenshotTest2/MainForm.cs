using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using Twitterizer;
using System.Drawing.Printing;
using SimpleTweet;

namespace AeroScreenshot {
    public partial class MainForm : Form {

        private OAuthTokens tokens = new OAuthTokens();
        
        private string consumerKey;
        private string consumerSecret;
        private string requestToken;

        private string accessToken, accessTokenSecret;

        private Image screenshotImg;
        private Image screenshotImgOriginal;
        private float currentScreenshotScale = 1.0f;

        private Point? mouseDown;
        private Point? mouseCurrent;

        private Rectangle selection = Rectangle.Empty;

        private SolidBrush opaqueBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));

        public MainForm() {
            InitializeComponent();

            // Key shortcuts:
            KeyDown += (s, e) => {
                if (e.Control) {
                    switch (e.KeyCode) {
                        // Save:
                        case Keys.S:
                            saveToolStripMenuItem.PerformClick();
                            break;
                        // Print:
                        case Keys.P:
                            printToolStripMenuItem.PerformClick();
                            break;
                        // Twitter:
                        case Keys.T:
                            if (shareImageToolStripMenuItem.Enabled) {
                                shareImageToolStripMenuItem.PerformClick();
                            } else {
                                newLoginToolStripMenuItem.PerformClick();
                            }
                            break;
                    }
                }
            };

            // Screenshot:
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height)) {
                using (Graphics gr = Graphics.FromImage(bitmap)) {
                    gr.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                //bitmap.Save("test.png", ImageFormat.Png);
                screenshotImg = (Image)bitmap.Clone();
                screenshotImgOriginal = (Image)bitmap.Clone();
            }

            // Cropping:
            //screenshotImg = Image.FromFile("test.png");
            //screenshotImgOriginal = Image.FromFile("test.png");
            pictureBox1.Image = screenshotImg;
            ResizePictureDisplay();
            SizeChanged += (s, e) => {
                ResizePictureDisplay();
            };
            Graphics g = null;
            Timer t = new Timer();
            t.Interval = 16;
            t.Tick += (s, e) => {
                if (mouseDown != null) {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    var x = mouseDown.Value.X;
                    var y = mouseDown.Value.Y;
                    var w = (int)((float)(mouseCurrent.Value.X - x) * currentScreenshotScale);
                    var h = (int)((float)(mouseCurrent.Value.Y - y) * currentScreenshotScale);
                    x = (int)((float)x * currentScreenshotScale);
                    y = (int)((float)y * currentScreenshotScale);
                    if (w < 0) {
                        w = -w;
                        x -= w;
                    }
                    if (h < 0) {
                        h = -h;
                        y -= h;
                    }
                    int pWidth = (int)(pictureBox1.Width * currentScreenshotScale);
                    int pHeight = (int)(pictureBox1.Height * currentScreenshotScale);
                    selection = new Rectangle(x, y, w, h);
                    Rectangle top = new Rectangle(0, 0, pWidth, y);
                    Rectangle left = new Rectangle(0, y, x, h);
                    Rectangle right = new Rectangle(x + w, y, pWidth, h);
                    Rectangle bottom = new Rectangle(0, y + h, pWidth, pHeight - (y + h));
                    g.DrawImage(screenshotImgOriginal, Point.Empty);
                    g.FillRectangle(opaqueBrush, top);
                    g.FillRectangle(opaqueBrush, bottom);
                    g.FillRectangle(opaqueBrush, left);
                    g.FillRectangle(opaqueBrush, right);
                    pictureBox1.Image = screenshotImg;
                }
            };
            // Mouse moved:
            pictureBox1.MouseMove += (s, e) => {
                if (mouseDown != null) {
                    mouseCurrent = new Point(e.X, e.Y);
                }
            };
            // Mouse down:
            pictureBox1.MouseDown += (s, e) => {
                g = Graphics.FromImage(screenshotImg);
                mouseDown = new Point(e.X, e.Y);
                mouseCurrent = mouseDown;
                selection = Rectangle.Empty;
                t.Start();
            };
            // Mouse up:
            pictureBox1.MouseUp += (s, e) => {
                t.Stop();
                mouseDown = null;
                if (selection.Width < 15 && selection.Height < 15) {
                    selection = Rectangle.Empty;
                    g.DrawImage(screenshotImgOriginal, Point.Empty);
                    pictureBox1.Image = screenshotImg;
                }
            };

            // Twitter auth:
            {
                Type type = Type.GetType("AeroScreenshot.Sensitive");
                if (type != null && type.IsClass) {
                    consumerKey = Sensitive.CONSUMER_KEY;
                    consumerSecret = Sensitive.CONSUMER_SECRET;
                } else {
                    Console.WriteLine("No sensitive class");
                }
            }

            OAuthTokenResponse reqToken = OAuthUtility.GetRequestToken(
                consumerKey,
                consumerSecret,
                "oob"
            );
            requestToken = reqToken.Token;
            tokens.ConsumerKey = consumerKey;
            tokens.ConsumerSecret = consumerSecret;
            if (Properties.Settings.Default.access_token != String.Empty) {
                accessToken = Properties.Settings.Default.access_token;
                accessTokenSecret = Properties.Settings.Default.access_token_secret;
                tokens.AccessToken = accessToken;
                tokens.AccessTokenSecret = accessTokenSecret;
                shareImageToolStripMenuItem.Enabled = true;
            } else {
                shareImageToolStripMenuItem.Enabled = false;
            }

        }

        private Image GetImageFromSelection() {
            Bitmap bmp = new Bitmap(screenshotImgOriginal);
            if (selection == Rectangle.Empty) {
                return bmp;
            } else {
                if (selection.X < 0) selection.X = 0;
                if (selection.Y < 0) selection.Y = 0;
                if (selection.Right > bmp.Width) selection.Width = (bmp.Width - selection.X);
                if (selection.Bottom > bmp.Height) selection.Height = (bmp.Height - selection.Y);
                if (selection.Width < 15 || selection.Height < 15) {
                    selection = Rectangle.Empty;
                    return GetImageFromSelection();
                } else {
                    return bmp.Clone(selection, bmp.PixelFormat);
                }
            }
        }

        private void ResizePictureDisplay() {
            pictureBox1.Size = new Size(screenshotImg.Width, screenshotImg.Height);
            int imgWidth = screenshotImg.Width;
            int imgHeight = screenshotImg.Height;
            int winWidth = ClientRectangle.Width;
            int winHeight = ClientRectangle.Height;
            if (imgWidth > winWidth || imgHeight > winHeight) {
                if (imgWidth > winWidth) {
                    float imgAspect = (float)imgHeight / (float)imgWidth;
                    imgWidth = winWidth;
                    imgHeight = (int)((float)imgWidth * imgAspect);
                    currentScreenshotScale = (float)screenshotImg.Height / (float)imgHeight;
                }
                if (imgHeight > winHeight) {
                    float imgAspect = (float)imgWidth / (float)imgHeight;
                    imgHeight = winHeight;
                    imgWidth = (int)((float)imgHeight * imgAspect);
                    currentScreenshotScale = (float)screenshotImg.Width / (float)imgWidth;
                }
                pictureBox1.Size = new Size(imgWidth, imgHeight);
            } else {
                pictureBox1.Size = new Size(screenshotImg.Width, screenshotImg.Height);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            new AboutBox().Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            Image img = GetImageFromSelection();
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.CheckPathExists = true;
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "png";
            saveDialog.Filter = "PNG (*.png) | *.png | JPEG (*.jpg, *.jpeg) | *.jpg; *.jpeg | BMP (*.bmp) | *.bmp";
            saveDialog.FileOk += (s, arg) => {
                string fileName = saveDialog.FileName;
                string extension = Path.GetExtension(fileName).ToLower();
                Console.WriteLine(extension);
                ImageFormat imgFormat;
                switch(extension) {
                    case ".bmp":
                        imgFormat = ImageFormat.Bmp;
                        break;
                    case ".jpg":
                    case ".jpeg":
                        imgFormat = ImageFormat.Jpeg;
                        break;
                    case ".png":
                    default:
                        imgFormat = ImageFormat.Png;
                        break;
                }
                img.Save(fileName, imgFormat);
                System.Diagnostics.Process.Start(fileName);
            };
            saveDialog.ShowDialog(this);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e) {
            PrintDialog printDialog = new PrintDialog();
            PrintDocument pd = new PrintDocument();
            printDialog.Document = pd;
            pd.PrintPage += (s, args) => {
                Image i = GetImageFromSelection();
                Rectangle m = args.MarginBounds;
                pd.DefaultPageSettings.Landscape = (i.Width > i.Height);
                if ((double)i.Width / (double)i.Height > (double)m.Width / (double)m.Height) {
                    m.Height = (int)((double)i.Height / (double)i.Width * (double)m.Width);
                } else {
                    m.Width = (int)((double)i.Width / (double)i.Height * (double)m.Height);
                }
                args.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                args.Graphics.DrawImage(i, m);
            };
            DialogResult result = printDialog.ShowDialog(this);
            if (result == DialogResult.OK) {
                pd.Print();
            }
        }

        private void newLoginToolStripMenuItem_Click(object sender, EventArgs e) {

            TwitterPinInput pinInput = new TwitterPinInput();
            pinInput.PinSubmitted += (s, pinEvent) => {
                int pin = pinEvent.GetPin();
                pinInput.Close();
                pinInput.Dispose();
                OAuthTokenResponse accessResponse = OAuthUtility.GetAccessToken(consumerKey, consumerSecret, requestToken, pin.ToString());
                accessToken = accessResponse.Token;
                accessTokenSecret = accessResponse.TokenSecret;
                tokens.AccessToken = accessToken;
                tokens.AccessTokenSecret = accessTokenSecret;
                Properties.Settings.Default.access_token = accessToken;
                Properties.Settings.Default.access_token_secret = accessTokenSecret;
                Properties.Settings.Default.Save();
                shareImageToolStripMenuItem.Enabled = true;
            };
            pinInput.Show();

            System.Diagnostics.Process p = System.Diagnostics.Process.Start(String.Format("https://api.twitter.com/oauth/authorize?oauth_token={0}", requestToken));

        }

        private void shareImageToolStripMenuItem_Click(object sender, EventArgs e) {
            Tweet tweet = new Tweet(GetImageFromSelection());
            tweet.TweetSubmitted += (s, args) => {
                string msg = args.GetTweet();
                Image img = args.GetImage();
                string fileName = Path.GetTempFileName();
                img.Save(fileName, ImageFormat.Png);
                TwitterClient twitterClient = new TwitterClient(new OAuth(
                    consumerKey,
                    consumerSecret,
                    accessToken,
                    accessTokenSecret
                ));
                TwitterResponse tweetResponse = twitterClient.SendTweetWithMedia(msg, fileName);
                if (tweetResponse.Failed) {
                    Console.WriteLine("Failed to post tweet");
                    Console.Write(tweetResponse.FailedMessage);
                } else {
                    Console.WriteLine("Successfully posted tweet and media");
                }
            };
            tweet.Show(this);
        }

    }

}
