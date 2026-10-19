DISK CLEANUP HELPER
===================

Un aiuto gratuito e open source per Windows 10 e Windows 11.
Mostra cosa riempie i dischi e libera spazio in sicurezza.
È un unico piccolo file di programma. Non invia nulla riguardo al tuo PC; chiede solo a GitHub se esiste una nuova versione.


COSA FA
-------
Pulizia               Pulizia sicura di cache di browser e app, file temporanei, vecchi file di aggiornamento,
                      residui di Windows e altro. Ogni elemento spiega cosa conservi e cosa perdi.
Analizza disco        Mostra le cartelle più grandi (doppio clic per vederne il contenuto) e i file più grandi
                      di qualsiasi unità, così puoi trovare cosa occupa il tuo spazio.
Integrità disco       Mostra se ogni disco e unità è integro e quanto è pieno,
                      con la temperatura in tempo reale e grafici della cronologia, e avvia il controllo errori di Windows.
Dettagli SMART        I dati di automonitoraggio completi di ogni disco.
Prestazioni disco     Attività del disco in tempo reale e un test di velocità.
Dettagli dispositivo  Windows, processore, memoria, grafica, schermi, archiviazione, batteria, rete
                      e audio, come lo Strumento di diagnostica DirectX di Windows. Copiali o salvali come file di testo.
Suggerimenti e FAQ    Semplici abitudini per evitare che il disco si riempia, e risposte alle domande frequenti.
Informazioni          Installalo sul PC con una voce nel menu Start e un collegamento sul desktop, oppure disinstallalo.
                      Controlla aggiornamenti.


COME AVVIARLO
-------------
1. Fai doppio clic su DiskCleanupHelper.exe.
2. Windows chiede "Consentire a questa app di apportare modifiche al dispositivo?". Fai clic su Sì.
   Se fai clic su No, si apre comunque, ma gli elementi che richiedono i diritti di amministratore sono in grigio.
3. Se Windows mostra "PC protetto da Windows", fai clic su "Ulteriori informazioni", poi su "Esegui comunque".
   Può comparire per i programmi nuovi che Microsoft non conosce ancora.
4. Lingua: fai clic sul pulsante della lingua (il globo) in alto a destra e scegli una lingua. La finestra passa
   subito a quella lingua e ricorda la tua scelta.
5. Chiaro o scuro: fai clic sul pulsante "Modalità scura" / "Modalità chiara" in alto a destra. L'app ricorda la tua scelta.
6. Per tenerlo: apri la scheda Informazioni e fai clic su "Installa su questo PC".
   Comparirà nel menu Start, sul desktop e in Impostazioni > App.
   Per installarlo non servono diritti di amministratore.


COME SI USA
-----------
Scheda Pulizia:
1. Chiudi i programmi che non ti servono, soprattutto browser, app di chat, editor di codice e giochi.
2. Le dimensioni vengono misurate da sole quando apri l'app. Richiede un minuto o due. Fai clic su "Interrompi" per fermare la misurazione,
   oppure su "Misura dimensioni" per misurare di nuovo.
3. Ogni gruppo mostra la sua dimensione totale. Fai clic sul titolo di un gruppo per chiuderlo o riaprirlo;
   l'app ricorda quali gruppi hai chiuso.
4. Fai clic su un elemento per leggere cos'è, dov'è e cosa conservi o perdi.
5. Seleziona la casella di ogni elemento che vuoi pulire, oppure fai clic su "Seleziona tutti i sicuri".
   Il titolo di ogni gruppo mostra quanti dei suoi elementi sono selezionati.
6. Fai clic su "Pulisci selezionati". Mostra l'elenco esatto e chiede conferma prima di eliminare qualsiasi cosa.

Scheda Analizza disco:
1. Scegli un'unità (o "Solo la mia cartella utente") e fai clic su "Scansiona". Un'intera unità può richiedere qualche minuto.
2. Lato sinistro: le cartelle più grandi. Fai doppio clic su una cartella per vederne il contenuto, fai clic su "Su" per tornare indietro.
3. Lato destro: i file più grandi. Seleziona i file e scegli "Sposta nel Cestino" o "Elimina definitivamente".
   I file di Windows e dei programmi sono protetti e non possono essere eliminati qui.

Scheda Integrità disco:
- Mostra ogni disco (con temperatura, usura e numero di errori se avviato con i diritti di amministratore)
  e quanto è piena ogni unità.
- La temperatura si aggiorna da sola ogni pochi secondi mentre la scheda è aperta.
- Passa il mouse su un grafico della cronologia per vedere il valore esatto e quando è stato salvato.
- "Cerca errori" esegue il controllo di sola lettura di Windows sull'unità di Windows.
- "Ottimizza unità" apre lo strumento di ottimizzazione unità di Windows.

Scheda Prestazioni disco:
- Mostra quanto è occupato il disco scelto in questo momento, con i grafici degli ultimi due minuti.
  Passa il mouse su un grafico per vedere i valori esatti.
- "Esegui test velocità" misura quanto velocemente il disco può davvero scrivere e leggere. Il risultato viene salvato,
  così puoi confrontarlo in seguito.


COSA SIGNIFICANO I GRUPPI DI PULIZIA
------------------------------------
Sicuro        Cache e file temporanei. I programmi li ricreano da soli.
              Le cache di browser e app conservano password, segnalibri, cronologia e accessi.
Admin         Vecchi file di aggiornamento dei programmi installati (tiene i 3 più recenti di ciascuno),
              e la pulizia dei componenti di Windows di Microsoft.
A tua scelta  Perdi qualcosa di piccolo: vecchia cronologia dei progetti, brani scaricati,
              anteprime video, file di dump di arresto anomalo, o ciò che è nel Cestino.
Impostazione  Opzioni di Windows che liberano spazio: ibernazione, Archiviazione riservata, CompactOS
              e Sensore memoria.
Solo info     Elementi grandi che lo strumento mostra e apre soltanto, senza mai eliminarli: installazione
              precedente di Windows, programmi più grandi, Ripristino configurazione di sistema, Docker, WSL, Download.

Sono elencati solo gli elementi presenti sul tuo PC.


SICUREZZA
---------
- Nulla viene eliminato finché non lo selezioni e confermi.
- I file eliminati dalla scheda Pulizia non finiscono nel Cestino e non possono essere ripristinati.
- La scheda Pulizia non tocca mai documenti, foto, musica, video, desktop o download.
- Ti chiede di chiudere un browser o un'app prima di pulirne la cache.
- Ti avvisa se il PC ha poca memoria prima di misurare, scansionare o pulire.
- Non segue mai i collegamenti a cartelle verso altre cartelle.
- Non eliminare mai a mano i file nelle cartelle Windows, WinSxS o Installer.


CRONOLOGIA
----------
Ciò che è stato pulito viene salvato solo su questo PC, in:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Aprilo dalla scheda Informazioni.


AGGIORNAMENTI
-------------
All'avvio, l'app chiede alla sua pagina GitHub se esiste una nuova versione (al massimo una volta al giorno).
Una nuova versione viene scaricata in background e controllata (SHA256 e numero di versione) prima di essere usata.
Poi una barra propone "Riavvia ora". Se scegli "Più tardi", l'aggiornamento viene installato quando chiudi l'app.
Per disattivare il controllo automatico, deseleziona la casella nella scheda Informazioni. "Controlla aggiornamenti" lì funziona comunque.
Non viene inviato nulla riguardo al tuo PC.


DISINSTALLAZIONE
----------------
Impostazioni > App > Disk Cleanup Helper > Disinstalla, oppure fai clic su "Disinstalla" nella scheda Informazioni.
Se non l'hai mai installato, elimina semplicemente DiskCleanupHelper.exe.


ANNULLARE UN'IMPOSTAZIONE DI WINDOWS
------------------------------------
Apri il Prompt dei comandi come amministratore ed esegui la riga che ti serve:
    powercfg /h on                                          (riattiva l'ibernazione)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (riattiva Archiviazione riservata)
    compact.exe /compactos:never                            (smette di comprimere i file di Windows)


REQUISITI
---------
Windows 10 o Windows 11. Nient'altro da installare: usa il .NET Framework integrato in Windows.


LICENZA E AUTORE
----------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Software libero e open source: puoi usarlo, modificarlo e condividerlo sotto la GNU General Public License v3
con condizioni aggiuntive (sezione 7): vedi LICENSE.txt e NOTICE.txt inclusi nel download, oppure "Licenza e condizioni" nella scheda Informazioni.
Codice sorgente: https://github.com/Philip-Droubi/disk-cleanup-helper
Contatti: https://philip-droubi.github.io/Philip_Droubi_portfolio/ e https://www.linkedin.com/in/philip-droubi/


ESCLUSIONE DI RESPONSABILITÀ
----------------------------
Questo programma è fornito così com'è, senza garanzie di alcun tipo. Usalo a tuo rischio.
Assicurati di avere un backup dei file importanti prima di pulire qualsiasi computer.
