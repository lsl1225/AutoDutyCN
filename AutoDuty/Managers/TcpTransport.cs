using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDuty.Managers
{
    public class TcpTransport : ITransport
    {
        private readonly IPAddress address;
        private readonly int port;
        private TcpListener? listener;

        public TcpTransport(string address, int port)
        {
            this.address = IPAddress.Parse(address);
            this.port = port;
        }
        public TcpTransport(int port)
        {
            this.address = IPAddress.Any;
            this.port = port;
        }

        public void StartServer(int backlog = 5)
        {
            if (listener != null) return;
            listener = new TcpListener(address, port);
            listener.Start(backlog);
        }

        public void StopServer()
        {
            try
            {
                listener?.Stop();
            }
            catch { }
            listener = null;
        }

        public async Task<Stream> AcceptConnectionAsync(CancellationToken ct)
        {
            if (listener == null) throw new InvalidOperationException("Listener not started");
            TcpClient client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            client.NoDelay = true;
            return client.GetStream();
        }

        public async Task<Stream> ConnectToServerAsync(CancellationToken ct)
        {
            TcpClient client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var connectTask = client.ConnectAsync(address, port);
            using (cts.Token.Register(() => { try { client.Close(); } catch { } }))
            {
                await connectTask.ConfigureAwait(false);
            }
            client.NoDelay = true;
            return client.GetStream();
        }

        public void Dispose()
        {
            StopServer();
        }
    }
}
