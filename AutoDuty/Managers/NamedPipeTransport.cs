using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommons;

namespace AutoDuty.Managers
{
    public class NamedPipeTransport : ITransport
    {
        private readonly string pipeName;
        private readonly string serverName;
        private readonly List<NamedPipeServerStream> availablePipes = [];
        private readonly List<Task> connectTasks = [];
        private readonly List<NamedPipeServerStream> usedPipes = [];
        private CancellationTokenSource? cts;
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
            cts = new CancellationTokenSource();
            
            for (int i = 0; i < backlog; i++)
            {
                var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                availablePipes.Add(pipe);
                connectTasks.Add(pipe.WaitForConnectionAsync(cts.Token));
            }
        }

        public void StopServer()
        {
            isRunning = false;
            cts?.Cancel();
            Task.WaitAll(connectTasks);
            connectTasks.Clear();
            availablePipes.ForEach(pipe => pipe?.Dispose());
            availablePipes.Clear();
            usedPipes.ForEach(pipe => pipe?.Dispose());
            usedPipes.Clear();
            cts?.Dispose();
            cts = null;
        }

        public async Task<Stream> AcceptConnectionAsync(CancellationToken ct)
        {
            if (!isRunning) throw new InvalidOperationException("Server not started");

            if (availablePipes.Count == 0)
            {
                await Task.Run(async () => {
                    while (usedPipes.All(pipe => pipe.IsConnected)) {
                        await Task.Delay(100, ct);
                        ct.ThrowIfCancellationRequested();
                    }
                }, cancellationToken: ct);
                ct.ThrowIfCancellationRequested();
                var closedPipes = usedPipes.Where(pipe => !pipe.IsConnected).ToList();
                closedPipes.Each(pipe =>
                { 
                    pipe.Dispose(); 
                    usedPipes.Remove(pipe);
                    var newPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    availablePipes.Add(newPipe);
                    connectTasks.Add(newPipe.WaitForConnectionAsync(cts!.Token));
                });
            }

            Task? connectedTask = null;
            using (ct.Register(() => cts?.Cancel())) {
                connectedTask = await Task.WhenAny(connectTasks);
            }
            ct.ThrowIfCancellationRequested();
            int index = connectTasks.IndexOf(connectedTask);
            connectTasks.RemoveAt(index);
            var pipe = availablePipes[index];
            availablePipes.RemoveAt(index);
            usedPipes.Add(pipe);
            ct.Register(pipe.Dispose);
            return pipe;
        }

        public async Task<Stream> ConnectToServerAsync(CancellationToken ct)
        {
            // For named pipes, we use the configured serverName and pipeName
            NamedPipeClientStream client = new(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            
            var connectTask = client.ConnectAsync(ct);
            using (ct.Register(() => { try { client.Close(); } catch { } }))
            {
                await connectTask;
            }
            
            ct.Register(client.Dispose);
            return client;
        }

        public void Dispose()
        {
            StopServer();
        }
    }
}
