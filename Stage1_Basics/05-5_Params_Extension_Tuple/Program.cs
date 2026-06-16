using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;

// [보강] 05-5. params, 확장 메서드, 튜플
// 기존 05_Methods에서 기본 메서드 문법만 다뤘기 때문에 보강
// Unity/게임 개발에서 자주 쓰이는 편의 기능들을 익히기 위한 챕터

// ==== params란? ====
// 메서드에 가변 개수의 인자를 배열 없이 전달할 수 있게 해주는 키워드
// Java의 varargs(...)와 동일한 개념

// ==== 확장 메서드란? ====
// 기존 클래스를 수정하지 않고 새 메서드를 "추가"하는 것처럼 보이게 하는 기법
// Unity에서 Transform, Vector3 등에 커스텀 유틸 메서드를 붙일 때 필수

// ==== 튜플이란? ====
// 여러값을 하나로 묶어 반환하는 가벼운 구조
// out 여러 개 쓰는 것보다 깔끔한 다중 반환 방법

// ==== 확장 메서드를 위한 static 클래스 ====
// 확장 메서드는 반드시 static 클래스 안의 static 메서드여야 함
// 첫 번쩨 매개변수에 this 키워드를 붙여서 대상 타입을 지정
static class StringExtensions
{
    // string에 "확장"되는 메서드 - 호출 시 "hello".ToTitleCase() 형태로 사용 가능
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }
    
    // 게임에서 유용: 문자열 반복
    public static string Repeat(this string str, int count)
    {
        StringBuilder sb = new StringBuilder(str);
        for (int i = 0; i < count; i++)
            sb.Append(str);
        return sb.ToString();
    }
}

static class IntExtensions
{
    // int에 확장: 범위 내인지 체크
    public static bool IsBetween(this int value, int min, int max)
    {
        return value >= min && value <= max;
    }
    
    // 게임에서 자주 쓰는 Clamp (범위 제한)
    public static int Clamp(this int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

static class ListExtensions
{
    // List에 확장: 랜덤 요소 뽑기 - 게임에서 드랍 테이블, 랜덤 스폰 등
    public static T GetRandom<T>(this List<T> list)
    {
        Random rand = new  Random();
        return list[rand.Next(list.Count)];
    }
}

class Program
{
    // ==== 1. params - 가변 인자 ====
    // params는 반드시 마지막 매개변수여야 함 -> 컴파일러가 어디까지가 params이고 어디부터가 다음 매개변수인지 구분할 수 없기 때문
    // 호출 시 배열을 만들지 않아도 됨
    static int Sum(params int[] numbers)
    {
        int total = 0;
        foreach (int n in numbers)
            total += n;
        return total;
    }
    
    // params와 일반 매개변수 혼합 가능 (params는 맨 뒤에)
    static void Log(string tag, params object[] messages)
    {
        Console.Write($"[{tag}] ");
        foreach (object msg in messages)
            Console.Write($"{msg} ");
        Console.WriteLine();
    }
    
    // ==== 2. 튜플 변환 - out 보다 깔끔한 다중 변환 ====
    // C# 7.0+ 문법, 이름을 붙일 수 있어서 가독성 좋음
    static (int min, int max, float average) GetStats(int[] numbers)
    {
        int min = numbers[0];
        int max = numbers[0];
        int sum = 0;

        foreach (int n in numbers)
        {
            if (n < min) min = n;
            if (n > max) max = n;
            sum += n;
        }
        return (min, max, (float)sum / numbers.Length);
    }
    
    // 게임 예시: 데미지 계산 결과를 튜플로 반환
    static (int damage, bool isCritical) CalculateDamage(int baseDamage, float critChance)
    {
        Random rand = new Random(42);
        bool crit = rand.NextDouble() < critChance;
        int finalDamage = crit ? baseDamage * 2 : baseDamage;
        return (finalDamage, crit);
    }

    static void Main(string[] args)
    {
        // ==== 1. params 사용 ====
        Console.WriteLine("==== 1. params (가변 인자) ====");
        
        // 인자를 원하는만큼 넘길 수 있음 - 배열 선언 불필요
        Console.WriteLine($"Sum(1,2,3,) =  {Sum(1, 2, 3)}");        // 6
        Console.WriteLine($"Sum(10, 20) = {Sum(10, 20)}");          // 30
        Console.WriteLine($"Sum() = {Sum()}");                                   // 0
        
        // 배열을 직접 넘겨도 됨
        int[] arr = { 1, 2, 3, 4, 5 };
        Console.WriteLine($"Sum(배열) = {Sum(arr)}");
        
        // 일반 매개변수 + params 혼합
        Log("INFO", "플레이어", "접속", "완료");
        Log("ERROR", "연결", "실패", 400);
        
        // ==== 2. 확장메서드 사용 ====
        Console.WriteLine("\n==== 2. 확장 메서드 ====");
        
        // 마치 string에 원래 있던 메서드처럼 호출
        string name = "hEELO wORLD";
        Console.WriteLine($"ToTitleCase() : {name.ToTitleCase()}");         // 대 <-> 소 변환!

        string star = "*".Repeat(5);                                        // *****
        
        // int 확장
        int hp = 150;
        Console.WriteLine($"HP {hp} 범위(0~100)? {hp.IsBetween(0, 100)}");   // False
        Console.WriteLine($"HP Clamp(0~100): {hp.Clamp(0, 100)}");          // 100

        int damage = -5;
        Console.WriteLine($"데미지 Clamp(0~999): {damage.Clamp(0, 999)}");    // 0
        
        // List 확장 - 랜덤 뽑기
        List<string> lootTable = new List<string> { "검", "방패", "물약", "금화", "스크롤" };
        Console.WriteLine($"랜덤 드랍: {lootTable.GetRandom()}");
        
        // ==== 3. 튜플 ====
        Console.WriteLine("\n==== 3. 튜플 (다중 반환) ====");
        
        // 이름 있는 튜플로 받기
        int[] scores = { 72, 95, 88, 65, 100 };
        var stats = GetStats(scores);
        Console.WriteLine($"최소: {stats.min}, 최대: {stats.max}, 평균: {stats.average}");
        
        // 구조 분해 (Deconstruct) - 변수에 바로 풀기
        (int min, int max, float avg) = GetStats(scores);
        Console.WriteLine($"구조분해 -> 최소: {min}, 최대: {max}, 평균: {avg}");
        
        // 게임 예시: 데미지 계산
        var result = CalculateDamage(50, 0.3f);
        Console.WriteLine($"데미지: {result.damage}, 크리티컬: {result.isCritical}");
        
        // 불필요한 값 버리기 - _ (discard)
        (int dmg, _) = CalculateDamage(100, 0.5f);
        Console.WriteLine($"데미지만 필여: {dmg}");
        
        // ==== 4. 인라인 튜플 (즉석 생성) ====
        Console.WriteLine("\n==== 4. 인라인 튜플 ====");
        
        // 메서드 없이도 여러 값을 묶을 수 있음
        var player = (Name: "용사", Level: 10, HP: 100);
        Console.WriteLine($"{player.Name} Lv.{player.Level} HP:{player.HP}");
        
        // 튜플끼리 비교 가능 (값 비교)
        var a = (1, 2);
        var b = (1, 2);
        Console.WriteLine($"튜플 비교: {a == b}");  // True
        
        // ==== 5. params vs 배열 vs 튜플 - 언제 뭘쓸까? ====
        Console.WriteLine("\n==== 5. 선택 가이드 ====");
        Console.WriteLine("params       -> 호출자가 개수를 모를때 (Log, Sum 등)");
        Console.WriteLine("배열/List     -> 이미 컬렉션으로 관리 중일 때");
        Console.WriteLine("튜플          -> 메서드에서 2~3개 값을 반환할 때");
        Console.WriteLine("out          -> 성공/실패 + 결과값 패턴 (TryParse 등)");
        Console.WriteLine("클래스         -> 반환값이 4개 이상이거나 재사용할 때");
    }
}

// 출력 결과
// ==== 1. params (가변 인자) ====
// Sum(1,2,3,) =  6
// Sum(10, 20) = 30
// Sum() = 0
// Sum(배열) = 15
// [INFO] 플레이어 접속 완료 
// [ERROR] 연결 실패 400 
//
// ==== 2. 확장 메서드 ====
// ToTitleCase() : Heelo world
// HP 150 범위(0~100)? False
// HP Clamp(0~100): 100
// 데미지 Clamp(0~999): 0
// 랜덤 드랍: 검
//
// ==== 3. 튜플 (다중 반환) ====
// 최소: 65, 최대: 100, 평균: 84
// 구조분해 -> 최소: 65, 최대: 100, 평균: 84
// 데미지: 50, 크리티컬: False
// 데미지만 필여: 100
//
// ==== 4. 인라인 튜플 ====
// 용사 Lv.10 HP:100
// 튜플 비교: True
//
// ==== 5. 선택 가이드 ====
// params       -> 호출자가 개수를 모를때 (Log, Sum 등)
// 배열/List     -> 이미 컬렉션으로 관리 중일 때
// 튜플          -> 메서드에서 2~3개 값을 반환할 때
// out          -> 성공/실패 + 결과값 패턴 (TryParse 등)
// 클래스         -> 반환값이 4개 이상이거나 재사용할 때