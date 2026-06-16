using System;
using System.Collections.Generic;

// [보강] 04-5. Collection 심화 (Queue, Stack, HashSet, LinkedList)
// 기존 04_Array_Collections에서 List, Dictionary만 다뤘기 때문에 보강
// 게임에서 버퍼(Queue), Undo(Stack), 중복제거(HashSet)에 자주 사용됨.

// ==== 컬렉션 선택 가이드 ====
// List<T>          - 순서 있음, 인덱스 접급, 범용적
// Dictionary       - 키-값 쌍, 빠른조회 0(1)
// Queue<T>         - FIFO (먼저 넣은게 먼저 나옴)
// Stack<T>         - LIFO (나중에 넣은게 먼저 나옴)
// HashSet<T>       - 중복 불가, 존재 여부 확인 0(1)
// LinkedList<T>    - 중간 삽입/삭제 0(1), 인덱스 접근 불가

class Program
{
    static void Main(string[] args)
    {
        // ==== 1. Queue<T> - FIFO (First In, First Out) ====
        Console.WriteLine("==== 1. Queue (FIFO) ====");
        
        // 게임 활용: 입력 버퍼, 메시지 큐, 스폰 대기열, 턴제 행동 큐
        Queue<string> commandQueue = new Queue<string>();
        
        // Enqueue: 뒤에 추가
        commandQueue.Enqueue("이동");
        commandQueue.Enqueue("공격");
        commandQueue.Enqueue("스킬");
        
        Console.WriteLine($"대기 중인 명령 수: {commandQueue.Count}");     // 3건 확인
        
        // Peek: 꺼내지 않고 맨 앞 확인
        Console.WriteLine($"다음 명령 (Peek): {commandQueue.Peek()}");    // 이동
        
        // Dequeue: 맨 앞에서 꺼냄
        string next = commandQueue.Dequeue();
        Console.WriteLine($"실행:  {next}");
        Console.WriteLine($"남은 명령 수: {commandQueue.Count}");        // 2건!
        
        // 전부 처리
        while (commandQueue.Count > 0)
        {
            Console.WriteLine($"실행: {commandQueue.Dequeue()}");
        }
        
        // ==== 2. Stack<T> - LIFO (Last IN, First Out)
        Console.WriteLine("\n==== 2. Stack (LIFO) ====");
        
        // 게임 활용: Undo 시스템, UI 네비게이션 히스토리, 재귀 대체
        Stack<string> undoStack = new Stack<string>();
        
        // Push: 위에 쌓기
        undoStack.Push("캐릭터 이동");
        undoStack.Push("아이템 사용");
        undoStack.Push("스킬 시전");
        
        Console.WriteLine($"히스토리 수: {undoStack.Count}");        // 3건 존재!
        
        // Peek: 꺼내지 않고 맨 위 확인
        Console.WriteLine($"마지막 행동 (Peek): : {undoStack.Peek()}"); // 스킬 시전
        
        // Pop: 맨 위에서 꺼냄 (가장 최근것부터)!
        string undone = undoStack.Pop();
        Console.WriteLine($"Undo: {undone}");   // 스킬 시전
        
        Console.WriteLine($"남은 이력 건수: {undoStack.Count}");      // 2
        
        // UI 뒤로가기 패턴
        Stack<string> screenHistory = new Stack<string>();
        screenHistory.Push("메인메뉴");
        screenHistory.Push("설정");
        screenHistory.Push("그래픽 설정");
        
        Console.WriteLine($"\n현재 화면: {screenHistory.Peek()}");  // 그래픽 설정
        screenHistory.Pop();        // 뒤로가기
        Console.WriteLine($"뒤로가기 후: {screenHistory.Peek()}");   // 설정
        
        // ==== 3. HashSet<T> - 중복 불가 집합 ====
        Console.WriteLine("\n==== 3. HashSet (중복불가) ====");
        
        // 게임 활용: 획득한 업적, 방문한 앱, 보유 아이템 ID, 접속 중인 유저 목록
        HashSet<string> achievements = new HashSet<string>();
        
        // Add: 추가 (중복이면 false 반환한다!, 예외없음.)
        Console.WriteLine($"업적 추가: {achievements.Add("첫 처치")}");         // True
        Console.WriteLine($"업적 추가: {achievements.Add("보스 클리어")}");      // True
        Console.WriteLine($"중복 추가: {achievements.Add("첫 처치")}");         // False
        
        Console.WriteLine($"업적 수: {achievements.Count}");   // 2
        
        // Contains: 0(1) 조회 - List의 Contains는 O(n)이라 대량 데이터에서는 HashSet이 압도적!
        Console.WriteLine($"첫 처지 달성? {achievements.Contains("첫 처치")}");     // True
        Console.WriteLine($"숨겨진 업적? {achievements.Contains("숨겨진 업적")}");   // False
        
        // 집합 연산 - 수학의 집합과 동일
        HashSet<int> setA = new HashSet<int> { 1, 2, 3, 4, 5 };
        HashSet<int> setB = new HashSet<int> { 3, 4, 5, 6, 7 };
        
        // 복사본을 만들어 원본 보존
        HashSet<int> intersection = new HashSet<int>(setA);
        intersection.IntersectWith(setB);       // 교집합!
        Console.WriteLine($"교집합: {string.Join(", ", intersection)}");   // 3, 4, 5
        
        HashSet<int> union = new HashSet<int>(setA);
        union.UnionWith(setB);              // 합집합!
        Console.WriteLine($"합집합: {string.Join(", ", union)}");      // 1, 2, 3, 4, 5, 6, 7
        
        HashSet<int> diff = new HashSet<int>(setA);
        diff.ExceptWith(setB);              // 차집합!
        Console.WriteLine($"차집합(A-B): {string.Join(", ", diff)}");  // 1, 2
        
        // ==== 4. LinkedList<T> - 연결 리스트 ====
        Console.WriteLine("\n==== 4. LinkedList ====");
        
        // 게임 활용: 버프/디버프 목록 (중간 삽입/삭제 빈번), 타임라인 이벤트
        // List는 중간 삽입 시 뒤의 모든 요소를 밀어야 함 O(n)
        // LinkedList는 노드 연결만 바꾸면 됨. O(1)
        LinkedList<string> buffs = new LinkedList<string>();

        buffs.AddLast("공격력 증가");
        buffs.AddLast("이속 증가");
        buffs.AddFirst("무적");               // 맨 앞에 추가
        
        Console.WriteLine("현재 버프:");
        foreach (string buff in buffs)
        {
            Console.WriteLine($" - {buff}");            
        }
        
        // 노드를 찾아서 그 앞/뒤에 삽입
        LinkedListNode<string>? node = buffs.Find("이속 증가");
        if (node != null)
        {
            buffs.AddBefore(node, "방어력 증가");        // 이속 증가 앞에 삽입
        }
        
        Console.WriteLine("\n방어력 증가 삽입 후:");
        foreach (string buff in buffs)
        {
            Console.WriteLine($" - {buff}");
        }
        
        // 제거 
        buffs.Remove("무적");     // 무적 버프 만료
        Console.WriteLine($"\n무적 제거 후 첫 버프 : {buffs.First.Value}");  // 공격력 버프 출력
        
        // ==== 5. 컬렉션 선택 실전 예제 ====
        Console.WriteLine("\n==== 5. 실전: 어떤 컬렉션을 쓸까? ====");
        
        // 시나리오 1: 플레이어 인벤토리 (순서가 있고, 인덱스 접근이 필요함)
        List<string> inventory = new List<string> { "무기", "방어구", "물약" };
        Console.WriteLine($"2번 슬롯: {inventory[1]}");        // 방어구
        
        // 시나리오 2: 스킬 쿨다운 관리 (키로 빠르게 조회 필요)
        Dictionary<string, float> cooldown = new Dictionary<string, float>
        {
            { "화염구", 3.0f },
            { "치료", 5.0f },
            { "순간이동", 10.0f }
        };
        Console.WriteLine($"치료 쿨타임: {cooldown["치료"]}초");
        
        // 시나리오 3: 처리 대기 중인 네트워크 패킷 (순서대로 처리)
        Queue<string> packetQueue = new Queue<string>();
        packetQueue.Enqueue("로그인 요청");
        packetQueue.Enqueue("위치 동기화");
        Console.WriteLine($"다음 처리할 패킷: {packetQueue.Peek()}");      // 로그인 요청!
        
        // 시나리오 4: 방문한 체크포인트 (중복 방문 무시)
        HashSet<string> visitedCheckpoints = new HashSet<string>();
        visitedCheckpoints.Add("마을");
        visitedCheckpoints.Add("던전입구");
        visitedCheckpoints.Add("마을");
        Console.WriteLine($"방문한 장소 수: {visitedCheckpoints.Count}"); // 2
        
    }
}

// 출력 결과
// ==== 1. Queue (FIFO) ====
// 대기 중인 명령 수: 3
// 다음 명령 (Peek): 이동
// 실행:  이동
// 남은 명령 수: 2
// 실행: 공격
// 실행: 스킬
//
// ==== 2. Stack (LIFO) ====
// 히스토리 수: 3
// 마지막 행동 (Peek): : 스킬 시전
// Undo: 스킬 시전
// 남은 이력 건수: 2
//
// 현재 화면: 그래픽 설정
// 뒤로가기 후: 설정
//
// ==== 3. HashSet (중복불가) ====
// 업적 추가: True
// 업적 추가: True
// 중복 추가: False
// 업적 수: 2
// 첫 처지 달성? True
// 숨겨진 업적? False
// 교집합: 3, 4, 5
// 합집합: 1, 2, 3, 4, 5, 6, 7
// 차집합(A-B): 1, 2
//
// ==== 4. LinkedList ====
// 현재 버프:
//  - 무적
//  - 공격력 증가
//  - 이속 증가
//
// 방어력 증가 삽입 후:
//  - 무적
//  - 공격력 증가
//  - 방어력 증가
//  - 이속 증가
//
// 무적 제거 후 첫 버프 : 공격력 증가
//
// ==== 5. 실전: 어떤 컬렉션을 쓸까? ====
// 2번 슬롯: 방어구
// 치료 쿨타임: 5초
// 다음 처리할 패킷: 로그인 요청
// 방문한 장소 수: 2