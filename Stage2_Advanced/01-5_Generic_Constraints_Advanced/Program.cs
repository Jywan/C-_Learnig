using System;
using System.Collections.Generic;


// [보강] 01-5. 제네릭 제약조건 심화, 공변/반공변
// 기존 01_Generics 에서 where IComparable만 간단히 다뤘기 때문에 보강
// Unity/서버에서 제네릭 팩토리, 오브젝트 풀, 이벤트 시스템을 만들때 필요한 심화 과정

// ==== 제약조건(Constraints) 종류 ====
// where T : struct             - T가 값 타입이어야함.
// where T : class              - T가 참조 타입이어야함.
// where T : new()              - T가 매개변수 없는 생성자를 가져야함.
// where T : 기반클래스          - T가 해당 클래스이거나 자식이어야함.
// where T : 인터페이스          - T가 해당 인터페이스를 구현해야함.
// where T : U                  - T가 다른 타입 매개변수 U이거나 U의 자식이어야함.
// 여러 제약조건 조합 가능: where T : class, IComparable, new()


// ==== 1. 다양한 제약조건 예제 ====

// struct 제약 - 값 타입만 허용 (int, float, struct)
// Nullable 래퍼를 직접 만드는 예시
struct Optional<T> where T : struct
{
    private T _value;
    public bool HasValue { get; private set; }

    public Optional(T value)
    {
        _value = value;
        HasValue = true;
    }

    public T Value
    {
        get
        {
            if (!HasValue) throw new InvalidOperationException("값이 없습니다!");
            return _value;
        }
    }
    
    public override string ToString() => HasValue ? _value.ToString()! : "(없음)";
}


// class + new() 제약 - 참조 타입 + 기본 생성자 필수
// 게임에서 오브젝트 풀, 팩토리 패턴에 핵심적으로 사용됨.
class ObjectPool<T> where T : class, new()
{
    private Queue<T> _pool = new Queue<T>();
    
    // 풀에서 꺼내기 - 없으면 새로고침
    public T Get()
    {
        if (_pool.Count > 0)
        {
            Console.WriteLine($"[Pool] 재사용: {typeof(T).Name}");
            return _pool.Dequeue();
        }
        Console.WriteLine($"[Pool] 새로 생성: {typeof(T).Name}");
        return new T();     // new() 제약이 있어야 가능
    }
    
    // 풀에 바노한
    public void Return(T item)
    {
        _pool.Enqueue(item);
        Console.WriteLine($"[Pool] 반환됨 (풀 크기: {_pool.Count})");
    }
}


// 인터페이스 제약 - 특정 능력을 가진 타입만 허용
interface IResettable
{
    void Reset();
}

interface IIdentifiable
{
    int Id { get; set; }
}


// 다중 제약조건 조합: class + 두 개의 인터페이스 + new()
class ManagedPool<T> where T : class, IResettable, IIdentifiable, new()
{
    private Queue<T> _pool = new Queue<T>();
    private int _nextId = 1;

    public T Get()
    {
        T item;
        if (_pool.Count > 0)
        {
            item= _pool.Dequeue();
            item.Reset();               // IResettable 보장
        }
        else
        {
            item = new T();             // new() 보장
        }
        item.Id = _nextId++;            // IIdentifiable 보장
        return item;
    }

    public void Return(T item)
    {
        _pool.Enqueue(item);
    }
}


// 풀에서 사용할 총알 클래스
class Bullet : IResettable, IIdentifiable
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool IsActive { get; set; }

    public void Reset()
    {
        X = 0;
        Y = 0;
        IsActive = false;
    }

    public override string ToString() => $"Bullet #{Id} ({X}, {Y} Active:{IsActive})";
}


// ==== 2. 기반 클래스 제약 ====
class Entity
{
    public string Name { get; set; }
    public virtual void Activate() => Console.WriteLine($"[{Name}]: 활성화");
}

class NPC : Entity
{
    public string Dialogue { get; set; } = "";
}

class Monster2 : Entity
{
    public int Damage { get; set; }
}

// T는 Entity이거나 그 자식이어야한다.
class EntityManager<T> where T : Entity
{
    private List<T> _entities = new List<T>();

    public void Register(T entity)
    {
        _entities.Add(entity);
        Console.WriteLine($"등록: {entity.Name}");
    }
    
    public void ActivateAll()
    {
        foreach (T entity in _entities)
            entity.Activate();          // Entity의 메서드 호출 보장
    }
}   


// ==== 3. 공변성(Covariance)과 반공변성(Contravariance)
// 공변성 : out T - "를 출력만 한다." (생산자)
// 자식타입을 부모타입으로 업스케일링 가능하게 해줌
// IEnumerable<out T>, IReadOnlyList<out T>가 대표적
interface IProducer<out T>
{
    T Produce();
}

// 반공변성 : in T - "T를 입력만 받는다" (소비자)
// 부모 타입을 자식 타입 자리에 넣을 수 있게 해줌
// IComparer<in T>, Action<in T>가 대표적
interface IConsumer<in T>
{
    void Consume(T message);
}

// 공변/반공변 데모용 클래스 계층
class Animal
{
    public string Name { get; set; }
    public Animal(string name) { Name = name; }
    public override string ToString() => $"Animal{Name}";
}

class Dog : Animal
{
    public string Breed { get; set; }
    public Dog(string name, string breed) : base(name) { Breed = breed; }
    public override string ToString() => $"Dog({Name}, {Breed})";
}

// 공변성 구현 - Dog 생산자를 Animal 생산자로 사용 가능
class DogFactory : IProducer<Dog>
{
    private int _count = 0;

    public Dog Produce()
    {
        _count++;
        return new Dog($"강아지 {_count}", "진돗개");
    }
}


// 반공변성 구현 - Animal 소비자를 Dog 소비자로 사용가능
class AnimalShelter : IConsumer<Animal>
{
    public void Consume(Animal item)
    {
        Console.WriteLine($"보호소에 {item} 입소");
    }
}

class Program
{
    // 제네릭 메서드에 제약조건
    static T CreateAndName<T>(string name) where T : Entity, new()
    {
        T entity = new T();
        entity.Name = name;
        return entity;
    }

    static void Main(string[] args)
    {
        // ==== 1. struct 제약 — Optional<T> ====
        Console.WriteLine("==== 1. struct 제약 (Optional) ====");

        Optional<int> someInt = new Optional<int>(42);
        Optional<int> noInt = default;    // HasValue = false

        Console.WriteLine($"someInt: {someInt}");       // 42
        Console.WriteLine($"noInt: {noInt}");           // (없음)

        // Optional<string> invalid;  // 컴파일 에러! string은 class(참조 타입)

        // ==== 2. class + new() 제약 — ObjectPool ====
        Console.WriteLine("\n==== 2. class + new() 제약 (ObjectPool) ====");

        ObjectPool<Bullet> bulletPool = new ObjectPool<Bullet>();

        Bullet b1 = bulletPool.Get();       // 새로 생성
        Bullet b2 = bulletPool.Get();       // 새로 생성
        bulletPool.Return(b1);              // 반환
        Bullet b3 = bulletPool.Get();       // 재사용!

        // ObjectPool<int> invalid;         // 컴파일 에러! int는 struct
        // ObjectPool<Animal> invalid;      // 컴파일 에러! Animal에 기본 생성자 없음 (매개변수 있음)

        // ==== 3. 다중 제약조건 — ManagedPool ====
        Console.WriteLine("\n==== 3. 다중 제약조건 (ManagedPool) ====");

        ManagedPool<Bullet> managedPool = new ManagedPool<Bullet>();

        Bullet mb1 = managedPool.Get();
        mb1.X = 10; mb1.Y = 20; mb1.IsActive = true;
        Console.WriteLine($"사용 중: {mb1}");

        managedPool.Return(mb1);
        Bullet mb2 = managedPool.Get();     // Reset 후 새 Id 부여
        Console.WriteLine($"재사용: {mb2}");  // X=0, Y=0, 새 Id

        // ==== 4. 기반 클래스 제약 ====
        Console.WriteLine("\n==== 4. 기반 클래스 제약 ====");

        EntityManager<NPC> npcManager = new EntityManager<NPC>();
        npcManager.Register(new NPC { Name = "상인", Dialogue = "어서오세요" });
        npcManager.Register(new NPC { Name = "대장장이", Dialogue = "뭘 만들까?" });
        npcManager.ActivateAll();

        // EntityManager<string> invalid;  // 컴파일 에러! string은 Entity가 아님

        // ==== 5. 제네릭 메서드 + 제약조건 ====
        Console.WriteLine("\n==== 5. 제네릭 메서드 + 제약조건 ====");

        NPC npc = CreateAndName<NPC>("마을 이장");
        Monster2 monster = CreateAndName<Monster2>("슬라임");
        npc.Activate();
        monster.Activate();

        // ==== 6. 공변성 (out T) ====
        Console.WriteLine("\n==== 6. 공변성 (Covariance, out T) ====");

        IProducer<Dog> dogProducer = new DogFactory();
        Dog dog = dogProducer.Produce();
        Console.WriteLine($"생산: {dog}");

        // 공변성: IProducer<Dog>를 IProducer<Animal>로 대입 가능!
        // Dog는 Animal의 자식이고, out이라서 T를 "꺼내기만" 하니까 안전
        IProducer<Animal> animalProducer = dogProducer;  // 이게 공변성!
        Animal animal = animalProducer.Produce();
        Console.WriteLine($"Animal로 받기: {animal}");   // 실제로는 Dog

        // 실제 예: IEnumerable<Dog>를 IEnumerable<Animal>로 대입 가능
        List<Dog> dogs = new List<Dog> { new Dog("백구", "진돗개"), new Dog("초코", "푸들") };
        IEnumerable<Animal> animals = dogs;  // List<Dog> → IEnumerable<Animal> OK!
        foreach (Animal a in animals)
            Console.WriteLine($"  {a}");

        // ==== 7. 반공변성 (in T) ====
        Console.WriteLine("\n==== 7. 반공변성 (Contravariance, in T) ====");

        IConsumer<Animal> animalConsumer = new AnimalShelter();
        animalConsumer.Consume(new Animal("고양이"));

        // 반공변성: IConsumer<Animal>을 IConsumer<Dog>로 대입 가능!
        // Animal을 받을 수 있으면 Dog도 당연히 받을 수 있으니까 안전
        IConsumer<Dog> dogConsumer = animalConsumer;  // 이게 반공변성!
        dogConsumer.Consume(new Dog("뽀삐", "말티즈"));

        // 실제 예: Action<Animal>을 Action<Dog>로 대입 가능
        Action<Animal> printAnimal = (a) => Console.WriteLine($"  동물: {a}");
        Action<Dog> printDog = printAnimal;  // Action<in T>라서 가능!
        printDog(new Dog("콩이", "비숑"));

        // ==== 정리 ====
        Console.WriteLine("\n==== 8. 공변/반공변 정리 ====");
        Console.WriteLine("공변 (out T): 생산자, T를 반환만 함");
        Console.WriteLine("  자식→부모 방향 대입 가능 (IProducer<Dog> → IProducer<Animal>)");
        Console.WriteLine("");
        Console.WriteLine("반공변 (in T): 소비자, T를 입력만 받음");
        Console.WriteLine("  부모→자식 방향 대입 가능 (IConsumer<Animal> → IConsumer<Dog>)");
        Console.WriteLine("");
        Console.WriteLine("불변 (T): 입력+출력 모두 → 어느 쪽도 대입 불가");
        Console.WriteLine("  List<Dog>를 List<Animal>에 대입 불가! (List<T>는 불변)");
    }
}
