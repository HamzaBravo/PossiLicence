namespace PossiLicence.Entityies;

public abstract class BaseDbEntity
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Benzersiz id int yerine guid kullanıyorum 
    public DateTime CreateAt { get; set; } = DateTime.Now; // Oluşturulma tarihi
}
