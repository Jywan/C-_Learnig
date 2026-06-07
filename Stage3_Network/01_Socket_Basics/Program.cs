using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

// ==== 1. 소켓이란? ====
// 네트워크 통신의 끝점(Endpoint) - IP주소 + 포트번호로 식별
// TCP: 연결 기반, 신뢰성 보장 (순서, 재전송)
// UDP: 비연결, 빠르지만 신뢰성 없음

class SocketBasic
{
    // 2. ==== TCP 서버 (단순 버전) ====
    public static void StartServer()
    {
        // TcpListener: 클라이언트 연결을 기다리는 서버 소켓
        TcpListener server = new TcpListener(IPAddress.Any, 8080);      // 모든 IP, 8080 포트
        server.Start();
        Console.WriteLine("서버 시작 - 포트 8080 대기중....");
        
        TcpClient client = server.AcceptTcpClient();        // 클라이언트 연결될 때까지 블로킹
        Console.WriteLine("클라이언트 연결됨!");
        
        NetworkStream stream = client.GetStream();
        
        // 데이터 수신
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"수신: {message}");
        
        // 데이터 송신
        byte[] response = Encoding.UTF8.GetBytes($"서버 응답: " + message);
        stream.Write(response, 0, response.Length);
        
        client.Close();
        server.Stop();
    }
    
    // ==== 3. TCP 클라이언트 (단순 테스트 버전) ====
    public static void StartClient()
    {
        TcpClient client = new TcpClient();
        client.Connect("127.0.0.1", 8080);      // 로컬 호스트 8080 포트에 연결
        Console.WriteLine("서버에 연결됨!");
        
        NetworkStream stream = client.GetStream();
        
        // 데이터 송신
        string message = "안녕하세요. 서버~";
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
        
        // 데이터 수신
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"수신: {response}");
        
        client.Close();
    }
}

// ==== 4. 소켓 핵심 개념 ====
class SocketConcept
{
    public static void ShowEndpointInfo()
    {
        // IPAddress: IP 주소를 나타내는 클래스
        IPAddress localIP = IPAddress.Parse("127.0.0.1");   // 루프 백 주소 = 자기 자신
        IPAddress AnyIp = IPAddress.Any;                    // 0.0.0.0 = 모든 인터페이스
        
        // IPEndPoint: IP + 포트를 합친 주소
        IPEndPoint endPoint = new IPEndPoint(localIP, 8080);
        Console.WriteLine($"EndPoint: {endPoint}");         // 127.0.0.1:8080
        
        // DNS로 호스트명 조회
        string hostName = Dns.GetHostName();
        Console.WriteLine($"호스트 명: {hostName}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        // 소켓 개념 확인
        SocketConcept.ShowEndpointInfo();
        
        Console.WriteLine("\n 서버와 클라이언트를 각각 실행해야 통신이 가능합니다.");
        Console.WriteLine("서버 실행: StartServer()");
        Console.WriteLine("클라이언트 실행: StartClient()");
        
        // 실제 통신 테스트는 다음 스텝에서 진행될 예정!
    }
}

// 실행 결과
// EndPoint: 127.0.0.1:8080
// 호스트 명: {내 데스크탑 호스트 출력}.local
//
//  서버와 클라이언트를 각각 실행해야 통신이 가능합니다.
// 서버 실행: StartServer()
// 클라이언트 실행: StartClient()