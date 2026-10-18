using System;
using System.Collections.Generic;
using System.Linq;

public class Kullanici
{
    public string KullaniciAdi { get; }
    public string Sifre { get; private set; }
    public bool IsAdmin { get; }

    public Kullanici(string kullaniciAdi, string sifre, bool isAdmin = false)
    {
        KullaniciAdi = kullaniciAdi;
        Sifre = sifre;
        IsAdmin = isAdmin;
    }

    public void SifreDegistir(string yeniSifre)
    {
        Sifre = yeniSifre;
    }
}

public static class KullaniciYoneticisi
{
    private static readonly Dictionary<string, Kullanici> _kullanicilar = new()
    {
        { "admin", new Kullanici("admin", "1234", true) }
    };

    public static Kullanici? GirisYap(string kullaniciAdi, string sifre)
    {
        if (_kullanicilar.ContainsKey(kullaniciAdi) && _kullanicilar[kullaniciAdi].Sifre == sifre)
        {
            return _kullanicilar[kullaniciAdi];
        }
        return null;
    }

    public static bool Kaydol(string kullaniciAdi, string sifre)
    {
        if (_kullanicilar.ContainsKey(kullaniciAdi)) return false;
        _kullanicilar.Add(kullaniciAdi, new Kullanici(kullaniciAdi, sifre));
        return true;
    }

    public static void SifreDegistir(Kullanici kullanici, string yeniSifre)
    {
        kullanici.SifreDegistir(yeniSifre);
    }
}

public class SepetKalemi
{
    public string Ad { get; set; }
    public double SatisFiyati { get; set; }
    public double AlisFiyati { get; set; }
    public string ParaBirimi { get; set; }
    public double KDVOrani { get; set; }
    public double IndirimOrani { get; set; }
    public int StokAdedi { get; set; }

    public double NetTutarTL(Dictionary<string, double> kurlarTLye)
    {
        double kur = kurlarTLye[ParaBirimi];
        return (SatisFiyati * kur) * (1 - IndirimOrani);
    }

    public double KDVTutariTL(Dictionary<string, double> kurlarTLye)
    {
        double netFiyat = SatisFiyati * kurlarTLye[ParaBirimi];
        double indirimliNetFiyat = netFiyat * (1 - IndirimOrani);
        return indirimliNetFiyat * KDVOrani;
    }

    public double ToplamTutarTL(Dictionary<string, double> kurlarTLye)
    {
        return NetTutarTL(kurlarTLye) + KDVTutariTL(kurlarTLye);
    }
}

public class Cuzdan
{
    private readonly Dictionary<string, double> _bakiye;
    private readonly Dictionary<string, double> _kurlarTLye;

    public Cuzdan(Dictionary<string, double> kurlarTLye)
    {
        _bakiye = new() { { "TL", 500.0 }, { "USD", 10.0 }, { "EUR", 5.0 }, { "GBP", 0.0 } };
        _kurlarTLye = kurlarTLye;
    }

    public double BakiyeGetir(string birim) => _bakiye.ContainsKey(birim) ? _bakiye[birim] : 0.0;
    public void BakiyeEkle(string birim, double miktar) { _bakiye[birim] = BakiyeGetir(birim) + miktar; }
    public bool BakiyeHarca(string birim, double miktar)
    {
        if (BakiyeGetir(birim) >= miktar)
        {
            _bakiye[birim] -= miktar;
            return true;
        }
        return false;
    }

    public string OzetAl()
    {
        return string.Join(" | ", _bakiye.Select(kv => $"{kv.Value:F2} {kv.Key}"));
    }
}

public class Sepet
{
    public List<SepetKalemi> Kalemler { get; } = new();
    private readonly Cuzdan _cuzdan;
    private readonly Dictionary<string, double> _kurlarTLye;

    public Sepet(Cuzdan cuzdan, Dictionary<string, double> kurlarTLye)
    {
        _cuzdan = cuzdan;
        _kurlarTLye = kurlarTLye;
    }

    public void KalemEkle(SepetKalemi kalem) { Kalemler.Add(kalem); }
    public void KalemSil(int index) { Kalemler.RemoveAt(index); }
    public void SepetTemizle() { Kalemler.Clear(); }

    public (double ToplamTL, double KDVTutariTL) ToplamHesapla()
    {
        double toplamTL = 0;
        double toplamKDV = 0;
        foreach (var kalem in Kalemler)
        {
            toplamTL += kalem.ToplamTutarTL(_kurlarTLye);
            toplamKDV += kalem.KDVTutariTL(_kurlarTLye);
        }
        return (toplamTL, toplamKDV);
    }

    public bool OdemeYap(string birim)
    {
        double toplamTL = ToplamHesapla().ToplamTL;
        double kur = _kurlarTLye[birim];

        if (kur == 0)
        {
            Console.WriteLine("[HATA] Geçersiz veya tanımsız kur.");
            return false;
        }

        double odenecekMiktar = toplamTL / kur;
        if (_cuzdan.BakiyeHarca(birim, odenecekMiktar))
        {
            foreach (var kalem in Kalemler)
            {
                double maliyetTL = kalem.AlisFiyati * _kurlarTLye[kalem.ParaBirimi];
                double satisTL = kalem.ToplamTutarTL(_kurlarTLye);
                double netKar = satisTL - maliyetTL;
                SirketMuhasebesi.ToplamGelirTL += netKar;

                Kayitci.KayitEkle("SATIS", $"Ürün: {kalem.Ad}, Maliyet: {maliyetTL:F2} TL, Satış: {satisTL:F2} TL, Net Kar: {netKar:F2} TL");
            }
            SepetTemizle();
            return true;
        }
        return false;
    }
}

public static class Kayitci
{
    private static readonly List<string> _islemler = new();
    public static void KayitEkle(string tur, string aciklama)
    {
        _islemler.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tur}] - {aciklama}");
    }

    public static void KayitlariGoster()
    {
        Console.WriteLine("\n--- TÜM İŞLEM KAYITLARI ---");
        if (_islemler.Count == 0) { Console.WriteLine("Henüz kayıt bulunmamaktadır."); return; }
        foreach (var kayit in _islemler) { Console.WriteLine(kayit); }
        Console.WriteLine("---------------------------\n");
    }
}

public class IslemYonetici
{
    private readonly Cuzdan _cuzdan;
    private readonly Sepet _sepet;
    private readonly Dictionary<string, double> _kurlarTLye;
    private readonly List<SepetKalemi> _magazaStogu;

    public IslemYonetici(Cuzdan cuzdan, Sepet sepet, Dictionary<string, double> kurlarTLye)
    {
        _cuzdan = cuzdan;
        _sepet = sepet;
        _kurlarTLye = kurlarTLye;

        _magazaStogu = new List<SepetKalemi>
        {
            new SepetKalemi { Ad = "Kitap", SatisFiyati = 100, AlisFiyati = 50, ParaBirimi = "TL", KDVOrani = 0.08, IndirimOrani = 0.0, StokAdedi = 5 },
            new SepetKalemi { Ad = "Mouse", SatisFiyati = 10, AlisFiyati = 5, ParaBirimi = "USD", KDVOrani = 0.20, IndirimOrani = 0.1, StokAdedi = 2 },
            new SepetKalemi { Ad = "Klavye", SatisFiyati = 20, AlisFiyati = 10, ParaBirimi = "EUR", KDVOrani = 0.15, IndirimOrani = 0.0, StokAdedi = 3 },
            new SepetKalemi { Ad = "Oyun", SatisFiyati = 50, AlisFiyati = 30, ParaBirimi = "TL", KDVOrani = 0.18, IndirimOrani = 0.0, StokAdedi = 0 }
        };
    }

    public void MusteriIslemleriMenusu(string aktifKullaniciAdi)
    {
        string secim;
        do
        {
            Console.Clear();
            Console.WriteLine($"--- MÜŞTERİ İŞLEMLERİ (Kullanıcı: {aktifKullaniciAdi}) ---");
            Console.WriteLine("Mevcut Bakiye: " + _cuzdan.OzetAl());
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("[1] Sepete Ürün Ekle (Alışveriş)");
            Console.WriteLine("[2] Ödeme Yap");
            Console.WriteLine("[3] Cüzdana Fon Ekle (Simülasyon)");
            Console.WriteLine("[4] Para Birimi Dönüştür");
            Console.WriteLine("[0] Çıkış Yap (Giriş Ekranına Dön)");
            Console.Write("Seçiminiz: ");
            secim = Console.ReadLine() ?? string.Empty;

            switch (secim)
            {
                case "1": SepeteKalemEkle(); break;
                case "2": SepetOdemeYap(aktifKullaniciAdi); break;
                case "3": FonlariYonlendir(); break;
                case "4": ParaBirimiDonustur(); break;
                case "0": break;
                default: Console.WriteLine("[HATA] Geçersiz seçim."); break;
            }
            if (secim != "0")
            {
                Console.WriteLine("\nDevam etmek için bir tuşa basın...");
                Console.ReadKey();
            }

        } while (secim != "0");
    }

    public void FinansVeStokYonetimMenusu(string aktifKullaniciAdi)
    {
        string secim;
        do
        {
            Console.Clear();
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine($"       FİNANS VE STOK YÖNETİMİ (Yönetici: {aktifKullaniciAdi}) ");
            Console.WriteLine($"Toplam Şirket Geliri: {SirketMuhasebesi.ToplamGelirTL:F2} TL"   );
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("[1] Yeni Ürünü Stoğa Ekle                                      |");
            Console.WriteLine("[2] Döviz Kurlarını Güncelle ");
            Console.WriteLine("[0] Geri Dön");
            Console.WriteLine("----------------------------------------------------------------");
            Console.Write("Seçiminiz: ");
            secim = Console.ReadLine() ?? string.Empty;

            switch (secim)
            {
                case "1": UrunuStogaEkle(aktifKullaniciAdi); break;
                case "2": KurAyarlariniYapilandir(); break;
                case "0": break;
                default: Console.WriteLine("[HATA] Geçersiz seçim."); break;
            }
            if (secim != "0")
            {
                Console.WriteLine("\nDevam etmek için bir tuşa basın...");
                Console.ReadKey();
            }

        } while (secim != "0");
    }


    public void UrunuStogaEkle(string aktifKullaniciAdi)
    {
        Console.WriteLine("\n--- YENİ ÜRÜN STOĞA EKLEME ---");
        Console.WriteLine("Geri dönmek için 'G' yazın.");

        Console.Write("Ürün Adı: ");
        string? ad = Console.ReadLine();
        if (ad?.ToUpper() == "G") return;

        double alisFiyati, satisFiyati, kdvOrani, indirimOrani;
        int stokAdedi;

        Console.Write("Alış (Maliyet) Fiyatı (Örn: 50.0): ");
        if (!double.TryParse(Console.ReadLine(), out alisFiyati) || alisFiyati <= 0) { Console.WriteLine("[HATA] Geçersiz fiyat."); return; }

        Console.Write("Satış Fiyatı (Örn: 100.0): ");
        if (!double.TryParse(Console.ReadLine(), out satisFiyati) || satisFiyati <= 0) { Console.WriteLine("[HATA] Geçersiz fiyat."); return; }

        Console.Write("KDV Oranı (%) (Örn: 18 -> 0.18): ");
        if (!double.TryParse(Console.ReadLine(), out kdvOrani) || kdvOrani < 0) { Console.WriteLine("[HATA] Geçersiz KDV."); return; }
        kdvOrani /= 100;

        Console.Write("İndirim Oranı (%) (Örn: 10 -> 0.10): ");
        if (!double.TryParse(Console.ReadLine(), out indirimOrani) || indirimOrani < 0 || indirimOrani >= 100) { Console.WriteLine("[HATA] Geçersiz İndirim."); return; }
        indirimOrani /= 100;

        Console.Write("Stok Adedi: ");
        if (!int.TryParse(Console.ReadLine(), out stokAdedi) || stokAdedi <= 0) { Console.WriteLine("[HATA] Geçersiz stok adedi."); return; }

        Console.Write("Para Birimi (TL/USD/EUR/GBP): ");
        string? birim = Console.ReadLine()?.ToUpper();
        if (birim?.ToUpper() == "G") return;
        if (!_kurlarTLye.ContainsKey(birim!)) { Console.WriteLine("[HATA] Geçersiz para birimi."); return; }

        double kur = _kurlarTLye[birim!];
        double alisTL = alisFiyati * kur;
        double netSatisTL = (satisFiyati * kur) * (1 - indirimOrani);
        double kdvTutari = netSatisTL * kdvOrani;
        double toplamSatisTL = netSatisTL + kdvTutari;
        double netKarTL = toplamSatisTL - alisTL;

        Console.WriteLine("\n--- KAR/ZARAR ANALİZİ (1 ADET ÜZERİNDEN) ---");
        Console.WriteLine($"Alış Maliyeti (TL): {alisTL:F2} TL");
        Console.WriteLine($"Net Satış (İndirimli/KDV'siz) (TL): {netSatisTL:F2} TL");
        Console.WriteLine($"KDV Tutarı (TL): {kdvTutari:F2} TL");
        Console.WriteLine($"TOPLAM SATIŞ FİYATI (TL): {toplamSatisTL:F2} TL");

        if (netKarTL <= 0)
        {
            Console.WriteLine($"[UYARI] Zarar Ediyorsunuz! Net Kar: {netKarTL:F2} TL");
            Console.Write("İşlemi onaylıyor musunuz? (E/H/G): ");
            string? onay = Console.ReadLine()?.ToUpper();
            if (onay != "E") return;
        }
        else
        {
            Console.WriteLine($"[BİLGİ] Muhtemel Net Kar: {netKarTL:F2} TL");
            Console.Write("Ürünü stoğa eklemek için 'E' tuşlayın (Geri dönmek için 'G'): ");
            string? onay = Console.ReadLine()?.ToUpper();
            if (onay != "E") return;
        }

        var yeniUrun = new SepetKalemi
        {
            Ad = ad!,
            SatisFiyati = satisFiyati,
            AlisFiyati = alisFiyati,
            ParaBirimi = birim!,
            KDVOrani = kdvOrani,
            IndirimOrani = indirimOrani,
            StokAdedi = stokAdedi
        };

        _magazaStogu.Add(yeniUrun);
        Kayitci.KayitEkle("STOK_EKLEME", $"Ürün: {ad} (Stok: {stokAdedi}) - Satış: {satisFiyati} {birim}, Alış: {alisFiyati} {birim}");
        Console.WriteLine($"\n[TAMAM] {ad} ürünü {stokAdedi} adet stoğa eklendi. (Net Kar: {netKarTL:F2} TL/Adet)");
    }

    public void SepeteKalemEkle()
    {
        Console.WriteLine("\n--- STOKTAKI ÜRÜNLER (Sadece Stok > 0) ---");
        var mevcutUrunler = _magazaStogu.Where(k => k.StokAdedi > 0).ToList();
        if (!mevcutUrunler.Any()) { Console.WriteLine("Mağazada stokta ürün bulunmamaktadır."); return; }

        for (int i = 0; i < mevcutUrunler.Count; i++)
        {
            var u = mevcutUrunler[i];
            Console.WriteLine($"[{i + 1}] {u.Ad} | Fiyat: {u.SatisFiyati:F2} {u.ParaBirimi} | KDV: {u.KDVOrani * 100}% | İndirim: {u.IndirimOrani * 100}% | Stok: {u.StokAdedi}");
        }

        Console.Write("Sepete eklenecek ürün numarasını girin ('G' Geri): ");
        string? secim = Console.ReadLine();
        if (secim?.ToUpper() == "G") return;

        if (int.TryParse(secim, out int index) && index > 0 && index <= mevcutUrunler.Count)
        {
            var secilenUrun = mevcutUrunler[index - 1];

            Console.Write($"Kaç adet '{secilenUrun.Ad}' eklemek istiyorsunuz? (Max: {secilenUrun.StokAdedi}): ");
            if (!int.TryParse(Console.ReadLine(), out int adet) || adet <= 0 || adet > secilenUrun.StokAdedi)
            {
                Console.WriteLine("[HATA] Geçersiz adet veya stokta yeterli ürün yok.");
                return;
            }

            secilenUrun.StokAdedi -= adet;

            for (int i = 0; i < adet; i++)
            {
                var sepetKalemi = new SepetKalemi
                {
                    Ad = secilenUrun.Ad,
                    SatisFiyati = secilenUrun.SatisFiyati,
                    AlisFiyati = secilenUrun.AlisFiyati,
                    ParaBirimi = secilenUrun.ParaBirimi,
                    KDVOrani = secilenUrun.KDVOrani,
                    IndirimOrani = secilenUrun.IndirimOrani,
                    StokAdedi = 1 
                };
                _sepet.KalemEkle(sepetKalemi);
            }

            Kayitci.KayitEkle("SEPET_EKLE", $"{adet} adet {secilenUrun.Ad} sepete eklendi. Kalan stok: {secilenUrun.StokAdedi}");
            Console.WriteLine($"[TAMAM] {adet} adet '{secilenUrun.Ad}' sepete eklendi. Kalan Stok: {secilenUrun.StokAdedi}");
        }
        else
        {
            Console.WriteLine("[HATA] Geçersiz ürün numarası.");
        }
    }

    public void SepetOdemeYap(string kullaniciAdi)
    {
        if (!_sepet.Kalemler.Any()) { Console.WriteLine("\n[BİLGİ] Sepetiniz boş."); return; }

        Console.WriteLine("\n--- SEPETİNİZ ---");
        double toplamKDV = 0;
        double genelToplam = 0;

        var gruplanmisKalemler = _sepet.Kalemler
            .GroupBy(k => new { k.Ad, k.SatisFiyati, k.ParaBirimi, k.KDVOrani, k.IndirimOrani })
            .Select((g, index) => new
            {
                Index = index + 1,
                Urun = g.Key,
                Adet = g.Count(),
                ToplamTutarTL = g.Sum(k => k.ToplamTutarTL(_kurlarTLye)),
                ToplamKDVTL = g.Sum(k => k.KDVTutariTL(_kurlarTLye))
            }).ToList();

        foreach (var g in gruplanmisKalemler)
        {
            Console.WriteLine($"[{g.Index}] {g.Urun.Ad} ({g.Adet} adet) | Fiyat: {g.Urun.SatisFiyati:F2} {g.Urun.ParaBirimi} | İnd: {g.Urun.IndirimOrani * 100}% | KDV: {g.Urun.KDVOrani * 100}% | TOPLAM: {g.ToplamTutarTL:F2} TL");
            toplamKDV += g.ToplamKDVTL;
            genelToplam += g.ToplamTutarTL;
        }

        Console.WriteLine($"\nNET KDV TUTARI: {toplamKDV:F2} TL");
        Console.WriteLine($"GENEL TOPLAM TUTAR: {genelToplam:F2} TL");

        Console.WriteLine("\nÖdeme yapabileceğiniz birimler (Bakiyeniz):");
        Console.WriteLine(_cuzdan.OzetAl());

        Console.Write("Ödeme yapılacak para birimini girin ('G' Geri): ");
        string? birim = Console.ReadLine()?.ToUpper();
        if (birim == "G") return;

        if (!_kurlarTLye.ContainsKey(birim!)) { Console.WriteLine("[HATA] Geçersiz para birimi."); return; }

        double odenecekMiktar = genelToplam / _kurlarTLye[birim!];
        double bakiye = _cuzdan.BakiyeGetir(birim!);

        Console.WriteLine($"\nToplam Ödeme: {odenecekMiktar:F2} {birim!} (Mevcut Bakiye: {bakiye:F2} {birim!})");

        if (bakiye < odenecekMiktar)
        {
            Console.WriteLine("[HATA] Yetersiz bakiye.");
            return;
        }

        Console.Write("Ödemeyi onaylıyor musunuz? (E/H): ");
        if (Console.ReadLine()?.ToUpper() == "E")
        {
            if (_sepet.OdemeYap(birim!))
            {
                Kayitci.KayitEkle("ODEME", $"{kullaniciAdi} {birim!} ile {odenecekMiktar:F2} ödeme yaptı. Genel Toplam TL: {genelToplam:F2} TL");
                Console.WriteLine("\n[TAMAM] Ödeme başarılı. Teşekkür ederiz.");
            }
            else
            {
                Console.WriteLine("[HATA] Ödeme sırasında beklenmedik bir hata oluştu.");
            }
        }
        else
        {
            Console.WriteLine("[BİLGİ] Ödeme iptal edildi.");
        }
    }

    public void FonlariYonlendir()
    {
        Console.WriteLine("\n--- CÜZDANA FON EKLEME ---");
        Console.WriteLine("Mevcut Bakiye: " + _cuzdan.OzetAl());
        Console.Write("Eklenecek Para Birimi (TL/USD/EUR/GBP - 'G' Geri): ");
        string? birim = Console.ReadLine()?.ToUpper();
        if (birim == "G") return;
        if (!_kurlarTLye.ContainsKey(birim!)) { Console.WriteLine("[HATA] Geçersiz para birimi."); return; }

        Console.Write($"Eklenecek Miktar ({birim}): ");
        if (!double.TryParse(Console.ReadLine(), out double miktar) || miktar <= 0) { Console.WriteLine("[HATA] Geçersiz miktar."); return; }

        _cuzdan.BakiyeEkle(birim!, miktar);
        Kayitci.KayitEkle("FON_EKLE", $"{miktar:F2} {birim!} cüzdana eklendi.");
        Console.WriteLine($"\n[TAMAM] {miktar:F2} {birim!} cüzdanınıza eklendi.");
    }

    public void ParaBirimiDonustur()
    {
        Console.WriteLine("\n--- PARA BİRİMİ DÖNÜŞTÜRME (Komisyon: %1) ---");
        Console.WriteLine("Mevcut Bakiye: " + _cuzdan.OzetAl());
        Console.WriteLine("Döviz Kurları (TL'ye Karşılık): " + string.Join(" | ", _kurlarTLye.Select(k => $"{k.Key}: {k.Value}")));

        Console.Write("Dönüştürülecek Kaynak Birimi (Örn: USD - 'G' Geri): ");
        string? kaynakBirim = Console.ReadLine()?.ToUpper();
        if (kaynakBirim == "G") return;
        if (!_kurlarTLye.ContainsKey(kaynakBirim!)) { Console.WriteLine("[HATA] Geçersiz kaynak birimi."); return; }

        Console.Write("Hedef Birim (Örn: EUR): ");
        string? hedefBirim = Console.ReadLine()?.ToUpper();
        if (!_kurlarTLye.ContainsKey(hedefBirim!)) { Console.WriteLine("[HATA] Geçersiz hedef birimi."); return; }

        Console.Write($"Dönüştürülecek Miktar ({kaynakBirim}): ");
        if (!double.TryParse(Console.ReadLine(), out double miktar) || miktar <= 0) { Console.WriteLine("[HATA] Geçersiz miktar."); return; }

        if (_cuzdan.BakiyeGetir(kaynakBirim!) < miktar) { Console.WriteLine("[HATA] Yetersiz bakiye."); return; }

        double tlKarsilik = miktar * _kurlarTLye[kaynakBirim!];
        double komisyon = tlKarsilik * 0.01;
        double netTL = tlKarsilik - komisyon;
        double hedefMiktar = netTL / _kurlarTLye[hedefBirim!];

        _cuzdan.BakiyeHarca(kaynakBirim!, miktar);
        _cuzdan.BakiyeEkle(hedefBirim!, hedefMiktar);
        SirketMuhasebesi.ToplamGelirTL += komisyon;

        Kayitci.KayitEkle("DÖNÜŞÜM", $"{miktar:F2} {kaynakBirim!} -> {hedefMiktar:F2} {hedefBirim!}. Komisyon: {komisyon:F2} TL");
        Console.WriteLine($"\n[TAMAM] Dönüşüm başarılı. {hedefMiktar:F2} {hedefBirim!} eklendi. (Komisyon: {komisyon:F2} TL)");
    }

    public void KurAyarlariniYapilandir()
    {
        Console.WriteLine("\n--- DÖVİZ KURLARI YAPILANDIRMASI ---");
        Console.WriteLine("Mevcut Kurlar (1 Birim = X TL): " + string.Join(" | ", _kurlarTLye.Select(k => $"{k.Key}: {k.Value:F4}")));

        Console.Write("Güncellenecek Para Birimi (USD/EUR/GBP - 'G' Geri): ");
        string? birim = Console.ReadLine()?.ToUpper();
        if (birim == "G") return;
        if (!_kurlarTLye.ContainsKey(birim!)) { Console.WriteLine("[HATA] Geçersiz para birimi."); return; }
        if (birim == "TL") { Console.WriteLine("[HATA] TL kuru güncellenemez (her zaman 1.0)."); return; }

        Console.Write($"Yeni Kur Değeri (1 {birim} = X TL): ");
        if (!double.TryParse(Console.ReadLine(), out double yeniKur) || yeniKur <= 0) { Console.WriteLine("[HATA] Geçersiz kur değeri."); return; }

        _kurlarTLye[birim!] = yeniKur;
        Kayitci.KayitEkle("KUR_GÜNCELLEME", $"{birim} kuru {yeniKur:F4} olarak güncellendi.");
        Console.WriteLine($"\n[TAMAM] {birim} kuru başarıyla güncellendi.");
    }
}
