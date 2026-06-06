class Program
{
    // ==== 1. 기본 메서드 ====
    static void Greet(string name)                      // void = 반환값 없음!
    {
        Console.WriteLine($"안녕하세요! {name}님!");       // 터미널 로그만 출력!
    }

    // ==== 2. 반환값이 있는 메서드 ====
    static int Add(int a, int b)                        // int (정수) 형태 반환!
    {
        return a + b;                                   // 반환 타입과 return 값의 타입이 일치해야 함!
    }

    // ==== 3. 기본값 매개변수 ====
    static void PrintInfo(string name, int age = 20)    // age 생략 시 20으로 설정
    {
        Console.WriteLine($"{name}, {age}세");
    }

    // ==== 4. 오버로딩 - class 안에서만 동작 ====
    static int Multiply(int a, int b)       // 같은 이름, 다른 매개변수 타입/개수
    {
        return a * b;
    }

    static double Multiply(double a, double b)
    {
        return a * b;
    }

    // ==== 5. ref / out 매개변수
    static void Double(ref int x)           // ref = 원본 변수를 직접 수정!
    {
        x = x * 2;
    }

    static void Divide(int a, int b, out int quotient, out int remainder) // out = 여러 값 반환
    {
        quotient = a / b;
        remainder = a % b;
    }

    // ==== 6. 재귀 ====
    static int Factorial(int n)             // 자기 자신을 호출 - 반드시 종료 조건이 필요합니다!
    {
        if (n <= 1) return 1;               // 종료 조건 없으면 스택 오버플로우 발생
        return n * Factorial(n - 1);
    }

    // ==== Main 에서 호출 ====
    static void Main(string[] args)
    {
        Greet("장 영 완");

        int sum = Add(3, 5);
        Console.WriteLine($"3 + 5 = {sum}");

        PrintInfo("A");
        PrintInfo("B", 30);

        Console.WriteLine(Multiply(3, 4));      // int 버전 호출
        Console.WriteLine(Multiply(1.5, 2.0));  // double 버전 호출

        int value = 10;
        Double(ref value);                      // ref는 호출할 때도 ref 키워드 필요
        Console.WriteLine(value);

        Divide(17, 5, out int q, out int r);   // out도 호출할 때 out 키워드 필요
        Console.WriteLine($"몫: {q}, 나머지: {r}");

        Console.WriteLine(Factorial(5));
    }
}

// 출력 로그!
// 안녕하세요! 장 영 완님!
// 3 + 5 = 8
// A, 20세
// B, 30세
// 12
// 3
// 20
// 몫: 3, 나머지: 2
// 120