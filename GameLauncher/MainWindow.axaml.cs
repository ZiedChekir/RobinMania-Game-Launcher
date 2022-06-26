using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace GameLauncher
{
    public partial class MainWindow : Window
    {
        public enum LauncherStatus
        {
            ready,
            downloading_game,
            downloading_updates,
            failed,
        }
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private string nftJson;
        private string nftDirectory;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloading_game:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloading_updates:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
            
        }

        
        public MainWindow()
        {
            InitializeComponent();

            
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Pirate Game.exe");
            nftJson = Path.Combine(rootPath, "nfts.json");
            nftDirectory = Path.Combine(rootPath, "nfts");

            //hashtest();

            //CheckForUpdates();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            hashtest();
        }
        private void hashtest()
        {

            string p = Path.Combine(rootPath, "logonew.png");
            uint x = 0;
            for (int i = 0; i < 100000; i++)
            {
                 x = MurmurHash3_x86_32(File.ReadAllBytes(p), 10, 2);

            }

            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", x.ToString()) ;
            messageBoxStandardWindow.Show();

        }
        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1R3GT_VINzmNoXKtvnvuJw6C86-k3Jr5s"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error checking for game updates: {ex}");
                    messageBoxStandardWindow.Show();
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }


        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloading_updates;
                }
                else
                {
                    Status = LauncherStatus.downloading_game;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1R3GT_VINzmNoXKtvnvuJw6C86-k3Jr5s"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1SNA_3P5wVp4tZi5NKhiGAAD6q4ilbaaf"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error installing game files: {ex}");
                messageBoxStandardWindow.Show();
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;


                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error finishing download: {ex}");
                messageBoxStandardWindow.Show();
            }
        }
        private void CheckForNftsUpdate()
        {
            if (File.Exists(nftJson))
            {
                //download the nftjson from server 
                //compare it to this one ( hash every nft and compare it to the downloaded one ) 
                //get list of nfts to download 
                //send them to server 
                //server zips them and sends them  
                //launcher download the nfts 
                //unzip in nftDirectory
                //ready to play
            }
            else
            {
                //download all the nfts 
                //unzip them
                //ready to play 
                DownloadNfts(true);
            }
        }
        private void DownloadNfts(bool donwloadAllNfts)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
        static public uint MurmurHash3_x86_32(byte[] data, uint length, uint seed)
        {
            uint nblocks = length >> 2;

            uint h1 = seed;

            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            //----------
            // body

            int i = 0;

            for (uint j = nblocks; j > 0; --j)
            {
                uint k1l = BitConverter.ToUInt32(data, i);

                k1l *= c1;
                k1l = rotl32(k1l, 15);
                k1l *= c2;

                h1 ^= k1l;
                h1 = rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;

                i += 4;
            }

            //----------
            // tail

            nblocks <<= 2;

            uint k1 = 0;

            uint tailLength = length & 3;

            if (tailLength == 3)
                k1 ^= (uint)data[2 + nblocks] << 16;
            if (tailLength >= 2)
                k1 ^= (uint)data[1 + nblocks] << 8;
            if (tailLength >= 1)
            {
                k1 ^= data[nblocks];
                k1 *= c1; k1 = rotl32(k1, 15); k1 *= c2; h1 ^= k1;
            }

            //----------
            // finalization

            h1 ^= length;

            h1 = fmix32(h1);

            return h1;
        }
        static uint fmix32(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }

        static uint rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
    struct NftsJson
    {
        //dictionary 
        //compare name and hash function
        //constructor create nftsJson from a string of Json data
        //Json data nfts name => hash
        //nfts not found list to send to the server 
      
    }
        struct Version
        {
            internal static Version zero = new Version(0, 0, 0);

            private short major;
            private short minor;
            private short subMinor;

            internal Version(short _major, short _minor, short _subMinor)
            {
                major = _major;
                minor = _minor;
                subMinor = _subMinor;
            }
            internal Version(string _version)
            {
                string[] versionStrings = _version.Split('.');
                if (versionStrings.Length != 3)
                {
                    major = 0;
                    minor = 0;
                    subMinor = 0;
                    return;
                }

                major = short.Parse(versionStrings[0]);
                minor = short.Parse(versionStrings[1]);
                subMinor = short.Parse(versionStrings[2]);
            }

            internal bool IsDifferentThan(Version _otherVersion)
            {
                if (major != _otherVersion.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVersion.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (subMinor != _otherVersion.subMinor)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{subMinor}";
            }
        }
    }
