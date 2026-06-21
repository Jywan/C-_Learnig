using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


// [보강] 04-5. 하트비트, 타임아웃, 재접속
// 기존 04_Async_TCP에서 연결 끊김 감지 없이 동작했기 때문에 보강
// 모바일 게임은 네트워크 끊김이 일상 - 서버는 죽은 연결을 감지하고 정리해야함!

// ==== 왜 하트비트가 필요한다? ====
// TCP 연결이 "살아있는지" 확인하는 방법이 없음!
// 문제 상황:
//      - 클라이언트가 비정상 종료 (앱 강제종료, 배터리 방전)
//      - 네트워크 장애 (지하철 진입, WIFI 끊김)
//      - 이 경우 서버는 FIN 패킷을 못받아서 연결이 살아있다고 착각함.
//      - 결과: 유령 세션이 쌓여서 서버 리소스 낭비
// 해결: 하트비트(Heartbeat)
//      - 주기적으로 "나 살아있음!" 패킷을 주고받음.
//      - 일정 시간 응답 없으면 연결 끊긴 것으로 판단 -> 세션 정리
//      - 게임 서버 표준 패턴

// ==== 하트비트 설정 ====
class HeartbeatConfig
{
    public static readonly int IntervalMs = 3000;           // 3초마다 핑 전송
    public static readonly int TimeoutMs = 10000;           // 10초 무응답이면 연결끊김 판정
    public static readonly int MaxReconnectAttempts = 5;    // 최대 재접속 시도 횟수
    public static readonly int ReconnectDelayMs = 2000;     // 재접속 간격 2초
}

// ==== 하트비트 서버 ====
class HeartbeatServer
{
    private TcpListener _listener;
    private DateTime _lastHeartbeat;
    private bool _running;

    public HeartbeatServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _running = true;
        Console.WriteLine("[서버] 하트비트 서버 시작");

        while (_running)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("[서버] 클라이언트 연결!");
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        _lastHeartbeat = DateTime.Now;
        byte[] buffer = new byte[1024];
        
        // 타임아웃 감시 태스크 - 별도로 돌면서 마지막 하트비트 시간 체크
        CancellationTokenSource cts = new CancellationTokenSource();
        _ = MonitorTimeoutAsync(client, cts.Token);

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // 정상 종료

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (message == "PING")
                {
                    // 하트비트 수신 인지
                    _lastHeartbeat = DateTime.Now;
                    byte[] pong = Encoding.UTF8.GetBytes("PONG");
                    await stream.WriteAsync(pong, 0, pong.Length);
                    Console.WriteLine($"[서버] PING 수신 -> PONG 응답 발송({DateTime.Now:HH:mm:ss})");
                }
                else
                {
                    // 일반 메세지 처리
                    Console.WriteLine($"[서버] 메세지 수신: {message}");
                    byte[] response = Encoding.UTF8.GetBytes($"에코 : {message}");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[서버] 연결 오류: {ex.Message}");
        }
        finally
        {
            cts.Cancel();       // 타임아웃 감시 중단
            client.Close();
            Console.WriteLine("[서버] 클라이언트 세션 정리 완료");
        }
        
    }
    
    // 타임아웃 감시: 마지막 하트비트 이후 일정시간 초과시 연결 종료
    private async Task MonitorTimeoutAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token); // 1초마다 체크

                TimeSpan elapsed = DateTime.Now - _lastHeartbeat;
                if (elapsed.TotalMilliseconds > HeartbeatConfig.TimeoutMs)
                {
                    Console.WriteLine($"[서버] 하트비트 타임아웃! ({elapsed.TotalSeconds:F1}초 무응답)");
                    client.Close(); // 강제 연결 종료 -> ReadAsync에서 예외 발생
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            // 정상 취소 된것으로 인지되어 무시하는 방향으로..
        }
    }
}


// ==== 하트비트 + 재접속 클라이언트 ====
class HeartbeatClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _connected;
    private CancellationTokenSource _heatbeatCts;

    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        _connected = true;
        Console.WriteLine("[클라이언트] 서버에 연결되었습니다!");
        
        // 하트비트 전송 시작
        _heatbeatCts = new CancellationTokenSource();
        _ = SendHeartbeatLoopAsync(_heatbeatCts.Token);
    }
    
    // 주기적 하트비트 전송
    private async Task SendHeartbeatLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _connected)
            {
                await Task.Delay(HeartbeatConfig.IntervalMs, token);
                
                byte[] ping = Encoding.UTF8.GetBytes("PING");
                await _stream.WriteAsync(ping, 0, ping.Length);
                Console.WriteLine($"[클라이언트] PING 전송 ({DateTime.Now:HH:mm:ss})");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("[클라이언트] 하트비트 전송 실패 - 연결 끊김 감지");
            _connected = false;
        }
    }
    
    // 메시지 송신
    public async Task SendAsync(string message)
    {
        if (!_connected) throw new Exception("연결되지 않음.");
        byte[] data = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(data, 0, data.Length);
    }
    
    // 메시지 수신
    public async Task<string> ReceiveAsync()
    {
        byte[] buffer =new byte[1024];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) throw new Exception("연결 종료");
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
    
    // 연결 해제
    public void Disconnect()
    {
        _connected = false;
        _heatbeatCts?.Cancel();
        _stream?.Close();
        _client?.Close();
        Console.WriteLine("[클라이언트] 연결 해제");
    }
    
    // ==== 재접속 로직 ====
    // 연결이 끊기면 일정 간격으로 재시도 - 모바일 게임의 필수 패턴!
    public async Task<bool> ReconnectAsync(string host, int port)
    {
        Console.WriteLine("[클라이언트] 재접속 시도 시작...");

        for (int attempt = 1; attempt <= HeartbeatConfig.MaxReconnectAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"[클라이언트] 재접속 시도 {attempt}/{HeartbeatConfig.MaxReconnectAttempts}");
                await Task.Delay(HeartbeatConfig.ReconnectDelayMs);
                
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _connected = true;
                
                // 하트비트 재시작
                _heatbeatCts = new CancellationTokenSource();
                _ = SendHeartbeatLoopAsync(_heatbeatCts.Token);
                
                Console.WriteLine("[클라이언트] 재접속 성공!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트] 재접속 실패: {ex.Message}");
            }
        }
        
        Console.WriteLine("[클라이언트] 최대 재접속 시도 초과 - 포기");
        return false;
    }
    
    public bool IsConnected => _connected;
}


class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("==== 하트비트 / 재접속 데모 ====");
            Console.WriteLine($"하트비트 간격: {HeartbeatConfig.IntervalMs}ms");
            Console.WriteLine($"타임아웃: {HeartbeatConfig.TimeoutMs}ms");
            Console.WriteLine($"최대 재접속: {HeartbeatConfig.MaxReconnectAttempts}회");
            Console.WriteLine($"재접속 간격: {HeartbeatConfig.ReconnectDelayMs}ms");
            Console.WriteLine();
            Console.WriteLine("서버: dotnet run -- server");
            Console.WriteLine("클라이언트: dotnet run -- client");
            return;
        }

        int port = 8085;

        if (args[0] == "server")
        {
            HeartbeatServer server = new HeartbeatServer(port);
            await server.StartAsync();
        }
        else if (args[0] == "client")
        {
            HeartbeatClient client = new HeartbeatClient();
            await client.ConnectAsync("127.0.0.1", port);
            
            // 수신 루프 (백그라운드)
            _ = Task.Run(async () =>
            {
                try
                {
                    while (client.IsConnected)
                    {
                        string response = await client.ReceiveAsync();
                        if (response != "PONG") // PONG은 하트비트 응답이라 무시
                            Console.WriteLine($"[클라이언트] 수신: {response}");
                    }
                }
                catch
                {
                    Console.WriteLine("[클라이언트] 수신 루프 종료 - 연결 끊김");
                }
            });
            
            // 입력 루프
            while (true)
            {
                Console.WriteLine("메시지 입력 (exit 종료): ");
                string input = Console.ReadLine();
                if (input == "exit") break;

                try
                {
                    await client.SendAsync(input);
                }
                catch
                {
                    Console.WriteLine("[클라이언트] 전송 실패 - 재접속 시도");
                    bool reconnected = await client.ReconnectAsync("127.0.0.1", port);
                    if (!reconnected) break;
                }
            }
            
            client.Disconnect();
        }
    }
}

// 실행 결과
// ==== 하트비트 / 재접속 데모 ====
// 하트비트 간격: 3000ms
// 타임아웃: 10000ms
// 최대 재접속: 5회
// 재접속 간격: 2000ms
//
// 서버: dotnet run -- server
// 클라이언트: dotnet run -- client

// 실행 결과 (서버)
// [서버] 하트비트 서버 시작
// [서버] 클라이언트 연결!
// [서버] PING 수신 -> PONG 응답 발송(18:10:38)
// [서버] PING 수신 -> PONG 응답 발송(18:10:41)
// [서버] PING 수신 -> PONG 응답 발송(18:10:44)
// [서버] 메세지 수신: 안
// [서버] PING 수신 -> PONG 응답 발송(18:10:47)
// [서버] PING 수신 -> PONG 응답 발송(18:10:50)
// [서버] PING 수신 -> PONG 응답 발송(18:10:53)
// [서버] 메세지 수신: 녕
// [서버] PING 수신 -> PONG 응답 발송(18:10:56)
// [서버] PING 수신 -> PONG 응답 발송(18:10:59)
// [서버] PING 수신 -> PONG 응답 발송(18:11:02)
// [서버] PING 수신 -> PONG 응답 발송(18:11:05)
// [서버] PING 수신 -> PONG 응답 발송(18:11:08)
// [서버] 클라이언트 세션 정리 완료

// 실행 결과 (클라이언트)
// [서버] 하트비트 서버 시작
// [서버] 클라이언트 연결!
// [서버] PING 수신 -> PONG 응답 발송(18:10:38)
// [서버] PING 수신 -> PONG 응답 발송(18:10:41)
// [서버] PING 수신 -> PONG 응답 발송(18:10:44)
// [서버] 메세지 수신: 안
// [서버] PING 수신 -> PONG 응답 발송(18:10:47)
// [서버] PING 수신 -> PONG 응답 발송(18:10:50)
// [서버] PING 수신 -> PONG 응답 발송(18:10:53)
// [서버] 메세지 수신: 녕
// [서버] PING 수신 -> PONG 응답 발송(18:10:56)
// [서버] PING 수신 -> PONG 응답 발송(18:10:59)
// [서버] PING 수신 -> PONG 응답 발송(18:11:02)
// [서버] PING 수신 -> PONG 응답 발송(18:11:05)
// [서버] PING 수신 -> PONG 응답 발송(18:11:08)
// [서버] 클라이언트 세션 정리 완료
