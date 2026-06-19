using System;

// [보강] 06-5. 접근 제한자 심화, static, 싱글톤 패턴
// 기존 06_Classes_Objects에사 private/public만 간단히 다뤘기 때문에 보강
// Unity에서 GameManager 싱글톤, static 유틸 클래스를 이해하기 위한 챕

// ==== 접근 제한자 종류 ====
// public               - 어디서든 접근 가능
// private              - 클래스 내부에서만 (기본값)
// protected            - 자신 + 자식 클래스에게만
// internal             - 같은 어셈블리(프로젝트) 내에서만
// protected internal   - protected OR internal
// private protected    - protected AND internal (같은 어셈블리의 자식만)

// Java와 비교:
// Java의 default(패키지) = C#의 internal (가장 비슷)
// Java에는 private protected 없음


// ==== 1. 접근 제한자 데모 ====
class Character
{
    public string Name;             // 어디서든 접근
    private int _hp;                // 클래스 내부에서만 - 외부에서 직접 수정 방지
    protected int _mp;              // 자식 클래스까지는 허용
    internal float _speed;          // 같은 프로젝트 내에서 접근 가능

    public Character(string name, int hp, int mp)
    {
        Name = name;
        _hp = hp;
        _mp = mp;
        _speed = 5.0f;
    }
    
    // private 필드는 프로퍼티로 제어된 접근을 허용
    public int HP
    {
        get { return _hp; }
        set { _hp = value < 0 ? 0 : value; }        // 음수 방지 로직 추가 가능
    }
    
    // protected - 외부는 못쓰지만 자식은 가능
    protected void RegenerateMp(int amount)
    {
        _mp += amount;
        Console.WriteLine($"{Name} MP 회복: {_mp}");
    }

    public void ShowStats()
    {
        Console.WriteLine($"[{Name}] HP: {_hp}, MP: {_mp}, Speed: {_speed}");
    }
}

// 자식 클래스에서 protected 멤버 접근
class Mage : Character
{
    public Mage(string name) : base(name, 80, 200) { }

    public void Meditate()
    {
        // protected 메서드 호출 가능
        RegenerateMp(50);
        // _mp 직접 접근도 가능 (protected 필드)
        Console.WriteLine($"현재 MP: {_mp}");
    }
}


// ==== 2. static - 인스턴스 없이 사용 ====
// static 멤버는 클래스에 딱 하나만 존재 - 모든 인스턴스가 공유
// Unity의 Time.deltaTime, Mathf.Clamp 등 static
class MathUtils
{
    // static 필드 - 클래스 레벨에서 하나만 존재
    public static readonly float PI = 3.14159f;
    
    // static 메서드 - 인스턴스 생성 없이 MathUtils.Clamp()로 호출
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Distance(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return (float) MathF.Sqrt(dx * dx + dy * dy);
    }
}


// ==== 3. static 필드로 인스턴스 카운트 ====
class Enemy
{
    // static 필드는 모든 인스턴스가 공유
    private static int _totalCount = 0;

    public string Type { get; }

    public Enemy(string type)
    {
        Type = type;
        _totalCount++;      // 생성될 때마다 증가
    }
    
    // static 메서드에서는 인스턴스 멤버(this) 접근 불가
    public static int GetTotalCount()
    {
        return _totalCount;
    }
    
    // 인스턴스 메서드에서는 static 멤버 접근 가능
    public void PrintInfo()
    {
        Console.WriteLine($"[{Type}] 전체 적 수: {_totalCount}");
    }
}

// ==== 4. 싱글톤 패턴 - 인스턴스가 딱 하나만 존재하도록 보장 ====
// 게임에서 자주 사용: GameManager, AudioManager, UIManager 등
// "전역 상태"를 관리하는 매니저 클래스에 한함.
class GameManager
{
    // static 필드에 유일한 인스턴스 보관
    private static GameManager? _instance;
    
    // 외부에서 new 못하게 생성자를 private으로 잠금
    private GameManager()
    {
        Score = 0;
        IsGameOver = false;
        Console.WriteLine("GameManager 생성됨 (단 1회)");
    }
    
    // 전역 접근점 - 없으면 만들고, 있으면 기존 것을 반환
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameManager();
            }
            return _instance;
        }
    }
    
    // 게임 상태
    public int Score { get; set; }
    public bool IsGameOver { get; set; }

    public void AddScore(int points)
    {
        Score += points;
        Console.WriteLine($"점수 추가: +{points} -> 총 {Score}점");
    }

    public void EndScore()
    {
        IsGameOver = true;
        Console.WriteLine($"게임 오버! 최종 점수: {Score}");
    }
}


// ==== 5. static class - 인스턴스 자체를 만들 수 없는 클래스 ====
// 유틸리티 메서드 모음에 적합
// 05-5에서 만든 확장 메서드 클래스도 static class였음.
static class Logger
{
    private static int _logCount = 0;

    public static void Info(string message)
    {
        _logCount++;
        Console.WriteLine($"[INFO #{_logCount}] {message}");
    }

    public static void Warning(string message)
    {
        _logCount++;
        Console.WriteLine($"[WARNING #{_logCount}] {message}");
    }

    public static void Error(string message)
    {
        _logCount++;
        Console.WriteLine($"[ERROR #{_logCount}] {message}");
    }
    
    public static int GetLogCount() => _logCount;
}


class Program
{
    static void Main(string[] args)
    {
        // ==== 접근 제한자 ====
        Console.WriteLine("==== 1. 접근 제한자 ====");

        Character warrior = new Character("전사", 100, 50);
        warrior.Name = "강한 전사";         // public 
        // warrior._hp = 999;                // private - 컴파일 에러
        // warrior._mp = 999;                // private - 컴파일 에러 (자식만 가능)
        warrior.HP = -10;                 // 프로퍼티를 통해 제어된 접근
        warrior.ShowStats();              // HP가 0으로 클램프됨

        Mage mage = new Mage("마법사");
        mage.Meditate();                  // protected 메서드를 자긷 내부에서 호출
        
        
        // ==== static ====
        Console.WriteLine("\n==== 2. static 멤버 ====");
        
        
        // 인스턴스 없이 클래스명으로 직접 호출
        Console.WriteLine($"PI: {MathUtils.PI}");
        Console.WriteLine($"Clamp(150, 0, 100): {MathUtils.Clamp(150, 0, 100)}");
        Console.WriteLine($"거리: {MathUtils.Distance(0, 0, 3, 4)}");     // 5
        
        
        // ==== static 필드 공유 ====
        Console.WriteLine("\n==== 3. static 필드 (인스턴스 카운트)");

        Enemy e1 = new Enemy("슬라임");
        Enemy e2 = new Enemy("고블린");
        Enemy e3 = new Enemy("드래곤");
        
        Console.WriteLine($"전체 적 수: {Enemy.GetTotalCount()}");      // 3
        e1.PrintInfo();     // [슬라임] 전체 적 수: 3
        
        // Instance로만 접근 - 처음 호출 시 생성
        GameManager gm1 = GameManager.Instance;
        GameManager gm2 = GameManager.Instance;
        Console.WriteLine($"같은 인스턴스? {ReferenceEquals(gm1, gm2)}"); // True
        
        gm1.AddScore(200);
        Console.WriteLine($"gm2에서 확인: {gm2.Score}"); // 200 - 같은 객체이기에..
        
        GameManager.Instance.EndScore();
        
        // ==== static class ====
        Console.WriteLine("\n==== 5. static class (Logger) ====");
        
        Logger.Info("게임 시작");
        Logger.Warning("메모리 사용량 높은");
        Logger.Error("연결 끊김");
        Console.WriteLine($"총 로그 수: {Logger.GetLogCount()}");       // 3

        // Logger logger = new Logger();               //static class는 인스턴스 생성 불가!
        
        // ==== 정리: static vs 싱글톤 ====
        Console.WriteLine("\n==== 6. static class vs 싱글톤 비교 ====");
        Console.WriteLine("static class:");
        Console.WriteLine("  - 인스턴스 자체가 없음");
        Console.WriteLine("  - 상속 불가");
        Console.WriteLine("  - 인터페이스 구현 불가");
        Console.WriteLine("  - 유틸리티/헬퍼에 적합 (Math, Logger)");
        Console.WriteLine("");
        Console.WriteLine("싱글톤:");
        Console.WriteLine("  - 인스턴스가 딱 하나 존재");
        Console.WriteLine("  - 상속 가능");
        Console.WriteLine("  - 인터페이스 구현 가능");
        Console.WriteLine("  - 상태를 가진 매니저에 적합 (GameManager)");
    }
}

// ==== 1. 접근 제한자 ====
// [강한 전사] HP: 0, MP: 50, Speed: 5
// 마법사 MP 회복: 250
// 현재 MP: 250
//
// ==== 2. static 멤버 ====
// PI: 3.14159
// Clamp(150, 0, 100): 100
// 거리: 5
//
// ==== 3. static 필드 (인스턴스 카운트)
// 전체 적 수: 3
// [슬라임] 전체 적 수: 3
// GameManager 생성됨 (단 1회)
// 같은 인스턴스? True
// 점수 추가: +200 -> 총 200점
// gm2에서 확인: 200
// 게임 오버! 최종 점수: 200
//
// ==== 5. static class (Logger) ====
// [INFO #1] 게임 시작
// [WARNING #2] 메모리 사용량 높은
// [ERROR #3] 연결 끊김
// 총 로그 수: 3
//
// ==== 6. static class vs 싱글톤 비교 ====
// static class:
//   - 인스턴스 자체가 없음
//   - 상속 불가
//   - 인터페이스 구현 불가
//   - 유틸리티/헬퍼에 적합 (Math, Logger)
//
// 싱글톤:
//   - 인스턴스가 딱 하나 존재
//   - 상속 가능
//   - 인터페이스 구현 가능
//   - 상태를 가진 매니저에 적합 (GameManager)