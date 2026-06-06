// ==== 1. if / else if / else ===
// java와 동일하다!
int score = 85;

if (score >= 90)
    Console.WriteLine("A");
else if (score >= 80)
    Console.WriteLine("B");             // 이게 출력됨
else if (score >= 70)
    Console.WriteLine("C");
else
    Console.WriteLine("F");

// ==== 2. switch ====
string day = "Monday";

switch (day)
{
    case "Saturday" :
    case "Sunday":
        Console.WriteLine("주말");
        break;
    case "Monday":
        Console.WriteLine("월요일");       // 이게 출력됨
        break;
    default:
        Console.WriteLine("평일");
        break;
}

// ==== 3. 삼항 연산자 ====
int age = 20;
string result = age >= 18 ? "성인" : "미성년자";
Console.WriteLine(result);

// ==== 4. for 루프 ====
for (int i = 0; i < 5; i++)
{
    Console.WriteLine($"{i} ");
}

// ==== 5.while 루프 ====
int count = 0;
while (count < 3)
{
    Console.WriteLine($"count: {count}");
    count++;        // 이게 없으면 count가 계속 0이라 조건이 영원히 true
}

// ==== 6. do-while ====
int num = 0;
do
{
    Console.WriteLine($"num: {num}");       // 조건이 false여도 최소 1번은 실행됩니다!
    num++;
} while (num < 3);


// ==== 7. foreach ====
string[] fruits = {"A" , "B", "C", "D", "E", "F"};
foreach (string fruit in fruits)            // 컬렉션 순회에 특화!
{
    Console.WriteLine(fruit);
}

// ==== 8. break / continue ====
for (int i = 0; i < 10; i++)
{
    if (i == 3) continue;                   // 3은 건너뜀!
    if (i == 6) break;                      // 6에서 루프 종료!
    Console.WriteLine($"{i} ");
}

// 실행 결과!!
// B
// 월요일
// 성인
// 0 
// 1 
// 2 
// 3 
// 4 
// count: 0
// count: 1
// count: 2
// num: 0
// num: 1
// num: 2
// A
// B
// C
// D
// E
// F
// 0 
// 1 
// 2 
// 4 
// 5