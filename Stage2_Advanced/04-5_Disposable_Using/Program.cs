using System;
using System.Collections.Generic;
using System.IO;

// [보강] 04-5 IDisposable, using 패턴
// 기존 04_Exception_Handling 에서 try/finally 만 다루었기에 보강
// 게임 서버에서 소켓, 파일, DB 연결 등 비관리 리소스 정리를 위한 챕터

// ==== 관리 리소스 vs 비관리 리소스 ====
// 관리 리소스: GC가 자동 수거 (일반 C# 객체, string, List 등)
// 비관리 리소스: GC가 모름, 직접 해제해야함.
//      - 파일 핸들, 네트워크 소켓, DB 연결
//      - 네이티브 메모리, GPU 리소스, 뮤텍스(이건 처음들어봄)
//      - Unity: NativeArray, ComputeBuffer, RenderTexture

// ==== IDisposable이란? ====
// "나는 정리해야 할 리소스를 가지고 있다"를 선언하는 인터페이스
// Dispose() 메서드를 구현하여 리소스 정리 로직을 작성
// using 문과 함께 사용하면 자동으로 Dispose() 호출 보장

// ==== Dispose 패턴의 필요성 ====
// finally에 직접 정리 코드를 쓸 수도 있지만:
//      - 매번 try/finally 작성은 번거롭고 실수 유발
//      - using 문은 컴파일러가 try/finally를 자동 생성 -> 안전 + 간결


// ==== 1. IDisposable 시본 구현 ====
class FileLogger : IDisposable
{
    private StreamWriter? _writer;
    private bool _disposed = false;     // 중복 Dispose 방지

    public FileLogger(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true);
        Console.WriteLine($"[FileLogger] 파일 열림: {filePath}");
    }

    public void Log(string message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileLogger));
        _writer!.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}]");
        Console.WriteLine($"[FileLogger] 기록: {message}");
    }
    
    // IDisposable 구현
    public void Dispose()
    {
        if (!_disposed)
        {
            _writer?.Close();           // 비관리 리소스 정리
            _writer = null;
            _disposed = true;
            Console.WriteLine("[FileLogger] 파일 닫힘 (Dispose 완료)");
        }
    }
}


// ==== 2. 전체 Dispose 패턴 (파이널라이저 포함) ====
// 실무에서 비관리 리소스를 직접 다룰 때 사용하는 완전한 패턴
// 파이널라이저(소멸자): Dispose를 안 불렀을 때 최후의 안정망
class NetworkConnection : IDisposable
{
    private IntPtr _nativeHandle;           // 비관리 리소스 시뮬레이션
    private List<byte>? _buffer;            // 관리 리소스
    private bool _disposed = false;
    
    public string Address { get; }

    public NetworkConnection(string address)
    {
        Address = address;
        _nativeHandle = new IntPtr(12345);      // 실제로는 OS 핸들
        _buffer = new List<byte>(1024);
        Console.WriteLine($"[Network] 연결됨: {address}");
    }

    public void Send(string data)
    {
        ObjectDisposedCheck();
        Console.WriteLine($"[Network] 전송: {data}");
    }
    
    // protected virtual - 자식클래스가 override 가능
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 관리 리소스 정리 (Dispose에서 호출될 때만)
                _buffer?.Clear();
                _buffer = null;
                Console.WriteLine("[Network] 관리 리소스 정리 완료");
            }
            
            // 비관리 리소스 정리 (항상 실행 - 파이널라이저에서도..)
            if (_nativeHandle != IntPtr.Zero)
            {
                // 실제로는: CloseHandle(_nativeHandle), socket.Close() 등
                _nativeHandle = IntPtr.Zero;
                Console.WriteLine("[Network] 비관리 리소스 해제 완료");
            }
            
            _disposed = true;
        }
    }
    
    // 공개 Dispose - using문에서 호출됨
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);      // 파이널라이저 호출 불필요하게 만듦
        Console.WriteLine($"[Network] 연결 종료: {Address}");
    }
    
    // 파이널라이저(소멸자) - Dispose 안 불렀을 때 최후의 안전망
    // GC가 수거할 때 호출됨, 하지만 호출 시점이 불확정적
    ~NetworkConnection()            // ~  해당 형태는 처음 접함.
    {
        Console.WriteLine("[Network] 파이널라이저 호출 (Dispose 안 불린 것)");
        Dispose(disposing: false);  // 관리 리소스는 이미 GC 됐을 수 있으므로 false
    }
    
    private void ObjectDisposedCheck()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NetworkConnection));
    }
}


// ==== 3. 여러 리소스를 관리하논 클래스 ====
class GameSession : IDisposable
{
    private NetworkConnection? _connection;
    private FileLogger _logger;
    private bool _disposed = false;

    public GameSession(string severAddress, string logPath)
    {
        _connection = new NetworkConnection(severAddress);
        _logger = new FileLogger(logPath);
        Console.WriteLine("[Session] 게임 세션 시작");
    }

    public void SendMessage(string msg)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GameSession));
        _logger!.Log($"송신: {msg}");
        _connection!.Send(msg);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // 소유한 IDisposable 리소스를 순서대로 정리
            _connection?.Dispose();
            _logger?.Dispose();
            _disposed = true;
            Console.WriteLine("[Session] 세션 종료\n");
        }
    }
}


// ==== 4. 제네릭 리소스 관리자 - 게임 오브젝트 풀에서 활용 ====
class ResourceHandle<T> : IDisposable where T : class
{
    public T Resource { get; }
    private Action<T>? _onDispose;      // 반환 롤백
    private bool _disposed = false;
    
    public ResourceHandle(T resource, Action<T> onDispose)
    {
        Resource = resource;
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _onDispose?.Invoke(Resource);       // 풀에 반환하는 동작
            _onDispose = null;
            _disposed = true;
        }
    }
}


class Program
{
    static void Main(string[] args)
    {
        // ==== 1. using 문 - 기본 사용 ====
        Console.WriteLine("==== 1. using 문 기본 ====");
        
        // using 블록이 끝나면 자동으로 Dispose() 호출
        // 예외가 발생해도 반드시 호출됨 (내부적으로 try/finally)
        using (FileLogger logger = new FileLogger("test.log"))
        {
            logger.Log("게임 시작");
            logger.Log("플레이어 접속");
        } // <- 여기서 자공 Dispose()
        
        Console.WriteLine("(using 블록 벗어남)\n");
        
        
        // ==== 2. using 선언 (C# 8+) - 더 간결한 문법 ====
        Console.WriteLine("==== 2. using 선언 (C# 8+) ====");
        
        // 세미 콜론으로 선언하면 스코프 끝에서 자동 Dispose
        using FileLogger logger2 = new FileLogger("game.log");
        logger2.Log("간결한 문법");
        logger2.Log("스코프 끝에서 정리됨");
        // 메서드 끝에서 또는 블록 끝에서 Dispose
        
        Console.WriteLine();
        
        
        // ==== 3. 전체 Dispose ====
        Console.WriteLine("==== 3. 전체 Dispose 패턴 ====");

        using (NetworkConnection conn = new NetworkConnection("192.168.1.1:7777"))
        {
            conn.Send("Hello, World!");
            conn.Send("Login Request");
        }
        Console.WriteLine();
        
        
        // ==== 4. 여러 리소스 중첩 ====
        Console.WriteLine("==== 4. 여러 리소스 관리 ====");

        using (GameSession session = new GameSession("game.server.com", "session.log"))
        {
            session.SendMessage("접속 요청");
            session.SendMessage("캐릭터 선택");
        }
        
        
        // ==== 5. using 과 예외 ====
        Console.WriteLine("==== 5. using + 예외 처리 ====");

        try
        {
            using (NetworkConnection conn = new NetworkConnection("crash.server.com"))
            {
                conn.Send("정상 데이터");
                throw new Exception("서버 오류 시뮬레이션");         // 예외 발생
                // 예외 이후 Send 는 실행 X
                // 단 예외가 발생해도 Dispose는 반드시 실행됨.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 : {ex.Message}");
        }
        Console.WriteLine();
        
        
        // ==== 6. Dispose 안 부르면? ====
        Console.WriteLine("==== 6. Dispose 누락 시 ====");
        Console.WriteLine("파이널라이저가 최후의 안전망이지만:");
        Console.WriteLine("  - 호출 시점 불확정 (GC가 결정)");
        Console.WriteLine("  - 성능 비용 높음 (파이널라이저 큐 거쳐야 함)");
        Console.WriteLine("  - 관리 리소스는 이미 수거됐을 수 있음");
        Console.WriteLine("  -> 반드시 using 또는 명시적 Dispose() 사용!\n");
        
        
        // ==== 7. 리소스 핸들 패턴 - 풀 변환 ====
        Console.WriteLine("==== 7. ResourceHandle (풀 반환) ====");
        
        Queue<string> connectionPool = new Queue<string>();
        connectionPool.Enqueue("Conn-A");
        connectionPool.Enqueue("Conn-B");
        
        // 풀에서 꺼래서 using으로 사용, 끝나면 자동 반환
        using (var handle = new ResourceHandle<string>(
                   connectionPool.Dequeue(),
                   conn =>
                   {
                       connectionPool.Enqueue(conn); // Dispose 시 풀에 반환
                       Console.WriteLine($" 풀에 반환: {conn}");
                   }))
        {
            Console.WriteLine($" 사용 중: {handle.Resource}");
        }
        Console.WriteLine($" 풀 남은 수: {connectionPool.Count}\n");        // 2 (반환됨)
        
        // ==== 8. 정리 ====
        Console.WriteLine("==== 8. IDisposable 사용 가이드 ====");
        Console.WriteLine("| 상황                    | 방법              |");
        Console.WriteLine("|------------------------|-------------------|");
        Console.WriteLine("| 지역 변수로 잠깐 사용   | using 문/선언     |");
        Console.WriteLine("| 클래스 필드로 보유      | 클래스도 IDisposable 구현 |");
        Console.WriteLine("| 풀에서 대여/반환        | ResourceHandle 패턴 |");
        Console.WriteLine("| 비관리 리소스 직접 보유  | 전체 Dispose 패턴  |");
        Console.WriteLine("");
        Console.WriteLine("게임에서 IDisposable이 필요한 것들:");
        Console.WriteLine("  - TCP 소켓 (서버/클라이언트 연결)");
        Console.WriteLine("  - 파일 스트림 (세이브/로드)");
        Console.WriteLine("  - Unity: NativeArray, ComputeBuffer, RenderTexture");
        Console.WriteLine("  - 구독 해제 토큰 (이벤트 구독 → Dispose로 해제)");
    }
}

// 실행 결과
// ==== 1. using 문 기본 ====
// [FileLogger] 파일 열림: test.log
// [FileLogger] 기록: 게임 시작
// [FileLogger] 기록: 플레이어 접속
// [FileLogger] 파일 닫힘 (Dispose 완료)
// (using 블록 벗어남)
//
// ==== 2. using 선언 (C# 8+) ====
// [FileLogger] 파일 열림: game.log
// [FileLogger] 기록: 간결한 문법
// [FileLogger] 기록: 스코프 끝에서 정리됨
//
// ==== 3. 전체 Dispose 패턴 ====
// [Network] 연결됨: 192.168.1.1:7777
// [Network] 전송: Hello, World!
// [Network] 전송: Login Request
// [Network] 관리 리소스 정리 완료
// [Network] 비관리 리소스 해제 완료
// [Network] 연결 종료: 192.168.1.1:7777
//
// ==== 4. 여러 리소스 관리 ====
// [Network] 연결됨: game.server.com
// [FileLogger] 파일 열림: session.log
// [Session] 게임 세션 시작
// [FileLogger] 기록: 송신: 접속 요청
// [Network] 전송: 접속 요청
// [FileLogger] 기록: 송신: 캐릭터 선택
// [Network] 전송: 캐릭터 선택
// [Network] 관리 리소스 정리 완료
// [Network] 비관리 리소스 해제 완료
// [Network] 연결 종료: game.server.com
// [FileLogger] 파일 닫힘 (Dispose 완료)
// [Session] 세션 종료
//
// ==== 5. using + 예외 처리 ====
// [Network] 연결됨: crash.server.com
// [Network] 전송: 정상 데이터
// [Network] 관리 리소스 정리 완료
// [Network] 비관리 리소스 해제 완료
// [Network] 연결 종료: crash.server.com
// 예외 : 서버 오류 시뮬레이션
//
// ==== 6. Dispose 누락 시 ====
// 파이널라이저가 최후의 안전망이지만:
//   - 호출 시점 불확정 (GC가 결정)
//   - 성능 비용 높음 (파이널라이저 큐 거쳐야 함)
//   - 관리 리소스는 이미 수거됐을 수 있음
//   -> 반드시 using 또는 명시적 Dispose() 사용!
//
// ==== 7. ResourceHandle (풀 반환) ====
//  사용 중: Conn-A
//  풀에 반환: Conn-A
//  풀 남은 수: 2
//
// ==== 8. IDisposable 사용 가이드 ====
// | 상황                    | 방법              |
// |------------------------|-------------------|
// | 지역 변수로 잠깐 사용   | using 문/선언     |
// | 클래스 필드로 보유      | 클래스도 IDisposable 구현 |
// | 풀에서 대여/반환        | ResourceHandle 패턴 |
// | 비관리 리소스 직접 보유  | 전체 Dispose 패턴  |
//
// 게임에서 IDisposable이 필요한 것들:
//   - TCP 소켓 (서버/클라이언트 연결)
//   - 파일 스트림 (세이브/로드)
//   - Unity: NativeArray, ComputeBuffer, RenderTexture
//   - 구독 해제 토큰 (이벤트 구독 → Dispose로 해제)
// [FileLogger] 파일 닫힘 (Dispose 완료)