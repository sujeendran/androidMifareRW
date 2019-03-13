namespace NfcXample
{
    using System;
    using System.Linq;
    using System.Text;

    using Android.App;
    using Android.Content;
    using Android.Net.Wifi;
    using Android.Nfc;
    using Android.Nfc.Tech;
    using Android.OS;
    using Android.Util;
    using Android.Views;
    using Android.Widget;

    using Java.IO;

    [Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        /// <summary>
        /// A mime type for the the string that this app will write to the NFC tag. Will be
        /// used to help this application identify NFC tags that is has written to.
        /// </summary>
        public const string ViewApeMimeType = "application/vnd.xamarin.nfcxample";
        public static readonly string NfcAppRecord = "xamarin.nfxample";
        public static readonly string Tag = "NfcXample";

        private bool _inWriteMode;
        private bool _inReadMode;
        private NfcAdapter _nfcAdapter;
        private TextView _textView;
        private Button _writeTagButton;
        private Button _readTagButton;
        private EditText _inputText;
        String nfcdata = "SampleData";
        /**
        * The default factory key.
        */
        public static byte[] KEY_DEFAULT =
                {(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF};
        /**
            * The well-known key for tags formatted according to the
            * MIFARE Application Directory (MAD) specification.
            */
        public static byte[] KEY_MIFARE_APPLICATION_DIRECTORY =
                {(byte)0xA0,(byte)0xA1,(byte)0xA2,(byte)0xA3,(byte)0xA4,(byte)0xA5};
        /**
            * The well-known key for tags formatted according to the
            * NDEF on MIFARE Classic specification.
            */
        public static byte[] KEY_NFC_FORUM =
                {(byte)0xD3,(byte)0xF7,(byte)0xD3,(byte)0xF7,(byte)0xD3,(byte)0xF7};

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            
            // Get a reference to the default NFC adapter for this device. This adapter 
            // is how an Android application will interact with the actual hardware.
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

            _writeTagButton = FindViewById<Button>(Resource.Id.write_tag_button);
            _writeTagButton.Click += WriteTagButtonOnClick;
            _readTagButton = FindViewById<Button>(Resource.Id.read_tag_button);
            _readTagButton.Click += ReadTagButtonOnClick;
            _textView = FindViewById<TextView>(Resource.Id.text_view);
            _inputText = FindViewById<EditText>(Resource.Id.input_text);
            _inputText.TextChanged += (object sender, Android.Text.TextChangedEventArgs e) => { nfcdata = e.Text.ToString(); };
            String ssid = getWifiInfo();
            DisplayMessage(ssid);
        }

        private static string getWifiInfo()
        {
            WifiManager wifi = (WifiManager)(Application.Context.GetSystemService(Context.WifiService));
            try
            {
                if (wifi != null)
                {
                    int id = wifi.ConnectionInfo.NetworkId;
                    return wifi.ConfiguredNetworks[id].Ssid.Trim('\"');
                }
            }
            catch(Exception e)
            {
                Log.Error(Tag, "Exception: WIFI service not available");
            }
            return "No wifi connections available";
        }

        /// <summary>
        /// This method is called when an NFC tag is discovered by the application.
        /// </summary>
        /// <param name="intent"></param>
        protected override void OnNewIntent(Intent intent)
        {
            if (_inWriteMode)
            {
                _inWriteMode = false;
                var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                if (tag == null)
                {
                    return;
                }

                byte[] data = Encoding.ASCII.GetBytes(nfcdata);
                if (!TryAndWriteToTag(tag, data))
                { 
                    DisplayMessage("Card Write failed!");
                }
            }
            if(_inReadMode)
            {
                _inReadMode = false;
                var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                if (tag == null)
                {
                    return;
                }
                if ( !TryAndReadFromTag(tag))
                {
                    DisplayMessage("Card Read failed!");
                }
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            // App is paused, so no need to keep an eye out for NFC tags.
			if (_nfcAdapter != null)
            	_nfcAdapter.DisableForegroundDispatch(this);
        }

        private void DisplayMessage(string message)
        {
            _textView.Text = message;
            Log.Info(Tag, message);
        }

        /// <summary>
        /// Identify to Android that this activity wants to be notified when 
        /// an NFC tag is discovered. 
        /// </summary>
        private void EnableWriteMode()
        {
            _inWriteMode = true;

            // Create an intent filter for when an NFC tag is discovered.  When
            // the NFC tag is discovered, Android will u
            var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
            var filters = new[] { tagDetected };
            
            // When an NFC tag is detected, Android will use the PendingIntent to come back to this activity.
            // The OnNewIntent method will invoked by Android.
            var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

			if (_nfcAdapter == null) {
				var alert = new AlertDialog.Builder (this).Create ();
				alert.SetMessage ("NFC is not supported on this device.");
				alert.SetTitle ("NFC Unavailable");
				alert.SetButton ("OK", delegate {
					_writeTagButton.Enabled = false;
					_textView.Text = "NFC is not supported on this device.";
				});
				alert.Show ();
			} else
            	_nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
        }

        /// <summary>
        /// Identify to Android that this activity wants to be notified when 
        /// an NFC tag is discovered. 
        /// </summary>
        private void EnableReadMode()
        {
            _inReadMode = true;

            // Create an intent filter for when an NFC tag is discovered.  When
            // the NFC tag is discovered, Android will u
            var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
            var filters = new[] { tagDetected };

            // When an NFC tag is detected, Android will use the PendingIntent to come back to this activity.
            // The OnNewIntent method will invoked by Android.
            var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

            if (_nfcAdapter == null)
            {
                var alert = new AlertDialog.Builder(this).Create();
                alert.SetMessage("NFC is not supported on this device.");
                alert.SetTitle("NFC Unavailable");
                alert.SetButton("OK", delegate {
                    _writeTagButton.Enabled = false;
                    _textView.Text = "NFC is not supported on this device.";
                });
                alert.Show();
            }
            else
                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
        }

        private void WriteTagButtonOnClick(object sender, EventArgs eventArgs)
        {
            var view = (View)sender;
            if (view.Id == Resource.Id.write_tag_button)
            {
                DisplayMessage("Touch and hold the tag against the phone to write.");
                EnableWriteMode();
            }
        }

        private void ReadTagButtonOnClick(object sender, EventArgs e)
        {
            var view = (View)sender;
            if (view.Id == Resource.Id.read_tag_button)
            {
                DisplayMessage("Touch and hold the tag against the phone to read.");
                EnableReadMode();
            }
        }

        private bool TryAndWriteToTag(Tag tag, byte[] data)
        {
            var mfc = MifareClassic.Get(tag);
            bool auth = false;
            int secCount = 1;// mfc.SectorCount;
            int bCount = 3; 
            int bIndex = 0;
            byte[] toWrite = new byte[MifareClassic.BlockSize];

            try
            {
                mfc.Connect();
                String UID = byte2hex(mfc.Tag.GetId());
                for (int j = 1; j <= secCount; j++)
                {
                    auth = mfc.AuthenticateSectorWithKeyB(j, KEY_DEFAULT);
                    if (auth)
                    {
                        //bCount = mfc.GetBlockCountInSector(j);
                        bIndex = mfc.SectorToBlock(j);
                        for (int i = 0; i < bCount; i++)
                        {
                            for (int k = 0; k < 16; k++)
                            {
                                if (k + (i * 16) < data.Length)
                                    toWrite[k] = data[k + (i * 16)];
                                else
                                    toWrite[k] = 0;
                            }
                            mfc.WriteBlock(bIndex,toWrite);
                            bIndex++;
                        }
                        DisplayMessage("Card write success");
                        mfc.Close();
                        return true;
                    }
                    else
                    {
                        Log.Error(Tag, "Sector write authentication failed");
                        return false;
                    }
                }
            }
            catch(IOException e)
            {
                Log.Debug(Tag, e.LocalizedMessage);
            }
            return false;
        }

        private bool TryAndReadFromTag(Tag tag)
        {
            MifareClassic mfc = MifareClassic.Get(tag);
            byte[] data;
            bool auth = false;
            try
            {
                mfc.Connect();
                String UID = byte2hex(mfc.Tag.GetId());
                String cardData = null;
                int secCount = 1;
                int bCount = 3; //Just read 2 block for now
                int bIndex = 0;
                for (int j = 1; j <= secCount; j++)
                {
                    auth = mfc.AuthenticateSectorWithKeyB(j, KEY_DEFAULT);
                    if (auth)
                    {
                        //bCount = mfc.GetBlockCountInSector(j);
                        bIndex = mfc.SectorToBlock(j);
                        for (int i = 0; i < bCount; i++)
                        {
                            data = mfc.ReadBlock(bIndex);
                            cardData += Encoding.ASCII.GetString(data);
                            bIndex++;
                        }
                        DisplayMessage(cardData);
                        mfc.Close();
                        return true;
                    }
                    else
                    {
                        Log.Error(Tag, "Sector read authentication failed");
                        mfc.Close();
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(Tag, e.Message);
            }
            return false;
        }

        static string byte2hex(byte[] data)
        {
            String s = "";
            for (int i = 0; i < data.Length; i++)
            {
                s += data[i].ToString("X2");
            }
            return s;
        }

        static string byte2string(byte[] data)
        {
            String s = "";
            for (int i = 0; i < data.Length; i++)
            {
                s += (char)data[i];
            }
            return s;
        }
    }
}
