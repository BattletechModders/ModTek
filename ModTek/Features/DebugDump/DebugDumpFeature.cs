using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using ModTek.Features.Manifest.Mods;
using UnityEngine;

namespace ModTek.Features.DebugDump;

internal class DebugDumpServer : MonoBehaviour
{
    private static Thread thread = null;
    private static HttpListener listener = null;
    private static readonly Queue<DumpHTTPRequest> requests = new Queue<DumpHTTPRequest>();
    internal class DumpHTTPRequest
    {
        public HttpListenerResponse response;
        public HttpListenerRequest request;
        public bool ready = false;
        public DumpHTTPRequest(HttpListenerRequest req, HttpListenerResponse resp)
        {
            request = req;
            response = resp;
            ready = false;
        }
    }
    private static DebugDumpServer _instance = null;
    public static DebugDumpServer Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("DebugDumpServer");
                _instance = go.AddComponent<DebugDumpServer>();
            }
            return _instance;
        }
    }
    public void Report()
    {
        Log.Main.Info?.Log($"DebugDumpServer instantine");
    }
    public void Awake()
    {
        try
        {
            UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)this.gameObject);
            UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)this);
            if (string.IsNullOrEmpty(ModTek.Config.Logging.DebugLogDumpServerListen)) { return; }
            if (DebugDumpServer.listener != null) { return; }
            if (DebugDumpServer.thread != null) { return; }
            Log.Main.Info?.Log($"Starting HTTP server {ModTek.Config.Logging.DebugLogDumpServerListen}");
            DebugDumpServer.listener = new HttpListener();
            listener.Prefixes.Add(ModTek.Config.Logging.DebugLogDumpServerListen);
            listener.Start();
            DebugDumpServer.thread = new Thread(DebugDumpServer.DoWork);
            DebugDumpServer.thread.Start();
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
    public void PerformDump()
    {
        ModDefsDatabase.DebugLogDump();
    }
    public void Update()
    {
        try
        {
            if (DebugDumpServer.requests.Count == 0) { return; }
            var modtek_request = DebugDumpServer.requests.Dequeue();
            if (modtek_request == null) { return; }
            if (modtek_request.request == null) { return; }
            if (modtek_request.response == null) { return; }
            if (modtek_request.ready) { return; }
            try
            {
                if (modtek_request.request.Url.AbsolutePath == "/dump")
                {
                    modtek_request.response.StatusCode = 200;
                    modtek_request.response.ContentType = "text/plain";
                    string body = "dumped";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                    modtek_request.response.ContentLength64 = buffer.Length;
                    Stream output = modtek_request.response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    modtek_request.ready = true;
                    PerformDump();
                    return;
                }
                else
                {
                    modtek_request.response.StatusCode = 404;
                    modtek_request.response.ContentType = "text/plain";
                    string body = "unknown";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                    modtek_request.response.ContentLength64 = buffer.Length;
                    Stream output = modtek_request.response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    modtek_request.ready = true;
                }
            }
            catch (Exception ex)
            {
                modtek_request.response.StatusCode = 200;
                modtek_request.response.ContentType = "text/plain";
                string body = ex.ToString();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                modtek_request.response.ContentLength64 = buffer.Length;
                Stream output = modtek_request.response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                modtek_request.ready = true;
                Log.Main.Error?.Log(ex.ToString());
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
    private static void DoWork()
    {
        try
        {
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                var modtek_request = new DumpHTTPRequest(request, response);
                DebugDumpServer.requests.Enqueue(modtek_request);
                while (modtek_request.ready == false) { Thread.Sleep(10); }
                response.Close();
            }
        } catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
}