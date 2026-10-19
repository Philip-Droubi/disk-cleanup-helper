DISK CLEANUP HELPER
===================

Windows 10 ve Windows 11 için ücretsiz ve açık kaynaklı bir yardımcı.
Disklerinizi neyin doldurduğunu gösterir ve güvenle yer açar.
Tek bir küçük program dosyasıdır. Bilgisayarınız hakkında hiçbir şey göndermez; yalnızca GitHub'a yeni bir sürüm olup olmadığını sorar.


NE YAPAR
--------
Temizle             Tarayıcı ve uygulama önbellekleri, geçici dosyalar, eski güncelleştirme dosyaları, Windows
                    artıkları ve daha fazlası için güvenli temizlik. Her öğe neyi koruyup neyi kaybedeceğinizi açıklar.
Diski çözümle       Herhangi bir sürücüdeki en büyük klasörleri (içine bakmak için çift tıklayın) ve en büyük
                    dosyaları gösterir; böylece yerinizi neyin kapladığını bulabilirsiniz.
Disk sağlığı        Her diskin ve sürücünün sağlıklı olup olmadığını ve ne kadar dolu olduğunu canlı sıcaklık
                    ve geçmiş grafikleriyle gösterir, Windows'un kendi hata taramasını başlatır.
SMART ayrıntıları   Her diskin tüm kendi kendini izleme verileri.
Disk performansı    Canlı disk etkinliği ve hız testi.
Cihaz ayrıntıları   Windows, işlemci, bellek, grafik, ekranlar, depolama, pil, ağ ve ses; Windows'un
                    DirectX Tanı Aracı gibi. Kopyalayın veya metin dosyası olarak kaydedin.
İpuçları ve SSS     Diskinizin dolmasını önleyen basit alışkanlıklar ve sık sorulan soruların yanıtları.
Hakkında            Bilgisayarınıza Başlat menüsü girdisi ve masaüstü kısayoluyla yükleyin veya kaldırın.
                    Güncelleştirmeleri denetleyin.


NASIL BAŞLATILIR
----------------
1. DiskCleanupHelper.exe dosyasına çift tıklayın.
2. Windows "Bu uygulamanın cihazınızda değişiklik yapmasına izin vermek istiyor musunuz?" diye sorar. Evet'e tıklayın.
   Hayır'a tıklarsanız yine açılır, ancak yönetici hakları gerektiren öğeler gri olur.
3. Windows "Windows bilgisayarınızı korudu" uyarısını gösterirse "Ek bilgi" seçeneğine, ardından "Yine de çalıştır" düğmesine tıklayın.
   Bu uyarı, Microsoft'un henüz tanımadığı yeni programlarda görülebilir.
4. Dil: sağ üstteki dil düğmesine (dünya simgesi) tıklayın ve bir dil seçin. Pencere hemen o dile
   geçer ve seçiminizi hatırlar.
5. Açık veya koyu: sağ üstteki "Koyu mod" / "Açık mod" düğmesine tıklayın. Uygulama seçiminizi hatırlar.
6. Saklamak için: Hakkında sekmesini açın ve "Bu bilgisayara yükle" düğmesine tıklayın.
   Ardından Başlat menüsünde, masaüstünde ve Ayarlar > Uygulamalar'da görünür.
   Yüklemek için yönetici hakları gerekmez.


NASIL KULLANILIR
----------------
Temizle sekmesi:
1. İhtiyacınız olmayan programları, özellikle tarayıcıları, sohbet uygulamalarını, kod düzenleyicilerini ve oyunları kapatın.
2. Boyutlar, uygulama açıldığında kendiliğinden ölçülür. Bir iki dakika sürer. Durdurmak için "Durdur" düğmesine,
   yeniden ölçmek için "Boyutları ölç" düğmesine tıklayın.
3. Her grup kendi toplam boyutunu gösterir. Bir grubu daraltmak veya yeniden açmak için grubun başlığına tıklayın;
   uygulama hangi grupları daralttığınızı hatırlar.
4. Ne olduğunu, nerede olduğunu ve neyi koruyup neyi kaybedeceğinizi okumak için herhangi bir öğeye tıklayın.
5. Temizlemek istediğiniz her öğenin kutusunu işaretleyin veya "Tüm güvenlileri seç" düğmesine tıklayın.
   Her grubun başlığı, öğelerinden kaçının seçili olduğunu gösterir.
6. "Seçilenleri temizle" düğmesine tıklayın. Tam listeyi gösterir ve herhangi bir şeyi silmeden önce sorar.

Diski çözümle sekmesi:
1. Bir sürücü (veya "Yalnızca kullanıcı klasörüm") seçin ve "Tara" düğmesine tıklayın. Tüm sürücü birkaç dakika sürebilir.
2. Sol taraf: en büyük klasörler. İçine bakmak için bir klasöre çift tıklayın, geri dönmek için "Yukarı" düğmesine tıklayın.
3. Sağ taraf: en büyük dosyalar. Dosyaları seçin ve "Geri Dönüşüm Kutusu'na taşı" veya "Kalıcı olarak sil" seçeneğini seçin.
   Windows ve program dosyaları korunur ve burada silinemez.

Disk sağlığı sekmesi:
- Her diski (yönetici haklarıyla başlatıldığında sıcaklık, aşınma ve hata sayılarıyla birlikte)
  ve her sürücünün ne kadar dolu olduğunu gösterir.
- Sekme açıkken sıcaklık birkaç saniyede bir kendiliğinden güncellenir.
- Tam değeri ve ne zaman kaydedildiğini görmek için imleci bir geçmiş grafiğinin üzerine getirin.
- "Hataları tara", Windows sürücüsünde Windows'un kendi salt okunur denetimini çalıştırır.
- "Sürücüleri iyileştir", Windows'un kendi sürücü iyileştiricisini açar.

Disk performansı sekmesi:
- Seçilen diskin şu anda ne kadar meşgul olduğunu, son iki dakikanın grafikleriyle gösterir.
  Tam değerleri görmek için imleci bir grafiğin üzerine getirin.
- "Hız testini çalıştır", diskin gerçekte ne kadar hızlı yazıp okuyabildiğini ölçer. Sonuç kaydedilir,
  böylece daha sonra karşılaştırabilirsiniz.


TEMİZLİK GRUPLARI NE ANLAMA GELİR
---------------------------------
Güvenli         Önbellekler ve geçici dosyalar. Programlar bunları kendiliğinden yeniden oluşturur.
                Tarayıcı ve uygulama önbellek temizliği parolalarınızı, yer işaretlerinizi, geçmişinizi ve oturumlarınızı korur.
Yönetici        Yüklü programların eski güncelleştirme dosyaları (her birinin en yeni 3 dosyası tutulur)
                ve Microsoft'un kendi Windows bileşenleri temizliği.
Karar sizin     Küçük bir şey kaybedersiniz: eski proje geçmişi, indirilen şarkılar,
                video önizlemeleri, çökme döküm dosyaları veya Geri Dönüşüm Kutusu'ndakiler.
Ayar            Yer açan Windows ayarları: hazırda bekletme, Ayrılmış Depolama, CompactOS
                ve Depolama Algısı.
Yalnızca bilgi  Aracın yalnızca gösterip açtığı, asla silmediği büyük öğeler: önceki Windows
                yüklemesi, en büyük programlar, Sistem Geri Yükleme, Docker, WSL, İndirilenler.

Yalnızca bilgisayarınızda bulunan öğeler listelenir.


GÜVENLİK
--------
- Siz seçip onaylamadan hiçbir şey silinmez.
- Temizle sekmesiyle silinen dosyalar Geri Dönüşüm Kutusu'na gitmez ve geri yüklenemez.
- Temizle sekmesi belgelerinize, fotoğraflarınıza, müziklerinize, videolarınıza, masaüstünüze veya indirilenlerinize asla dokunmaz.
- Bir tarayıcının veya uygulamanın önbelleğini temizlemeden önce onu kapatmanızı ister.
- Ölçme, tarama veya temizleme öncesinde bilgisayarın belleği azsa sizi uyarır.
- Klasör bağlantılarını izleyerek başka klasörlere asla geçmez.
- Windows, WinSxS veya Installer klasörlerindeki dosyaları asla elle silmeyin.


GEÇMİŞ
------
Temizlenenler yalnızca bu bilgisayara, şu dosyaya kaydedilir:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Hakkında sekmesinden açın.


GÜNCELLEŞTİRMELER
-----------------
Uygulama başlarken kendi GitHub sayfasına yeni bir sürüm olup olmadığını sorar (günde en fazla bir kez).
Yeni sürüm arka planda indirilir ve kullanılmadan önce denetlenir (SHA256 ve sürüm numarası).
Ardından bir çubuk "Şimdi yeniden başlat" seçeneğini sunar. "Daha sonra" seçeneğini belirlerseniz güncelleştirme, uygulamayı kapattığınızda yüklenir.
Otomatik denetimi kapatmak için Hakkında sekmesinde bu seçeneğin işaretini kaldırın. Oradaki "Güncelleştirmeleri denetle" düğmesi yine de çalışır.
Bilgisayarınız hakkında hiçbir şey gönderilmez.


KALDIRMA
--------
Ayarlar > Uygulamalar > Disk Cleanup Helper > Kaldır veya Hakkında sekmesinde "Kaldır" düğmesine tıklayın.
Hiç yüklemediyseniz DiskCleanupHelper.exe dosyasını silmeniz yeterlidir.


BİR WINDOWS AYARINI GERİ ALMA
-----------------------------
Komut İstemi'ni yönetici olarak açın ve gereken satırı çalıştırın:
    powercfg /h on                                          (hazırda bekletme yeniden açılır)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (Ayrılmış Depolama yeniden açılır)
    compact.exe /compactos:never                            (Windows dosyalarını sıkıştırmayı durdurur)


GEREKSİNİMLER
-------------
Windows 10 veya Windows 11. Başka bir şey yüklemeniz gerekmez: Windows'ta yerleşik .NET Framework'ü kullanır.


LİSANS VE GELİŞTİRİCİ
---------------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Özgür ve açık kaynaklı yazılım: GNU General Public License v3 kapsamında kullanabilir, değiştirebilir ve paylaşabilirsiniz;
ek koşullar (bölüm 7) geçerlidir: indirdiğiniz paketteki LICENSE.txt ve NOTICE.txt dosyalarına veya Hakkında sekmesindeki "Lisans ve koşullar" bağlantısına bakın.
Kaynak kodu: https://github.com/Philip-Droubi/disk-cleanup-helper
İletişim: https://philip-droubi.github.io/Philip_Droubi_portfolio/ ve https://www.linkedin.com/in/philip-droubi/


SORUMLULUK REDDİ
----------------
Bu program olduğu gibi, hiçbir garanti olmaksızın sağlanır. Kullanım riski size aittir.
Herhangi bir bilgisayarı temizlemeden önce önemli dosyaların yedeklendiğinden emin olun.
