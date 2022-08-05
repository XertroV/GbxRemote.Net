﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GbxRemoteNet.Exceptions;
using GbxRemoteNet.XmlRpc.Packets;
using GbxRemoteNet.XmlRpc.Types;
using Microsoft.Extensions.Logging;

namespace GbxRemoteNet.XmlRpc;

public class NadeoXmlRpcClient
{
    /// <summary>
    ///     Action for the OnCallback event.
    /// </summary>
    /// <param name="call">Information about the call.</param>
    /// <returns></returns>
    public delegate Task CallbackAction(MethodCall call);

    /// <summary>
    ///     Generic action for events.
    /// </summary>
    /// <returns></returns>
    public delegate Task TaskAction();

    private readonly ILogger _logger;

    // connection
    private readonly string connectHost;
    private readonly int connectPort;

    private uint handler = 0x80000000;
    private readonly object handlerLock = new();
    private CancellationTokenSource recvCancel;
    private readonly ConcurrentDictionary<uint, ManualResetEvent> responseHandles = new();
    private readonly ConcurrentDictionary<uint, ResponseMessage> responseMessages = new();

    // recvieve
    private Task taskRecvLoop;
    private TcpClient tcpClient;
    private XmlRpcIO xmlRpcIO;

    public NadeoXmlRpcClient(string host, int port, ILogger logger = null)
    {
        connectHost = host;
        connectPort = port;

        _logger = logger;
    }

    /// <summary>
    ///     Invoked when the client is connected to the XML-RPC server.
    /// </summary>
    public event TaskAction OnConnected;

    /// <summary>
    ///     Called when a callback occured from the XML-RPC server.
    /// </summary>
    public event CallbackAction OnCallback;

    /// <summary>
    ///     Triggered when the client has been disconnected from the server.
    /// </summary>
    public event TaskAction OnDisconnected;

    /// <summary>
    ///     Handles all responses from the XML-RPC server.
    /// </summary>
    private async void RecvLoop()
    {
        try
        {
            _logger?.LogDebug("Recieve loop initiated.");

            while (!recvCancel.IsCancellationRequested)
            {
                var response = await ResponseMessage.FromIOAsync(xmlRpcIO);

                _logger?.LogTrace("================== MESSAGE START ==================");
                _logger?.LogTrace("Message length: {Length}", response.Header.MessageLength);
                _logger?.LogTrace("Handle: {Handle}", response.Header.Handle);
                _logger?.LogTrace("Is callback: {IsCallback}", response.Header.IsCallback);
                _logger?.LogTrace("{Xml}", response.MessageXml.ToString());
                _logger?.LogTrace("================== MESSAGE END ==================");

                if (response.IsCallback)
                {
                    // invoke listeners and
                    // run callback handler in a new thread to avoid blocking of new responses
                    _ = Task.Run(() => OnCallback?.Invoke(new MethodCall(response)));
                }
                else if (responseHandles.ContainsKey(response.Header.Handle))
                {
                    // attempt to signal the call method
                    responseMessages[response.Header.Handle] = response;
                    responseHandles[response.Header.Handle].Set();
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Receive loop raised an exception: {Msg}", e.Message);
            await DisconnectAsync();
        }
    }

    /// <summary>
    ///     Connect to the remote XMLRPC server.
    /// </summary>
    /// <param name="retries">Number of times to re-try connection.</param>
    /// <param name="retryTimeout">Number of milliseconds to wait between each re-try.</param>
    /// <returns></returns>
    public async Task<bool> ConnectAsync(int retries = 0, int retryTimeout = 1000)
    {
        _logger?.LogDebug("Client connecting to the remote XML-RPC server");
        var connectAddr = await Dns.GetHostAddressesAsync(connectHost);

        tcpClient = new TcpClient();

        // try to connect
        while (retries >= 0)
        {
            try
            {
                await tcpClient.ConnectAsync(connectAddr[0], connectPort);

                if (tcpClient.Connected)
                    break;
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception occured when trying to connect to server: {Msg}", e.Message);
            }

            _logger?.LogError("Failed to connect to server");

            retries--;

            if (retries < 0)
                break;

            await Task.Delay(retryTimeout);
        }

        if (retries < 0)
            return false; // connection failed

        xmlRpcIO = new XmlRpcIO(tcpClient);

        _logger?.LogDebug("Client connected to XML-RPC server with IP: {connectAddr}", connectAddr);

        // Cancellation token to cancel task if it takes longer than a second
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(1000);

        // check header
        ConnectHeader header = null;
        try
        {
            header = await ConnectHeader.FromIOAsync(xmlRpcIO, cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            _logger?.LogError("Exception occured when trying to get connect header: {Msg}", e.Message);
            _logger?.LogError("Failed to get connect header");
            return false;
        }

        if (!header.IsValid)
        {
            _logger?.LogError("Client is using an invalid header protocol: {Protocol}", header.Protocol);
            throw new InvalidProtocolException(header.Protocol);
        }

        recvCancel = new CancellationTokenSource();
        taskRecvLoop = new Task(RecvLoop, recvCancel.Token);
        taskRecvLoop.Start();

        OnConnected?.Invoke();
        return true;
    }

    /// <summary>
    ///     Stop the recieve loop and disconnect.
    /// </summary>
    /// <returns></returns>
    public async Task DisconnectAsync()
    {
        _logger?.LogDebug("Client is disconnecting from XML-RPC server");
        try
        {
            recvCancel.Cancel();
            await taskRecvLoop;
            tcpClient.Close();
        }
        catch (Exception e)
        {
            _logger?.LogWarning("An exception occured when trying to disconnect: {Message}", e.Message);
        }

        OnDisconnected?.Invoke();

        _logger?.LogDebug("Client disconnected from XML-RPC server");
    }

    /// <summary>
    ///     Get the next handler value.
    /// </summary>
    /// <returns>The next handle value.</returns>
    public async Task<uint> GetNextHandle()
    {
        // lock because we may access this in multiple threads
        lock (handlerLock)
        {
            if (handler + 1 == 0xffffffff)
                handler = 0x80000000;

            _logger?.LogTrace("Next handler value: {Handler}", handler);

            return handler++;
        }
    }

    /// <summary>
    ///     Call a remote method.
    /// </summary>
    /// <param name="method">Method name</param>
    /// <param name="args">Arguments to the method if available.</param>
    /// <returns>Response returned by the call.</returns>
    public async Task<ResponseMessage> CallAsync(string method, params XmlRpcBaseType[] args)
    {
        var handle = await GetNextHandle();
        MethodCall call = new(method, handle, args);

        _logger?.LogTrace("Calling remote method: {Method}", method);
        _logger?.LogTrace("================== CALL START ==================");
        _logger?.LogTrace("{Xml}", call.Call.MainDocument.ToString());
        _logger?.LogTrace("================== CALL END ==================");

        responseHandles[handle] = new ManualResetEvent(false);

        var data = await call.Serialize();
        await xmlRpcIO.WriteBytesAsync(data);


        // wait for response
        responseHandles[handle].WaitOne();
        var message = responseMessages[handle];
        return message;
    }
}