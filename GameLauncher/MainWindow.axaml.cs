using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using Standart.Hash.xxHash;

namespace GameLauncher
{
    public partial class MainWindow : Window
    {

        private bool rendered = false;

        public enum LauncherStatus
        {
            ready,
            downloading_game,
            downloading_updates,
            downloading_nfts,
            failed,
        }

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameDirectory;
        private string gameExe;
        private string nftJsonFile;
        private string nftZip;
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
                    case LauncherStatus.downloading_nfts:
                        PlayButton.Content = "Downloading NFTs";
                        break;
                    default:
                        break;
                }
            }

        }


        public MainWindow()
        {
            InitializeComponent();
            rendered = false;
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "game.zip");
            gameExe = Path.Combine(rootPath, "game", "networking.exe");
            nftJsonFile = Path.Combine(rootPath, "Nfts.json");
            nftDirectory = Path.Combine(rootPath, "nfts");
            gameDirectory = Path.Combine(rootPath, "game");
            nftZip = Path.Combine(rootPath, "Nfts.zip");
        }


        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            if (rendered)
                return;
            CheckForUpdates();
            rendered = true;
        }

        private uint hash(byte[] data,  uint seed = 0)
        {
            return xxHash32.ComputeHash(data, data.Length, seed);
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile) && Directory.Exists(gameDirectory))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(Constants.VERSION_DOWNLOAD_URI));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        CheckForNftsUpdate();
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
                    _onlineVersion = new Version(webClient.DownloadString(Constants.VERSION_DOWNLOAD_URI));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri(Constants.GAME_DOWNLOAD_URI), gameZip, _onlineVersion);
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
                ZipFile.ExtractToDirectory(gameZip, gameDirectory, true);
                File.Delete(gameZip);
                File.WriteAllText(versionFile, onlineVersion);
                VersionText.Text = onlineVersion;
                CheckForNftsUpdate();

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
            try
            {


                WebClient webClient = new WebClient();
                string onlineNftJsonData = webClient.DownloadString(Constants.NFTJSON_DOWNLOAD_URI);

                NftsJson onlineNftVersion = new NftsJson(onlineNftJsonData);
                if (Directory.Exists(nftDirectory))
                {
                    foreach (string nftFileName in onlineNftVersion.NFTJSON.Keys)
                    {
                        string nftFileNamePath = Path.Combine(nftDirectory, nftFileName);
                        if (File.Exists(nftFileNamePath))
                        {
                            uint LocalFileHash = hash(File.ReadAllBytes(nftFileNamePath));
                            onlineNftVersion.CheckHash(nftFileName, LocalFileHash.ToString());
                        }
                        else
                        {
                            onlineNftVersion.AddMissingNft(nftFileName);
                        }
                    }
                    if (onlineNftVersion.MissingNfts.Count > 0)
                    {

                        string json = JsonConvert.SerializeObject(onlineNftVersion.MissingNfts, Formatting.None);
                        DownloadNfts(false, json);

                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                else
                {
                    File.WriteAllText(nftJsonFile, onlineNftJsonData);
                    DownloadNfts(true);
                }
            }catch(Exception ex)
            {
                Status = LauncherStatus.failed;
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error NFT update: {ex}");
                messageBoxStandardWindow.Show();
            }


        }

        private void DownloadNfts(bool donwloadAllNfts, string JsonNftToDownload = null)
        {
            try
            {


                Status = LauncherStatus.downloading_nfts;
                WebClient webClient = new WebClient();
                if (donwloadAllNfts)
                {
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadNftsCompletedCallback);
                    webClient.DownloadFileAsync(new Uri(Constants.NFTS_DOWNLOAD_URI), nftZip, "123");
                }
                else
                {
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadNftsCompletedCallback);
                    webClient.DownloadFileAsync(new Uri(Constants.NFTS_DOWNLOAD_CUSTOM_URI + "/?nfts=" + JsonNftToDownload), nftZip, "1234");
                }
            }catch(Exception ex)
            {
                Status = LauncherStatus.failed;
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error NFT download: {ex}");
                messageBoxStandardWindow.Show();
            }
        }

        //private void posthttp()
        //{
        //    HttpClient client = new HttpClient();
        //    Debug.WriteLine(JsonNftToDownload);
        //    StringContent content = new StringContent(JsonNftToDownload, Encoding.UTF8, "application/json");

        //    Debug.WriteLine(content);
        //    var response = client.PostAsync(Constants.NFTS_DOWNLOAD_CUSTOM_URI, content).ConfigureAwait(false);
        //}
        private void DownloadNftsCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                ZipFile.ExtractToDirectory(nftZip, nftDirectory, true);
                File.Delete(nftZip);
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", $"Error  downloading NFTS: {ex}");
                messageBoxStandardWindow.Show();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "game");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
    }
    struct NftsJson
    {
        private Dictionary<string, string> nftjson;
        private List<string> missingNfts;

        public Dictionary<string, string> NFTJSON => nftjson;
        public List<string> MissingNfts => missingNfts;
        internal NftsJson(string data)
        {
            nftjson = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            missingNfts = new List<string>();
        }



        internal bool HashCorrect(string key, string hash)
        {
            return nftjson[key] == hash;
        }
        internal void AddMissingNft(string missingNft)
        {
            missingNfts.Add(missingNft);
        }

        internal void CheckHash(string key, string hash)
        {
            if (HashCorrect(key, hash))
                return;
            else
                AddMissingNft(key);
        }


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
