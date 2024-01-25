namespace MTCG.Models;

public class CardDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float Damage { get; set; }
    public string Element { get; set; }
    public string Class { get; set; }
    public string Type { get; set; }
}