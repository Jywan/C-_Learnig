using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;


// [보강] 03-5. LINQ 성능과 게임 루프 내 주의점
// 기존 03_Lambda_LINQ에서 LINQ 사용법만 다뤘기 때문에 보강
// Unity Update()에서 LINQ를 쓰면 왜 GC 스파이크가 생기는지 이해하지 위한 챕터

// ==== LINQ의 숨겨진 비용 ====
// 1. 매 호출마다 IEnumerator 객체 생성 -> 힙 할당 -> GC
// 2. 람다 캡쳐 시 클로저 객체 생성 -> 추가 힙 할당
// 3. ToList(), ToArray() 호출 시 새 컬렉션 할당
// 4. 지연 실행(Lazy Evaluation) - 열거할 때마다 재계산

// ==== 게임 루프에서의 원칙 ====
// 매 프레임 실행되는 코드(Update)에서는:
//      - 힙 할당을 최소화해야 GC 스파이크 방지
//      - LINQ 대신 for/foreach + 캐싱된 컬렉션 사용
//      - 결과를 캐싱하고, 값이 변할 때만 재계산


// ==== 데모용 게임 데이터 ====
class Enemy
{
    public string Name { get; set; }
    public int HP { get; set; }
    public float Distance { get; set; }     // 플레이어와의 거리
    public bool IsActive { get; set; }

    public Enemy(string name, int hp, float distance, bool isActive)
    {
        Name = name;
        HP = hp;
        Distance = distance;
        IsActive = isActive;
    }
}

class Program
{
    static void Main(string[] args)
    {
        // ==== 테스트 데이터 생성 ====
        List<Enemy> enemies = new List<Enemy>();
        Random rand = new Random(42);
        for (int i = 0; i < 10000; i++)
        {
            enemies.Add(new Enemy(
                $"적{i}",
                rand.Next(1, 200),
                (float)(rand.NextDouble() * 100),
                rand.Next(0, 10) > 1        // 90% 확률로 활성
                ));
        }
        
        // ==== 1. 지연 실행 (Lazy Evaluation) 이해 ====
        Console.WriteLine("==== 1. 지연 실행 ====");
        
        // LINQ 쿼리는 정의 시점에 실행되지 않음
        IEnumerable<Enemy> query = enemies.Where(e => e.IsActive && e.HP > 100);
        // 여기까지는 아무 계산도 안함.
        
        Console.WriteLine("쿼리 정의 완료 (아직 실행 안 됨)");
        
        // 열거 시 실행됨
        // JPA Lazy 옵션을 생각하면 편한것같다.
        int count = 0;
        foreach (Enemy e in query)      // 이 시점에 Where 조건 평가 시작
        {
            count++;
            if (count >= 3) break;      // 3개만 확인
        }
        Console.WriteLine($"처음 3개만 확인: {count}개");
        
        // 주의: 매번 열거할 때마다 재계산
        int firstcount = query.Count();     // 전체 순회 1회
        int secondcount = query.Count();    // 전체 순회 또 1회
        Console.WriteLine($"Count 두번 호출: {firstcount}, {secondcount}");
        Console.WriteLine("-> 캐싱 안하면 같은 계산을 반복함\n");
        
        
        // ==== 2. LINQ vs for문 성능 비교 ====
        Console.WriteLine("==== 2. 성능 비교 ====");
        
        Stopwatch sw = new Stopwatch();
        int iterations = 1000;
        
        // ---- LINQ 방식 ----
        sw.Start();
        for (int i = 0; i < iterations; i++)
        {
            // 매 반복마다: Where 열거자 + 람다 + ToList (새 리스트) 생성
            List<Enemy> result = enemies
                .Where(e => e.IsActive && e.Distance < 30)
                .OrderBy(e => e.Distance)
                .ToList();
        }
        sw.Stop();
        long linqTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"LINQ {iterations}회: {linqTime}ms");
        
        // ---- for문 방식 (재사용 리스트) ----
        List<Enemy> reusableList = new List<Enemy>(enemies.Count);
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            reusableList.Clear();   // 새로 할당하지 않고 재사용!
            for (int j = 0; j < enemies.Count; j++)
            {
                Enemy e = enemies[j];
                if (e.IsActive && e.Distance < 30)
                    reusableList.Add(e);
            }
            // 간단한 삽입 정렬 대신 Sort 사용 (이것도 할당 최소)
            reusableList.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }
        sw.Stop();
        long forTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"for문 {iterations}회: {forTime}ms");
        Console.WriteLine($"차이: LINQ가 약 {(double)linqTime / Math.Max(forTime, 1):F1}배 느림\n");
        
        
        // ==== 3. 클로저(Closure)의 힙 할당 ====
        Console.WriteLine("==== 3. 클로저 할당 문제 ====");
        
        // 외부 변수를 캡쳐하면 컴파일러가 클로저 클래스를 생성 -> 힙 할당
        float threshold = 50f;
        
        // 나쁜 예: 매 프레임 호출하면 매번 클로저 객체 생성
        Func<Enemy, bool> filter = e => e.Distance < threshold; // threshold 캡쳐!
        Console.WriteLine($"클로저 필터 결과: {enemies.Count(filter)}개");
        Console.WriteLine("-> 이 람다는 threshold를 캡쳐하므로 클로저 객체가 힙에 생성됨");
        Console.WriteLine("-> 매 프레임 새 람다를 만들면 GC 대상이 계속 쌓임\n");
        
        
        // ==== 4. 게임 루프 안전 패턴들 ====
        Console.WriteLine("==== 4. 게임 루프 안전 패턴 ====");

        // 패턴 1: 결과 캐싱 - 데이터가 변할 때만 재계산
        Console.WriteLine("---- 패턴 1: 결과 캐싱 ----");

        List<Enemy> cachedNearby = new List<Enemy>();
        bool isDirty = true;        // 적 목록이 변경되면 true로 설정
        
        // Update() 시뮬레이션
        for (int frame = 0; frame < 5; frame++)
        {
            if (isDirty)
            {
                cachedNearby.Clear();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i].IsActive && enemies[i].Distance < 20)
                        cachedNearby.Add(enemies[i]);
                }
                isDirty = false;
                Console.WriteLine($" 프레임 {frame}: 재계산 (근처 적: {cachedNearby.Count})");
            }
            else
            {
                Console.WriteLine($" 프레임 {frame}: 캐시 사용 (할당 없음)");
            }
        }
        
        // 패턴 2: 미리 할당된 배열/리스트 재사용
        Console.WriteLine("\n---- 패턴 2: 리스트 재사용 ----");
        
        List<Enemy> targetBuffer = new List<Enemy>(64);  // 미리 용량 확보
        
        // 매 프레임 Clear -> 재사용 (할당없음)
        targetBuffer.Clear();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].IsActive && enemies[i].HP < 50)
            {
                targetBuffer.Add(enemies[i]);
                if (targetBuffer.Count >= 10) break;    // 최대 10개만
            }
        }
        Console.WriteLine($"  약한 적 타겟: {targetBuffer.Count}개 (힙 할당 없음)");

        // 패턴 3: 인덱스 기반 접근 (foreach도 열거자 생성함)
        Console.WriteLine("\n---- 패턴 3: for vs foreach ----");
        
        // foreach - IEnumerator 객체 생성 (List<T>는 struct 열거자라 OK이긴 함)
        int foreachCount = 0;
        foreach (Enemy e in enemies)
        {
            if (e.HP > 150) foreachCount++;
        }

        // for — 어떤 할당도 없음 (가장 안전)
        int forCount = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].HP > 150) forCount++;
        }
        Console.WriteLine($"  foreach 결과: {foreachCount}, for 결과: {forCount}");
        Console.WriteLine("  → List<T>의 foreach는 struct 열거자라 사실 괜찮음");
        Console.WriteLine("  → 하지만 인터페이스(IEnumerable)로 받으면 boxing 발생!\n");

        // ==== 5. LINQ를 써도 되는 곳 ====
        Console.WriteLine("==== 5. LINQ 사용 OK인 상황 ====");

        // 초기화 시 (게임 시작, 씬 로드)
        List<Enemy> sortedByHp = enemies
            .OrderByDescending(e => e.HP)
            .Take(5)
            .ToList();
        Console.WriteLine("게임 시작 시 HP 상위 5:");
        foreach (Enemy e in sortedByHp)
            Console.WriteLine($"  {e.Name}: HP {e.HP}");

        // 에디터 도구, 디버그용
        int activeCount = enemies.Count(e => e.IsActive);
        Console.WriteLine($"\n디버그: 활성 적 수 = {activeCount}");

        // 빈도가 낮은 이벤트 (레벨업, 아이템 획득 등)
        Enemy? nearest = enemies
            .Where(e => e.IsActive)
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
        Console.WriteLine($"가장 가까운 적: {nearest?.Name} (거리: {nearest?.Distance:F1})");

        // ==== 6. 정리 표 ====
        Console.WriteLine("\n==== 6. 사용 가이드 정리 ====");
        Console.WriteLine("| 상황                | LINQ | for문 |");
        Console.WriteLine("|---------------------|------|-------|");
        Console.WriteLine("| 매 프레임 (Update)  |  ✗   |  ✓   |");
        Console.WriteLine("| 초기화/로딩         |  ✓   |  ✓   |");
        Console.WriteLine("| 에디터/디버그       |  ✓   |  ✓   |");
        Console.WriteLine("| 이벤트 핸들러(가끔) |  ✓   |  ✓   |");
        Console.WriteLine("| 서버 로직(비실시간) |  ✓   |  ✓   |");
        Console.WriteLine("");
        Console.WriteLine("핵심 규칙:");
        Console.WriteLine("  1. 매 프레임에서는 힙 할당 금지");
        Console.WriteLine("  2. 결과를 캐싱하고 dirty 플래그로 갱신");
        Console.WriteLine("  3. 리스트는 Clear()로 재사용, new 하지 않기");
        Console.WriteLine("  4. LINQ는 빈도 낮은 코드에서만 (가독성 이점 활용)");
    }
}

// 실행 결과

// ==== 1. 지연 실행 ====
// 쿼리 정의 완료 (아직 실행 안 됨)
// 처음 3개만 확인: 3개
// Count 두번 호출: 4035, 4035
// -> 캐싱 안하면 같은 계산을 반복함
//
// ==== 2. 성능 비교 ====
// LINQ 1000회: 271ms
// for문 1000회: 301ms
// 차이: LINQ가 약 0.9배 느림
//
// ==== 3. 클로저 할당 문제 ====
// 클로저 필터 결과: 4966개
// -> 이 람다는 threshold를 캡쳐하므로 클로저 객체가 힙에 생성됨
// -> 매 프레임 새 람다를 만들면 GC 대상이 계속 쌓임
//
// ==== 4. 게임 루프 안전 패턴 ====
// ---- 패턴 1: 결과 캐싱 ----
//  프레임 0: 재계산 (근처 적: 1601)
//  프레임 1: 캐시 사용 (할당 없음)
//  프레임 2: 캐시 사용 (할당 없음)
//  프레임 3: 캐시 사용 (할당 없음)
//  프레임 4: 캐시 사용 (할당 없음)
//
// ---- 패턴 2: 리스트 재사용 ----
//   약한 적 타겟: 10개 (힙 할당 없음)
//
// ---- 패턴 3: for vs foreach ----
//   foreach 결과: 2487, for 결과: 2487
//   → List<T>의 foreach는 struct 열거자라 사실 괜찮음
//   → 하지만 인터페이스(IEnumerable)로 받으면 boxing 발생!
//
// ==== 5. LINQ 사용 OK인 상황 ====
// 게임 시작 시 HP 상위 5:
//   적82: HP 199
//   적418: HP 199
//   적537: HP 199
//   적594: HP 199
//   적651: HP 199
//
// 디버그: 활성 적 수 = 7996
// 가장 가까운 적: 적1663 (거리: 0.0)
//
// ==== 6. 사용 가이드 정리 ====
// | 상황                | LINQ | for문 |
// |---------------------|------|-------|
// | 매 프레임 (Update)  |  ✗   |  ✓   |
// | 초기화/로딩         |  ✓   |  ✓   |
// | 에디터/디버그       |  ✓   |  ✓   |
// | 이벤트 핸들러(가끔) |  ✓   |  ✓   |
// | 서버 로직(비실시간) |  ✓   |  ✓   |
//
// 핵심 규칙:
//   1. 매 프레임에서는 힙 할당 금지
//   2. 결과를 캐싱하고 dirty 플래그로 갱신
//   3. 리스트는 Clear()로 재사용, new 하지 않기
//   4. LINQ는 빈도 낮은 코드에서만 (가독성 이점 활용)