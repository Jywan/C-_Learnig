using System;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using MessagePack;


// [보강] 03-5. protobuf / MessagePack 직렬화
// 기존 03_Packet_Serialization에서 JSON과 수동 바이너리만 다뤘기에 보강
// 실제 게임서버에서는 JSON 대신 바이너리 직렬화 라이브러리를 사용함. - 크기 작고 빠름!

// ==== 사전준비: NuGet 패키지 설치 ====
// dotnet add package Google.Protobuf
// dotnet add package MessagePack

// ==== 왜 JSON이 게임에 부적합한가? ====
// 1. 크기가 큼 - 필드명이 문자열로 들어감 ("Type": 1 vs 그냥 바이트 1개)
// 2. 파싱이 느림 - 문자열 파싱은 CPU 비용이 높음
// 3. GC 압박 - 매번 string 객체 생성 -> 게임 루프에서 프레임 드랍 유발

// 대안: 바이너리 직렬화 라이브러리
//      - protobuf (Google Protocol Buffer): 구글이 만든 업계표준. gRPC 기반.
//      - MessagePack: C#/.NET 최적화, Unity에서 많이 사용, 속도 빠름

// 비교:
// | 항목         | JSON        | protobuf     | MessagePack  |
// // |-------------|-------------|--------------|--------------|
// // | 크기         | 큼          | 매우 작음     | 작음          |
// // | 속도         | 느림        | 빠름          | 매우 빠름     |
// // | 사람 읽기    | 가능        | 불가          | 불가          |
// // | 스키마       | 없음        | .proto 필요   | 어트리뷰트    |
// // | Unity 지원   | 기본 제공   | 가능          | 공식 지원     |


// ==== 1. MessagePack 패킷 정의 ====
// [MessagePackObject]: 이 클래스를 MessagePack으로 직렬화 가능하게 만듦
// [Key(n)]: 필드 순서 번호 - 필드명 대신 숫자로 식별 (크기 절약!)
[MessagePackObject]
public class MovePacket
{
    [Key(0)]
    public int PlayerId { get; set; }
    
    [Key(1)]
    public float X { get; set; }
    
    [Key(2)]
    public float Y { get; set; }
    
    [Key(3)]
    public float Z { get; set; }
    
    [Key(4)]
    public long Timestamp { get; set; }
}

[MessagePackObject]
public class ChatPacket
{
    [Key(0)]
    public int PlayerId { get; set; }
    
    [Key(1)]
    public string Message { get; set; }
    
    [Key(2)]
    public long Timestamp { get; set; }
}


// ==== 2. JSON 비교용 클래스 ====
public class MovePacketJson
{
    public int PlayerId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public long Timestamp { get; set; }
}


class Program
{
    static void Main(string[] args)
    {
        // ==== 1. MessagePack 직렬화/역직렬화 ====
        Console.WriteLine("==== MessagePack ====");
        MovePacket move = new MovePacket
        {
            PlayerId = 42,
            X = 10.5f,
            Y = 0.0f,
            Z = -3.2f,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 직렬화: 객체 → byte[]
        byte[] msgpackBytes = MessagePackSerializer.Serialize(move);
        Console.WriteLine($"MessagePack 크기: {msgpackBytes.Length} bytes");
        Console.Write("바이트: ");
        Console.WriteLine(BitConverter.ToString(msgpackBytes));

        // 역직렬화: byte[] → 객체
        MovePacket deserialized = MessagePackSerializer.Deserialize<MovePacket>(msgpackBytes);
        Console.WriteLine($"복원: Player={deserialized.PlayerId}, ({deserialized.X}, {deserialized.Y}, {deserialized.Z})");

        // ==== 2. JSON과 크기 비교 ====
        Console.WriteLine("\n==== JSON vs MessagePack 크기 비교 ====");
        MovePacketJson jsonMove = new MovePacketJson
        {
            PlayerId = 42,
            X = 10.5f,
            Y = 0.0f,
            Z = -3.2f,
            Timestamp = move.Timestamp
        };

        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(jsonMove));
        Console.WriteLine($"JSON 크기:        {jsonBytes.Length} bytes");
        Console.WriteLine($"MessagePack 크기: {msgpackBytes.Length} bytes");
        Console.WriteLine($"절약률: {100 - (msgpackBytes.Length * 100 / jsonBytes.Length)}%");

        // ==== 3. 채팅 패킷 예시 ====
        Console.WriteLine("\n==== 채팅 패킷 ====");
        ChatPacket chat = new ChatPacket
        {
            PlayerId = 1,
            Message = "안녕하세요! 게임 서버 테스트입니다.",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        byte[] chatBytes = MessagePackSerializer.Serialize(chat);
        byte[] chatJsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chat));
        Console.WriteLine($"JSON 크기:        {chatJsonBytes.Length} bytes");
        Console.WriteLine($"MessagePack 크기: {chatBytes.Length} bytes");

        ChatPacket chatRestored = MessagePackSerializer.Deserialize<ChatPacket>(chatBytes);
        Console.WriteLine($"복원: [{chatRestored.PlayerId}] {chatRestored.Message}");

        // ==== 4. 대량 직렬화 성능 체감 ====
        Console.WriteLine("\n==== 성능 비교 (1만 패킷) ====");
        int count = 10000;

        // MessagePack
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            byte[] data = MessagePackSerializer.Serialize(move);
            MessagePackSerializer.Deserialize<MovePacket>(data);
        }
        sw.Stop();
        Console.WriteLine($"MessagePack: {sw.ElapsedMilliseconds}ms");

        // JSON
        sw.Restart();
        for (int i = 0; i < count; i++)
        {
            string json = JsonSerializer.Serialize(jsonMove);
            JsonSerializer.Deserialize<MovePacketJson>(json);
        }
        sw.Stop();
        Console.WriteLine($"JSON:        {sw.ElapsedMilliseconds}ms");

        // ==== 5. 프레이밍과 조합 ====
        // 실제 게임 서버에서는 [패킷타입 2바이트][길이 4바이트][MessagePack 바디] 형태!
        Console.WriteLine("\n==== 프레이밍 + MessagePack 조합 ====");
        short packetType = 3;   // Move = 3
        byte[] body = MessagePackSerializer.Serialize(move);
        byte[] fullPacket = new byte[2 + 4 + body.Length];

        BinaryPrimitives.WriteInt16BigEndian(fullPacket.AsSpan(0), packetType);
        BinaryPrimitives.WriteInt32BigEndian(fullPacket.AsSpan(2), body.Length);
        Buffer.BlockCopy(body, 0, fullPacket, 6, body.Length);

        Console.WriteLine($"전체 패킷 크기: {fullPacket.Length} bytes");
        Console.WriteLine($"  헤더: 타입({packetType}) + 길이({body.Length})");
        Console.WriteLine($"  바디: MessagePack ({body.Length} bytes)");

        // 수신 측 파싱
        short readType = BinaryPrimitives.ReadInt16BigEndian(fullPacket.AsSpan(0));
        int readLen = BinaryPrimitives.ReadInt32BigEndian(fullPacket.AsSpan(2));
        byte[] readBody = new byte[readLen];
        Buffer.BlockCopy(fullPacket, 6, readBody, 0, readLen);

        MovePacket parsed = MessagePackSerializer.Deserialize<MovePacket>(readBody);
        Console.WriteLine($"  파싱: 타입={readType}, Player={parsed.PlayerId}, 위치=({parsed.X}, {parsed.Y}, {parsed.Z})");
    }
}

// 실행 결과
// ==== MessagePack ====
// MessagePack 크기: 26 bytes
// 바이트: 95-2A-CA-41-28-00-00-CA-00-00-00-00-CA-C0-4C-CC-CD-CF-00-00-01-9E-E9-3A-5F-85
// 복원: Player=42, (10.5, 0, -3.2)
//
// ==== JSON vs MessagePack 크기 비교 ====
// JSON 크기:        65 bytes
// MessagePack 크기: 26 bytes
// 절약률: 60%
//
// ==== 채팅 패킷 ====
// JSON 크기:        148 bytes
// MessagePack 크기: 63 bytes
// 복원: [1] 안녕하세요! 게임 서버 테스트입니다.
//
// ==== 성능 비교 (1만 패킷) ====
// MessagePack: 5ms
// JSON:        11ms
//
// ==== 프레이밍 + MessagePack 조합 ====
// 전체 패킷 크기: 32 bytes
//   헤더: 타입(3) + 길이(26)
//   바디: MessagePack (26 bytes)
//   파싱: 타입=3, Player=42, 위치=(10.5, 0, -3.2)