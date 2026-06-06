class Person
{
    // ==== 1. 필드 & 프로퍼티 ====
    private string _name;                   // 필드 - 외부에서 직접 접근 불가 (캡슐화)
    public int Age { get; set; }            // 자동 프로퍼티 - get/set 컴파일러가 생성
    public string Name                      // 수동 프로퍼티 - 로직 추가 가능
    {
        get { return _name; }
        set { _name = value; }              // value = set 에 전달된 값
    }
    
    // ==== 2. 생성자 ====
    public Person(string name, int age)
    {
        _name = name;
        Age = age;
    }
    
    // ==== 3. 메서드 ====
    public void Intoduce()
    {
        Console.WriteLine($"이름: {_name}, 나이: {Age}");
    }

    public string GetInf()
    {
        return $"{_name} ({Age})세";
    }
}

class Program
{
    static void Main(string[] args)
    {
        // ==== 4. 객체 생성 ====
        Person p1 = new Person("장영완", 32);
        Person p2 = new Person("테스트", 27);
        
        p1.Intoduce();
        p2.Intoduce();
        
        // ==== 5. 프로퍼티 접근 ====
        p1.Age = 33;                        // set 호출
        Console.WriteLine(p1.Age);          // get 호출 -> 33
        Console.WriteLine(p1.GetInf());
        
        // ==== 6. 객체 초기화 문법 ====
        Person p3 = new Person("김철수", 20)
        {
            Age = 22                        // 생성자 호출 후 프로퍼티 추가 발생
        };
        p3.Intoduce();
        
        // ==== 7. null ====
        Person p4 = null;                   // 참조형은 null이 가능하다!
        Console.WriteLine(p4 == null);      // True
        // p4.Intoduce();                   // NullReferenceException 발생!
    }
}

// 실행!
// 이름: 장영완, 나이: 32
// 이름: 테스트, 나이: 27
// 33
// 장영완 (33)세
// 이름: 김철수, 나이: 22
// True