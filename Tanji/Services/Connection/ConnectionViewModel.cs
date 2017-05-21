using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

using Microsoft.Win32;

using Tanji.Helpers;

using Tangine.Habbo;
using Tangine.Crypto;
using Tangine.Network;

using Eavesdrop;

using Flazzy;

namespace Tanji.Services.Connection
{
    public class ConnectionViewModel : ObservableObject, IReceiver, IHaltable
    {
        #region Status Constants
        private const string STANDING_BY = "Standing By...";

        private const string INTERCEPTING_CLIENT = "Intercepting Client...";
        private const string INTERCEPTING_CONNECTION = "Intercepting Connection...";
        private const string INTERCEPTING_CLIENT_PAGE_DATA = "Intercepting Client Page...";

        private const string MODIFYING_CLIENT = "Modifying Client...";
        private const string INJECTING_CLIENT = "Injecting Client...";
        private const string GENERATING_MESSAGE_HASHES = "Generating Message Hashes...";

        private const string ASSEMBLING_CLIENT = "Assembling Client...";
        private const string DISASSEMBLING_CLIENT = "Disassembling Client...";

        private const string SYNCHRONIZING_GAME = "Synchronizing Game...";
        private const string SYNCHRONIZING_GAME_DATA = "Synchronizing Game Data...";
        #endregion

        private readonly OpenFileDialog _openClientDialog;
        private readonly SaveFileDialog _saveCertificateDialog;

        public bool IsConnecting
        {
            get { return (Status != STANDING_BY); }
        }

        private string _status = STANDING_BY;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaiseOnPropertyChanged();
                RaiseOnPropertyChanged(nameof(IsConnecting));
            }
        }

        public ushort _proxyPort = 8282;
        public ushort ProxyPort
        {
            get { return _proxyPort; }
            set
            {
                _proxyPort = value;
                RaiseOnPropertyChanged();
            }
        }

        private string _customClientPath = string.Empty;
        public string CustomClientPath
        {
            get { return _customClientPath; }
            set
            {
                _customClientPath = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isAutomaticServerExtraction = true;
        public bool IsAutomaticServerExtraction
        {
            get { return _isAutomaticServerExtraction; }
            set
            {
                _isAutomaticServerExtraction = value;
                RaiseOnPropertyChanged();
            }
        }

        private HotelEndPoint _hotelServer = null;
        public HotelEndPoint HotelServer
        {
            get { return _hotelServer; }
            set
            {
                _hotelServer = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command BrowseCommand { get; }
        public Command CancelCommand { get; }
        public Command ConnectCommand { get; }

        public Command DestroyCertificatesCommand { get; }
        public Command ExportCertificateAuthorityCommand { get; }

        public ConnectionViewModel()
        {
            _saveCertificateDialog = new SaveFileDialog();
            _saveCertificateDialog.DefaultExt = "cer";
            _saveCertificateDialog.FileName = "Eavesdrop Authority";
            _saveCertificateDialog.Title = "Tanji - Export Root Certificate";
            _saveCertificateDialog.Filter = "X.509 Certificate (*.cer, *.crt)|*.cer;*.crt";

            _openClientDialog = new OpenFileDialog();
            _openClientDialog.Title = "Tanji - Select Custom Client";
            _openClientDialog.Filter = "Shockwave Flash File (*.swf)|*.swf";

            BrowseCommand = new Command(Browse);
            CancelCommand = new Command(Cancel);
            ConnectCommand = new Command(Connect);
            DestroyCertificatesCommand = new Command(DestroyCertificates);
            ExportCertificateAuthorityCommand = new Command(ExportCertificateAuthority);
        }

        private void InjectGameClient(object sender, RequestInterceptedEventArgs e)
        {
            if (e.Request.RequestUri.Query.EndsWith("-Tanji"))
            {
                Eavesdropper.RequestIntercepted -= InjectGameClient;

                Uri remoteUrl = e.Request.RequestUri;
                string clientPath = Path.GetFullPath($"Modified Clients/{remoteUrl.Host}/{remoteUrl.LocalPath}");
                if (!string.IsNullOrWhiteSpace(CustomClientPath))
                {
                    clientPath = CustomClientPath;
                }
                if (!File.Exists(clientPath))
                {
                    Status = INTERCEPTING_CLIENT;
                    Eavesdropper.ResponseIntercepted += InterceptGameClient;
                }
                else
                {
                    Status = DISASSEMBLING_CLIENT;
                    App.Master.Game = new HGame(clientPath);
                    App.Master.Game.Disassemble();

                    if (App.Master.Game.IsPostShuffle)
                    {
                        Status = GENERATING_MESSAGE_HASHES;
                        App.Master.Game.GenerateMessageHashes();
                    }

                    Status = SYNCHRONIZING_GAME;
                    App.Master.Synchronize(App.Master.Game);

                    TerminateProxy();
                    InterceptConnection();
                    e.Request = WebRequest.Create(new Uri(clientPath));
                }
            }
        }
        private void InterceptGameClient(object sender, ResponseInterceptedEventArgs e)
        {
            if (!e.Response.ResponseUri.Query.EndsWith("-Tanji")) return;
            if (e.Response.ContentType != "application/x-shockwave-flash") return;
            Eavesdropper.ResponseIntercepted -= InterceptGameClient;

            Uri remoteUrl = e.Response.ResponseUri;
            string clientPath = Path.GetFullPath($"Modified Clients/{remoteUrl.Host}/{remoteUrl.LocalPath}");

            string clientDirectory = Path.GetDirectoryName(clientPath);
            Directory.CreateDirectory(clientDirectory);

            Status = DISASSEMBLING_CLIENT;
            App.Master.Game = new HGame(e.Payload);
            App.Master.Game.Disassemble();

            if (App.Master.Game.IsPostShuffle)
            {
                Status = GENERATING_MESSAGE_HASHES;
                App.Master.Game.GenerateMessageHashes();
            }

            Status = MODIFYING_CLIENT;
            if (App.Master.Game.IsPostShuffle)
            {
                App.Master.Game.DisableHostChecks();
                App.Master.Game.InjectKeyShouter(4001);
            }
            if (App.Master.GameData.Hotel == HHotel.Unknown)
            {
                App.Master.Game.InjectEndPoint("127.0.0.1", HotelServer.Port);
            }

            Status = SYNCHRONIZING_GAME;
            App.Master.Synchronize(App.Master.Game);

            CompressionKind compression = CompressionKind.ZLIB;
#if DEBUG
            compression = CompressionKind.None;
#endif

            Status = ASSEMBLING_CLIENT;
            e.Payload = App.Master.Game.ToArray(compression);
            File.WriteAllBytes(clientPath, e.Payload);

            TerminateProxy();
            InterceptConnection();
        }
        private void InterceptClientPage(object sender, ResponseInterceptedEventArgs e)
        {
            if (!e.Response.ContentType.Contains("text/html")) return;
            string body = Encoding.UTF8.GetString(e.Payload);
            if (!body.Contains("info.host") && !body.Contains("info.port")) return;

            Eavesdropper.ResponseIntercepted -= InterceptClientPage;
            App.Master.GameData.Source = body;

            if (IsAutomaticServerExtraction)
            {
                string[] ports = App.Master.GameData.InfoPort.Split(',');

                if (ports.Length == 0 ||
                    !ushort.TryParse(ports[0], out ushort port) ||
                    !HotelEndPoint.TryParse(App.Master.GameData.InfoHost, port, out HotelEndPoint endpoint))
                {
                    Cancel(null);
                    return;
                }

                HotelServer = endpoint;
            }

            body = body.Replace(App.Master.GameData.InfoHost, "127.0.0.1");
            body = body.Replace(".swf", $".swf?{DateTime.Now.Ticks}-Tanji");

            Status = SYNCHRONIZING_GAME_DATA;
            App.Master.Synchronize(App.Master.GameData);

            e.Payload = Encoding.UTF8.GetBytes(body);

            Status = INJECTING_CLIENT;
            Eavesdropper.RequestIntercepted += InjectGameClient;
        }

        private void TerminateProxy()
        {
            Eavesdropper.Terminate();
            Eavesdropper.RequestIntercepted -= InjectGameClient;
            Eavesdropper.ResponseIntercepted -= InterceptClientPage;
            Eavesdropper.ResponseIntercepted -= InterceptGameClient;
        }
        private void InterceptConnection()
        {
            App.Master.Connection.SocketSkip =
                (App.Master.Game.IsPostShuffle ? 2 : 0);

            Status = INTERCEPTING_CONNECTION;
            Task connectTask = App.Master.Connection.InterceptAsync(HotelServer);
        }

        private void Browse(object obj)
        {
            _openClientDialog.FileName = string.Empty;
            if (_openClientDialog.ShowDialog() ?? false)
            {
                CustomClientPath = _openClientDialog.FileName;
            }
        }
        private void Cancel(object obj)
        {
            TerminateProxy();
            App.Master.Connection.Disconnect();

            if (IsAutomaticServerExtraction)
            {
                HotelServer = null;
            }
            Status = STANDING_BY;
        }
        private void Connect(object obj)
        {
            if (App.Master.Connection.IsConnected)
            {
                if (MessageBox.Show("Are you sure you want to disconnect from the current session?", "Tanji ~ Alert!",
                         MessageBoxButton.YesNo,
                         MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                {
                    App.Master.Connection.Disconnect();
                }
                else return;
            }

            if (!IsAutomaticServerExtraction && HotelServer == null)
            {
                MessageBoxResult result = MessageBox.Show("Hotel server endpoint must be provided; Would you like to attempt an automatic extraction of the endpoint instead?",
                    "Tanji - Alert!", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

                if (result == MessageBoxResult.Yes)
                {
                    IsAutomaticServerExtraction = true;
                }
                else return;
            }

            if (Eavesdropper.Certifier.CreateTrustedRootCertificate())
            {
                Eavesdropper.ResponseIntercepted += InterceptClientPage;
                Eavesdropper.Initiate(ProxyPort);

                Status = INTERCEPTING_CLIENT_PAGE_DATA;
            }
        }

        private void DestroyCertificates(object obj)
        {
            Eavesdropper.Certifier.DestroyCertificates();
        }
        private void ExportCertificateAuthority(object obj)
        {
            if (!Eavesdropper.Certifier.CreateTrustedRootCertificate()) return;
            if ((_saveCertificateDialog.ShowDialog() ?? false))
            {
                Eavesdropper.Certifier
                    .ExportTrustedRootCertificate(_saveCertificateDialog.FileName);
            }
        }

        #region IHaltable Implementation
        public void Halt()
        {
            Cancel(null);
        }
        public void Restore()
        {
            IsReceiving = true;
            Status = STANDING_BY;
        }
        #endregion
        #region IReceiver Implementation
        public bool IsReceiving { get; private set; }
        public void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (e.Packet.Id == 4001)
            {
                string sharedKeyHex = e.Packet.ReadUTF8();
                if (sharedKeyHex.Length % 2 != 0)
                {
                    sharedKeyHex = ("0" + sharedKeyHex);
                }

                byte[] sharedKey = Enumerable.Range(0, sharedKeyHex.Length / 2)
                    .Select(x => Convert.ToByte(sharedKeyHex.Substring(x * 2, 2), 16))
                    .ToArray();

                App.Master.Connection.Remote.Encrypter = new RC4(sharedKey);
                App.Master.Connection.Remote.IsEncrypting = true;

                e.IsBlocked = true;
                IsReceiving = false;
            }
            else if (e.Step >= 10)
            {
                IsReceiving = false;
            }
        }
        public void HandleIncoming(DataInterceptedEventArgs e)
        { }
        #endregion
    }
}