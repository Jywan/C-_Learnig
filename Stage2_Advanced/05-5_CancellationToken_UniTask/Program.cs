using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


// [보강] 05-5. CancellationToken, UniTask 소개
// 기존 05_Async_Await에서 기본 async/await만 다뤘기 때문에 보강
// 게임에서 비동기 작업 취소(씬 전환, 접속 끊김)와 Unity 특화 비동기를 이해하기 위한 챕터

// ==== CancellationToken이란? ====
// 비동기 작업에 "취소 신호"를 전달하는 메커니즘
// 예시 상황:
//      - 씬 전환 시 이전 씬의 비동기 로딩 취소
//      - 유저가 매칭 취소 버튼 누름
//      - 서버 응답 타임아웃
//      - 게임 오브젝트 파괴 시 진행 중인 코루틴/Task 취소

// ==== CancellationTokenSource -> CancellationToken ====
// CancellationTokenSource: 취소를 "발행"하는 측 (Cancel() 호출)
// CancellationToken: 취소를 "감지"하는 측 (비동기 메서드에 전달)

// ==== UniTask란? (개념 소개) ====
// Unity에서 Task 대신 사용하는 경량 비동기 라이브러리
// Task의 문제: 힙할당, 스레드풀 사용, Unity 메인 스레드와 충돌
// UniTask: 구조체 기반(할당 제로), Unity 생명주기와 통합, PlayerLoop 기반


// ==== 1. 취소 가능한 비동기 작업 시뮬레이션
class NetworkClient
{
    // CancellationToken을 매개변수로 받는 패턴
    public static async Task<string> ConnectAsync(string server, CancellationToken token)
    {
        Console.WriteLine($"[Net] {server} 연결 시도...");
        
        // 연결 시뮬레이션 (3단계)
        for (int i = 1; i <= 3; i++)
        {
            // 취소 확인 방법 1: ThrowIfCancellationRequested
            // 취소되면 OperationCanceledException 발생
            token.ThrowIfCancellationRequested();

            await Task.Delay(500, token);       // Delay도 token을 받으면 중간에 취소 가능
            Console.WriteLine($"[Net] 단계 {i}/3 완료");
        }

        return $"{server} 연결 성공!";
    }
    
    // 취소 확인 방법 2: IsCancellationRequested로 수동 체크
    public static async Task DownloadDataAsync(string url, CancellationToken token)
    {
        Console.WriteLine($"[Download] 시작: {url}");

        int totalChunks = 10;
        for (int i = 1; i <= totalChunks; i++)
        {
            // 예외 대신 조건문으로 정상 종료
            if (token.IsCancellationRequested)
            {
                Console.WriteLine($"[Download] 취소됨 ({i -  1}/{totalChunks} 완료)");
                return;     // 예외 없이 깨끗하게 종료
            }
            
            await Task.Delay(200);
            Console.WriteLine($"[Download] {i}/{totalChunks} 다운로드 중...");
        }
        
        Console.WriteLine("[Download] 완료!");
    }
}


// ==== 2. 타임아웃 패턴 ====
class TimeoutExample
{
    // 느린 작업 시뮬레이션
    public static async Task<string> SlowOperationAsync(CancellationToken token)
    {
        await Task.Delay(5000, token);  // 5초 걸리는 작업
        return "결과 데이터";
    }
    
    // 타임아웃 래퍼
    public static async Task<string> WithTimeout(
        Func<CancellationToken, Task<string>> operation,
        int timeoutMs)
    {
        // CancellationTokenSource에 타임아웃 설정
        using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);

        try
        {
            return await operation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[Timeout] {timeoutMs}ms 초과 - 작업 취소됨");
            return null;
        }
    }
}


// ==== 3. 여러 Token 연결 (Linked Token) ====
// 유저 취소 + 타임아웃을 동시에 적용
class LinkedTokenExample
{
    public static async Task MatchmakingAsync(CancellationToken userToken)
    {
        // 유저 취소 토큰 + 30초 타임아웃을 결합
        using CancellationTokenSource timeoutCts = new CancellationTokenSource(3000);   // 데모용 30초
        using CancellationTokenSource linkedCts = 
            CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);
        
        CancellationToken token = linkedCts.Token;

        Console.WriteLine("[매칭] 매칭 시작 (유저 취소 또는 3초 타임아웃)");

        try
        {
            for (int i = 1; i <= 10; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(500, token);
                Console.WriteLine($"[매칭] 탐색중... {i}초");
            }

            Console.WriteLine("[매칭] 매칭 성공!");
        }
        catch (OperationCanceledException)
        {
            if (userToken.IsCancellationRequested)
                Console.WriteLine("[매칭] 유저가 취소함");
            else
                Console.WriteLine("[매칭] 타임아웃 (상대를 찾지 못함)");
        }
    }
}


// ==== 4. CancellationToken 등록 콜백 ====
class CleanupOnCancel
{
    public static async Task LongTaskWithCleanup(CancellationToken token)
    {
        // 취소 시 실행될 콜백 등록
        using CancellationTokenRegistration registration = token.Register(() =>
        {
            Console.WriteLine("[Cleanup] 취소 감지 -> 리소스 정리 실행!");
        });
        
        Console.WriteLine("[Task] 장시간 작업 시작....");

        try
        {
            await Task.Delay(10000, token); // 10초 작업
            Console.WriteLine("[Task] 작업 완료!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Task] 작업이 취소됨");
        }
    }
}

// ==== 5. 게임서버 시뮬레이션 - 실전 패턴 ====
class GameServer
{
    private CancellationTokenSource _serverCts = new CancellationTokenSource();

    public async Task StartAsync()
    {
        Console.WriteLine("[Server] 서버 시작");

        try
        {
            await GameLoopAsync(_serverCts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Server] 서버 종료됨");
        }
    }

    private async Task GameLoopAsync(CancellationToken token)
    {
        int tick = 0;
        while (!token.IsCancellationRequested)
        {
            tick++;
            Console.WriteLine($"[Server] Tick: #{tick}");
            await Task.Delay(500, token);       // 500ms 마다 틱

            if (tick >= 5)              // 데모용: 5틱 후 외부에서 종료 신호
            {
                break;
            }
        }
    }
    
    public void Shutdown()
    {
        Console.WriteLine("[Server] 종료 신호 전송");
        _serverCts.Cancel();
    }
}


class Program
{
    static async Task Main(string[] args)
    {
        // ==== 1. 기본 취소 ====
        Console.WriteLine("==== 1. 기본 취소 CancellationToken ====");
        
        CancellationTokenSource cts1 = new CancellationTokenSource();
        
        // 1초 후 취소 예약
        cts1.CancelAfter(1200);

        try
        {
            string result = await NetworkClient.ConnectAsync("game.server.com", cts1.Token);
            Console.WriteLine(result);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Main] 연결 취소됨!\n");
        }
        finally
        {
            cts1.Dispose();         // CancellationTokenSource도 IDisposable!
        }
        
        
        // ==== 2. 수동 취소 체크 (IsCancellationRequested) ====
        Console.WriteLine("==== 2. 수동 취소 체크 ====");
        
        CancellationTokenSource cts2 = new CancellationTokenSource();
        cts2.CancelAfter(1500);     // 1.5초 후 취소
        
        await NetworkClient.DownloadDataAsync("https://assets.game.com/map.dat", cts2.Token);
        cts2.Dispose();
        Console.WriteLine();
        
        
        // ==== 3. 타임아웃 패턴 ====
        Console.WriteLine("==== 3. 타임아웃 ====");

        string? data = await TimeoutExample.WithTimeout(
            TimeoutExample.SlowOperationAsync,
            2000        // 2초 타임아웃 (작업은 5초 걸림)
            );
        Console.WriteLine($"결과: {data ?? "(타임아웃)"}\n");
        
        
        // ==== 4. Linked Token ====
        Console.WriteLine("==== 4. Linked Token (유저 취소 + 타임아웃) ====");
        
        CancellationTokenSource userCts = new CancellationTokenSource();
        // 유저가 취소 안 누르면 -> 타임아웃(3초)으로 종료됨
        await LinkedTokenExample.MatchmakingAsync(userCts.Token);
        userCts.Dispose();
        Console.WriteLine();
        
        
        // ==== 5.취소 콜백 등록 ====
        Console.WriteLine("==== 5. 취소 콜백 (Register) ====");
        
        CancellationTokenSource cts3 = new CancellationTokenSource();
        cts3.CancelAfter(1000);     // 1초 후 취소
        
        await CleanupOnCancel.LongTaskWithCleanup(cts3.Token);
        cts3.Dispose();
        Console.WriteLine();
        
        
        // ==== 6. 게임 서버 루프 ====
        Console.WriteLine("==== 6. 게임 서버 루프 ====");
        
        GameServer server = new GameServer();
        await server.StartAsync();
        Console.WriteLine();
        
        // ==== 7. UniTask 개념 소개 ====
        // 콘솔로그로 보충설명 추가
        Console.WriteLine("==== 7. UniTask 소개 (Unity 전용) ====");
        Console.WriteLine("Task의 Unity 문제점:");
        Console.WriteLine("  - Task는 힙에 할당됨 → GC 스파이크");
        Console.WriteLine("  - Task.Delay는 스레드풀 사용 → Unity 메인 스레드 아님");
        Console.WriteLine("  - await 복귀 시점이 불확실 (SynchronizationContext 이슈)");
        Console.WriteLine("");
        Console.WriteLine("UniTask 장점:");
        Console.WriteLine("  - struct 기반 → 힙 할당 제로");
        Console.WriteLine("  - PlayerLoop 기반 → Update/FixedUpdate 타이밍에 정확히 복귀");
        Console.WriteLine("  - CancellationToken과 완벽 호환");
        Console.WriteLine("  - UniTask.Delay, UniTask.WaitUntil 등 Unity 친화 API");
        Console.WriteLine("");
        Console.WriteLine("UniTask 예시 (Unity에서 사용):");
        Console.WriteLine("  async UniTask FadeOutAsync(CancellationToken token) {");
        Console.WriteLine("      float alpha = 1f;");
        Console.WriteLine("      while (alpha > 0) {");
        Console.WriteLine("          token.ThrowIfCancellationRequested();");
        Console.WriteLine("          alpha -= Time.deltaTime;");
        Console.WriteLine("          image.color = new Color(1,1,1, alpha);");
        Console.WriteLine("          await UniTask.Yield();  // 다음 프레임까지 대기");
        Console.WriteLine("      }");
        Console.WriteLine("  }");
        Console.WriteLine("");
        Console.WriteLine("  // 호출: GameObject 파괴 시 자동 취소");
        Console.WriteLine("  var token = this.GetCancellationTokenOnDestroy();");
        Console.WriteLine("  await FadeOutAsync(token);");

        // ==== 정리 ====
        Console.WriteLine("\n==== 8. 정리 ====");
        Console.WriteLine("| 개념                  | 역할                          |");
        Console.WriteLine("|----------------------|-------------------------------|");
        Console.WriteLine("| CancellationTokenSource | 취소 발행 (Cancel, CancelAfter) |");
        Console.WriteLine("| CancellationToken      | 취소 감지 (전달용)             |");
        Console.WriteLine("| ThrowIfCancellation    | 취소 시 예외 발생              |");
        Console.WriteLine("| IsCancellationRequested| 취소 여부 수동 체크            |");
        Console.WriteLine("| Register()            | 취소 시 콜백 실행              |");
        Console.WriteLine("| LinkedTokenSource     | 여러 취소 조건 결합            |");
        Console.WriteLine("| UniTask               | Unity용 경량 async/await       |");
    }
}

// 실행 결과
// ==== 1. 기본 취소 CancellationToken ====
// [Net] game.server.com 연결 시도...
// [Net] 단계 1/3 완료
// [Net] 단계 2/3 완료
// [Main] 연결 취소됨!
//
// ==== 2. 수동 취소 체크 ====
// [Download] 시작: https://assets.game.com/map.dat
// [Download] 1/10 다운로드 중...
// [Download] 2/10 다운로드 중...
// [Download] 3/10 다운로드 중...
// [Download] 4/10 다운로드 중...
// [Download] 5/10 다운로드 중...
// [Download] 6/10 다운로드 중...
// [Download] 7/10 다운로드 중...
// [Download] 8/10 다운로드 중...
// [Download] 취소됨 (8/10 완료)
//
// ==== 3. 타임아웃 ====
// [Timeout] 2000ms 초과 - 작업 취소됨
// 결과: (타임아웃)
//
// ==== 4. Linked Token (유저 취소 + 타임아웃) ====
// [매칭] 매칭 시작 (유저 취소 또는 3초 타임아웃)
// [매칭] 탐색중... 1초
// [매칭] 탐색중... 2초
// [매칭] 탐색중... 3초
// [매칭] 탐색중... 4초
// [매칭] 탐색중... 5초
// [매칭] 타임아웃 (상대를 찾지 못함)
//
// ==== 5. 취소 콜백 (Register) ====
// [Task] 장시간 작업 시작....
// [Task] 작업이 취소됨
// [Cleanup] 취소 감지 -> 리소스 정리 실행!
//
// ==== 6. 게임 서버 루프 ====
// [Server] 서버 시작
// [Server] Tick: #1
// [Server] Tick: #2
// [Server] Tick: #3
// [Server] Tick: #4
// [Server] Tick: #5
//
// ==== 7. UniTask 소개 (Unity 전용) ====
// Task의 Unity 문제점:
//   - Task는 힙에 할당됨 → GC 스파이크
//   - Task.Delay는 스레드풀 사용 → Unity 메인 스레드 아님
//   - await 복귀 시점이 불확실 (SynchronizationContext 이슈)
//
// UniTask 장점:
//   - struct 기반 → 힙 할당 제로
//   - PlayerLoop 기반 → Update/FixedUpdate 타이밍에 정확히 복귀
//   - CancellationToken과 완벽 호환
//   - UniTask.Delay, UniTask.WaitUntil 등 Unity 친화 API
//
// UniTask 예시 (Unity에서 사용):
//   async UniTask FadeOutAsync(CancellationToken token) {
//       float alpha = 1f;
//       while (alpha > 0) {
//           token.ThrowIfCancellationRequested();
//           alpha -= Time.deltaTime;
//           image.color = new Color(1,1,1, alpha);
//           await UniTask.Yield();  // 다음 프레임까지 대기
//       }
//   }
//
//   // 호출: GameObject 파괴 시 자동 취소
//   var token = this.GetCancellationTokenOnDestroy();
//   await FadeOutAsync(token);
//
// ==== 8. 정리 ====
// | 개념                  | 역할                          |
// |----------------------|-------------------------------|
// | CancellationTokenSource | 취소 발행 (Cancel, CancelAfter) |
// | CancellationToken      | 취소 감지 (전달용)             |
// | ThrowIfCancellation    | 취소 시 예외 발생              |
// | IsCancellationRequested| 취소 여부 수동 체크            |
// | Register()            | 취소 시 콜백 실행              |
// | LinkedTokenSource     | 여러 취소 조건 결합            |
// | UniTask               | Unity용 경량 async/await       |