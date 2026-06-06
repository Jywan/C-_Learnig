// ==== 1. 인터 페이스 ====

interface IShape                            // 규격 정의 - 구현은 없음
{
    double GetArea();                       // 반드시 구현해야하는 메서드!
    double GetPerimeter();
    void PrintInfo();
}

// ==== 2. 추상 클래스 ====
abstract class Shape                        // 직접 인스턴스 생성 불가
{
    public string Color { get; set; }

    public Shape(string color)
    {
        Color = color;
    }
    
    public abstract double GetArea();       // abstract = 자식에서 반드시 구현!!

    public void PrintColor()                // 일반 메서드는 그대로 상속됨
    {
        Console.WriteLine($"색상: {Color}");
    }
}

// ==== 3. 인터페이스 + 추상 클래스 구현 ====
class Circle : Shape, IShape                // 추상 클래스 상속 + 인터페이스 구현
{
    public double Radius { get; set; }

    public Circle(string color, double radius) : base(color)
    {
        Radius = radius;
    }

    public override double GetArea()        // Shape의 abstract 구현
    {
        return Math.PI * Radius * Radius;
    }

    public double GetPerimeter()            // IShape 구현
    {
        return 2 * Math.PI * Radius;
    }

    public void PrintInfo()
    {
        Console.WriteLine($"원 - 반지름: {Radius}, 넓이: {GetArea():F2}");
    }
}

class Rectangle : Shape, IShape
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Rectangle(string color, double width, double height) : base(color)
    {
        Width = width;
        Height = height;
    }

    public override double GetArea()
    {
        return Width * Height;
    }

    public double GetPerimeter()
    {
        return 2 * (Width + Height);
    }

    public void PrintInfo()
    {
        Console.WriteLine($"사각형 - 가로: {Width}, 세로: {Height}, 넓이: {GetArea():F2}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        Circle circle = new Circle("빨강", 5.0);
        Rectangle rect = new Rectangle("파랑", 4.0, 6.0);
        
        // ==== 4. 인터페이스 타입으로 관리 ====
        IShape[] shapes = { circle, rect };

        foreach (IShape shape in shapes)
        {
            shape.PrintInfo();                  // 각 타입에 맞는 구현 호출!
            Console.WriteLine($"둘레: {shape.GetPerimeter():F2}");
        }
        
        // ==== 5. 추상 클래스 타입으로 관리 ====
        Shape[] abstractShapes = { circle, rect };

        foreach (Shape shape in abstractShapes)
        {
            shape.PrintColor();                 // Shape의 일반 메서드
            Console.WriteLine($"넓이: {shape.GetArea():F2}");
        }
    }
}

// 실행 결과!
// 원 - 반지름: 5, 넓이: 78.54
// 둘레: 31.42
// 사각형 - 가로: 4, 세로: 6, 넓이: 24.00
// 둘레: 20.00
// 색상: 빨강
// 넓이: 78.54
// 색상: 파랑
// 넓이: 24.00