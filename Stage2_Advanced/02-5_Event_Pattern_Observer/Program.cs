using System;
using System.Collections.Generic;

// [보강] 02-5. 이벤트 패턴 심화, 옵저버 패턴
// 기존 02_Delegates_Events에서 event 기본 사용만 다웠기 때문에 보강
// Unity의 UnityEvent, 게임 시스템 간 느슨한 결합을 이해하기 위한 챕터

// ==== 옵저버 패턴이란? ====
// "구독자(Observer)가 발행자(Subject)를 관찰하다가, 상태 변경 시 알림 받는 패턴"
// C#의 event사 이 패턴의 언어 헤벨 구현
// 게임에서: 플레이어 사망 -> UI 갱신, 사운드 재생, 업적 체크 등 여서 시스템이 반은

// ==== event vs delegate 필드 - 왜 event를 쓰는가? ====
// delegate 필드: 외부에서 = 로 덮어쓰기 가능, Invoke 호출 가능 (위험)
// event: 외부에서 += / -= 만 가능, 소유 클래스만 Invoke 가능 (안전)


// ==== 1. EventArgs 패턴 - .NET 표준 이벤트 패턴 ====
// 이벤트에 데이터를 함께 전달할 때 사용하는 표준 방식
// 형태: event EventHandler<TEventArgs> 이벤트명;
// 핸들러 시그니쳐: void Handler(object sender, TEventArgs e)

// 커스텀 이벤트 데이터 클래스
class DamageEventArgs : EventArgs
{
    public int Damage { get; }
    public string Source { get; }
    public bool IsCritical { get; }

    public DamageEventArgs(int damage, string source, bool isCritical)
    {
        Damage = damage;
        Source = source;
        IsCritical = isCritical;
    }
}

class DeathEventArgs : EventArgs
{
    public string Killer { get; }
    public DeathEventArgs(string killer)
    {
        Killer = killer;
    }
}

// ==== 2. 이벤트를 발행하는 클래스 (Subject/Publisher) ====
class Player
{
    public string Name { get; }
    public int HP { get; private set; }
    public int MaxHP { get; }
    
    // 표준 이벤트 패턴: EventHandler<T>
    public event EventHandler<DamageEventArgs> OnDamaged;
    public event EventHandler<DeathEventArgs> OnDeath;
    public event EventHandler? OnHealed;        // 데이터 없으면 기본 EventHandler

    public Player(string name, int hp)
    {
        Name = name;
        HP = hp;
        MaxHP = hp;
    }

    public void TakeDamage(int amount, string source, bool isCritical = false)
    {
        int actualDamage = isCritical ? amount * 2 : amount;
        HP -= actualDamage;
        
        // 이벤트 발행 - 구독자 전체에게 알림
        OnDamaged?.Invoke(this, new DamageEventArgs(amount, source, isCritical));

        if (HP <= 0)
        {
            HP = 0;
            OnDeath?.Invoke(this, new DeathEventArgs(source));
        }
    }

    public void Heal(int amount)
    {
        HP = Math.Min(HP + amount, MaxHP);
        OnHealed?.Invoke(this, EventArgs.Empty);
    }
}


// ==== 3. 구독자 클래스들 (Observer/Subscriber) ====
// 각 시스템이 독립적으로 이벤트에 반응 - 느슨한 결합

class UISystem
{
    public void OnPlayerDamaged(object? sender, DamageEventArgs e)
    {
        string crit = e.IsCritical ? " *크리티컬*" : "";
        Console.WriteLine($"[UI] {e.Damage} 데미지!{crit} (from: {e.Source})");
    }

    public void OnPlayerDeath(object? sender, DeathEventArgs e)
    {
        Console.WriteLine($"[UI] ==== GAME OVER ==== (killed by: {e.Killer})");
    }

    public void OnPlayerHealed(object? sender, EventArgs e)
    {
        Player? p = sender as Player;
        Console.WriteLine($"[UI] HP 회복! 현재: {p?.HP}");
    }
}


class SoundSystem
{
    public void OnPlayerDamaged(object? sender, DamageEventArgs e)
    {
        if (e.IsCritical)
            Console.WriteLine("[SOUND] 크리티컬 효과음 출력!");
        else
            Console.WriteLine("[SOUND] 피격 효과음 출력!");
    }

    public void OnPlayerDeath(object? sender, DeathEventArgs e)
    {
        Console.WriteLine("[SOUND] 사망 음원 재생...");
    }
}

class AchievementSystem
{
    private int _damageCount = 0;
    public void OnPlayerDamaged(object? sender, DamageEventArgs e)
    {
        _damageCount++;
        if (_damageCount == 3)
            Console.WriteLine("[업적] '세번 맞기' 업적달성!");
    }
    
    public void OnPlayerDeath(object? sender, DeathEventArgs e)
    {
        Console.WriteLine("[업적] '첫 죽음' 달성!");
    }
}


// ==== 4. 옵저버 패턴 - 인터페이스 기반 직접 구현 ====
// event 없이 순수 OOP로 구현하는 전통적인 방식
// Unity에선 event가 편하지만, 서버에서는 이 방식도 자주 사용됨

interface IGameEventListener
{
    void OnEvent(string eventName, object? data);
}

class GameEventBus
{
    // 이벤트 이름별로 구독자 목록 관리
    private Dictionary<string, List<IGameEventListener>> _listeners 
        = new Dictionary<string, List<IGameEventListener>>();
    
    // 구독
    public void Subscribe(string eventName, IGameEventListener listener)
    {
        if (!_listeners.ContainsKey(eventName))
            _listeners[eventName] = new List<IGameEventListener>();
        
        _listeners[eventName].Add(listener);
        Console.WriteLine($"[EventBus] {eventName} 구독 등록!");
    }
    
    // 구독 해제
    public void Unsubscribe(string eventName, IGameEventListener listener)
    {
        if (_listeners.ContainsKey(eventName))
            _listeners[eventName].Remove(listener);
    }
    
    // 이벤트 발행 - 해당 이벤트의 모든 구독자에게 알림
    public void Publish(string eventName, object? data = null)
    {
        Console.WriteLine($"\n[EventBus] >>> '{eventName}' 발행!");
        if (!_listeners.ContainsKey(eventName)) return;
        
        // 순회 중 구독 해제 대비 - 복사본으로 순회
        List<IGameEventListener> copy = new List<IGameEventListener>(_listeners[eventName]);
        foreach (IGameEventListener listener in copy)
        {
            listener.OnEvent(eventName, data);
        }
    }
}


// EventBus 구독자들
class ScoreManager : IGameEventListener
{
    public int Score { get; private set; } = 0;

    public void OnEvent(string eventName, object? data)
    {
        if (eventName == "enemy_killed")
        {
            int point = data is int p ? p : 10;
            Score += point;
            Console.WriteLine($"[Score] +{point}점! 총: {Score}");
        }
    }
}


class ComboSystem : IGameEventListener
{
    private int _combo = 0;

    public void OnEvent(string eventName, object? data)
    {
        if (eventName == "enemy_killed")
        {
            _combo++;
            Console.WriteLine($"[Combo] {_combo} COMBO!");
        }
        else if (eventName == "plyer_hit")
        {
            if (_combo > 0) Console.WriteLine($"[Combo] 콤보 끊김! ({_combo}) -> 0");
            _combo = 0;
        }
    }
}

class SpawnManager : IGameEventListener
{
    public void OnEvent(string eventName, object? data)
    {
        if (eventName == "enemy_killed")
        {
            Console.WriteLine($"[Spawn] 새 적 스폰!");
        }
    }
}


// ==== 5. 메모리 누수 주의!! : 구독해제의 중요성! ====
class TemporaryEffect
{
    private Player _player;

    public TemporaryEffect(Player player)
    {
        _player = player;
        _player.OnDamaged += HandleDamage; // 구독
        Console.WriteLine("[Effect] 보호막 활성화 (구독)");
    }

    private void HandleDamage(object? sender, DamageEventArgs e)
    {
        Console.WriteLine($"[Effect] 보호막이 {e.Damage} 데미지 흡수!");
    }
    
    // 반드시 해제해야 함! 안하면 GC(Garbage Collection)가 이 객체를 수거 못함 (메모리 누수)
    public void Dispose()
    {
        _player.OnDamaged -= HandleDamage;      // 구독 해제
        Console.WriteLine("[Effect] 보호막 완료 (구독 해제)");
    }
}


class Program
{
    static void Main(string[] args)
    {
        // ==== EventHandler 패턴 ====
        Console.WriteLine("==== 1. EventHandler 패턴 ====");

        Player player = new Player("영웅", 100);
        UISystem ui = new UISystem();
        SoundSystem sound = new SoundSystem();
        AchievementSystem achievement = new AchievementSystem();
        
        // 구독 등록 - 각 시스템이 독립적으로 플레이어 이벤트에 반응
        player.OnDamaged += ui.OnPlayerDamaged;
        player.OnDamaged += sound.OnPlayerDamaged;
        player.OnDamaged += achievement.OnPlayerDamaged;
        player.OnDeath += ui.OnPlayerDeath;
        player.OnDeath += sound.OnPlayerDeath;
        player.OnDeath += achievement.OnPlayerDeath;
        player.OnHealed += ui.OnPlayerHealed;
        
        player.TakeDamage(20, "고블린");
        Console.WriteLine($"HP: {player.HP}\n");
        
        player.Heal(10);
        Console.WriteLine();
        
        player.TakeDamage(30, "오크", isCritical: true);
        Console.WriteLine($"HP: {player.HP}\n");

        player.TakeDamage(15, "슬라임");  // 3번째 피격 → 업적!
        Console.WriteLine($"HP: {player.HP}\n");
        
        // ==== 구독 해제 ====
        Console.WriteLine("==== 2. 구독 해제 ====");
        
        // 사운드 시스템 끄기
        player.OnDamaged -= sound.OnPlayerDamaged;
        player.TakeDamage(10, "박쥐");        // 사운드 없이 UI+업적만 반응
        Console.WriteLine($"HP: {player.HP}\n");
        
        // ==== 메모리 누수 데모 ====
        Console.WriteLine("==== 3. 메모리 누수 방지 ====");
        
        TemporaryEffect shield = new TemporaryEffect(player);
        player.TakeDamage(5, "독");  // 보호막 반응
        shield.Dispose();                           // 반드시 해제!
        player.TakeDamage(5, "독");  // 보호막 반응 
        Console.WriteLine();
        
        // ==== EventBus 패턴 (옵저버 패턴 직접 구현) ====
        Console.WriteLine("==== 4. EventBus (옵저버 패턴) ====");
        
        GameEventBus eventBus = new GameEventBus();
        ScoreManager scoreManager = new ScoreManager();
        ComboSystem comboSystem = new ComboSystem();
        SpawnManager spawnManager = new SpawnManager();
        
        // 구독
        eventBus.Subscribe("enemy_killed", scoreManager);
        eventBus.Subscribe("enemy_killed", comboSystem);
        eventBus.Subscribe("enemy_killed", spawnManager);
        eventBus.Subscribe("player_hit", comboSystem);

        // 이벤트 발행
        eventBus.Publish("enemy_killed", 50);   // 50점짜리 적 처치
        eventBus.Publish("enemy_killed", 30);   // 30점짜리
        eventBus.Publish("enemy_killed", 100);  // 100점짜리 (3콤보!)
        eventBus.Publish("player_hit");                 // 콤보 끊김
        eventBus.Publish("enemy_killed", 20);   // 다시 시작
        
        Console.WriteLine($"\n최종 점수: {scoreManager.Score}");

        // ==== 구독 해제 후 ====
        Console.WriteLine("\n==== 5. EventBus 구독 해제 ====");
        eventBus.Unsubscribe("enemy_killed", spawnManager);
        eventBus.Publish("enemy_killed", 10);   // SpawnManager는 반응 안 함

        // ==== 정리 ====
        Console.WriteLine("\n==== 6. event vs EventBus 비교 ====");
        Console.WriteLine("C# event (EventHandler<T>):");
        Console.WriteLine("  - 언어 내장, 타입 안전");
        Console.WriteLine("  - 발행자를 직접 참조해야 구독 가능");
        Console.WriteLine("  - Unity에서 가장 많이 사용");
        Console.WriteLine("");
        Console.WriteLine("EventBus (중앙 집중형):");
        Console.WriteLine("  - 발행자/구독자가 서로를 모름 (완전 느슨한 결합)");
        Console.WriteLine("  - 문자열 기반이라 타입 안전성 낮음");
        Console.WriteLine("  - 대규모 시스템, 서버에서 유용");
        Console.WriteLine("");
        Console.WriteLine("공통 주의점:");
        Console.WriteLine("  - 구독 해제 안 하면 메모리 누수!");
        Console.WriteLine("  - 순환 참조 주의");
        Console.WriteLine("  - 이벤트 핸들러에서 예외 발생 시 전파 문제");
    }
}

// 실행 결과
// ==== 1. EventHandler 패턴 ====
// [UI] 20 데미지! (from: 고블린)
// [SOUND] 피격 효과음 출력!
// HP: 80
//
// [UI] HP 회복! 현재: 90
//
// [UI] 30 데미지! *크리티컬* (from: 오크)
// [SOUND] 크리티컬 효과음 출력!
// HP: 30
//
// [UI] 15 데미지! (from: 슬라임)
// [SOUND] 피격 효과음 출력!
// [업적] '세번 맞기' 업적달성!
// HP: 15
//
// ==== 2. 구독 해제 ====
// [UI] 10 데미지! (from: 박쥐)
// HP: 5
//
// ==== 3. 메모리 누수 방지 ====
// [Effect] 보호막 활성화 (구독)
// [UI] 5 데미지! (from: 독)
// [Effect] 보호막이 5 데미지 흡수!
// [UI] ==== GAME OVER ==== (killed by: 독)
// [SOUND] 사망 음원 재생...
// [업적] '첫 죽음' 달성!
// [Effect] 보호막 완료 (구독 해제)
// [UI] 5 데미지! (from: 독)
// [UI] ==== GAME OVER ==== (killed by: 독)
// [SOUND] 사망 음원 재생...
// [업적] '첫 죽음' 달성!
//
// ==== 4. EventBus (옵저버 패턴) ====
// [EventBus] enemy_killed 구독 등록!
// [EventBus] enemy_killed 구독 등록!
// [EventBus] enemy_killed 구독 등록!
// [EventBus] player_hit 구독 등록!
//
// [EventBus] >>> 'enemy_killed' 발행!
// [Score] +50점! 총: 50
// [Combo] 1 COMBO!
// [Spawn] 새 적 스폰!
//
// [EventBus] >>> 'enemy_killed' 발행!
// [Score] +30점! 총: 80
// [Combo] 2 COMBO!
// [Spawn] 새 적 스폰!
//
// [EventBus] >>> 'enemy_killed' 발행!
// [Score] +100점! 총: 180
// [Combo] 3 COMBO!
// [Spawn] 새 적 스폰!
//
// [EventBus] >>> 'player_hit' 발행!
//
// [EventBus] >>> 'enemy_killed' 발행!
// [Score] +20점! 총: 200
// [Combo] 4 COMBO!
// [Spawn] 새 적 스폰!
//
// 최종 점수: 200
//
// ==== 5. EventBus 구독 해제 ====
//
// [EventBus] >>> 'enemy_killed' 발행!
// [Score] +10점! 총: 210
// [Combo] 5 COMBO!
//
// ==== 6. event vs EventBus 비교 ====
// C# event (EventHandler<T>):
//   - 언어 내장, 타입 안전
//   - 발행자를 직접 참조해야 구독 가능
//   - Unity에서 가장 많이 사용
//
// EventBus (중앙 집중형):
//   - 발행자/구독자가 서로를 모름 (완전 느슨한 결합)
//   - 문자열 기반이라 타입 안전성 낮음
//   - 대규모 시스템, 서버에서 유용
//
// 공통 주의점:
//   - 구독 해제 안 하면 메모리 누수!
//   - 순환 참조 주의
//   - 이벤트 핸들러에서 예외 발생 시 전파 문제