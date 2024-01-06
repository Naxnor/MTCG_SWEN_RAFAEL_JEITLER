using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MTCG.Server
{
    public sealed class HttpSvr
    {
        private TcpListener? _Listener;
        public event IncomingEventHandler? Incoming;
        public bool Active { get; set; } = false;

        public void Run()
        {
            if(Active) return;

            Active = true;
            _Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 10001);
            _Listener.Start();

            while(Active) 
            {
                TcpClient client = _Listener.AcceptTcpClient();

                // Handle each client in a new thread
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }

            _Listener.Stop();
        }

        private void HandleClient(TcpClient client)
        {
            byte[] buf = new byte[256];
            string data = string.Empty;

            try
            {
                while(client.GetStream().DataAvailable || (string.IsNullOrEmpty(data)))
                {
                    int n = client.GetStream().Read(buf, 0, buf.Length);
                    data += Encoding.ASCII.GetString(buf, 0, n);
                }

                Incoming?.Invoke(this, new HttpSvrEventArgs(client, data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
                // Handle exceptions or log errors
            }
            finally
            {
                client.Close(); // Ensure the client is closed after handling
            }
        }

        public void Stop()
        {
            Active = false;
        }
    }
}