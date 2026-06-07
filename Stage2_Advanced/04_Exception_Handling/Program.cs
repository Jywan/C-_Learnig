// ==== 3. 커스텀 예외 ====
class InsufficientBalanceException : Exception
{
    public int Balance { get; }
    public int Amount { get; }

    public InsufficientBalanceException(int balance, int amount)
        : base($"잔액 부족 - 잔액: {balance}, 요청: {amount}")
    {
        Balance = balance;
        Amount = amount;
    }
}

class BankAccount
{
    public int Balance { get; private set; }

    public BankAccount(int balance)
    {
        Balance = balance;
    }

    public void Withdraw(int amount)
    {
        if (amount > Balance)
            throw new InsufficientBalanceException(Balance, amount);    // 예외 던지기!
        Balance -= amount;
        Console.WriteLine($"출금 완료 - 잔액: {Balance}");
    }
}

// ==== 4. 예외 처리 전략 ====
class Program
{
    static int ParseNumber(string input)
    {
        if (!int.TryParse(input, out int result)) // TryParse = 예외 없이 실패 여부 반환
            throw new ArgumentException($"숫자가 아닙니다: {input}");

        return result;
    }

    static void Main(string[] args)
    {
        // ==== 1. 기본 try/catch ====
        try
        {
            int a = 10, b = 0;
            int result = a / b; // DivideByZeroException 발생!
            Console.WriteLine(result); // 실행 되지 않음
        }
        catch (DivideByZeroException ex)
        {
            Console.WriteLine($"0으로 나눌 수 없습니다.{ex.Message}");
        }

        // ==== 2. 여러 catch / finally ====
        try
        {
            string input = "abc";
            int number = int.Parse(input); // FormatException 발생
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"형식 오류: {ex.Message}");
        }
        catch (OverflowException ex)
        {
            Console.WriteLine($"범위 초과: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"알 수 없는 오류: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("finally는 예외 여부와 관계없이 항상 실핻됩니다!");
        }

        // 커스텀 예외
        BankAccount account = new BankAccount(1000);

        try
        {
            account.Withdraw(500);      // 출금 완료 - 잔액: 500
            account.Withdraw(700);      // InsufficientBalanceException 발생
        }
        catch (InsufficientBalanceException ex)
        {
            Console.WriteLine(ex.Message);
        }

        // TryParse
        try
        {
            Console.WriteLine(ParseNumber("123")); // 123
            Console.WriteLine(ParseNumber("abc")); // ArgumentException 발생
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
// 실행 결과
// 0으로 나눌 수 없습니다.Attempted to divide by zero.
// 형식 오류: The input string 'abc' was not in a correct format.
// finally는 예외 여부와 관계없이 항상 실핻됩니다!
// 출금 완료 - 잔액: 500
// 잔액 부족 - 잔액: 500, 요청: 700
// 123
// 숫자가 아닙니다: abc