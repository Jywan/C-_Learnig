using System;
using System.Buffers.Binary;
using System.Net;

// [보강] 01-5. 바이트 오더 (엔디안), 네트워크 바이트 순서
// 기존 01_Socket_Basics에서 BitConverter로 바이트 변환만 했기 때문에 보강
// 크로스플랫폼 통신 시 엔디안 안맞으면 데이터가 깨지는 문제를 이해하기 위한 챕터

// ==== 엔디안(Endianness)이란? ====
// 멀티바이트 데이터를 메모리에 저장하는 순서

// 리틀 엔디안 (Little-Endian):
//      - 낮은 바이트가 앞에 옴 (Intel x86, ARM 기본)
//      - 0x12345678 -> [78 ,56, 34, 12]
//      - Window, macOs, 대부분의 PC

// 빅 엔디안 (Big-Endian)
//      - 높은 바이트가 앞에 옴 (네트워크 표준, 일부 임베디드)
//      - 0x12345678 -> [12, 34, 56, 78]
//      - 네트워크 바이트 순서(Network Byte Order) = 빅 엔디안

// 왜 중요한가? 
//      - 서버(리틀)와 클라이언트(빅)가 다르면 int 1이 16777216으로 읽힘
//      - 게임 서버는 다양한 플랫폼(PC, 모바일, 콘솔)이 접속하므로 통일 필수!
//      - 해결법: 네트뤄크로 보낼 때 빅엔디안으로 통일하거나, 프로토콜에 명시


class Program
{
    static void Main(string[] args)
    {
        // ==== 1. 현재 시스템의 엔디안 확인 ====
        Console.WriteLine("==== 1. 시스템 엔디안 ====");
        Console.WriteLine($"리틀 엔디안? {BitConverter.IsLittleEndian}");        // 대부분은 True 출력됨
        
        // ==== 2. 같은 숫자, 다른 바이트 순서 ====
        Console.WriteLine("\n==== 2. 바이트 순서 비교 ====");
        int value = 0x12345678;
        
        byte[] littleBytes = BitConverter.GetBytes(value);      // 시스템 순서 (리틀)
        Console.WriteLine("리틀 엔디안: ");
        PrintBytes(littleBytes);            // 78, 56, 34,12
        
        // 빅 엔디안으로 변환
        byte[] bigBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bigBytes, value);
        Console.Write("빅 엔디안: ");
        PrintBytes(bigBytes);               // 12, 34, 56, 78
        
        
        // ==== 3. 네트워크 바이트 순서 변환 ====
        // IPAddress.HostToNetworkOrder: 호스트 -> 네트워크(빅) 변환
        // IPAddress.NetworkToHostOrder: 네트워크(빅) -> 호스트 변환
        Console.WriteLine("\n==== 3. 네트워크 바이트 순서 ====");
        int hostValue = 1000;
        int networkValue = IPAddress.HostToNetworkOrder(hostValue);
        int backToHost = IPAddress.NetworkToHostOrder(networkValue);
        
        Console.WriteLine($"호스트 값: {hostValue}");
        Console.WriteLine($"네트워크 순서: {networkValue}");      // 리틀 -> 빅 변환된 값
        Console.WriteLine($"다시 호스트: {backToHost}");         // 원래 값 복원
        
        Console.Write("호스트 바이트: ");
        PrintBytes(BitConverter.GetBytes(hostValue));         // E8 03 00 00
        Console.Write("네트워크 바이트: ");
        PrintBytes(BitConverter.GetBytes(networkValue));      // 00 00 03 E8
        
        
        // ==== 4. BinaryPrimitives (현대적 방식) ====
        // .NET core부터 권장. Span<byte> 기반으로 할당 없이 변환
        Console.WriteLine("\n==== 4. BinaryPrimitives ====");
        byte[] buffer = new byte[8];
        
        // 쓰기
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0), 42);
        BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(4), 7777);
        
        Console.Write("버퍼 내용: ");
        PrintBytes(buffer);
        
        // 읽기
        int readInt = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0));
        short readShort = BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(4));
        Console.WriteLine($"읽은 int: {readInt}");        // 42
        Console.WriteLine($"읽은 short: {readShort}");    // 7777
        
        
        // ==== 5. 게임 패킷에서의 실전 예시 ====
        Console.WriteLine("\n==== 5. 게임 패킷 헤더 (빅 엔디안) ====");
        byte[] packet = CreatePacketHeader(packetType: 2, bodyLength: 128);
        Console.Write("패킷 헤더: ");
        PrintBytes(packet);
        
        // 수신 측에서 파싱
        ParsePacketHeader(packet);
        
        
        // ==== 6. 엔디안 실수 시뮬레이션 ====
        Console.WriteLine("\n==== 6. 엔디안 불일치 시뮬레이션 ====");
        int sendValue = 1;
        byte[] sent = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(sent, sendValue);      // 빅으로 보냄
        
        // 수신 측이 리틀로 잘못 읽으면??
        int wrongRead = BitConverter.ToInt32(sent, 0);      // 리틀로 읽음
        int correctRead = BinaryPrimitives.ReadInt32BigEndian(sent);    // 빅으로 읽음
        
        Console.WriteLine($"보낸 값: {sendValue}");
        Console.WriteLine($"엔디안 틀리게 읽음: {wrongRead}");      // 16777216
        Console.WriteLine($"엔디안 맞게 읽음: {correctRead}");     // 1
    }
    
    // 패킷 헤더 생성 (빅 엔디안): [타입 2바이트][길이 4바이트]
    static byte[] CreatePacketHeader(short packetType, int bodyLength)
    {
        byte[] header = new byte[6];
        BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(0), packetType);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(2), bodyLength);
        return header;
    }

    // 패킷 헤더 파싱
    static void ParsePacketHeader(byte[] header)
    {
        short type = BinaryPrimitives.ReadInt16BigEndian(header.AsSpan(0));
        int length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(2));
        Console.WriteLine($"패킷 타입: {type}, 바디 길이: {length}");
    }
    
    // 바이트 배열 출력 헬퍼
    static void PrintBytes(byte[] bytes)
    {
        Console.WriteLine(BitConverter.ToString(bytes).Replace("-", " "));
    }
}

// 실행 결과
// ==== 1. 시스템 엔디안 ====
// 리틀 엔디안? True
//
// ==== 2. 바이트 순서 비교 ====
// 리틀 엔디안: 
// 78 56 34 12
// 빅 엔디안: 12 34 56 78
//
// ==== 3. 네트워크 바이트 순서 ====
// 호스트 값: 1000
// 네트워크 순서: -402456576
// 다시 호스트: 1000
// 호스트 바이트: E8 03 00 00
// 네트워크 바이트: 00 00 03 E8
//
// ==== 4. BinaryPrimitives ====
// 버퍼 내용: 00 00 00 2A 1E 61 00 00
// 읽은 int: 42
// 읽은 short: 7777
//
// ==== 5. 게임 패킷 헤더 (빅 엔디안) ====
// 패킷 헤더: 00 02 00 00 00 80
// 패킷 타입: 2, 바디 길이: 128
//
// ==== 6. 엔디안 불일치 시뮬레이션 ====
// 보낸 값: 1
// 엔디안 틀리게 읽음: 16777216
// 엔디안 맞게 읽음: 1