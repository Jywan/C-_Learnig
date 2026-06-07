// ==== 1. 델리게이트 기본 ====
delegate int Calculate(int a, int b);   // 메서드 시그니처를 타입으로 정의

class Calculator
{
    public static int Add(int a, int b) => a + b;
    public static int Subtract(int a, int b) => a - b;
    public static int Multiply(int a, int b) => a * b;
}

// ==== 2. 이벤트 ====
class Button
{
    public event Action OnClick;    // Action = 반환값 없는 델리게이트(void)

    public void Click()
    {
        Console.WriteLine("버튼 클릭됨!");
        OnClick?.Invoke();          // ? = 구독자 없으면 null이므로 안전 호출
    }
}

class EventLogger
{
    public void Log()
    {
        Console.WriteLine("[LOG] 클릭 이벤트 발생");
    }
}

// ==== 3. Func, Action ====
// Func<T1. T2. TResult> : 반환값이 있는 델리게이트
// Action<T1, T2>        : 반환값이 없는 델리게이트 (void)
class MathHelper
{
    public static void Excute(int a, int b, Func<int, int, int> operation)
    {
        int result = operation(a, b);        // 어떤 연산을 할지 외부에서 주입받음
        Console.WriteLine($"결과: {result}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        // ==== 1. 델리게이트 사용 ====
        Calculate calc = Calculator.Add;    // 메서드를 변수에 담음
        Console.WriteLine(calc(3, 5));      // 8
        
        calc = Calculator.Subtract;         // 다른 메서드로 교체
        Console.WriteLine(calc(10, 4));     // 6
        
        // 멀티캐스트 - 여러 메서드를 하나의 델리게이트에 연결
        Calculate multi = Calculator.Add;
        multi += Calculator.Subtract;       // += 로 메서드 추가
        multi(10, 5);                       // Add, Subtract 순서로 들다 실행!
        
        // ==== 2. 이벤트 사용 ====
        Button button = new Button();
        EventLogger logger = new EventLogger();

        button.OnClick += logger.Log;       // 이벤트 구독
        button.OnClick += () => Console.WriteLine("[ALERT] 클릭 감지");     // 람다도 가능하다!
        button.Click();                     // 버튼 클릭됨! → Log → ALERT 순으로 실행

        button.OnClick -= logger.Log;       // 이벤트 구독 해제
        button.Click();                     // 버튼 클릭됨! → ALERT만 실행
        
        // ==== 3. Func / Action 사용 ====
        MathHelper.Excute(10, 3, Calculator.Add);           // 결과 : 13
        MathHelper.Excute(10, 3, Calculator.Subtract);      // 결과 : 7

        Action<string> print = msg => Console.WriteLine($"메세지: {msg}");
        print("안녕하세요!");

        Func<int, int, bool> isGreater = (a, b) => a > b;
        Console.WriteLine(isGreater(5, 3));         // True
        Console.WriteLine(isGreater(2, 7));         // False
    }
}

// 실행 결과!!
// 8
// 6
// 버튼 클릭됨!
// [LOG] 클릭 이벤트 발생
// [ALERT] 클릭 감지
// 버튼 클릭됨!
// [ALERT] 클릭 감지
// 결과: 13
// 결과: 7
// 메세지: 안녕하세요!
// True
// False