// ==== 1. 제네릭이 없을 때의 문제 ====
class IntBox
{
    public int Value { get; set; }
}

class StringBox                         // 타입만 다르고 코드가 중복되는 케이스!
{
    public string Value { get; set; }
}

// ==== 2. 제네릭 클래스 ====
class Box<T>                            // T = 타입 파라미터, 호출 시정에 타입이 결정됩니다!
{
    public T Value { get; set; }

    public void Print()
    {
        Console.WriteLine($"[{typeof(T).Name}] {Value}");
    }
}

// ==== 3. 제네릭 메서드 ====
class Util
{
    public static T Max<T>(T a, T b) where T : IComparable<T>       // where = 타입 제약조건
    {
        return a.CompareTo(b) > 0 ? a : b;                          // IComparable 없으면 비교 자체가 불가합니다!
    }

    public static void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
}

// ==== 4. 제네릭 Stack 직접 구현
class Stack<T>
{
    private T[] _items = new T[100];
    private int _top = 0;

    public void Push(T item)
    {
        _items[_top++] =  item;
    }

    public T Pop()
    {
        return _items[--_top];              // LIFO - 마지막에 넣은것이 먼저 나오게끔
    }
    
    public T Peak() => _items[_top - 1];    // 꺼내지 않고 맨 위 값만 확인
    public bool IsEmpty() => _top == 0;
}

class Program
{
    static void Main(string[] args)
    {
        // ==== 2. 제네릭 클래스 사용 ====
        Box<int> intBox = new Box<int>();
        intBox.Value = 42;
        intBox.Print();                         // [Int32] 42
        
        Box<string> strBox = new Box<string>();
        strBox.Value = "안녕하세요.";
        strBox.Print();                         // [String] 안녕하세요.
        
        var doubleBox = new Box<double>();      // var로 타입 추론
        doubleBox.Value = 3.14;
        doubleBox.Print();
        
        // ==== 3. 제네릭 메서드 사용 ====
        Console.WriteLine(Util.Max(3, 7));                  // 7
        Console.WriteLine(Util.Max("apple", "banana"));     // banana (알파벳순)

        int x = 10, y = 20;
        Util.Swap(ref x, ref y);
        Console.WriteLine($"x={x}, y={y}");                 // x=20, y=10
        
        // ==== 4. 제네릭 Stack 사 ====
        Stack<int> stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        
        Console.WriteLine(stack.Pop());                     // 3
        Console.WriteLine(stack.Peak());                    // 2
    }
}

// 실행 결과!!
// [Int32] 42
// [String] 안녕하세요.
// [Double] 3.14
// 7
// banana
// x=20, y=10
// 3
// 2