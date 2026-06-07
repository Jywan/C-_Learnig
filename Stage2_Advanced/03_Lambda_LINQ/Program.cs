// ==== 1. 람다 기본 ====
// (매개변수) => 표현식 또는 (매개변수) => { 문장; }
Func<int, int> square = x => x * x;         // 한 줄은 중괄호 생략 가능
Func<int, int, int> add = (a, b) => a + b;
Action<string> greet = name => Console.WriteLine($"안녕, {name}");

Console.WriteLine(square(5));           // 25
Console.WriteLine(add(3, 4));           // 7
greet("장영완");                           // 안녕, 장영완!


// ==== 2. LINQ 기본 ====
List<int> numbers = new List<int> { 1, 2, 3, 4, 5,6 , 7, 8, 9, 10 };

// Where: 조건 필터링
List<int> evens = numbers.Where(n => n % 2 == 0).ToList();
Console.WriteLine(string.Join(", ", evens));            // 2, 4, 6, 8, 10

// Select: 반환 (map)
List<int> squared = numbers.Select(n => n * n).ToList();
Console.WriteLine(string.Join(", ", squared));          // 1, 4, 9, 16, 25, 36, 49, 64, 81, 100

// OrderBy / OrderByDescending: 정렬
List<int> sorted = numbers.OrderByDescending(n => n).ToList();
Console.WriteLine(string.Join(", ", sorted));           // 10, 9, 8, 7, 6, 5, 4, 3, 2, 1

// ==== 3. LINQ ====
Console.WriteLine(numbers.Sum());               // 55
Console.WriteLine(numbers.Average());           // 5.5
Console.WriteLine(numbers.Max());               // 10
Console.WriteLine(numbers.Min());               // 1

// ==== 4. LINQ 체이닐 ====
// 짝수만 골라서 제곱하고 내림차순 정렬
List<int> result = numbers
    .Where(n => n % 2 == 0)
    .Select(n => n * n)
    .OrderByDescending(n => n)
    .ToList();
Console.WriteLine(string.Join(", ", result));   // 100, 64, 36, 16, 4

// ==== 5. 객체 컬렉션에 LINQ ====
List<(string Name, int Score)> students = new List<(string, int)>
{
    ("A", 95),
    ("B", 72),
    ("C", 88),
    ("D", 60),
    ("E", 91)
};

// 80점 이상인 학생 이름만. 점수 내림차순
List<string> topStudents = students
    .Where(s => s.Score >= 80)
    .OrderByDescending(s => s.Score)
    .Select(s => $"{s.Name}({s.Score})")
    .ToList();

foreach (string student in topStudents)
{
    Console.WriteLine(student);
}


// ==== 6. First / Any / All ====
Console.WriteLine(students.First(s => s.Score >= 90).Name);
Console.WriteLine(students.First(s => s.Score < 70));
Console.WriteLine(students.First(s => s.Score >= 60));

// 실행 결과
// 25
// 7
// 안녕, 장영완
// 2, 4, 6, 8, 10
// 1, 4, 9, 16, 25, 36, 49, 64, 81, 100
// 10, 9, 8, 7, 6, 5, 4, 3, 2, 1
// 55
// 5.5
// 10
// 1
// 100, 64, 36, 16, 4
// A(95)
// E(91)
// C(88)
// A
// (D, 60)
// (A, 95)