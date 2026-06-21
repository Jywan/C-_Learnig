using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


// [보강] 05-5. 게임 루프 (Tick), 상태 동기화
// 기존 05_GameServer_Architecture가 이벤트 드리븐이었기 때문에 보강
// 실제 게임 서버는 고정 주기(Tick)로 월드 상태를 갱신함 - Unity의 FixedUpdate와 대응!

// ==== 이벤트 드리븐 vs Tick 기반 ====
// 이벤트 드리븐 (기존 방식):
//      - 패킷이 올 때마다 즉시 처리
//      - 단순하지만 처리 순서가 불규칙
//      - 물리 시뮬레이션, 충돌 판정에 부적합

// Tick 기반 (게임 서버 표준):
//      - 고정 간격(예: 33ms = 30fps, 16ms = 60fps)으로 루프 실행
//      - 1 Tick 동안 모아진 입력을 한번에 처리 -> 상태 갱신 -> 결과 브로드캐스트
//      - 모든 클라이언트에게 동일한 월드 상태 보장
//      - Unity: FixedUpdate(물리) = 고정 Tick, Update(랜더) = 가변 프레임

// Tick 루프 구조:
//  while (running)
//  {
//      1. 입력 쿠에서 명령 꺼내기
//      2. 게임 로직 처리 (이동, 충돌, 스킬 등)
//      3. 상태 변경을 클라이언트에 브로드캐스트
//      4. 남은 시간만큼 Sleep (Tick 간격 유지)
//  }


// ==== 플레이어 상태 ====
class PlayerState
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Speed { get; set; } = 5.0f; // 초당 5 유닛 이동

    public override string ToString() => $"Player{Id}({X:F1}, {Y:F1})";
}

// ==== 입력 명령 ====
enum CommandType
{
    Move,
    Stop,
    Chat
}

class GameCommand
{
    public int PlayerId { get; set; }
    public CommandType Type { get; set; }
    public float DirX  { get; set; }
    public float DirY { get; set; }
    public string Message { get; set; } = "";
}


// ==== Tick 기반 게임 서버 ====
class TickGameServer
{
    private const int TickRate = 30;                        // 초당 30틱
    private const int TickIntervalMs = 1000 / TickRate;     // 약 33ms
    
    private ConcurrentQueue<GameCommand> _commandQueue = new(); // 스레드 안전한 입력 큐
    private Dictionary<int, PlayerState> _players = new();
    private TcpListener _listener;
    private List<TcpClient> _clients = new();
    private bool _running;
    private int _tickCount;

    public TickGameServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _running = true;
        Console.WriteLine($"[서버] Tick 게임 서버 시작 - {TickRate}fps ({TickIntervalMs}ms/tick)");
        
        // 접속 수락 루프(별도 태스크)
        _ = AcceptClientsAsync();
        
        // 메인 게임 루프 (Tick)
        await GameLoopAsync();
    }
    
    private async Task AcceptClientsAsync()
    {
        int nextId = 1;

        while (_running)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            int playerId = nextId++;
            
            _clients.Add(client);
            _players[playerId] = new PlayerState { Id = playerId, X = 0, Y = 0 };
            Console.WriteLine($"[서버] Player{playerId} 접속 (총 {_players.Count}명)");
            
            // 수신 루프 (입력을 큐에 넣음)
            _ = ReceiveCommandsAsync(client, playerId);
        }
    }
    
    // 클라이언트 입력 수신 -> 큐에 적재
    private async Task ReceiveCommandsAsync(TcpClient client, int playerId)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (_running)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string input = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                GameCommand cmd = ParseCommand(playerId, input);
                if (cmd != null)
                    _commandQueue.Enqueue(cmd);
            }
        }
        catch
        {
        }
        finally
        {
            _players.Remove(playerId);
            _clients.Remove(client);
            client.Close();
            Console.WriteLine($"[서버] Player{playerId} 퇴장");
        }
    }
    
    // 입력 파싱: "move 1 0" -> 오른쪽 이동, "stop" -> 정지
    private GameCommand ParseCommand(int playerId, string input)
    {
        string[] parts = input.Trim().Split(' ');

        switch (parts[0])
        {
            case "move" when parts.Length >= 3:
                return new GameCommand
                {
                    PlayerId = playerId,
                    Type = CommandType.Move,
                    DirX = float.Parse(parts[1]),
                    DirY = float.Parse(parts[2])
                };
            case "stop":
                return new GameCommand
                {
                    PlayerId = playerId,
                    Type = CommandType.Stop,
                };
            case "chat" when parts.Length >= 2:
                return new GameCommand
                {
                    PlayerId = playerId,
                    Type = CommandType.Chat,
                    Message = string.Join(" ", parts[1..])
                };
            default:
                return null;
        }
    }
    
    // ==== 메인 게임 루프 ====
    private async Task GameLoopAsync()
    {
        Stopwatch sw = new Stopwatch();
        // 각 플레이어의 현재 이동 방향 저장
        Dictionary<int, (float dx, float dy)> moveDirections = new();

        while (_running)
        {
            sw.Restart();
            _tickCount++;
            
            // 1단계: 큐에서 명령을 꺼내 처리
            while (_commandQueue.TryDequeue(out GameCommand cmd))
            {
                switch (cmd.Type)
                {
                    case CommandType.Move:
                        moveDirections[cmd.PlayerId] = (cmd.DirX, cmd.DirY);
                        break;
                    case CommandType.Stop:
                        moveDirections.Remove(cmd.PlayerId);
                        break;
                    case CommandType.Chat:
                        Console.WriteLine($"[채팅] Player{cmd.PlayerId}: {cmd.Message}");
                        break;
                }
            }
            
            // 2단계: 게임 로직 처리(이동)
            float deltaTime = TickIntervalMs / 1000f;       // Tick 간격을 초 단위로
            foreach (var kvp in moveDirections)
            {
                if (_players.TryGetValue(kvp.Key, out PlayerState player))
                {
                    player.X += kvp.Value.dx * player.Speed * deltaTime;
                    player.Y += kvp.Value.dy * player.Speed * deltaTime;
                }
            }
            
            // 3단계: 10틱마다 상태 브로드캐스트 (매 틱 보내면 대역폭 낭비)
            if (_tickCount % 10 == 0)
            {
                string state = BuildStateMessage();
                await BroadcastAsync(state);
                
                if (_players.Count > 0)
                    Console.WriteLine($"[Tick {_tickCount}] {state}");
            }
            
            // 4단계: 남은 시간 대기 (Tick 간격 유지)
            sw.Stop();
            int elapsed = (int)sw.ElapsedMilliseconds;
            int sleepTime = TickIntervalMs - elapsed;
            if (sleepTime > 0)
                await Task.Delay(sleepTime);
        }
    }
    
    // 필드 상태를 문자열로 조립
    private string BuildStateMessage()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("STATE");
        foreach (var player in _players.Values)
        {
            sb.Append($" {player}");
        }
        return sb.ToString();
    }
    
    // 모든 클라이언트에 브로드캐스트
    private async Task BroadcastAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in _clients.ToArray())
        {
            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch { }
        }
    }
}


// ==== 클라이언트 ====
class TickGameClient
{
    private TcpClient _client;
    private NetworkStream _stream;

    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        Console.WriteLine("[클라이언트] 서버에 연결됨");
        
        // 수신 루프
        _ = ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (true)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[수신] {msg}");
            }
        }
        catch {  }
    }

    public async Task SendAsync(string command)
    {
        byte[] data = Encoding.UTF8.GetBytes(command);
        await _stream.WriteAsync(data, 0, data.Length);
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
            Console.WriteLine("==== Tick 기반 게임 루프 데모 ====");
            Console.WriteLine("서버: dotnet run -- server");
            Console.WriteLine("클라이언트: dotnet run -- client");
            Console.WriteLine();
            Console.WriteLine("명령어:");
            Console.WriteLine("  move [dx] [dy]  - 방향 이동 (예: move 1 0 = 오른쪽)");
            Console.WriteLine("  stop            - 이동 멈춤");
            Console.WriteLine("  chat [메시지]   - 채팅");
            Console.WriteLine("  exit            - 종료");
            return;
        }
        
        int port = 8084;

        if (args[0] == "server")
        {
            TickGameServer server = new TickGameServer(port);
            await server.StartAsync();
        }
        else if (args[0] == "client")
        {
            TickGameClient client = new TickGameClient();
            await client.ConnectAsync("127.0.0.1", port);

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (input == "exit") break;
                await client.SendAsync(input);
            }

            client.Disconnect();
        }
    }
}

// 실행 결과 
// ==== Tick 기반 게임 루프 데모 ====
// 서버: dotnet run -- server
// 클라이언트: dotnet run -- client
//
// 명령어:
//   move [dx] [dy]  - 방향 이동 (예: move 1 0 = 오른쪽)
//   stop            - 이동 멈춤
//   chat [메시지]   - 채팅
//   exit            - 종료

// [서버] Tick 게임 서버 시작 - 30fps (33ms/tick)
// [서버] Player1 접속 (총 1명)
// [Tick 10] STATE Player1(0.0, 0.0)
// [Tick 20] STATE Player1(1.7, 0.0)
// [Tick 30] STATE Player1(3.3, 0.0)
// [채팅] Player1: 안녕하세요
// [Tick 40] STATE Player1(5.0, 0.0)
// [Tick 50] STATE Player1(5.0, 0.0)       ← stop 이후 멈춤

// 실행 결과 (클라이언트)
// [클라이언트] 서버에 연결됨!
// > move 1 0
// [수신] STATE Player1(0.0, 0.0)
// [수신] STATE Player1(1.7, 0.0)
// [수신] STATE Player1(3.3, 0.0)
// > chat 안녕하세요
// > stop
// [수신] STATE Player1(5.0, 0.0)
// > exit
