using System;
using System.Windows.Forms;

namespace AeroScreenshot {

    public delegate void PinSubmittedHandler(object sender, PinEventArgs e);

    public partial class TwitterPinInput : Form {

        public event PinSubmittedHandler PinSubmitted;

        public TwitterPinInput() {
            InitializeComponent();
            pinTextBox.TextChanged += PinTextBox_TextChanged;
            btnSubmit.Enabled = false;
        }

        private void PinTextBox_TextChanged(object sender, EventArgs e) {
            btnSubmit.Enabled = pinTextBox.MaskCompleted;
        }

        private void btnSubmit_Click(object sender, EventArgs e) {
            PinSubmitted.Invoke(btnSubmit, new PinEventArgs(Int32.Parse(pinTextBox.Text)));
        }

    }

    public class PinEventArgs : EventArgs {
        private int Pin;
        public PinEventArgs(int pin) {
            Pin = pin;
        }
        public int GetPin() {
            return Pin;
        }
    }
}
