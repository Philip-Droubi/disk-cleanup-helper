DISK CLEANUP HELPER
===================

Alat bantu gratis dan sumber terbuka untuk Windows 10 dan Windows 11.
Alat ini menunjukkan apa yang memenuhi disk Anda dan mengosongkan ruang dengan aman.
Hanya satu file program kecil. Tidak mengirim apa pun tentang PC Anda; aplikasi hanya menanyakan ke GitHub apakah ada versi baru.


FUNGSI APLIKASI
---------------
Bersihkan         Pembersihan aman untuk cache browser dan aplikasi, file sementara, file pembaruan lama,
                  sisa-sisa Windows, dan lainnya. Setiap item menjelaskan apa yang tetap ada dan apa yang hilang.
Analisis disk     Menampilkan folder terbesar (klik dua kali untuk melihat isinya) dan file terbesar
                  di drive mana pun, sehingga Anda bisa menemukan apa yang memakan ruang Anda.
Kesehatan disk    Menunjukkan apakah setiap disk dan drive sehat dan seberapa penuh,
                  dengan suhu langsung dan grafik riwayat, serta menjalankan pemindaian kesalahan milik Windows.
Detail SMART      Data pemantauan mandiri lengkap dari setiap disk.
Performa disk     Aktivitas disk langsung dan uji kecepatan.
Detail perangkat  Windows, prosesor, memori, grafis, layar, penyimpanan, baterai, jaringan
                  dan suara, seperti Alat Diagnostik DirectX Windows. Salin atau simpan sebagai file teks.
Tips & FAQ        Kebiasaan sederhana agar disk Anda tidak cepat penuh, dan jawaban atas pertanyaan umum.
Tentang           Instal di PC Anda dengan entri menu Mulai dan pintasan desktop, atau copot pemasangannya.
                  Periksa pembaruan.


CARA MEMULAI
------------
1. Klik dua kali DiskCleanupHelper.exe.
2. Windows bertanya "Apakah Anda ingin mengizinkan aplikasi ini membuat perubahan?". Klik Ya.
   Jika Anda mengklik Tidak, aplikasi tetap terbuka, tetapi item yang memerlukan hak admin berwarna abu-abu.
3. Jika Windows menampilkan "Windows melindungi PC Anda", klik "Info selengkapnya", lalu "Tetap jalankan".
   Ini bisa muncul untuk program baru yang belum dikenal oleh Microsoft.
4. Bahasa: klik tombol bahasa (bola dunia) di kanan atas dan pilih bahasa. Jendela langsung beralih
   ke bahasa itu dan mengingat pilihan Anda.
5. Terang atau gelap: klik tombol "Mode gelap" / "Mode terang" di kanan atas. Aplikasi mengingat pilihan Anda.
6. Untuk menyimpannya: buka tab Tentang dan klik "Instal di PC ini".
   Setelah itu aplikasi muncul di menu Mulai, di desktop, dan di Pengaturan > Aplikasi.
   Tidak perlu hak admin untuk menginstal.


CARA MENGGUNAKAN
----------------
Tab Bersihkan:
1. Tutup program yang tidak Anda perlukan, terutama browser, aplikasi chat, editor kode, dan game.
2. Ukuran dihitung sendiri saat aplikasi dibuka. Prosesnya satu atau dua menit. Klik "Hentikan" untuk berhenti,
   atau "Hitung ukuran" untuk menghitung ulang.
3. Setiap grup menampilkan ukuran totalnya. Klik judul grup untuk menciutkannya atau membukanya lagi;
   aplikasi mengingat grup mana yang Anda ciutkan.
4. Klik item mana pun untuk membaca apa itu, di mana lokasinya, dan apa yang tetap ada atau hilang.
5. Centang kotak setiap item yang ingin Anda bersihkan, atau klik "Pilih semua yang aman".
   Judul setiap grup menampilkan berapa banyak itemnya yang dipilih.
6. Klik "Bersihkan yang dipilih". Aplikasi menampilkan daftar persisnya dan bertanya sebelum menghapus apa pun.

Tab Analisis disk:
1. Pilih drive (atau "Folder pengguna saya saja") dan klik "Pindai". Seluruh drive bisa memakan waktu beberapa menit.
2. Sisi kiri: folder terbesar. Klik dua kali folder untuk melihat isinya, klik "Naik" untuk kembali.
3. Sisi kanan: file terbesar. Pilih file lalu pilih "Pindahkan ke Keranjang Sampah" atau "Hapus permanen".
   File Windows dan file program dilindungi dan tidak dapat dihapus di sini.

Tab Kesehatan disk:
- Menampilkan setiap disk (dengan suhu, keausan, dan jumlah kesalahan saat dijalankan dengan hak admin)
  dan seberapa penuh setiap drive.
- Suhu diperbarui sendiri setiap beberapa detik selama tab terbuka.
- Arahkan penunjuk ke grafik riwayat untuk melihat nilai persisnya dan kapan nilai itu disimpan.
- "Pindai kesalahan" menjalankan pemeriksaan hanya-baca milik Windows pada drive Windows.
- "Optimalkan drive" membuka pengoptimal drive milik Windows.

Tab Performa disk:
- Menampilkan seberapa sibuk disk yang dipilih saat ini, dengan grafik dua menit terakhir.
  Arahkan penunjuk ke grafik untuk melihat nilai persisnya.
- "Jalankan uji kecepatan" mengukur seberapa cepat disk benar-benar bisa menulis dan membaca. Hasilnya disimpan,
  sehingga Anda bisa membandingkannya nanti.


ARTI GRUP PEMBERSIHAN
---------------------
Aman            Cache dan file sementara. Program membuatnya ulang sendiri.
                Item cache browser dan aplikasi tetap menyimpan kata sandi, bookmark, riwayat, dan login Anda.
Admin           File pembaruan lama dari program terinstal (menyimpan 3 yang terbaru dari masing-masing),
                dan pembersihan komponen Windows milik Microsoft.
Keputusan Anda  Anda kehilangan sesuatu yang kecil: riwayat proyek lama, lagu yang diunduh,
                pratinjau video, file crash dump, atau isi Keranjang Sampah.
Pengaturan      Sakelar Windows yang mengosongkan ruang: hibernasi, Penyimpanan Cadangan, CompactOS,
                dan Storage Sense.
Hanya info      Hal besar yang hanya ditampilkan dan dibuka alat ini, tidak pernah dihapus: penginstalan
                Windows sebelumnya, program terbesar, Pemulihan Sistem, Docker, WSL, Unduhan.

Hanya item yang ada di PC Anda yang dicantumkan.


KEAMANAN
--------
- Tidak ada yang dihapus sampai Anda memilihnya dan mengonfirmasi.
- File yang dihapus oleh tab Bersihkan tidak masuk ke Keranjang Sampah dan tidak dapat dipulihkan.
- Tab Bersihkan tidak pernah menyentuh dokumen, foto, musik, video, desktop, atau unduhan Anda.
- Aplikasi meminta Anda menutup browser atau aplikasi sebelum membersihkan cache-nya.
- Aplikasi memperingatkan Anda jika memori PC hampir habis sebelum menghitung ukuran, memindai, atau membersihkan.
- Aplikasi tidak pernah mengikuti tautan folder ke folder lain.
- Jangan pernah menghapus file di folder Windows, WinSxS, atau Installer secara manual.


RIWAYAT
-------
Apa yang dibersihkan hanya disimpan di PC ini, di:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Buka dari tab Tentang.


PEMBARUAN
---------
Saat dimulai, aplikasi menanyakan ke halaman GitHub-nya apakah ada versi baru (paling banyak sekali sehari).
Versi baru diunduh di latar belakang dan diperiksa (SHA256 dan nomor versi) sebelum digunakan.
Setelah itu, sebuah bilah menawarkan "Mulai ulang sekarang". Jika Anda memilih "Nanti", pembaruan diinstal saat Anda menutup aplikasi.
Untuk menonaktifkan pemeriksaan otomatis, hapus centangnya di tab Tentang. "Periksa pembaruan" di sana tetap berfungsi.
Tidak ada apa pun tentang PC Anda yang dikirim.


COPOT PEMASANGAN
----------------
Pengaturan > Aplikasi > Disk Cleanup Helper > Copot pemasangan, atau klik "Copot pemasangan" di tab Tentang.
Jika Anda tidak pernah menginstalnya, cukup hapus DiskCleanupHelper.exe.


MEMBATALKAN PENGATURAN WINDOWS
------------------------------
Buka Prompt Perintah sebagai administrator dan jalankan baris yang Anda perlukan:
    powercfg /h on                                          (hibernasi aktif kembali)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (Penyimpanan Cadangan aktif kembali)
    compact.exe /compactos:never                            (berhenti mengompres file Windows)


PERSYARATAN
-----------
Windows 10 atau Windows 11. Tidak ada lagi yang perlu diinstal: aplikasi memakai .NET Framework bawaan Windows.


LISENSI DAN PEMBUAT
-------------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Perangkat lunak bebas dan sumber terbuka: Anda boleh menggunakan, mengubah, dan membagikannya di bawah GNU General Public License v3
dengan ketentuan tambahan (bagian 7): lihat LICENSE.txt dan NOTICE.txt dalam paket unduhan, atau "Lisensi dan ketentuan" di tab Tentang.
Kode sumber: https://github.com/Philip-Droubi/disk-cleanup-helper
Kontak: https://philip-droubi.github.io/Philip_Droubi_portfolio/ dan https://www.linkedin.com/in/philip-droubi/


PENAFIAN
--------
Program ini disediakan apa adanya, tanpa jaminan apa pun. Gunakan dengan risiko Anda sendiri.
Pastikan file penting sudah dicadangkan sebelum membersihkan komputer mana pun.
