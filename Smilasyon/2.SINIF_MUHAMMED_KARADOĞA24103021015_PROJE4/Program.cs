using System;
using System.Collections.Generic;
using System.Linq;

public static class SirketMuhasebesi
{
    public static double ToplamGelirTL { get; set; } = 0.0;
}

public class Program
{
    private static readonly Dictionary<string, double> KurlarTLye = new()
    {
        { "TL", 1.0 },
        { "USD", 32.0 },
        { "EUR", 35.0 },
        { "GBP", 40.0 }
    };

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine("|               MARKETİNGİNİZ                     |");
        Console.WriteLine("-------------------------------------------------");


        Kullanici? aktifKullanici = null;
        Cuzdan kullaniciCuzdani = new Cuzdan(KurlarTLye);
        Sepet kullaniciSepeti = new Sepet(kullaniciCuzdani, KurlarTLye);
        IslemYonetici yonetici = new IslemYonetici(kullaniciCuzdani, kullaniciSepeti, KurlarTLye);

        bool uygulamaCalisiyor = true;
        while (uygulamaCalisiyor)
        {
            aktifKullanici = null;
            while (aktifKullanici == null)
            {
                aktifKullanici = KullaniciGirisVeyaKayit();
                if (aktifKullanici == null)
                {
                    Console.WriteLine("\n[HATA] Geçersiz kullanıcı adı, şifre veya işlem. Tekrar deneyin.");
                    Console.ReadKey();
                }
            }
            if (aktifKullanici != null)
            {
                AnaMenuyuBaslat(aktifKullanici, yonetici);
                Console.WriteLine("\n[BİLGİ] Oturum sonlandırıldı. Giriş ekranına dönülüyor...");
                Console.ReadKey();
            }
        }
    }

    private static Kullanici? KullaniciGirisVeyaKayit()
    {
        Console.WriteLine("\n-------------------------------------------------");
        Console.WriteLine("|                 GİRİŞ EKRANI                  |");
        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine("|[1] Giriş Yap                                  |");
        Console.WriteLine("|[2] Kaydol                                     |");
        Console.WriteLine("-------------------------------------------------");
        Console.Write("Seçiminiz: ");
        string? secim = Console.ReadLine();

        Console.Write("Kullanıcı Adı: ");
        string? kullaniciAdi = Console.ReadLine()?.ToLower();

        Console.Write("Şifre: ");
        string? sifre = Console.ReadLine();

        if (string.IsNullOrEmpty(kullaniciAdi) || string.IsNullOrEmpty(sifre)) return null;

        if (secim == "1")
        {
            return KullaniciYoneticisi.GirisYap(kullaniciAdi, sifre);
        }
        else if (secim == "2")
        {
            if (KullaniciYoneticisi.Kaydol(kullaniciAdi, sifre))
            {
                Console.WriteLine("[TAMAM] Kayıt başarılı. Giriş yapılıyor.");
                return KullaniciYoneticisi.GirisYap(kullaniciAdi, sifre);
            }
            else
            {
                Console.WriteLine("[HATA] Kullanıcı adı zaten mevcut.");
                return null;
            }
        }
        return null;
    }

    private static void AnaMenuyuBaslat(Kullanici aktifKullanici, IslemYonetici yonetici)
    {
        string secim;
        do
        {
            Console.Clear();
            Console.WriteLine(" -------------------------------------------------------------");
            Console.WriteLine($"|                   HOŞ GELDİNİZ: {aktifKullanici.KullaniciAdi.ToUpper()}                 |");
            Console.WriteLine(" -------------------------------------------------------------");

            if (aktifKullanici.IsAdmin)
            {
                Console.WriteLine(" ------------------------------------------------------------");
                Console.WriteLine("| YÖNETİCİ ANA MENÜSÜ                                         |");
                Console.WriteLine("|[1] FİNANS VE STOK YÖNETİMİ (Kur/Ürün Güncelleme)           |");
                Console.WriteLine("|[2] Tüm İşlem Kayıtlarını Görüntüle                         |");
                Console.WriteLine("|[3] Şifre Değiştirme                                        |");
                Console.WriteLine("|[0] Çıkış Yap (Giriş Ekranına Dön)                          |");
                Console.WriteLine("-------------------------------------------------------------");
                Console.Write("Seçiminiz: ");
                secim = Console.ReadLine() ?? string.Empty;

                switch (secim)
                {
                    case "1": yonetici.FinansVeStokYonetimMenusu(aktifKullanici.KullaniciAdi); break;
                    case "2": Kayitci.KayitlariGoster(); break;
                    case "3": SifreDegistirmeIslemi(aktifKullanici); break;
                    case "0": Console.WriteLine("[BİLGİ] Oturum kapatılıyor..."); break;
                    default: Console.WriteLine("[HATA] Geçersiz seçim."); break;
                }
            }
            else
            {
                Console.WriteLine("--- KULLANICI MENÜSÜ (Alışveriş) ---");
                yonetici.MusteriIslemleriMenusu(aktifKullanici.KullaniciAdi);
                secim = "0";
            }

            if (secim != "0")
            {
                Console.WriteLine("\nAna menüye dönmek için bir tuşa basın...");
                Console.ReadKey();
            }

        } while (secim != "0");
    }

    private static void SifreDegistirmeIslemi(Kullanici kullanici)
    {
        Console.WriteLine("\n--- ŞİFRE DEĞİŞTİRME ---");
        Console.Write("Yeni Şifrenizi Girin: ");
        string? yeniSifre = Console.ReadLine();
        if (!string.IsNullOrEmpty(yeniSifre))
        {
            KullaniciYoneticisi.SifreDegistir(kullanici, yeniSifre);
            Console.WriteLine("[TAMAM] Şifreniz başarıyla değiştirildi.");
            Kayitci.KayitEkle("GÜVENLİK", $"Kullanıcı {kullanici.KullaniciAdi} şifresini değiştirdi.");
        }
        else
        {
            Console.WriteLine("[HATA] Şifre boş bırakılamaz.");
        }
    }
}
