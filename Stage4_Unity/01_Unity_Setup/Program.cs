// 이번 유니티 학습 챕터는 유니티 환경설정이라 코드보다는 개념위주로 진행됩니다!

// ==== Unity 프로젝트 구조 ====
// Assets/              - 모든 게임 리소스 (스크립트, 이미지, 사운드 등)
// Assets/Scripts/      - C# 스크립트 위치
// Assets/Scenes/       - 씬 파일
// Library/             - Unity 내부 캐시 (중요! git에 올리지는 않음)
// ProjectSettings/     - 프로젝트 설정

// ==== Unity C# vs 일반 C# ====
// 1. 진입점이 없음!                                  - Main() 대신 MonoBehaviour 라이프사이클을 사용한다!
// 2. Console.WriteLine() 대신 Debug.Log() 사용
// 3. Unity API (GameObject, Transform 등)를 사용하기 위해 using UnityEngine을 선언, 필요하다!

// ==== 더미 코드 (Unity 없이 구조 이해용) ====

// Unity의 MonoBehaviour를 흉내낸 더미 클래스
class MonoBehaviour
{
  public string gameObject = "GameObject";
}

// Unity 스크립트는 항상 MonoBehaviour를 상속
// 학습목적 호출로 인해, Start(), Update(), OnDestroy()에 public 사용
class HelloUnity : MonoBehaviour
{
  // Start: 오브젝트가 활성화될깨 1회 실행 - 초기화에 사용된다!(중요)
  public void Start()
  {
    Console.WriteLine("[Unity] Start() 호출 - 초기화");
    Console.WriteLine($"[Unity] 오브젝트 이름: {gameObject}");
  }
  
  // Update: 매 프레임마다 실행 - 게임 로직에 사용! (중요!)
  public void Update()
  {
    Console.WriteLine("[Unity] Update() 호출 - 매프레임 실행");
  }
  
  // OnDestroy: 오브젝트가 제거될 때 실행 - 리소스 해제에 사용됨! (중요!)
  public void OnDestroy()
  {
    Console.WriteLine("[Unity] OnDestroy() 호출 - 정리");
  }
  
  // 여기에는 다 못적었지만 실제 Unity 라이프 사이클의 순서는..
  // 1. Awake()       - 오브젝트 생성 직후, 컴포넌트 간 참조 초기화
  // 2. Start()       - 첫 프레임 직전 1회, 게임로직 초기화
  // 3. Update()      - 매 프레임, 입력처리 혹은 게임로직
  // 4. FixedUpdate() - 고정 시간 간격, 물리 연산 용도
  // 5. OnDestroy()   - 오브젝트 제거 시, 리소스 해제
}

class Program
{
  static void Main(string[] args)
  {
    Console.WriteLine("==== Unity 라이프사이클 시뮬레이션 ====");
    HelloUnity obj = new HelloUnity();
    
    // Unity 엔진이 내부적으로 이 순서로 호출된다
    obj.Start();
    
    // 3프레임 시뮬레이션
    for (int i = 0; i < 3; i++)
    {
      Console.WriteLine($"--- 프레임 {i + 1} ---");
      obj.Update();
    }
    
    obj.OnDestroy();
    
    Console.WriteLine("\n==== Unity vs 일반 C# ====");
    Console.WriteLine("Debug.Log()     -> Console.WriteLine() 역할");
    Console.WriteLine("MonoBehaviour   -> 모든 Unity 스크립트의 기반 클래스");
    Console.WriteLine("Start()         -> 초기화 (Main 역할)");
    Console.WriteLine("Update()        -> 매 프레임 실행 (게임 루프)");
  }
}

// 실행 결과
// ==== Unity 라이프사이클 시뮬레이션 ====
// [Unity] Start() 호출 - 초기화
// [Unity] 오브젝트 이름: GameObject
// --- 프레임 1 ---
// [Unity] Update() 호출 - 매프레임 실행
// --- 프레임 2 ---
// [Unity] Update() 호출 - 매프레임 실행
// --- 프레임 3 ---
// [Unity] Update() 호출 - 매프레임 실행
// [Unity] OnDestroy() 호출 - 정리

// ==== Unity vs 일반 C# ====
// Debug.Log()     -> Console.WriteLine() 역할
// MonoBehaviour   -> 모든 Unity 스크립트의 기반 클래스
// Start()         -> 초기화 (Main 역할)
// Update()        -> 매 프레임 실행 (게임 루프)