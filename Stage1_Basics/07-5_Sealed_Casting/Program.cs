using System;


// [보강] 07-5. sealed, new, 업/다운 캐스팅 (as/is)
// 기존 07_Inheritance_Polymorphism 에서 virtual/override만 다루었기에 보강
// Unity에서 상속 계층 제어, 안전한 타입 변환을 이해하기 위한 챕터

// ==== sealed 이란? ====
// 클래스에 붙이면: 더 이상 상속 불가
// 메서드에 붙이면: 더 이상 override 불가 (override된 메서드에만 사용 가능)
// 왜 사용하는건가? -> 설계 의도 명확화, 성능 최적화(JIT가 가상 호출을 인라인 가능)

// ==== new 키워드 (메서드 숨기기) ====
// 부모의 메서드를 override가 아닌 "숨기기(hiding)"로 재정의
// override: 다형성 동작 (부모 타입으로 호출해도 자식 것 실행)
// new: 참조 타입 기준으로 호출됨 (다형성 깨짐)
// 의도적으로 쓰는 경우는 드물고, 보통은 실수 방지를 위해 이해해두는 것


// ==== 1. 기본 상속 계층 ====
class Monster
{
    public string Name { get; set; }
    public int HP { get; set; }

    public Monster(string name, int hp)
    {
        Name = name;
        HP = hp;
    }

    public virtual void Attack()
    {
        Console.WriteLine($"[{Name}] 기본 공격!");
    }

    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
        Console.WriteLine($"[{Name}] {damage} 데미지! 남은 HP: {HP}");
    }
}


// ==== 2. sealed class - 상속 금지 ====
// 이 클래스를 더 이상 누군가가 상속받지 못하게 막음
// 의도 "BossMonster는 최종 형태다, 더 파생시키지 마라"
sealed class BossMonster : Monster
{
    public string SpecialSkill { get; }

    public BossMonster(string name, int hp, string skill) : base(name, hp)
    {
        SpecialSkill = skill;
    }

    public override void Attack()
    {
        Console.WriteLine($"[BOSS {Name}] {SpecialSkill} 발동!");
    }
}

// class UltraBoss : BossMonster {  } // 즉시 컴파일 에러 확인됨 : sealed라서 상속 불가


// ==== 3. sealed override - 메서드만 봉인 ====
class Dragon : Monster
{
    public Dragon(string name, int hp) : base(name, hp) {  }
    
    // 이 메서드를 자식이 더 override 못하게 봉인
    public sealed override void Attack()
    {
        Console.WriteLine($"[{Name}] 화염 브레스!");
    }
    
    public override void TakeDamage(int damage)
    {
        // 드래곤은 방어력이 있어서 데미지 절반
        int reduced = damage / 2;
        base.TakeDamage(reduced);  // 부모 로직 재사용
    }
}

class BabyDragon : Dragon
{
    public BabyDragon(string name) : base(name, 50) { }

    // public override void Attack() { }  // 컴파일 에러! Dragon에서 sealed 처리됨
    
    // TakeDamage는 sealed 아니라서 override 가능
    public override void TakeDamage(int damage)
    {
        Console.WriteLine($"[{Name}] 아기라서 더 아프다!");
        HP -= damage;  // 방어력 없이 풀 데미지
        Console.WriteLine($"[{Name}] 남은 HP: {HP}");
    }
}

// ==== 4. new 키워드 - 메서드 숨기기 (hiding) ====
class BaseUnit
{
    public virtual void Move()
    {
        Console.WriteLine("BaseUnit: 이동");
    }

    public void Info() // virtual X
    {
        Console.WriteLine("BaseUnit 정보");
    }
}

class FlyingUnit : BaseUnit
{
    public override void Move()     // 정상적인 override = 다형성 동작
    {
        Console.WriteLine("FlyingUnit: 날아서 이동");
    }
    
    // new - 부모의 비virtual 메서드를 숨김
    public new void Info()
    {
        Console.WriteLine("FlyingUnit 정보 (new로 숨김)");
    }
}


// ==== 5. 캐스팅용 클래스 계층 ====
class Item
{
    public string Name { get; set; }
    public Item(string name) { Name = name; }

    public virtual void Use()
    {
        Console.WriteLine($"[{Name}] 사용");
    }
}

class Weapon : Item
{
    public int Damage { get; set; }
    public Weapon(string name, int damage) : base(name) { Damage = damage; }

    public override void Use()
    {
        Console.WriteLine($"[{Name}] 무기장착! 공격력: {Damage}");
    }

    public void Sharpen()
    {
        Damage += 5;
        Console.WriteLine($"[{Name}] 연마! 공격력: {Damage}");
    }
}

class Potion : Item
{
    public int HealAmount { get; set; }
    public Potion(string name, int heal) : base(name) { HealAmount = heal; }

    public override void Use()
    {
        Console.WriteLine($"[{Name}] 회복 +{HealAmount} HP");
    }
}


class Program
{
    static void Main(string[] args)
    {
        // ==== sealed class ====
        Console.WriteLine("==== 1. sealed class ====");

        BossMonster boss = new BossMonster("보스", 1000, "강력한 일격!");
        boss.Attack();          // [BOSS 보스] 강력한 일격! 발동!
        // BossMonster를 상속받아 UltraBoss를 만들 수 없음. -> 설계 의도 보호
        
        // ==== sealed override ====
        Console.WriteLine("\n==== 2. sealed override ====");

        Dragon dragon = new Dragon("레드 드래곤", 500);
        dragon.Attack();            // 화염 브레스! (sealed — 자식도 이 방식)
        dragon.TakeDamage(100);     // 50 데미지 (절반 감소)
        
        BabyDragon baby = new BabyDragon("아기 드래곤");
        baby.Attack();              // 화염 브레스! (부모 sealed 메서드 그대로)
        baby.TakeDamage(30);        // 풀 데미지 (자체 override)
        
        // ==== new vs override ====
        Console.WriteLine("\n==== 3. new vs override ====");

        BaseUnit unit1 = new FlyingUnit();      // 부모 타입 변수에 자식 대입
        FlyingUnit unit2 = new FlyingUnit();

        unit1.Move();                           // 날아서 이동 - override라서 실제 타입 기준
        unit2.Move();                           // 날아서 이동
        
        unit1.Info();                           // BaseUnit 정보 출력 new라서 참조타입(BaseUnit) 기준
        unit2.Info();                           // FlyingUnit 정보 출력 FlyingUnit 타입으로 호출하면 자식것이 출력됨
        
        // 이것이 new의 함정! 부모타입으로 다루면 부모 메서드가 호출됨
        // -> 다형성이 필요하면 반드시 virtual + override 사용
        
        // ==== 업캐스팅 (UpCasting) ====
        Console.WriteLine("\n==== 4. 업캐스팅 ====");
        
        // 자식 → 부모 타입으로 대입 (항상 안전, 암시적 변환)
        Weapon sword = new Weapon("엑스칼리버", 50);
        Item item = sword;              // Weapon → Item (업캐스팅, 자동)

        item.Use();                     // "무기 장착!" — 다형성 동작 (override)
        // item.Sharpen();              // 컴파일 에러! Item 타입에는 Sharpen이 없음
        // 업캐스팅하면 부모 인터페이스만 보임, 자식 고유 기능은 숨겨짐
        
        // ==== 다운 캐스팅 (DownCasting) ====
        Console.WriteLine("\n==== 5. 다운캐스팅 ====");
        
        // 부모 -> 자식 타입으로 변환  (위험할수 있음, 명시적 캐스트 필요함.)
        
        // 방법 1. 직접 캐스팅 - 실패 시 InvalidCastException
        Weapon w1 = (Weapon)item;       // item이 실제로 Weapon이니까 OK
        w1.Sharpen();
        
        // 방법 2. as - 실패 시 null 반환 (예외 없음)
        Item mysteryItem = new Potion("체력 포션", 50);
        Weapon? w2 = mysteryItem as Weapon;         // Potion이므로 실패 -> null
        Console.WriteLine($"as 캐스팅 결과: {w2}");    // null (출력 안됨 또는 빈 문자열)

        if (w2 != null)
            w2.Sharpen();
        else 
            Console.WriteLine("무기가 아닙니다!");
        
        // 방법 3: is + 패턴 매칭 - 가장 안전하고 권장되는 방식
        if (mysteryItem is Potion potion)
        {
            Console.WriteLine($"{potion.Name} 발견! 회복량: {potion.HealAmount}");
            potion.Use();
        }
        
        // ==== 실전: 인벤토리에서 타입별 처리 ==== 
        Console.WriteLine("\n==== 6. 실전 - 인벤토리 타입별 처리 ====");

        Item[] inventory = new Item[]
        {
            new Weapon("단검", 15),
            new Potion("마나 포션", 30),
            new Weapon("활", 25),
            new Potion("해독제", 10)
        };

        int weaponCount = 0;
        int potionCount = 0;

        foreach (Item i in inventory)
        {   
            // is 패턴 매칭으로 안전하게 분기
            if (i is Weapon wp)
            {
                weaponCount++;
                Console.WriteLine($" 무기: {wp.Name}, 공격력: {wp.Damage}");
            }
            else if (i is Potion pt)
            {
                potionCount++;
                Console.WriteLine($" 포션: {pt.Name}, 회복략: {pt.HealAmount}");
            }
        }
        Console.WriteLine($"무기 {weaponCount}개, 포션 {potionCount}개");

        // ==== as vs is vs 직접캐스팅 정리 ====
        Console.WriteLine("\n==== 7. 캐스팅 방식 비교 ====");
        Console.WriteLine("(Weapon)item     → 실패 시 InvalidCastException (확실할 때만)");
        Console.WriteLine("item as Weapon   → 실패 시 null 반환 (null 체크 필요)");
        Console.WriteLine("item is Weapon w → 성공 시 바로 사용 가능 (가장 권장)");
    }
}

// 출력 결과
// ==== 1. sealed class ====
// [BOSS 보스] 강력한 일격! 발동!
//
// ==== 2. sealed override ====
// [레드 드래곤] 화염 브레스!
// [레드 드래곤] 50 데미지! 남은 HP: 450
// [아기 드래곤] 화염 브레스!
// [아기 드래곤] 아기라서 더 아프다!
// [아기 드래곤] 남은 HP: 20
//
// ==== 3. new vs override ====
// FlyingUnit: 날아서 이동
// FlyingUnit: 날아서 이동
// BaseUnit 정보
// FlyingUnit 정보 (new로 숨김)
//
// ==== 4. 업캐스팅 ====
// [엑스칼리버] 무기장착! 공격력: 50
//
// ==== 5. 다운캐스팅 ====
// [엑스칼리버] 연마! 공격력: 55
// as 캐스팅 결과: 
// 무기가 아닙니다!
// 체력 포션 발견! 회복량: 50
// [체력 포션] 회복 +50 HP
//
// ==== 6. 실전 - 인벤토리 타입별 처리 ====
//  무기: 단검, 공격력: 15
//  포션: 마나 포션, 회복략: 30
//  무기: 활, 공격력: 25
//  포션: 해독제, 회복략: 10
// 무기 2개, 포션 2개
//
// ==== 7. 캐스팅 방식 비교 ====
// (Weapon)item     → 실패 시 InvalidCastException (확실할 때만)
// item as Weapon   → 실패 시 null 반환 (null 체크 필요)
// item is Weapon w → 성공 시 바로 사용 가능 (가장 권장)