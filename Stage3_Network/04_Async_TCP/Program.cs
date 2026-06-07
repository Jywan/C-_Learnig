using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// ==== 비동기 서버 ====
class AsyncServer
{
    private TcpListener _listener;
    private bool _running = false;

    public AsyncServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _running = true;
        Console.WriteLine("[서버] 비동기 서버 시작 - 포트 8081");

        while (_running)
        {
            // await로 대기 - 스레드 블로킹 없이 연결 수락
            TcpClient client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("[서버] 클라이언트 연결됨!");
            
            // 각 클라이언트를 별도 Task로 처리 - await 없이 바로 다음 연결 대기
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[서버] 수신: {message}");

                string response = $"에코: {message}";
                byte[] data = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(data, 0, data.Length); // 비동기 송신
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[서버] 연결 종료: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    public void Stop()
    {
        _running = false;
        _listener.Stop();
    }
}

// ==== 비동기 클라이언트 ====
class AsyncClient
{
    private TcpClient _client;
    private NetworkStream _stream;

    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);     // 비동기 연결
        _stream = _client.GetStream();
        Console.WriteLine("[클라이언트] 서버에 연결됨!");
    }

    public async Task SendAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await _stream.WriteAsync(data, 0, data.Length);
    }

    public async Task<string> ReceiveAsync()
    {
        byte[] buffer = new byte[1024];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
        Console.WriteLine("[클라이언트] 연결 종료!");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {   
            // 02에서 테스트한것과 동일하게 로그 출력! (서버/클라이언트 테스트 잊지말기!)
            Console.WriteLine("사용법: dotnet run -- server 또는 dotnet run -- client");
            return;
        }

        if (args[0] == "server")
        {
            AsyncServer server = new AsyncServer(8081);
            await server.StartAsync();
        }
        else if (args[0] == "client")
        {
            AsyncClient client = new AsyncClient();
            await client.ConnectAsync("127.0.0.1", 8081);

            while (true)
            {
                Console.WriteLine("메시지 입력 (exit 종료): ");
                string input = Console.ReadLine();
                if (input == "exit") break;
                
                await client.SendAsync(input);
                string response = await client.ReceiveAsync();
                Console.WriteLine($"[클라이언트] 수신: {response}");
            }
            client.Disconnect();
        }
    }
}

// 테스트 주의 사항
// 터미널 두개 사용해야합니다! (하나는 서버 역할 / 하나는 클라이언트 역할)
// 서버 역할부터 기동하고 클라이언트 접속하는 식으로 진행해야합니다. (02 스레드 학습과 동일!)

// ==== 실행 결과 (서버) ====
// [서버] 비동기 서버 시작 - 포트 8081
// [서버] 클라이언트 연결됨!
// [서버] 수신: 테스트
// [서버] 수신: 확인완료!
// [서버] 수신: 종료합니다

// ==== 실행 결과 (클라이언트) ==== 
// [클라이언트] 서버에 연결됨!
// 메시지 입력 (exit 종료): 
// 테스트
// [클라이언트] 수신: 에코: 테스트
// 메시지 입력 (exit 종료): 
// 확인완료!
// [클라이언트] 수신: 에코: 확인완료!
// 메시지 입력 (exit 종료): 
// 종료합니다
// [클라이언트] 수신: 에코: 종료합니다
// 메시지 입력 (exit 종료): 
// exit
// [클라이언트] 연결 종료!