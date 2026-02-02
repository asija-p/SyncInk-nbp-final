public class Stroke
{
    public string Id {get; set;}
    public string UserId {get; set;}
    public Position[] Points { get; set; }
    public string Color { get; set; }
    public double Size { get; set; }
    public DateTime StrokeDate { get; set; }
     public bool Visible { get; set; } = true; 
}

public class Position
{
    public double X { get; set; }
    public double Y { get; set; }
}


