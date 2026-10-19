DISK CLEANUP HELPER
===================

Darmowy pomocnik o otwartym kodzie źródłowym dla systemów Windows 10 i Windows 11.
Pokazuje, co zapełnia dyski, i bezpiecznie zwalnia miejsce.
To jeden mały plik programu. Nie wysyła żadnych informacji o Twoim komputerze; sprawdza tylko w serwisie GitHub, czy jest nowa wersja.


CO ROBI
-------
Oczyszczanie         Bezpieczne czyszczenie pamięci podręcznych przeglądarek i aplikacji, plików
                     tymczasowych, starych plików aktualizacji, pozostałości systemu Windows i nie tylko.
                     Każdy element wyjaśnia, co zachowasz, a co stracisz.
Analiza dysku        Pokazuje największe foldery (kliknij dwukrotnie, aby zajrzeć do środka) i największe
                     pliki na dowolnym dysku, dzięki czemu znajdziesz to, co zajmuje miejsce.
Kondycja dysku       Pokazuje, czy każdy dysk i wolumin jest w dobrej kondycji i jak bardzo jest zapełniony,
                     wraz z bieżącą temperaturą i wykresami historii, oraz uruchamia wbudowane skanowanie
                     błędów systemu Windows.
Szczegóły SMART      Pełne dane samodiagnostyki każdego dysku.
Wydajność dysku      Bieżąca aktywność dysku i test szybkości.
Szczegóły urządzenia Windows, procesor, pamięć, grafika, ekrany, magazyn, bateria, sieć i dźwięk,
                     jak w Narzędziu diagnostycznym DirectX systemu Windows. Można je skopiować
                     lub zapisać jako plik tekstowy.
Porady i FAQ         Proste nawyki, dzięki którym dysk się nie zapełnia, oraz odpowiedzi na częste pytania.
Informacje           Zainstaluj aplikację na komputerze z pozycją w menu Start i skrótem na pulpicie
                     albo ją odinstaluj.
                     Sprawdź aktualizacje.


JAK URUCHOMIĆ
-------------
1. Kliknij dwukrotnie DiskCleanupHelper.exe.
2. System Windows zapyta: „Czy chcesz zezwolić tej aplikacji na wprowadzanie zmian na Twoim urządzeniu?”. Kliknij Tak.
   Jeśli klikniesz Nie, aplikacja i tak się otworzy, ale elementy wymagające uprawnień administratora będą wyszarzone.
3. Jeśli system Windows wyświetli komunikat „System Windows ochronił ten komputer”, kliknij „Więcej informacji”,
   a następnie „Uruchom mimo to”.
   Może się on pojawiać w przypadku nowych programów, których firma Microsoft jeszcze nie zna.
4. Język: kliknij przycisk języka (globus) w prawym górnym rogu i wybierz język. Okno od razu przełączy się
   na ten język, a aplikacja zapamięta wybór.
5. Jasny lub ciemny: kliknij przycisk „Tryb ciemny” / „Tryb jasny” w prawym górnym rogu. Aplikacja zapamięta wybór.
6. Aby ją zachować: otwórz kartę Informacje i kliknij „Zainstaluj na tym komputerze”.
   Pojawi się wtedy w menu Start, na pulpicie oraz w Ustawienia > Aplikacje.
   Do instalacji nie są potrzebne uprawnienia administratora.


JAK UŻYWAĆ
----------
Karta Oczyszczanie:
1. Zamknij niepotrzebne programy, zwłaszcza przeglądarki, komunikatory, edytory kodu i gry.
2. Rozmiary są mierzone automatycznie po otwarciu aplikacji. Trwa to minutę lub dwie.
   Kliknij „Zatrzymaj”, aby przerwać, lub „Zmierz rozmiary”, aby zmierzyć ponownie.
3. Każda grupa pokazuje swój łączny rozmiar. Kliknij tytuł grupy, aby ją zwinąć lub ponownie rozwinąć;
   aplikacja zapamiętuje, które grupy zostały zwinięte.
4. Kliknij dowolny element, aby przeczytać, co to jest, gdzie się znajduje oraz co zachowasz, a co stracisz.
5. Zaznacz pole każdego elementu, który chcesz wyczyścić, lub kliknij „Zaznacz bezpieczne”.
   Tytuł każdej grupy pokazuje, ile jej elementów jest zaznaczonych.
6. Kliknij „Wyczyść zaznaczone”. Aplikacja pokaże dokładną listę i zapyta, zanim cokolwiek usunie.

Karta Analiza dysku:
1. Wybierz dysk (lub „Tylko mój folder użytkownika”) i kliknij „Skanuj”. Cały dysk może zająć kilka minut.
2. Lewa strona: największe foldery. Kliknij dwukrotnie folder, aby zajrzeć do środka, kliknij „W górę”, aby wrócić.
3. Prawa strona: największe pliki. Zaznacz pliki i wybierz „Przenieś do Kosza” lub „Usuń trwale”.
   Pliki systemu Windows i programów są chronione i nie można ich tu usunąć.

Karta Kondycja dysku:
- Pokazuje każdy dysk (z temperaturą, zużyciem i liczbą błędów, gdy aplikacja działa z uprawnieniami administratora)
  oraz stopień zapełnienia każdego woluminu.
- Temperatura odświeża się sama co kilka sekund, gdy karta jest otwarta.
- Wskaż wykres historii, aby zobaczyć dokładną wartość i czas jej zapisania.
- „Szukaj błędów” uruchamia wbudowane sprawdzanie dysku systemu Windows (tylko odczyt).
- „Optymalizuj dyski” otwiera wbudowane narzędzie optymalizacji dysków systemu Windows.

Karta Wydajność dysku:
- Pokazuje, jak bardzo wybrany dysk jest obciążony w tej chwili, z wykresami z ostatnich dwóch minut.
  Wskaż wykres, aby zobaczyć dokładne wartości.
- „Uruchom test szybkości” mierzy, jak szybko dysk naprawdę zapisuje i odczytuje. Wynik jest zapisywany,
  dzięki czemu można go później porównać.


CO OZNACZAJĄ GRUPY OCZYSZCZANIA
-------------------------------
Bezpieczne     Pamięci podręczne i pliki tymczasowe. Programy same je odtwarzają.
               Czyszczenie pamięci podręcznej przeglądarek i aplikacji zachowuje hasła, zakładki, historię
               i sesje logowania.
Admin          Stare pliki aktualizacji zainstalowanych programów (zachowuje 3 najnowsze dla każdego)
               oraz wbudowane oczyszczanie składników systemu Windows firmy Microsoft.
Twoja decyzja  Tracisz coś drobnego: starą historię projektów, pobrane utwory,
               podglądy wideo, pliki zrzutu awaryjnego lub zawartość Kosza.
Ustawienie     Przełączniki systemu Windows, które zwalniają miejsce: hibernacja, Magazyn zarezerwowany,
               CompactOS i Czujnik pamięci.
Tylko info     Duże elementy, które narzędzie tylko pokazuje i otwiera, nigdy nie usuwa: poprzednia
               instalacja systemu Windows, największe programy, Przywracanie systemu, Docker, WSL, Pobrane.

Pokazane są tylko elementy, które istnieją na komputerze.


BEZPIECZEŃSTWO
--------------
- Nic nie zostanie usunięte, dopóki tego nie zaznaczysz i nie potwierdzisz.
- Pliki usunięte na karcie Oczyszczanie nie trafiają do Kosza i nie można ich przywrócić.
- Karta Oczyszczanie nigdy nie narusza dokumentów, zdjęć, muzyki, filmów, pulpitu ani pobranych plików.
- Prosi o zamknięcie przeglądarki lub aplikacji przed wyczyszczeniem jej pamięci podręcznej.
- Ostrzega, jeśli przed mierzeniem, skanowaniem lub czyszczeniem w komputerze brakuje pamięci.
- Nigdy nie przechodzi przez łącza folderów do innych folderów.
- Nigdy nie usuwaj ręcznie plików z folderów Windows, WinSxS ani Installer.


HISTORIA
--------
To, co wyczyszczono, jest zapisywane tylko na tym komputerze, w pliku:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Otwórz go z karty Informacje.


AKTUALIZACJE
------------
Przy uruchomieniu aplikacja sprawdza na swojej stronie w serwisie GitHub, czy jest nowa wersja (najwyżej raz dziennie).
Nowa wersja jest pobierana w tle i sprawdzana (SHA256 i numer wersji), zanim zostanie użyta.
Następnie pojawia się pasek z przyciskiem „Uruchom ponownie teraz”. Jeśli wybierzesz „Później”, aktualizacja zostanie zainstalowana po zamknięciu aplikacji.
Aby wyłączyć automatyczne sprawdzanie, usuń zaznaczenie tej opcji na karcie Informacje. Przycisk „Sprawdź aktualizacje” nadal tam działa.
Żadne informacje o Twoim komputerze nie są wysyłane.


ODINSTALOWANIE
--------------
Ustawienia > Aplikacje > Disk Cleanup Helper > Odinstaluj lub kliknij „Odinstaluj” na karcie Informacje.
Jeśli aplikacja nie była instalowana, po prostu usuń plik DiskCleanupHelper.exe.


COFANIE USTAWIENIA SYSTEMU WINDOWS
----------------------------------
Otwórz Wiersz polecenia jako administrator i uruchom potrzebny wiersz:
    powercfg /h on                                          (ponowne włączenie hibernacji)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (ponowne włączenie Magazynu zarezerwowanego)
    compact.exe /compactos:never                            (zatrzymanie kompresji plików systemu Windows)


WYMAGANIA
---------
Windows 10 lub Windows 11. Nic więcej nie trzeba instalować: aplikacja używa platformy .NET Framework
wbudowanej w system Windows.


LICENCJA I AUTOR
----------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Wolne i otwarte oprogramowanie: możesz z niego korzystać, zmieniać je i udostępniać na licencji GNU General Public License v3
z dodatkowymi warunkami (sekcja 7): zobacz pliki LICENSE.txt i NOTICE.txt w pobranym pakiecie lub „Licencja i warunki” na karcie Informacje.
Kod źródłowy: https://github.com/Philip-Droubi/disk-cleanup-helper
Kontakt: https://philip-droubi.github.io/Philip_Droubi_portfolio/ i https://www.linkedin.com/in/philip-droubi/


ZASTRZEŻENIA
------------
Ten program jest udostępniany w stanie, w jakim jest, bez jakiejkolwiek gwarancji. Używasz go na własne ryzyko.
Przed czyszczeniem dowolnego komputera upewnij się, że masz kopię zapasową ważnych plików.
