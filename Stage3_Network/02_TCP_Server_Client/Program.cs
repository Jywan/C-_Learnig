using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// ==== 서버 ====
class Server
{
    public static void Start()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Console.WriteLine("[서버] 시작 - 포트 8080 대기중......");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();    // 연결 대기 (블로킹)
            Console.WriteLine("[서버] 클라이언트 연결됨!");
            
            // 클라이언트마다 별도 스레드 - 동시에 여러 클라이언트 처리 가능
            Thread thread = new Thread(() => HandleClient(client));
            thread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // 클라이언트 연결 종료

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[서버] 수신: {message}");

                string response = $"에코: {message}"; // 받은 메시지를 그대로 돌려줌
                byte[] data = Encoding.UTF8.GetBytes(response);
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[서버] 연결 종료: {ex.Message}");
        }
        finally
        {
            stream.Close();
        }
    }
}

// ==== 클라이언트 ====
class Client
{
    public static void Start()
    {
        TcpClient client = new TcpClient();
        client.Connect("127.0.0.1", 8080);
        Console.WriteLine("[클라이언트] 서버에 연결됨!");
        
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            Console.Write("메세지 입력(exit 종료): ");
            string input = Console.ReadLine();
            if (input == "exit") break;
            
            byte[] data = Encoding.UTF8.GetBytes(input);
            stream.Write(data, 0, data.Length);
            
            int byteRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, byteRead);
            Console.WriteLine($"[클라이언트] 수신: {response}");
        }
        
        client.Close();
        Console.WriteLine("[클라이언트] 연결 종료");
    }
}

// ==== 진입점 ====
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("사용법: dotnet run -- server 또는 dotnet run -- client");
            return;
        }
        
        if (args[0] == "server")
            Server.Start();
        else if (args[0] == "client")
            Client.Start();
        else
            Console.WriteLine("server 또는 client 를 입력하세요.");
    }
}
// 테스트 주의 사항
// 터미널 두개 사용해야합니다! (하나는 서버 역할 / 하나는 클라이언트 역할)
// 서버 역할부터 기동하고 클라이언트 접속하는 식으로 진행해야합니다.

// ==== 실행 결과 (서버) ====
// [서버] 시작 - 포트 8080 대기중......
// [서버] 클라이언트 연결됨!
// [서버] 수신: 안녕!

// ==== 실행 결과 (클라이언트) ====
// [클라이언트] 서버에 연결됨!
// 메세지 입력(exit 종료): 안녕!
// [클라이언트] 수신: 에코: 안녕!
// 메세지 입력(exit 종료): exit
// [클라이언트] 연결 종료