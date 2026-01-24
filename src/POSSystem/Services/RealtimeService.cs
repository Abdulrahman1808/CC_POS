using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using POSSystem.Models;

namespace POSSystem.Services;

/// <summary>
/// Handles Supabase Realtime subscriptions for live updates from Web Dashboard.
/// </summary>
public class RealtimeService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private bool _isDisposed;

    /// <summary>
    /// Fired when a product is updated from the Web Dashboard.
    /// </summary>
    public event EventHandler<ProductUpdateEventArgs>? ProductUpdated;

    /// <summary>
    /// Fired when connection status changes.
    /// </summary>
    public event EventHandler<bool>? ConnectionStatusChanged;

    public RealtimeService(IConfiguration configuration)
    {
        _configuration = configuration;
        _supabaseUrl = configuration["Supabase:Url"] ?? "";
        _supabaseKey = configuration["Supabase:ApiKey"] ?? "";
    }

    /// <summary>
    /// Connects to Supabase Realtime and subscribes to product updates.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
        {
            Debug.WriteLine("[Realtime] Supabase not configured, skipping realtime connection");
            return;
        }

        try
        {
            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            // Convert HTTPS to WSS for realtime
            var realtimeUrl = _supabaseUrl
                .Replace("https://", "wss://")
                .Replace("http://", "ws://");
            realtimeUrl += "/realtime/v1/websocket?apikey=" + _supabaseKey;

            Debug.WriteLine($"[Realtime] Connecting to {realtimeUrl[..50]}...");
            await _webSocket.ConnectAsync(new Uri(realtimeUrl), _cts.Token);

            _isConnected = true;
            ConnectionStatusChanged?.Invoke(this, true);
            Debug.WriteLine("[Realtime] Connected!");

            // Subscribe to products table
            await SubscribeToTableAsync("products");

            // Start listening for messages
            _ = Task.Run(() => ListenForMessagesAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] Connection failed: {ex.Message}");
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
        }
    }

    private async Task SubscribeToTableAsync(string tableName)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        var subscribeMessage = new
        {
            @event = "phx_join",
            topic = $"realtime:public:{tableName}",
            payload = new { },
            @ref = Guid.NewGuid().ToString()
        };

        var json = JsonSerializer.Serialize(subscribeMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cts?.Token ?? CancellationToken.None
        );

        Debug.WriteLine($"[Realtime] Subscribed to {tableName}");
    }

    private async Task ListenForMessagesAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];

        while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    ct
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.WriteLine("[Realtime] Server closed connection");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessMessage(message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Realtime] Error receiving: {ex.Message}");
            }
        }

        _isConnected = false;
        ConnectionStatusChanged?.Invoke(this, false);
    }

    private void ProcessMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // Check for INSERT/UPDATE/DELETE events
            if (root.TryGetProperty("event", out var eventProp))
            {
                var eventType = eventProp.GetString();

                if (eventType == "INSERT" || eventType == "UPDATE")
                {
                    if (root.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("record", out var record))
                    {
                        // Check LastUpdatedBy - only process web dashboard updates
                        if (record.TryGetProperty("last_updated_by", out var updatedBy))
                        {
                            var source = updatedBy.GetString();
                            
                            // Ignore updates from desktop (we made them)
                            if (source == "Desktop")
                            {
                                Debug.WriteLine("[Realtime] Ignoring own update");
                                return;
                            }
                        }

                        // Parse product update
                        var product = JsonSerializer.Deserialize<Product>(
                            record.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (product != null)
                        {
                            Debug.WriteLine($"[Realtime] ✓ Product updated from web: {product.Name} = ${product.Price}");
                            ProductUpdated?.Invoke(this, new ProductUpdateEventArgs(product, eventType));
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore non-JSON messages (heartbeats, etc.)
        }
    }

    /// <summary>
    /// Disconnects from Supabase Realtime.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None
            );
        }

        _cts?.Cancel();
        _isConnected = false;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _webSocket?.Dispose();
    }
}

public class ProductUpdateEventArgs : EventArgs
{
    public Product Product { get; }
    public string EventType { get; }

    public ProductUpdateEventArgs(Product product, string eventType)
    {
        Product = product;
        EventType = eventType;
    }
}
