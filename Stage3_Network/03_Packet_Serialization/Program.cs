using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

// ==== 1. 패킷이란? ====
// 네트워크로 주고받는 데이터 단위
// 헤더(타입, 길이 등) + 바디(실제 데이터) 로 구성

// ==== 2. 패킷 타입 정의 ====
enum PacketType
{
    Login = 1,
    Chat = 2,
    Move = 3,
    Logout = 4
}

// ==== 3. 패킷 클래스 ====
class Packet
{
    public PacketType Type { get; set; }
    public string Data { get; set; }
    public DateTime TimeStamp { get; set; }

    public Packet(PacketType type, string data)
    {
        Type = type;
        Data = data;
        TimeStamp = DateTime.Now;
    }
}

// ==== 4. 패킷 직렬화 - JSON ====
class JsonSerializer
{
    public static byte[] Serialize(Packet packet)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(packet);
        return Encoding.UTF8.GetBytes(json);        // 문자열 -> 바이트 전환
    }

    public static Packet Deserialize(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        return System.Text.Json.JsonSerializer.Deserialize<Packet>(json);
    }
}

// ==== 5. 패킷 직렬화 - 수동 바이너리 ====
// JSON보다 크기가 작고 빠름 - 게임 서버에서 자주 사용됩니다!
class BinaryPacket
{
    public static byte[] Serialize(int type, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] packet = new byte[4 + 4 + messageBytes.Length];      // type(4) + lenght(4) + data
        
        // BitConverter: 기본 타입을 byte[]로 변환
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, packet, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(messageBytes.Length), 0, packet, 4, 4);
        Buffer.BlockCopy(messageBytes, 0, packet, 8, messageBytes.Length);
        
        return packet;
    }

    public static (int type, string message) Deserialize(byte[] packet)
    {
        int type = BitConverter.ToInt32(packet, 0);
        int length = BitConverter.ToInt32(packet, 4);
        string message = Encoding.UTF8.GetString(packet, 8, length);
        
        return (type, message);
    }
}

class Program
{
    static void Main(string[] args)
    {
        // ==== JSON 직렬화 ====
        Packet loginPacket = new Packet(PacketType.Login, "장영완");
        Packet chatPacket = new Packet(PacketType.Chat, "안녕하세요!");
        
        byte[] loginData = JsonSerializer.Serialize(loginPacket);
        byte[] chatData = JsonSerializer.Serialize(chatPacket);
        
        Console.WriteLine("==== JSON 직렬화 ====");
        Console.WriteLine($"로그인 패킷 크기: {loginData.Length} bytes");
        Console.WriteLine($"채팅 패킷 크기: {chatData.Length} bytes");
        
        // 역질렬화
        Packet received = JsonSerializer.Deserialize(loginData);
        Console.WriteLine($"수신 타입: {received.Type}, 데이터: {received.Data}");
        
        // 패킷 타입으로 분기
        switch (received.Type)
        {
            case PacketType.Login:
                Console.WriteLine($"[LOGIN] {received.Data} 접속");
                break;
            case PacketType.Chat:
                Console.WriteLine($"[CHAT] {received.Data}");
                break;
            case PacketType.Move:
                Console.WriteLine($"[MOVE] {received.Data}");
                break;
            case PacketType.Logout:
                Console.WriteLine($"[LOGOUT] {received.Data}");
                break;
        }
        
        // ==== 바이너리 직렬화 ====
        Console.WriteLine("\n==== 바니너리 직렬화 ====");
        byte[] binaryPacket = BinaryPacket.Serialize((int) PacketType.Chat, "안녕!");
        Console.WriteLine($"바이너리 패킷 크기 : {binaryPacket.Length} bytes");
        
        var (type, message) =  BinaryPacket.Deserialize(binaryPacket);
        Console.WriteLine($"수신 타입: {(PacketType)type}, 메시지: {message}");
    }
}

// 실행 결과!
// ==== JSON 직렬화 ====
// 로그인 패킷 크기: 85 bytes
// 채팅 패킷 크기: 98 bytes
// 수신 타입: Login, 데이터: 장영완
// [LOGIN] 장영완 접속
//
// ==== 바니너리 직렬화 ====
// 바이너리 패킷 크기 : 15 bytes
// 수신 타입: Chat, 메시지: 안녕!