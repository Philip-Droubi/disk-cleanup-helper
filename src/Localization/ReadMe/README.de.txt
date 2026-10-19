DISK CLEANUP HELPER
===================

Ein kostenloser, quelloffener Helfer für Windows 10 und Windows 11.
Er zeigt, was Ihre Datenträger füllt, und gibt sicher Speicherplatz frei.
Er besteht aus einer kleinen Programmdatei. Er sendet nichts über Ihren PC; er fragt nur bei GitHub nach, ob es eine neue Version gibt.


FUNKTIONEN
----------
Bereinigen     Sichere Bereinigung von Browser- und App-Caches, temporären Dateien, alten Updatedateien,
               Windows-Resten und mehr. Jeder Eintrag erklärt, was Sie behalten und was Sie verlieren.
Analyse        Zeigt die größten Ordner (Doppelklick, um hineinzusehen) und die größten Dateien
               auf jedem Laufwerk, damit Sie finden, was Ihren Platz belegt.
Zustand        Zeigt, ob jeder Datenträger und jedes Laufwerk fehlerfrei ist und wie voll es ist,
               mit Live-Temperatur und Verlaufsdiagrammen, und startet die Windows-eigene Fehlerprüfung.
SMART-Details  Die vollständigen Selbstüberwachungsdaten jedes Datenträgers.
Leistung       Live-Aktivität der Datenträger und ein Speedtest.
Gerätedetails  Windows, Prozessor, Arbeitsspeicher, Grafik, Bildschirme, Speicher, Akku, Netzwerk
               und Sound, wie das DirectX-Diagnoseprogramm von Windows. Kopieren Sie die Angaben oder speichern Sie sie als Textdatei.
Tipps & FAQ    Einfache Gewohnheiten, damit Ihr Datenträger nicht voll wird, und Antworten auf häufige Fragen.
Info           Installieren Sie das Programm mit Startmenüeintrag und Desktopverknüpfung auf Ihrem PC, oder deinstallieren Sie es.
               Nach Updates suchen.


SO STARTEN SIE
--------------
1. Doppelklicken Sie auf DiskCleanupHelper.exe.
2. Windows fragt: „Möchten Sie zulassen, dass durch diese App Änderungen an Ihrem Gerät vorgenommen werden?“ Klicken Sie auf „Ja“.
   Wenn Sie auf „Nein“ klicken, öffnet sich das Programm trotzdem, aber die Einträge, die Administratorrechte brauchen, sind grau.
3. Zeigt Windows „Der Computer wurde durch Windows geschützt“, klicken Sie auf „Weitere Informationen“ und dann auf „Trotzdem ausführen“.
   Das kann bei neuen Programmen erscheinen, die Microsoft noch nicht kennt.
4. Sprache: Klicken Sie oben rechts auf die Sprachschaltfläche (die Weltkugel) und wählen Sie eine Sprache. Das Fenster
   wechselt sofort in diese Sprache und merkt sich Ihre Wahl.
5. Hell oder dunkel: Klicken Sie oben rechts auf die Schaltfläche „Dunkler Modus“ / „Heller Modus“. Die App merkt sich Ihre Wahl.
6. Zum Behalten: Öffnen Sie die Registerkarte „Info“ und klicken Sie auf „Auf diesem PC installieren“.
   Danach erscheint das Programm im Startmenü, auf dem Desktop und unter Einstellungen > Apps.
   Für die Installation sind keine Administratorrechte nötig.


BEDIENUNG
---------
Registerkarte „Bereinigen“:
1. Schließen Sie Programme, die Sie nicht brauchen, vor allem Browser, Chat-Apps, Code-Editoren und Spiele.
2. Die Größen werden beim Öffnen der App von selbst gemessen. Das dauert ein bis zwei Minuten. Klicken Sie auf „Stopp“, um abzubrechen,
   oder auf „Größen messen“, um erneut zu messen.
3. Jede Gruppe zeigt ihre Gesamtgröße. Klicken Sie auf den Titel einer Gruppe, um sie einzuklappen oder wieder aufzuklappen;
   die App merkt sich, welche Gruppen Sie eingeklappt haben.
4. Klicken Sie auf einen Eintrag, um zu lesen, was er ist, wo er liegt und was Sie behalten oder verlieren.
5. Haken Sie jeden Eintrag an, den Sie bereinigen möchten, oder klicken Sie auf „Alle sicheren wählen“.
   Der Titel jeder Gruppe zeigt, wie viele ihrer Einträge ausgewählt sind.
6. Klicken Sie auf „Auswahl bereinigen“. Das Tool zeigt die genaue Liste und fragt, bevor etwas gelöscht wird.

Registerkarte „Analyse“:
1. Wählen Sie ein Laufwerk (oder „Nur mein Benutzerordner“) und klicken Sie auf „Scannen“. Ein ganzes Laufwerk kann einige Minuten dauern.
2. Links: die größten Ordner. Doppelklicken Sie auf einen Ordner, um hineinzusehen, und klicken Sie auf „Nach oben“, um zurückzugehen.
3. Rechts: die größten Dateien. Wählen Sie Dateien aus und wählen Sie „In den Papierkorb verschieben“ oder „Endgültig löschen“.
   Windows- und Programmdateien sind geschützt und können hier nicht gelöscht werden.

Registerkarte „Zustand“:
- Zeigt jeden Datenträger (mit Temperatur, Abnutzung und Fehlerzahlen, wenn mit Administratorrechten gestartet)
  und wie voll jedes Laufwerk ist.
- Die Temperatur wird alle paar Sekunden von selbst aktualisiert, solange die Registerkarte geöffnet ist.
- Zeigen Sie auf ein Verlaufsdiagramm, um den genauen Wert und den Zeitpunkt seiner Speicherung zu sehen.
- „Auf Fehler prüfen“ startet die Windows-eigene Prüfung des Windows-Laufwerks, die nur liest.
- „Laufwerke optimieren“ öffnet die Windows-eigene Laufwerksoptimierung.

Registerkarte „Leistung“:
- Zeigt, wie stark der gewählte Datenträger gerade ausgelastet ist, mit Diagrammen der letzten zwei Minuten.
  Zeigen Sie auf ein Diagramm, um die genauen Werte zu sehen.
- „Speedtest starten“ misst, wie schnell der Datenträger wirklich schreiben und lesen kann. Das Ergebnis wird gespeichert,
  sodass Sie später vergleichen können.


BEDEUTUNG DER BEREINIGUNGSGRUPPEN
---------------------------------
Sicher        Caches und temporäre Dateien. Programme erstellen sie selbst neu.
              Bei Browser- und App-Caches bleiben Ihre Kennwörter, Lesezeichen, Ihr Verlauf und Ihre Anmeldungen erhalten.
Admin         Alte Updatedateien installierter Programme (die neuesten 3 jedes Programms bleiben)
              und Microsofts eigene Bereinigung der Windows-Komponenten.
Ihre Wahl     Sie verlieren etwas Kleines: alten Projektverlauf, heruntergeladene Songs,
              Videovorschauen, Absturzabbilddateien oder den Inhalt des Papierkorbs.
Einstellung   Windows-Schalter, die Platz freigeben: Ruhezustand, Reservierter Speicher, CompactOS
              und Speicheroptimierung.
Nur Info      Große Dinge, die das Tool nur zeigt und öffnet, nie löscht: vorherige Windows-Installation,
              größte Programme, Systemwiederherstellung, Docker, WSL, Downloads.

Nur Einträge, die auf Ihrem PC vorhanden sind, werden aufgeführt.


SICHERHEIT
----------
- Nichts wird gelöscht, bevor Sie es auswählen und bestätigen.
- Dateien, die auf der Registerkarte „Bereinigen“ gelöscht werden, kommen nicht in den Papierkorb und können nicht wiederhergestellt werden.
- Die Registerkarte „Bereinigen“ rührt Ihre Dokumente, Fotos, Musik, Videos, Ihren Desktop und Ihre Downloads nie an.
- Das Tool bittet Sie, einen Browser oder eine App vor dem Bereinigen des Caches zu schließen.
- Das Tool warnt Sie vor dem Messen, Scannen oder Bereinigen, wenn der Arbeitsspeicher des PCs knapp ist.
- Das Tool folgt nie Ordnerlinks in andere Ordner.
- Löschen Sie nie Dateien in den Ordnern Windows, WinSxS oder Installer von Hand.


VERLAUF
-------
Was bereinigt wurde, wird nur auf diesem PC gespeichert, in:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Öffnen Sie die Datei über die Registerkarte „Info“.


UPDATES
-------
Beim Start fragt die App ihre GitHub-Seite, ob es eine neue Version gibt (höchstens einmal am Tag).
Eine neue Version wird im Hintergrund heruntergeladen und geprüft (SHA256 und Versionsnummer), bevor sie verwendet wird.
Danach bietet eine Leiste „Jetzt neu starten“ an. Wenn Sie „Später“ wählen, wird das Update beim Schließen der App installiert.
Um die automatische Prüfung auszuschalten, entfernen Sie den Haken auf der Registerkarte „Info“. „Nach Updates suchen“ funktioniert dort weiterhin.
Es wird nichts über Ihren PC gesendet.


DEINSTALLIEREN
--------------
Einstellungen > Apps > Disk Cleanup Helper > Deinstallieren, oder klicken Sie auf der Registerkarte „Info“ auf „Deinstallieren“.
Wenn Sie das Programm nie installiert haben, löschen Sie einfach DiskCleanupHelper.exe.


WINDOWS-EINSTELLUNG RÜCKGÄNGIG MACHEN
-------------------------------------
Öffnen Sie die Eingabeaufforderung als Administrator und führen Sie die benötigte Zeile aus:
    powercfg /h on                                          (Ruhezustand wieder ein)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (Reservierter Speicher wieder ein)
    compact.exe /compactos:never                            (Windows-Dateien nicht mehr komprimieren)


VORAUSSETZUNGEN
---------------
Windows 10 oder Windows 11. Sonst ist nichts zu installieren: Das Programm nutzt das in Windows integrierte .NET Framework.


LIZENZ UND AUTOR
----------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Freie und quelloffene Software: Sie dürfen das Programm unter der GNU General Public License v3
mit zusätzlichen Bedingungen (Abschnitt 7) nutzen, ändern und weitergeben: siehe LICENSE.txt und NOTICE.txt im Download oder „Lizenz und Bedingungen“ auf der Registerkarte „Info“.
Quellcode: https://github.com/Philip-Droubi/disk-cleanup-helper
Kontakt: https://philip-droubi.github.io/Philip_Droubi_portfolio/ und https://www.linkedin.com/in/philip-droubi/


HAFTUNGSAUSSCHLUSS
------------------
Dieses Programm wird ohne Mängelgewähr und ohne jegliche Garantie bereitgestellt. Die Nutzung erfolgt auf eigenes Risiko.
Stellen Sie sicher, dass wichtige Dateien gesichert sind, bevor Sie einen Computer bereinigen.
