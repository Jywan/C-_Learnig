using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// [보강] 02-5. 패킷 프레이밍 (길이 기반 메시지 분리)
// 기존 02_TCP_Server_Client에서 Read 한번에 메시지 하나라고 가정했기에 보강
// TCP는 스트림이라 메시지 경계가 없음 - 실제 게임 서버에서는 반드시 프레이밍 필요!

// ==== TCP 스트림의 문제 ====
// TCP는 바이트 스트림이다. 메시지 단위가 아님!

// 보내는 쪽 : "Hello" + "World" (2번 write)
// 받는 쪽에서 일어날 수 있는 경우 :
//      1. "HelloWorld"         -> 두 메세지가 붙어서 옴(합쳐짐)
//      2. "Hel" + "loWorld"    -> 하나가 쪼개져서 옴 (분리됨)
//      3. "Hello" + "World"    -> 운 좋게 딱 맞음 (보장 안됨)

// 해결법: 패킷 프레이밍 (Packet Framing)
//      - 각 메시지 앞에 길이를 붙여서 경계를 명시
//      - [4바이트 길이][메시지 데이터] 형식이 가장 일반적

// ==== 패킷 구조 ====
// [Header: 4바이트 길이(빅 엔디안)] + [Body: 실제 데이터]
// 수신 측은 먼저 4바이트를 읽어 길이를 파악한 뒤, 그만큼 더 읽음

class PacketFramer
{
    // ==== 메시지를 프레임으로 감싸기 ====
    // [4바이트 길이][메시지 바이트] 형태로 생성
    public static byte[] Frame(string message)
    {
        byte[] body = Encoding.UTF8.GetBytes(message);
        byte[] packet = new byte[4 + body.Length];
        
        // 길이를 빅 엔디안으로 기록 (Stage3 01-5 학습 내용)
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(0), body.Length);
        Buffer.BlockCopy(body, 0, packet, 4, body.Length);
        
        return packet;
    }
    
    
    // ==== 스트림에서 정확히 n바이트 읽기 ====
    // TCP는 요청한 만큼 한번에 안 올 수 있음 - 반복해서 채워야함!
    public static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;

        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer, offset, count - offset);
            if (read == 0)
                throw new Exception("연결 종료됨");      // 상대방이 끊음
            offset += read;
        }
        
        return buffer;
    }
    
    
    // ==== 프레임 단위로 메시지 수신 ====
    // 1. 헤더(4바이트) 읽기 - 길이 파악
    // 2. 바디(길이만큼) 읽기 - 메시지 복원
    public static async Task<string> ReceiveMessageAsync(NetworkStream stream)
    {
        byte[] header = await ReadExactAsync(stream, 4);
        int bodyLength = BinaryPrimitives.ReadInt32BigEndian(header);
        
        byte[] body = await ReadExactAsync(stream, bodyLength);
        return Encoding.UTF8.GetString(body);
    }
    
    
    // ==== 프레임 단위로 메시지 송신 ====
    public static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        byte[] packet = Frame(message);
        await stream.WriteAsync(packet, 0, packet.Length);
    }
}


// ==== 에코 서버 (프레이밍 적용) ====
class FramedServer
{
    public static async Task StartAsync(int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[서버] 프레이밍 서버 시작 - 포트: {port}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("[서버] 클라이언트 연결됨!");
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        try
        {
            while (true)
            {
                // 프레이밍된 메시지 수신 - 길이 기반이라 경계 문제 없음!
                string message = await PacketFramer.ReceiveMessageAsync(stream);
                Console.WriteLine($"[서버] 수신: {message}");

                // 에코 응답
                string response = $"에코: {message}";
                await PacketFramer.SendMessageAsync(stream, response);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 연결 종료: {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}


// ==== 클라이언트 (프레이밍 적용) ====
class FrameClient
{
    public static async Task StartAsync(int port)
    {
        TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", port);       // 로컬 테스트 진행
        Console.WriteLine("[클라이언트] 서버에 연결됨!");
        
        NetworkStream stream = client.GetStream();

        while (true)
        {
            Console.Write("메시지 입력 (exit 종료): ");
            string input = Console.ReadLine();
            if (input == "exit") break;
            
            // 프레이밍해서 송신
            await PacketFramer.SendMessageAsync(stream, input);
            
            // 프레이밍된 응답 수신
            string response = await PacketFramer.ReceiveMessageAsync(stream);
            Console.WriteLine($"[클라이언트] 수신: {response}");
        }
        
        client.Close();
        Console.WriteLine("[클라이언트] 연결 종료");
    }
}


// ==== 프레이밍 vs 비프레이밍 비교 데모 ====
class FramingDemo
{
    public static void ShowFrameStructure()
    {
        Console.WriteLine("==== 프레임 구조 데모 ====");

        string msg1 = "Hello";
        string msg2 = "게임서버";
        string msg3 = "패킷 프레이밍 테스트!";
        
        byte[] frame1 = PacketFramer.Frame(msg1);
        byte[] frame2 = PacketFramer.Frame(msg2);
        byte[] frame3 = PacketFramer.Frame(msg3);
        
        Console.WriteLine($"\"{msg1}\" -> {frame1.Length}바이트 (헤더4 + 바디{frame1.Length - 4}) ");
        Console.Write(" 바이트: ");
        Console.WriteLine(BitConverter.ToString(frame1));
        
        Console.WriteLine($"\"{msg2}\" → {frame2.Length}바이트 (헤더4 + 바디{frame2.Length - 4})");
        Console.Write("  바이트: ");
        Console.WriteLine(BitConverter.ToString(frame2));

        Console.WriteLine($"\"{msg3}\" → {frame3.Length}바이트 (헤더4 + 바디{frame3.Length - 4})");
        Console.Write("  바이트: ");
        Console.WriteLine(BitConverter.ToString(frame3));
        
        // 여러 메시지가 연속으로 왔을 때 파싱 시뮬레이션
        Console.WriteLine("\n==== 연속 프레임 파싱 시뮬레이션 ====");
        byte[] combined = new byte[frame1.Length + frame2.Length + frame3.Length];
        Buffer.BlockCopy(frame1, 0, combined, 0, frame1.Length);
        Buffer.BlockCopy(frame2, 0, combined, frame1.Length, frame2.Length);
        Buffer.BlockCopy(frame3, 0, combined, frame1.Length + frame2.Length, frame3.Length);
        
        Console.WriteLine($"합쳐진 버퍼 크기: {combined.Length}바이트");
        Console.WriteLine("파싱 시작...");

        int offset = 0;
        int msgIndex = 0;

        while (offset < combined.Length)
        {
            // 헤더에서 길이 읽기 
            int bodyLen = BinaryPrimitives.ReadInt32BigEndian(combined.AsSpan(offset));
            offset += 4;
            
            // 바디 읽기
            string parsed = Encoding.UTF8.GetString(combined, offset, bodyLen);
            offset += bodyLen;
            
            Console.WriteLine($" 메시지 {msgIndex}: \"{parsed}\" + ({bodyLen}바이트)");
            msgIndex++;
        }
    }
}


class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            // 인자 없으면 프레임 구조 데모만 실행
            FramingDemo.ShowFrameStructure();
            
            Console.WriteLine("\n==== TCP 테스트 ====");
            Console.WriteLine("서버: dotnet run -- server");
            Console.WriteLine("클라이언트: dotnet run -- client");
            return;
        }

        int port = 8084;
        
        if (args[0] == "server")
            await FramedServer.StartAsync(port);
        else if (args[0] == "client")
            await FrameClient.StartAsync(port);
    }
}

// 실행 결과
// ==== 프레임 구조 데모 ====
// "Hello" -> 9바이트 (헤더4 + 바디5) 
//  바이트: 00-00-00-05-48-65-6C-6C-6F
// "게임서버" → 16바이트 (헤더4 + 바디12)
//   바이트: 00-00-00-0C-EA-B2-8C-EC-9E-84-EC-84-9C-EB-B2-84
// "패킷 프레이밍 테스트!" → 34바이트 (헤더4 + 바디30)
//   바이트: 00-00-00-1E-ED-8C-A8-ED-82-B7-20-ED-94-84-EB-A0-88-EC-9D-B4-EB-B0-8D-20-ED-85-8C-EC-8A-A4-ED-8A-B8-21
//
// ==== 연속 프레임 파싱 시뮬레이션 ====
// 합쳐진 버퍼 크기: 59바이트
// 파싱 시작...
//  메시지 0: "Hello" + (5바이트)
//  메시지 1: "게임서버" + (12바이트)
//  메시지 2: "패킷 프레이밍 테스트!" + (30바이트)
//
// ==== TCP 테스트 ====
// 서버: dotnet run -- server
// 클라이언트: dotnet run -- client

// 실행 결과 (서버/클라이언트 테스트 시)
// [서버] 프레이밍 서버 시작 - 포트 8084
// [서버] 클라이언트 연결됨!
// [서버] 수신: 안녕하세요
//
// [클라이언트] 서버에 연결됨!
// 메시지 입력 (exit 종료): 안녕하세요
// [클라이언트] 수신: 에코: 안녕하세요