using System;
using System.Diagnostics;
using System.Text;

// [보강] 02-5. string 심화 (불변성, StringBuilder, 성능)
// 기존 02_Variables 에서 string을 단순 타입으로만 다웠기 때문에 보강
// 게임에서 매 프레임 문자열을 조합하면 GC 폭탄이 터지는 이유를 이해하기 위한 챕터

// ==== string의 특수한 위치 ====
// string은 참조 타입(class)이지만, 값 타입처럼 동작하는 부분이 있다
//  - 힙에 저장됨 (참조 타입)
//  - 하지만 불변(immutable) - 한번 생성되면 내용을 바꿀수 없음
//  - == 비교 시 주소가 아닌 내용을 비교 (연산자 오버로딩)
//  - 대입 시 "값처럼" 느껴지지만 실제로는 새 객체를 가리키는 것

// ==== 불변성(Immutability)이란? ====
// string 객체는 생성 후 수정 불가능
// "수정" 처럼 보이는 모든 연산은 실제로 새 string 객체를 만듦
// -> 반복적인 문자열 연결 = 반복적인 힙 할당 = GC 부담

// ==== 왜 불변으로 설계했을까? ====
// 1. 스레드 안전 - 여러 스레드가 같은 string을 읽어도 안전
// 2. 해시 캐싱 - Dictionary 키로 사용 시 해시값을 한번만 계산
// 3. 문자열 인터닝 - 동일 리터럴을 메모리에 하나만 유지가능

class Program
{
    static void Main(string[] args)
    {
        
        // ==== 1. 불변성 확인 ====
        Console.WriteLine("==== 1. 불변성 (Immutability) ====");

        string original = "hi";
        string modified = original + " jyw!";               // original은 변하지 않음, 새 객체 생성
        
        Console.WriteLine($"original: {original}");         // hi
        Console.WriteLine($"modified: {modified}");         // hi jywan!
        Console.WriteLine($"같은 객체? {ReferenceEquals(original, modified)}"); // False
        
        // ToUpper() 또한 새 객체를 반환
        string upper = original.ToUpper();
        Console.WriteLine($"upper: {upper}");                     // HI
        Console.WriteLine($"original이 변했나? {original}");        // hi - 안변함..
        
        // ==== 2. 문자열 인터닝 (String Interning) ====
        Console.WriteLine("\n==== 2. 문자열 인터닝 ====");
        
        // 컴파일 타임 리터럴은 같은 주소를 공유 (인터닝)
        string a = "Hello";
        string b = "Hello";
        Console.WriteLine($"리터럴 비교: {ReferenceEquals(a, b)}");      // True - 같은 객체
        
        // 런타임에 조합된 문자열은 인터닝되지 않음.
        string c = "Hel" + "lo";        // 컴파일러가 "Hello"로 최적화 -> 인터닝됨 
        string d = "Hel";
        d += "lo";                      // 런타임 연결 -> 새 객체
        Console.WriteLine($"컴파일타임 조합: {ReferenceEquals(a, c)}");    // True
        Console.WriteLine($"런타임 조합: {ReferenceEquals(a, d)}");       // False
        
        // ==== 3. 반복된 연결의 문제 ====
        Console.WriteLine("\n==== 3. 반복 연결 성능 문제 ====");
        
        // 나쁜 예: 매번 새 string 객체가 생김 (O(n²) 메모리 할당)
        Stopwatch sw = Stopwatch.StartNew();
        string result = "";
        for (int i = 0; i < 10000; i++)
        {
            result += "a";  // 매 반복마다: 기존 문자열 복사 + "a" 추가 -> 새 객체
        }
        sw.Stop();
        Console.WriteLine($"string 연결 10000회: {sw.ElapsedMilliseconds}ms");
    
        // 좋은 예: StringBuilder는 내부 버퍼를 확장하며 연결 (O(n))
        sw.Restart();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 10000; i++)
        {
            sb.Append("a"); // 내부 char[] 버퍼에 추가, 필요 시만 확장
        }
        string resultSb = sb.ToString();    // 최종적으로 한번만 string 생성
        sw.Stop();
        Console.WriteLine($"StringBuilder 10000회 : {sw.ElapsedMilliseconds}ms");
        
        // ==== 4. StringBuilder 사용법 ====
        Console.WriteLine("\n==== 4. StringBuilder 기본 사용 ====");
        
        StringBuilder builder = new StringBuilder("Player");
        builder.Append(" HP: ");
        builder.Append(100);
        builder.Append(" / ");
        builder.Append("100");
        Console.WriteLine(builder.ToString());  // Player HP: 100/100
        
        // 용량 지정 = 미리 버퍼 크기를 잡으면 확장 비용 감소
        StringBuilder preAllocated = new StringBuilder(256);
        preAllocated.Append("미리 256 char 버퍼 확보");
        Console.WriteLine(preAllocated.ToString());
        
        // 체이닝 가능
        string chained = new StringBuilder()
            .Append("[")
            .Append("INFO")
            .Append("]")
            .Append("게임 시작")
            .ToString();
        Console.WriteLine(chained); // [INFO] 게임 시작
        
        // ==== 5. 문자열 보간 vs 연결 vs Format ====
        Console.WriteLine("\n==== 5. 문자열 생성 방식 비교 ====");

        string playerName = "장영완";
        int score = 9999;
        
        // 방법 1: + 연결 (간단할 때 OK BUT 반복문에서는 비효율)
        string msg1 = "플레이어: " + playerName + " 점수: " + score;
        
        // 방법 2: string.Format (구식이지만 여전히 사용된다고 한다... 구식인지는 잘모르겠다, java에서는 logging 할때 자주씀.)
        string msg2 = string.Format("플레이어: {0} 점수: {1}", playerName, score);
        
        // 방법 3: 보간 문자열 $ (가독성 최고, C# 6+)
        string msg3 = $"플레이어: {playerName} 점수: {score}";
        
        Console.WriteLine(msg1);
        Console.WriteLine(msg2);
        Console.WriteLine(msg3);
        
        // 셋다 결과는 동일, 보간이 가독성과 성능 모두 양호하다고 함.
        // 컴파일러가 보간을 string.Concat 또는 DefaultInterpolatedStringHandler로 최적화함
        
        // ==== 6. 유용한 string 메서드 ====
        Console.WriteLine("\n==== 6. 자주 쓰는 string 메서드 ====");
        
        string data = " Hello, C# World! ";
        
        Console.WriteLine($"Trim: '{data.Trim()}'");                    // 앞뒤 공백 체크 (익숙하다)
        Console.WriteLine($"Contains: {data.Contains("C#")}");          // 포함 여부 (이것도 익숙하다. java stream filter사용할때 자주씀.)
        Console.WriteLine($"Replace: {data.Replace("C#", "Unity")}");   // 치환

        string[] words = data.Trim().Split(',', ' ');                   // 여러 구분자로 분리
        foreach (string word in words)
        {
            if (word != "") Console.WriteLine($"    [{word}]");
        }
        
        Console.WriteLine($"StartWith: {data.Trim().StartsWith("Hello")}"); // True
        Console.WriteLine($"Substring: {data.Trim().Substring(0, 5)}"); // Hello
        
        // ==== 7. 게임에서의 string 주의사항 ====
        Console.WriteLine("\n==== 7. 게임 개발 주의사항 ====");
        
        // Unity Update()에서 매 프레임 string을 생성하면 GC 스파이크 발생
        // 나쁜 예시 :
        // void Update() {
        //      scoreText.text = "Score: " + score;     // 매 프레임 새 string -> GC
        //  }
        
        // 좋은 패턴: 값이 변할 때만 갱신
        int previousScore = -1;
        int currentScore = 100;

        if (currentScore > previousScore)
        {
            string scoreText = $"Score: {currentScore}";        // 변할 때만 생성
            Console.WriteLine($"UI 갱신: {scoreText}");
            previousScore = currentScore;
        }
        
        // StringBuilder를 캐싱해서 재사용하는 패턴
        StringBuilder cachedSb = new StringBuilder(64);
        cachedSb.Clear();           // 매번 새로 만들지 않고 Clear()로 재사용
        cachedSb.Append("HP: ");
        cachedSb.Append(85);
        cachedSb.Append("/100");
        Console.WriteLine($"캐싱된 SB: {cachedSb}");
        
        // ==== 8. string 비교 주의점 ====
        Console.WriteLine("\n==== 8. 문자열 비교 ====");
        
        // == : 내용 비교 (C#에서 연산자 오버로딩)
        string x = "hello";
        string y = "HELLO";
        Console.WriteLine($"== 비교: {x == y}");      // False : 대소분자 비교됨
        Console.WriteLine($"대소문자 무시: {x.Equals(y, StringComparison.OrdinalIgnoreCase)}"); // True java와는 메서드가 다르다.
        
        // 게임에서 아이템 이름, 커맨드 비교 시 대소문자 무시가 자주 필요함.
        string command = "Attack";
        if (command.Equals("attack", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("공격!");
        }
    }
}

// 출력 결과
// ==== 1. 불변성 (Immutability) ====
// original: hi
// modified: hi jyw!
// 같은 객체? False
// upper: HI
// original이 변했나? hi
// 
// ==== 2. 문자열 인터닝 ====
// 리터럴 비교: True
// 컴파일타임 조합: True
// 런타임 조합: False
// 
// ==== 3. 반복 연결 성능 문제 ====
// string 연결 10000회: 4ms
// StringBuilder 10000회 : 0ms
// 
// ==== 4. StringBuilder 기본 사용 ====
// Player HP: 100 / 100
// 미리 256 char 버퍼 확보
// [INFO]게임 시작
// 
// ==== 5. 문자열 생성 방식 비교 ====
// 플레이어: 장영완 점수: 9999
// 플레이어: 장영완 점수: 9999
// 플레이어: 장영완 점수: 9999
// 
// ==== 6. 자주 쓰는 string 메서드 ====
// Trim: 'Hello, C# World!'
// Contains: True
// Replace:  Hello, Unity World! 
//     [Hello]
//     [C#]
//     [World!]
// StartWith: True
// Substring: Hello
// 
// ==== 7. 게임 개발 주의사항 ====
// UI 갱신: Score: 100
// 캐싱된 SB: HP: 85/100
// 
// ==== 8. 문자열 비교 ====
// == 비교: False
// 대소문자 무시: True
// 공격!