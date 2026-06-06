// ==== 1. 기반 클래스 ====
class Animal
{
    public string Name { get; set; }

    public Animal(string name)
    {
        Name = name;
    }

    public void Eat()
    {
        Console.WriteLine($"{Name}이(가) 먹습니다.");
    }

    public virtual void Speak()                     // virtual : 자식 클래스에서 재정의 가능합니다!
    {
        Console.WriteLine($"{Name}이(가) 소리를 냅니다.");
    }

    public override string ToString()               // Object 클래스의 ToString 재정의
    {
        return $"Animal({Name})";
    }
}

// ==== 2. 상속 ====
class Dog : Animal                                  // Animal을 상속받음!
{
    public string Bread { get; set; }               // 추가 프로퍼티
    public Dog(string name, string bread) : base(name)      // base =  부모 생성자를 호출!
    {
        Bread = bread;      
    }

    public override void Speak()                    // override = 부모의 virtual 메서드 재정의!
    {
        Console.WriteLine($"{Name}이(가) 멍멍!");
    }
}

class Cat : Animal
{
    public Cat(string name) : base(name) { }

    public override void Speak()
    {
        Console.WriteLine($"{Name}이(가) 야옹~");
    }
}

// ==== 3. 다형성 ====
class Program
{
    static void Main(string[] args)
    {
        Dog dog = new Dog("백구", "진돗개");
        Cat cat = new Cat("바리");
        
        dog.Eat();                      // 부모 메서트 그대로 사용!
        dog.Speak();                    // 재정의된 메서드가 호출됨! (멍멍)
        cat.Speak();                    // 재정의된 메서드가 호출됨! (야옹)
        
        Console.WriteLine(dog.Bread);   // 진돗개 (Dog만 가진 프로퍼티)
        Console.WriteLine(dog);         // Animal(백구) -ToString 호출
        
        // 부모타입으로 자식 객체를 담을 수 있다!
        Animal a1 = new Dog("크래커", "말티즈");
        Animal a2 = new Cat("고등어");
        
        a1.Speak();                     // 멍멍 (실제타입 기준으로 호출)
        a2.Speak();                     // 야옹 (실제타입 기준으로 호출)
        
        // ==== 4. 배열로 다형성 활용 ====
        Animal[] animals = { new Dog("두부", "포메"), new Cat("솜이"), new Dog("콩이", "비숑") };
        
        foreach (Animal animal in animals)
            animal.Speak();             // 각 타입에 맞는 Speak 호출
        
        // ==== 5. 타입 확인 ====
        Animal unknwon = new Dog("모카", "골든 리트리버");
        Console.WriteLine(unknwon is Dog);      // True
        Console.WriteLine(unknwon is Cat);      // False
        
        if (unknwon is Dog d)                   // is 패턴 매칭 - 캐스팅과 동시에 확인
            Console.WriteLine(d);               // 골든 리트리버 !
    }
}

// 실행 결과!
// 백구이(가) 먹습니다.
// 백구이(가) 멍멍!
// 바리이(가) 야옹~
// 진돗개
// Animal(백구)
// 크래커이(가) 멍멍!
// 고등어이(가) 야옹~
// 두부이(가) 멍멍!
// 솜이이(가) 야옹~
// 콩이이(가) 멍멍!
// True
// False
// Animal(모카)