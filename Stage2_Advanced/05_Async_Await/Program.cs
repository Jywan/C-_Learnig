using System;
using System.Net.Http;
using System.Threading.Tasks;

// ==== 1. async/await 기본
class AsyncBasic
{
    // async = 이 메서드는 비동기로 동작함을 선언
    // Task = 비동기 작업의 결과ㅡㄹ 나타내는 타입 (void 대신 사용)
    public static async Task DoWorkAsync()
    {
        Console.WriteLine("작업 시작");
        await Task.Delay(1000);         // 1초 대기 - Thread를 블로킹 하지 않는다
        Console.WriteLine("1초 후 작업 완료");
    }
    
    // Task<T> = 결과값이 있는 비동기 메서드
    public static async Task<int> GetNumberAsync()
    {
        await Task.Delay(500);
        return 42;                      // async 메서드에서 T를 return 하면 Task<T>로 자동 래핑 
    }
}

// ==== 2. 여러작업 동시 실행 ====
class ParallelAsync
{
    static async Task<string> FetchDataAsync(string name, int delay)
    {
        await Task.Delay(delay);
        return $"{name} 완료 ({delay} ms))";
    }

    public static async Task RunAsync()
    {
        // 순차 실행 - 총 3초
        Console.WriteLine("==== 순차 실행 ====");
        string r1 = await FetchDataAsync("작업A", 1000);
        string r2 = await FetchDataAsync("작업B", 2000);
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        
        // 병렬 실행 - 총 2초! (가장 오래 걸리는 작업 기준)
        Console.WriteLine("==== 병렬 실행 ====");
        Task<string> t1 = FetchDataAsync("작업A", 1000);  // await 없이 시작만합니다!
        Task<string> t2 = FetchDataAsync("작업B", 2000);
        string[] results = await Task.WhenAll(t1, t2);  // 둘 다 끝 날때까지 대기
        foreach (string result in results)
            Console.WriteLine(result);
    }
}


// ==== 3. 예외 처리 ====
class AsyncException
{
    static async Task<int> FailAsync()
    {
        await Task.Delay(500);
        throw new InvalidOperationException("비동기 작업 실패!");
    }

    public static async Task RunAsync()
    {
        try
        {
            int result = await FailAsync(); // 예외는 await 시점에 전파됨
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"예외 처리: {ex.Message}");
        }
    }
}

class Program
{
    static async Task Main(string[] args)     // Main도 async로 선언 가능!
    {
        // ==== 1. 기본 ====
        await AsyncBasic.DoWorkAsync();
        
        int number = await AsyncBasic.GetNumberAsync();
        Console.WriteLine($"받은 값: {number}");
        
        // ==== 2. 병렬 실행 ====
        await ParallelAsync.RunAsync();
        
        // ==== 3. 예외 처리 ====
        await AsyncException.RunAsync();
        
        Console.WriteLine("모든 작업 완료!");
    }
}

// 실행 결과!
// 작업 시작
// 1초 후 작업 완료
// 받은 값: 42
// ==== 순차 실행 ====
// 작업A 완료 (1000 ms))
// 작업B 완료 (2000 ms))
// ==== 병렬 실행 ====
// 작업A 완료 (1000 ms))
// 작업B 완료 (2000 ms))
// 예외 처리: 비동기 작업 실패!
// 모든 작업 완료!