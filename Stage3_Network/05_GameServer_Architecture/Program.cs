using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// ==== 1. 패킷 정의 ====
enum PacketType
{
    Login = 1,
    Chat = 2,
    Move = 3,
    Logout = 4
}

class Packet
{
    public PacketType Type { get; set; }
    public string PlayerId { get; set; }
    public string Data { get; set; }
}

// ==== 2. 플레이어 ====
class Player
{
    public string Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public TcpClient Client { get; set; }
    public NetworkStream Stream => Client.GetStream();

    public Player(string id, TcpClient client)
    {
        Id = id;
        Client = client;
        X = 0;
        Y = 0;
    }
}

// ==== 3. 게임 서버 ====
class GameServer
{
    // ConcurrentDictionary: 멀티스레드 환경에서 안전한 딕셔너리
    private ConcurrentDictionary<string, Player> _players = new();
    private TcpListener _listener;

    public GameServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("[GameServer] 시작 - 포트 9090");

        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        Player player = null;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Packet packet = JsonSerializer.Deserialize<Packet>(json);

                player = await ProcessPacketAsync(packet, client, player);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameServer] 오류: {ex.Message}");
        }
        finally
        {
            if (player != null)
            {
                _players.TryRemove(player.Id, out _);
                await BroadcastAsync($"{player.Id} 퇴장", player.Id);
                Console.WriteLine($"[GameServer] {player.Id} 퇴장");
            }
            client.Close();
        }
    }

    private async Task<Player> ProcessPacketAsync(Packet packet, TcpClient client, Player current)
    {
        switch (packet.Type)
        {
            case PacketType.Login:
                Player player = new Player(packet.PlayerId, client);
                _players[packet.PlayerId] =  player;
                Console.WriteLine($"[GameServer] {packet.PlayerId} 입장 (총 {_players.Count}명)");
                await BroadcastAsync($"{packet.PlayerId} 입장!", packet.PlayerId);
                return player;
            case PacketType.Chat:
                Console.WriteLine($"[chat] {packet.PlayerId}: {packet.Data}");
                await  BroadcastAsync($"{packet.PlayerId}: {packet.Data}", null);   // 전체 브로드캐스트
                return current;
            case PacketType.Move:
                if (current != null)
                {
                    string[] pos = packet.Data.Split(',');
                    if (pos.Length < 2) return current;  // x,y 형식이 아니면 무시
                    current.X = float.Parse(pos[0]);
                    current.Y = float.Parse(pos[1]);
                    Console.WriteLine($"[move] {current.Id} → ({current.X}, {current.Y})");
                }
                return current;
            case PacketType.Logout:
                if (current != null)
                {
                    _players.TryRemove(current.Id, out _);
                    await BroadcastAsync($"{current.Id} 퇴장", current.Id);
                    Console.WriteLine($"[GameServer] {current.Id} 로그아웃");
                }
                return null;
        }
        return current;
    }
    
    // 모든 플레이어에게 메시지 전송
    private async Task BroadcastAsync(string message, string excludeId)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var pair in _players)
        {
            if (pair.Key ==  excludeId) continue;       // 발신자 제외
            try
            {
                await pair.Value.Stream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                // 연결이 끊긴 클라이언트 무시
            }
        }
    }
}

// ==== 4. 게임 클라이언트 ====
class GameClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private string _playerId;

    public async Task ConnectAsync(string playerId)
    {
        _playerId = playerId;
        _client = new TcpClient();
        await _client.ConnectAsync("127.0.0.1", 9090);
        _stream = _client.GetStream();
        
        // 수신 루프를 백그라운드 Task로 실행
        _ = ReceiveLoopAsync();

        await SendPacketAsync(PacketType.Login, "");
        Console.WriteLine($"[{_playerId}] 서버 접속 완료");
    }

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (true)
            {
                int byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteRead == 0) break;
                Console.WriteLine($"[수신] {Encoding.UTF8.GetString(buffer, 0, byteRead)}");
            }
        }
        catch { }
    }

    public async Task SendPacketAsync(PacketType type, string data)
    {
        Packet packet = new Packet { Type = type, PlayerId =  _playerId, Data = data };
        string json = JsonSerializer.Serialize(packet);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }

    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("사용법: dotnet run -- server");
            Console.WriteLine("클라이언트: dotnet run -- client <플레이어 ID>" );
            return;
        }

        if (args[0] == "server")
        {
            GameServer server = new GameServer(9090);
            await server.StartAsync();
        }
        else if (args[0] == "client" && args.Length >= 2)
        {
            GameClient client = new GameClient();
            await client.ConnectAsync(args[1]);

            while (true)
            {
                Console.WriteLine("명령 (chat/move/logout): ");
                string input = Console.ReadLine();
                string[] parts = input.Split(' ', 2);
                
                if (parts[0] == "chat")
                    await client.SendPacketAsync(PacketType.Chat, parts.Length > 1 ? parts[1] : "");
                else if (parts[0] == "move")
                    await client.SendPacketAsync(PacketType.Move, parts.Length > 1 ? parts[1] : "0,0");
                else if (parts[0] == "logout")
                {
                    await client.SendPacketAsync(PacketType.Logout, "");
                    break;
                }
            }
            
            client.Disconnect();
        }
    }
}

// 테스트 주의 사항
// 터미널 두개 사용해야합니다! (하나는 서버 역할 / 하나는 클라이언트 역할)
// 서버 역할부터 기동하고 클라이언트 접속하는 식으로 진행해야합니다. (02 스레드 학습과 동일!)

// ==== 실행 결과 (서버) ====
// [GameServer] 시작 - 포트 9090
// [GameServer] 장영완 입장 (총 1명)
// [chat] 장영완: 안녕하세요
// [move] 장영완 → (1, 2)
// [move] 장영완 → (0, 0)
// [GameServer] 장영완 로그아웃

// ==== 실행 결과 (클라이언트) ==== 
// [장영완] 서버 접속 완료
// 명령 (chat/move/logout): 
// chat 안녕하세요
// 명령 (chat/move/logout): 
// [수신] 장영완: 안녕하세요
// move 1, 2
// 명령 (chat/move/logout): 
// move
// 명령 (chat/move/logout): 
// logout
