// ==== 1. 정수형 ====
int age = 32;                           // 가장 많이 쓰는 정수형, 약 ±21억 범위를 갖음.
long bigNum = 10000000000L;             // int 범위 초과시 long 사용, "L" 접미사 필수!

// ==== 2. 실수형 ====
float f = 3.14f;                        // 소수점 7자리, "f" 접미사 필수!
double d = 3.14159265;                  // 소수점 15자리, 기본 실수형
decimal money = 9.99m;                  // 금융 계산용 - 부동소수점 오차 없음!, "m" 접미사 필수!

// ==== 3. 문자/문자열 ====
char letter = 'A';                      // 단일 문자, (중요!) 작은 따옴표사용
string text = "안녕하세요!";               // 문자열, (중요!) 큰따옴표

// ==== 4. 불리언(논리식) ====
bool isOn = true;
bool isOff = false;

// ==== 5. var(타입 추론) ====
var num = 42;                           // 컴파일러가 int로 추론 - 선언과 동시에 초기화는 필수!
var message = "반가워요!";                // 컴파일러가 string으로 추론

// ==== 6. 산술 연산자 ====
int a = 10, b = 3;
Console.WriteLine(a + b);               // 13
Console.WriteLine(a - b);               // 7
Console.WriteLine(a * b);               // 30
Console.WriteLine(a / b);               // 3  (정수 나눗셈 - 소수점 버림 주의)
Console.WriteLine(a % b);               // 1  (나머지)

// ==== 7. 비교/논리 연산자 ====
Console.WriteLine(a > b);               // True
Console.WriteLine(a == b);              // False
Console.WriteLine(a != b);              // True
Console.WriteLine(true && false);       // False (AND 연산자)
Console.WriteLine(true || false);       // True (OR 연산자)
Console.WriteLine(!true);               // False (NOT 연산자)

// ==== 8. 형변환 ====
int x = 10;
double result = x;                      // 묵시적 변환 - 범위가 큰 쪽으로는 자동 변환
int y = (int)3.99;                      // 명시적 변환(캐스팅) - 소수점 버림, 데이터 손실 가능
string numStr = "123";
int parsed = int.Parse(numStr);         // 문자열 -> 정수 변환
Console.WriteLine($"result={result}, y={y}, parsed={parsed}");

// 할당되었지만 사용되지 않았습니다.로그가 여러개 나오지만 말 그대로 선언만하고 사용하지 않아서 생긴 로그들!
// 선언된 데이터들은 출력된다!
// 13
// 7
// 30
// 3
// 1
// True
// False
// True
// False
// True
// False
// result=10, y=3, parsed=123