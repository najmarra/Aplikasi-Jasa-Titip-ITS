using System.Text.Json.Serialization;
 
namespace JastipITS;
 
public enum StatusPesanan
{
    Menunggu,
    Dibeli,
    Diantar,
    Selesai,
    Dibatalkan
}
 
public class Pesanan
{
    public int Id { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public string NamaPemesan { get; set; } = "";
    public string NoWa { get; set; } = "";
    public string NamaMenu { get; set; } = "";
    public string Kantin { get; set; } = "";
    public int Jumlah { get; set; } = 1;
    public decimal HargaSatuan { get; set; }
    public decimal OngkosTitip { get; set; }
    public string Lokasi { get; set; } = "";
    public StatusPesanan Status { get; set; } = StatusPesanan.Menunggu;
    public string Catatan { get; set; } = "";
 
    [JsonIgnore] public decimal Subtotal => Jumlah * HargaSatuan;
    [JsonIgnore] public decimal Total => Subtotal + OngkosTitip;
}
 
public class MenuMakanan
{
    public int Id { get; set; }
    public string Nama { get; set; } = "";
    public string Kantin { get; set; } = "";
    public decimal Harga { get; set; }
}