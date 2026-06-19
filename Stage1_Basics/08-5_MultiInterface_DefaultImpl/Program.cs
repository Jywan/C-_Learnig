using System;
using System.Collections.Generic;

// [보강] 08-5. 다중 인터페이스, 기본 구현
// 기존 08_Interface_Abstract에서 인터페이스 1개 구현만 다루었기에 보강
// Unity에서 여러 인터페이스를 조합해 컴포넌트 능력을 정의하는 패턴을 위한 챕터

// ==== 다중 인터페이스란? ====
// C#은 다중 상속 불가 (클래스는 1개만 상속)
// 하지만 인터페이스는 여러개 구현 가능 -> 능력을 조합하는 설계
// Java도 동일한 제약 (extends 1개, implements 여러개)

// ==== 기본 구현 (Default Implementation)이란? ====
// C# 8.0 부터 인터페이스에 메서드 본문을 작성 가능
// 기존 구현체를 깨뜨리지 않고 인터페이스에 새 메서드를 추가할 수 있음
// Java 8의 default method와 동일한 개념


// ==== 1. 게임능력을 인터페이스로 정의 ====
interface IDamageable
{
    int HP { get; set; }
    void TakeDamage(int damage);
    
    // 기본 구현 - 구현 클래스에서 override하지 않아도 됨
    bool IsDead() => HP <= 0;
}


interface IMovable
{
    float Speed { get; set; }
    void Move(float x, float y);

    void Stop()
    {
        Console.WriteLine("정지");
    }
}

interface IAttackable
{
    int AttackPower { get; set; }
    void Attack(IDamageable target);
}


// 특수 능력 인터페이스
interface IHealable
{
    void Heal(int amount);
}

interface IFlyable
{
    float FlyHeight { get; set; }
    void Fly();
    
    // 기본 구현
    void Land()
    {
        FlyHeight = 0;
        Console.WriteLine("착지!");
    }
}


// ==== 2. 다중 인터페이스 구현 - 전사(warrior) ====
class Warrior : IDamageable, IMovable, IAttackable
{
    public string Name { get; set; }
    public int HP { get; set; }
    public float Speed { get; set; }
    public int AttackPower { get; set; }

    public Warrior(string name, int hp, int attackPower)
    {
        Name = name;
        HP = hp;
        Speed = 3.0f;
        AttackPower = attackPower;
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        Console.WriteLine($"[{Name}] {amount} 데미지! HP: {HP}");
    }

    public void Move(float x, float y)
    {
        Console.WriteLine($"[{Name}] {x}, {y}로 이동 (속도: {Speed})");
    }

    public void Attack(IDamageable target)
    {
        Console.WriteLine($"[{Name}] 공격! ({AttackPower}) 데미지");
        target.TakeDamage(AttackPower);
    }
}


// ==== 3. 힐러 - 데미지 + 이동 + 힐 가능 ====
class Healer : IDamageable, IMovable, IHealable
{
    public string Name { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public float Speed { get; set; }
    
    public Healer(string name, int hp)
    {
        Name = name;
        HP = hp;
        MaxHP = hp;
        Speed = 2.5f;
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        Console.WriteLine($"[{Name}] {amount} 데미지! HP: {HP}");
    }
    
    public void Move(float x, float y)
    {
        Console.WriteLine($"[{Name}] ({x}, {y})로 이동");
    }

    public void Heal(int amount)
    {
        int before = HP;
        HP = Math.Min(HP + amount, MaxHP);                   // 최대 체력 초과 방지
        Console.WriteLine($"[{Name}] 힐! {before} → {HP}");
    }
}


// ==== 4. 드래곤 — 모든 능력 조합 (공격 + 이동 + 비행 + 데미지) ====
class DragonUnit : IDamageable, IMovable, IAttackable, IFlyable
{
    public string Name { get; set; }
    public int HP { get; set; }
    public float Speed { get; set; }
    public int AttackPower { get; set; }
    public float FlyHeight { get; set; }

    public DragonUnit(string name, int hp, int attackPower)
    {
        Name = name;
        HP = hp;
        Speed = 5.0f;
        AttackPower = attackPower;
        FlyHeight = 0;
    }

    public void TakeDamage(int amount)
    {
        // 비행 중이면 데미지 감소
        int finalDamage = FlyHeight > 0 ? amount / 2 : amount;
        HP -= finalDamage;
        Console.WriteLine($"[{Name}] {finalDamage} 데미지! (비행: {FlyHeight > 0}) HP: {HP}");
    }

    public void Move(float x, float y)
    {
        Console.WriteLine($"[{Name}] ({x}, {y})로 비행 이동");
    }

    public void Attack(IDamageable target)
    {
        Console.WriteLine($"[{Name}] 브레스 공격! ({AttackPower} 데미지)");
        target.TakeDamage(AttackPower);
    }

    public void Fly()
    {
        FlyHeight = 10.0f;
        Console.WriteLine($"[{Name}] 이륙! 고도: {FlyHeight}");
    }
}


// ==== 5. 명시적 인터페이스 구현 (Explicit Implementation) ====
// 두 인터페이스에 같은 이름의 메서드가 있을 때 충돌 해결
interface IFileLogger
{
    void Log(string message);
}

interface IConsoleLogger
{
    void Log(string message);
}

// 같은 이름(Log)을 각 인터페이스별로 다르게 구현
class DualLogger : IFileLogger, IConsoleLogger
{
    // 명시적 구현 - 인터페이스 타입으로 캐스팅해야만 호출 가능
    void IFileLogger.Log(string message)
    {
        Console.WriteLine($"[FILE] {message}");
    }

    void IConsoleLogger.Log(string message)
    {
        Console.WriteLine($"[CONSOLE] {message}");
    }
    
    // 일반 public 메서드로 기본 동작 제공
    public void Log(string message)
    {
        Console.WriteLine($"[DEFAULT] {message}");
    }
}

// ==== 6. 인터페이스 기반 시스템 설계 ====
// 인터페이스 타입으로 받으면, 구체 타입 몰라도 동작가능
class BattleSystem
{
    public static void ProcessTurn(IAttackable attacker, IDamageable target)
    {
        attacker.Attack(target);
    }

    public static void MoveAll(List<IMovable> units, float x, float y)
    {
        foreach (IMovable unit in units)
        {
            unit.Move(x, y);
        }
    }

    public static void HealAllDamaged(List<IDamageable> units)
    {
        foreach (IDamageable unit in units)
        {
            // is로 IHealable도 구현했는지 확인
            if (unit.HP < 100 && unit is IHealable healable)
            {
                healable.Heal(30);
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // ==== 다중 인터페이스 조합 ====
        Console.WriteLine("==== 1. 다중 인터페이스 ====");

        Warrior warrior = new Warrior("전사", 150, 30);
        Healer healer = new Healer("성직자", 80);
        DragonUnit dragon = new DragonUnit("레드 드래곤", 300, 50);
        
        warrior.Move(5, 3);
        warrior.Attack(dragon);
        dragon.Fly();
        dragon.Attack(warrior);
        
        // ==== 기본 구현 호출 ====
        Console.WriteLine("\n==== 2. 기본 구현 (Default Implementation) ====");
        
        // 기본 구현은 인터페이스 타입으로 참조해야 호출가능!
        IDamageable damageable = warrior;
        Console.WriteLine($"전사 사망? {damageable.IsDead()}");         // false
        
        IMovable movable = warrior;
        movable.Stop();
        movable.Stop();             // "정지" 문자열 출력

        IFlyable flyable = dragon;
        flyable.Land();             // "착지!"
        Console.WriteLine($"드래곤 고도: {dragon.FlyHeight}");           // 0
        
        // ==== 힐러 ====
        Console.WriteLine("\n==== 3. 힐러 ====");
        healer.TakeDamage(40);
        healer.Heal(25);
        
        // ==== 명시적 구현 ====
        Console.WriteLine("\n==== 4. 명시적 인터페이스 구현 ====");

        DualLogger logger = new DualLogger();
        logger.Log("일반 호출");                    // [DEFAULT]

        // 인터페이스 타입으로 캐스팅하면 해당 구현 호출
        IFileLogger fileLogger = logger;
        fileLogger.Log("파일에 기록");              // [FILE]

        IConsoleLogger consoleLogger = logger;
        consoleLogger.Log("콘솔에 출력");           // [CONSOLE]
        
        // ==== 인터페이스 기반 시스템 ====
        Console.WriteLine("\n==== 5. 인터페이스 기반 시스템 설계 ====");

        // 구체 타입과 무관하게 인터페이스로 동작
        BattleSystem.ProcessTurn(warrior, dragon);
        BattleSystem.ProcessTurn(dragon, warrior);

        // IMovable 목록으로 일괄 이동
        List<IMovable> allUnits = new List<IMovable> { warrior, healer, dragon };
        BattleSystem.MoveAll(allUnits, 10, 0);

        // 체력 낮은 유닛 중 힐 가능한 유닛만 자동 힐
        List<IDamageable> damaged = new List<IDamageable> { warrior, healer, dragon };
        BattleSystem.HealAllDamaged(damaged);

        // ==== 인터페이스 vs 추상클래스 최종 정리 ====
        Console.WriteLine("\n==== 6. 인터페이스 vs 추상클래스 ====");
        Console.WriteLine("인터페이스:");
        Console.WriteLine("  - 다중 구현 가능");
        Console.WriteLine("  - 상태(필드) 가질 수 없음 (프로퍼티는 가능)");
        Console.WriteLine("  - 기본 구현 가능 (C# 8+)");
        Console.WriteLine("  - '할 수 있다(능력)' 정의에 적합");
        Console.WriteLine("");
        Console.WriteLine("추상클래스:");
        Console.WriteLine("  - 단일 상속만 가능");
        Console.WriteLine("  - 필드, 생성자, 상태 가질 수 있음");
        Console.WriteLine("  - '~이다(존재)' 정의에 적합");
        Console.WriteLine("  - 공통 로직을 자식에게 물려줄 때 유리");
    }
}

// 출력 결과
// ==== 1. 다중 인터페이스 ====
// [전사] 5, 3로 이동 (속도: 3)
// [전사] 공격! (30) 데미지
// [레드 드래곤] 30 데미지! (비행: False) HP: 270
// [레드 드래곤] 이륙! 고도: 10
// [레드 드래곤] 브레스 공격! (50 데미지)
// [전사] 50 데미지! HP: 100
//
// ==== 2. 기본 구현 (Default Implementation) ====
// 전사 사망? False
// 정지
// 정지
// 착지!
// 드래곤 고도: 0
//
// ==== 3. 힐러 ====
// [성직자] 40 데미지! HP: 40
// [성직자] 힐! 40 → 65
//
// ==== 4. 명시적 인터페이스 구현 ====
// [DEFAULT] 일반 호출
// [FILE] 파일에 기록
// [CONSOLE] 콘솔에 출력
//
// ==== 5. 인터페이스 기반 시스템 설계 ====
// [전사] 공격! (30) 데미지
// [레드 드래곤] 30 데미지! (비행: False) HP: 240
// [레드 드래곤] 브레스 공격! (50 데미지)
// [전사] 50 데미지! HP: 50
// [전사] 10, 0로 이동 (속도: 3)
// [성직자] (10, 0)로 이동
// [레드 드래곤] (10, 0)로 비행 이동
// [성직자] 힐! 65 → 80
//
// ==== 6. 인터페이스 vs 추상클래스 ====
// 인터페이스:
//   - 다중 구현 가능
//   - 상태(필드) 가질 수 없음 (프로퍼티는 가능)
//   - 기본 구현 가능 (C# 8+)
//   - '할 수 있다(능력)' 정의에 적합
//
// 추상클래스:
//   - 단일 상속만 가능
//   - 필드, 생성자, 상태 가질 수 있음
//   - '~이다(존재)' 정의에 적합
//   - 공통 로직을 자식에게 물려줄 때 유리