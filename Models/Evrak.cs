using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirtanaArsivTakip.Models;

public class Evrak
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Konu { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sayi { get; set; } = string.Empty;

    public DateTime Tarih { get; set; }

    public string? Aciklama { get; set; }

    public int KlasorId { get; set; }

    [ForeignKey("KlasorId")]
    public virtual Klasor? Klasor { get; set; }

    [MaxLength(200)]
    public string? PdfDosyaAdi { get; set; }

    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

    public DateTime? SilmeTarihi { get; set; }

    public bool Silindi { get; set; } = false;
}