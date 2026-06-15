using System;

// [보강] 01-5. 값 타입 vs 참조 타입 (struct, enum 심화)
// 기존 06_Classes_Objects에서 class만 다뤘기 때문에 보강
// Unity에서 Vector3, Color 등이 struct인 이유를 이해하기 위한 챕터

// ==== 값 타입 vs 참조타입 ====
// C#의 모든 타입은 값 타입과 참조 타입 둘 중 하나다

// 값 타입 (Value Type) : struct, enum, int, float, bool 등
//  - 스택에 저장됨
//  - 대입 시 값이 복사됨 (독립적인 복사본)
//  - null 불가 (Nullable<T>로 감싸야 가능)
//  - Unity 예시 : Vector3, Color, Quaternion, Ray

// 참조 타입 (Reference Type) : class, interface, delegate, string
//  - 힙에 저장, 변수는 주소만 가짐
//  - 대입 시 주소가 복사됨 (같은 객체를 공유)
//  - null 가능
//  - Unity 예시 : GameObject, Transform, MonoBehaviour

// Java/JS 에서는 객체가 전부 참조 타입이라 이 구분을 의식할 일은 없었다.
// C#(특히 Unity)에서는 Vector3 같은 핵심 타입이 struct라서 반드시 이해야해한다!


// ==== 1. struct = 값 타입 ====
struct PointStruct
{
    public float X;
    public float Y;

    public PointStruct(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

// ==== 2. class = 참조 타입 ====
class PointClass
{
    public float X;
    public float Y;

    public PointClass(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

// ==== 3. Unity의 Vector3를 흉내낸 struct ====
// Unity에서 Vector3가 struct인 이유 :
//  - 매 프레임 수천 개의 위치/방향 계산이 발생함
//  - 힙 할당 없이 스택에서 바로 처리하면 GC 부담이 없음
//  - 크기가 작아서(12바이트) 복사 비용도 저렴함
struct Vector3
{
    public float X, Y, Z;

    public Vector3(float x, float y, float z)
    {
        X = x; Y = y; Z = z;
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}

// ==== 4. Unity transform.position 패턴 재현 ====
// transform.position은 struct를 프로퍼티로 변환함
// 프로퍼티의 get은 복사본을 돌려주므로, .X = 5 같은 직접 수정이 불가능하다
class TransformExample
{
    private Vector3 _position = new Vector3(0, 0, 0);

    public Vector3 Position
    {
        get { return _position; }            // 복사본을 반환
        set { _position = value; }
    }
}

// ==== 5. enum = 값 타입의 열거형 ====
// 익숙한 필드!
// 단 Java의 enum은 class 기반(무거움), C#의 enum은 정수 기반(가벼운편)
// 게임에서 상태, 방향, 타입 구분에 매우 자주 사용됨.
enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}

// ==== 6. [Flags] enum ====
// 비트 연산으로 여러 값을 동시에 가질 수 있는 enum
// 게임게서 버프, 권한, 상태 조합등에 사용
[Flags]
enum Permission
{
    None = 0,
    Read = 1,                       // 0001
    Write = 2,                      // 0010
    Execute = 4,                    // 0100
    All = Read | Write | Execute    // 0111
}

class Program
{
    // 값 타입은 복사되므로 원본에 영향 없음.
    static void MoveStruct(PointStruct p)
    {
        p.X += 10;      // 복사본을 수정 - 원본 무관
    }

    // 참조 타입은 같은 객체를 가리키므로 원본이 바뀜
    static void MoveClass(PointClass p)
    {
        p.X += 10;      // 원본 객체를 직접 수정
    }

    // ref 키워드로 값 타입의 원본을 넘길 수 있음
    static void MoveStructRef(ref PointStruct p)
    {
        p.X += 10;
    }

    static void Main(string[] args)
    {
        // ==== 대입 시 차이 ====
        Console.WriteLine("==== 값 타입 (struct) ====");
        PointStruct s1 = new PointStruct(1, 2);
        PointStruct s2 = s1;            // 값 복사! s2는 독립적인 복사본
        s2.X = 99;
        Console.WriteLine($"s1: {s1}");     // (1, 2) - 원본이 바뀌지 않음.
        Console.WriteLine($"s2: {s2}");     // (99, 2)

        Console.WriteLine("\n==== 참조 타입 (class) ====");
        PointClass c1 = new PointClass(1, 2);
        PointClass c2 = c1;             // 주소 복사, c2는 c1과 같은 객체를 가리킴
        c2.X = 99;
        Console.WriteLine($"c1: {c1}");     // (99, 2) - 원본의 값이 변경됨.
        Console.WriteLine($"c2: {c2}");     // (99, 2)
    
        // ==== 메서드에 전달할 때 차이 ====
        Console.WriteLine("\n==== 메서드 전달 ====");
        PointStruct ps = new PointStruct(5, 5);
        MoveStruct(ps);
        Console.WriteLine($"struct 전달 후 : {ps}");    // (5, 5) - 안 바뀜

        PointClass pc = new PointClass(5, 5);
        MoveClass(pc);
        Console.WriteLine($"class 전달 후 : {pc}");     // (15, 5) - 바뀜

        MoveStructRef(ref ps);
        Console.WriteLine($"struct ref 전달 후 : {ps}");    // (15, 5) - ref라 바뀜

        // ==== Unity transform.position 실수 패턴 ====
        Console.WriteLine("\n==== Unity transform.position 패턴 ====");
        TransformExample transform = new TransformExample();

        // transform.Position.X = 5;    // 컴파일 에러! 복사본의 필드를 수정하는 꼴
        // 올바른 방법: 통째로 꺼내서 수정후 다시 넣기!
        Vector3 pos = transform.Position;
        pos.X = 5;
        transform.Position = pos;
        Console.WriteLine($"position: {transform.Position}");   // (5, 0, 0)
        
        // ==== enum 사용 ====
        Console.WriteLine("\n==== enum ====");
        Direction dir = Direction.Up;
        Console.WriteLine($"방향: {dir}");              // Up
        Console.WriteLine($"방향 (int): {(int) dir}");  // 0

        switch (dir)
        {
            case Direction.Up:
                Console.WriteLine("위로 이동");
                break;
            case Direction.Down:
                Console.WriteLine("아래로 이동");
                break;
            case Direction.Left:
                Console.WriteLine("왼쪽으로 이동");
                break;
            case Direction.Right:
                Console.WriteLine("오른쪽으로 이동");
                break;
        }

        // ==== Flags enum - 비트 조합 ====
        Console.WriteLine("\n==== Flags enum ====");
        Permission perm = Permission.Read | Permission.Write;   // 비트 OR로 권한 조합
        Console.WriteLine($"권한: {perm}");                      // Read, Write
        Console.WriteLine($"읽기 가능? {perm.HasFlag(Permission.Read)}");   // True
        Console.WriteLine($"실행 가능? {perm.HasFlag(Permission.Execute)}");// False

        perm |= Permission.Execute;           // 비트 OR 대입으로 권한 추가
        Console.WriteLine($"추가 후: {perm}");  // ALL

        // ==== null 차이 ====
        Console.WriteLine("\n==== null ====");
        PointClass nullRef = null;          // 참조 타입은 null 이 불가능하다
        Console.WriteLine(nullRef == null); // True

        // PointStruct nullVal = null;      // 컴파일 에러! 값 타입은 null 불가능
        PointStruct? nullable = null;       // Nullable<T> = T? 로 감싸면 가능
        Console.WriteLine(nullable.HasValue);   // False

    }
}

// 출력 결과값
// ==== 값 타입 (struct) ====
// s1: (1, 2)
// s2: (99, 2)
//
// ==== 참조 타입 (class) ====
// c1: (99, 2)
// c2: (99, 2)
//
// ==== 메서드 전달 ====
// struct 전달 후 : (5, 5)
// class 전달 후 : (15, 5)
// struct ref 전달 후 : (15, 5)
//
// ==== Unity transform.position 패턴 ====
// position: (5, 0, 0)
//
// ==== enum ====
// 방향: Up
// 방향 (int): 0
// 위로 이동
//
// ==== Flags enum ====
// 권한: Read, Write
// 읽기 가능? True
// 실행 가능? False
// 추가 후: All
//
// ==== null ====
// True
// False