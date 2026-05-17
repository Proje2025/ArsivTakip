using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArsivTakip.Models;

public class Klasor
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string KlasorAdi { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public int? UstKlasorId { get; set; }

    [ForeignKey("UstKlasorId")]
    public virtual Klasor? UstKlasor { get; set; }

    [MaxLength(50)]
    public string? Tarih { get; set; }

    [NotMapped]
    public string GorunumAdi => string.IsNullOrWhiteSpace(Tarih) ? KlasorAdi : $"{KlasorAdi}({Tarih})";

    public virtual ICollection<Klasor> AltKlasorler { get; set; } = new List<Klasor>();

    public virtual ICollection<Evrak> Evraklar { get; set; } = new List<Evrak>();
}