// ==== 1. 기본 출력 ====
Console.WriteLine("헬로우~ C#!");              // WriteLine(): 출력 후 줄바꿈
Console.Write("줄바꿈 X");                     // Write(): 줄바꿈 없이 출력
Console.Write("이어서 출력합니다~ \n");           // \n을 넣게되면 줄바꿈이 이루어집니다

// ==== 2. 문자열 보간 ====
string name = "홍길동";
Console.WriteLine($"안녕하세요. {name}님!");     // $ : 변수를 문자열 안에 직접 삽입

// ==== 3. 입력 받기 ====
Console.Write("이름을 입력해주세요: ");
string input = Console.ReadLine();            // ReadLine() : 항상 string 반환, null 가능성 주의!!
Console.WriteLine($"입력한 이름: {input}");

// ==== 4. 환경 정보 출력 ====
// Environment: 런타임/OS 정보를 제공하는 정적 클래스!
Console.WriteLine("CLR 버전: " + Environment.Version);
Console.WriteLine("OS: " + Environment.OSVersion);

// cd ~/RiderProjects/C-_Learnig/Stage1_Basics/01_Introduction && dotnet run 으로 실행시 출력값
// 헬로우~ C#!
// 줄바꿈 X이어서 출력합니다~ 
// 안녕하세요. 홍길동님!
// 이름을 입력해주세요: 장영완   --- 입력
// 입력한 이름: 장영완
// CLR 버전: 10.0.8
// OS: Unix 26.5.0

 