// ==== 1. 배열 (Array) ====
int[] numbers = {1 ,2, 3, 4, 5};
Console.WriteLine(numbers[0]);              // 인덱스는 0부터 시작합니다!
Console.WriteLine(numbers.Length);

// 배열 선언 후 초기화
string[] names = new string[3];             // 크기 고정 - 한번 정하면 변경 불가
names[0] = "A";
names[1] = "B";
names[2] = "C";

foreach (string name in names)
    Console.WriteLine(name);

// ==== 2. 2차원 배열 ====
int[,] matrix =
{
    { 1, 2, 3 },
    { 4, 5, 6 }
};
Console.WriteLine(matrix[0, 2]);            // 0행 2열
Console.WriteLine(matrix[1, 1]);            // 1행 1열

// ==== 3. List<T> ====
List<int> list = new List<int>();
list.Add(10);
list.Add(20);
list.Add(30);
list.Remove(20);
list.RemoveAt(0);                      // 값으로 제거!
Console.WriteLine(list.Count);              // 남은 리스트 건수!
Console.WriteLine(list[0]);                 // 첫번째 값!

List<string> fruits = new List<string> { "apple", "banana", "cherry" };
Console.WriteLine(fruits.Contains("banana"));                   // True
fruits.Add("mango");
foreach (string fruit in fruits)
    Console.WriteLine(fruit);

// ==== 4. Dictionary<K, V> ====
Dictionary<string, int> scores = new Dictionary<string, int>();
scores["A"] = 95;
scores["B"] = 80;
scores["C"] = 70;

Console.WriteLine(scores["A"]);             // 95 점수 출력!
Console.WriteLine(scores.ContainsKey("B")); // True, 존재 여부 체크

foreach (KeyValuePair<string, int> pair in scores)
    Console.WriteLine($"{pair.Key}: {pair.Value}");

// ==== 5. Array vs List 비교 ====
// 크기가 고정이고 성능이 중요하면 Array
// 크리가 유동적이면 List
int[] fixedArr = new int[5];                // 크기 변경 불가
List<int> dynamicList = new List<int>();    // 크기 자동 확장
dynamicList.Add(1);
dynamicList.Add(2);
Console.WriteLine($"List 크기: {dynamicList.Count}");

// Run 시 출력값!1
// 5
// A
// B
// C
// 3
// 5
// 1
// 30
// True
// apple
// banana
// cherry
// mango
// 95
// True
// A: 95
// B: 80
// C: 70
// List 크기: 2