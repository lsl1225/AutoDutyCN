using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDuty.Managers
{
    public class NamedPipeTransport : ITransport
    {
        private readonly string pipeName;
        private readonly string serverName;
        private readonly Queue<NamedPipeServerStream> availablePipes = new();
        private bool isRunning = false;
        private int maxInstances;

        public NamedPipeTransport(string pipeName, string serverName = ".")
        {
            this.pipeName = pipeName;
            this.serverName = serverName;
        }

        public void StartServer(int backlog = 3)
        {
            if (isRunning) return;
            isRunning = true;
            maxInstances = backlog;
            
            // Pre-create the first pipe instance
            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            availablePipes.Enqueue(pipe);
        }

        public void StopServer()
        {
            isRunning = false;
            try
            {
                while (availablePipes.Count > 0)
                {
                    var pipe = availablePipes.Dequeue();
                    pipe?.Close();
                    pipe?.Dispose();
                }
            }
            catch { }
        }

        public async Task<Stream> AcceptConnectionAsync(CancellationToken ct)
        {
            if (!isRunning) throw new InvalidOperationException("Server not started");
            
            // Get or create a pipe to wait on
            NamedPipeServerStream? pipeToWaitOn = null;
            lock (availablePipes)
            {
                if (availablePipes.Count > 0)
                {
                    pipeToWaitOn = availablePipes.Dequeue();
                }
            }
            
            if (pipeToWaitOn == null)
            {
                pipeToWaitOn = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            }
            
            await pipeToWaitOn.WaitForConnectionAsync(ct);
            
            // Create a new pipe for the next connection and queue it
            var nextPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            lock (availablePipes)
            {
                availablePipes.Enqueue(nextPipe);
            }
            
            return pipeToWaitOn;
        }

        public async Task<Stream> ConnectToServerAsync(CancellationToken ct)
        {
            // For named pipes, we use the configured serverName and pipeName
            NamedPipeClientStream client = new(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            
            var connectTask = client.ConnectAsync(ct);
            using (ct.Register(() => { try { client.Close(); } catch { } }))
            {
                await connectTask.ConfigureAwait(false);
            }
            
            return client;
        }

        public void Dispose()
        {
            StopServer();
        }
    }
}
