DISK CLEANUP HELPER
===================

Um assistente gratuito e de código aberto para Windows 10 e Windows 11.
Ele mostra o que ocupa seus discos e libera espaço com segurança.
É um único arquivo de programa pequeno. Não envia nada sobre o seu PC; só pergunta ao GitHub se existe uma nova versão.


O QUE ELE FAZ
-------------
Limpeza         Limpeza segura de caches de navegadores e aplicativos, arquivos temporários, arquivos de atualização antigos,
                restos do Windows e mais. Cada item explica o que você mantém e o que você perde.
Analisar disco  Mostra as maiores pastas (clique duas vezes para ver o conteúdo) e os maiores arquivos
                de qualquer unidade, para você descobrir o que ocupa seu espaço.
Saúde do disco  Mostra se cada disco e unidade está saudável e quão cheio está,
                com a temperatura ao vivo e gráficos de histórico, e inicia a verificação de erros do próprio Windows.
Detalhes SMART  Os dados completos de automonitoramento de cada disco.
Desempenho do disco  Atividade do disco ao vivo e um teste de velocidade.
Detalhes do dispositivo  Windows, processador, memória, gráficos, telas, armazenamento, bateria, rede
                         e som, como a Ferramenta de Diagnóstico do DirectX do Windows. Copie ou salve como arquivo de texto.
Dicas e FAQ     Hábitos simples que evitam que o disco encha e respostas para perguntas comuns.
Sobre           Instale-o no seu PC com uma entrada no menu Iniciar e um atalho na área de trabalho, ou desinstale-o.
                Verificar se há atualizações.


COMO INICIAR
------------
1. Clique duas vezes em DiskCleanupHelper.exe.
2. O Windows pergunta "Deseja permitir que este aplicativo faça alterações no seu dispositivo?". Clique em Sim.
   Se você clicar em Não, ele abre mesmo assim, mas os itens que precisam de direitos de administrador ficam cinza.
3. Se o Windows mostrar "O Windows protegeu o computador", clique em "Mais informações" e depois em "Executar assim mesmo".
   Isso pode aparecer para programas novos que ainda não são conhecidos pela Microsoft.
4. Idioma: clique no botão de idioma (o globo) no canto superior direito e escolha um idioma. A janela muda
   na hora para esse idioma e lembra sua escolha.
5. Claro ou escuro: clique no botão "Modo escuro" / "Modo claro" no canto superior direito. O aplicativo lembra sua escolha.
6. Para mantê-lo: abra a guia Sobre e clique em "Instalar neste PC".
   Ele passa a aparecer no menu Iniciar, na área de trabalho e em Configurações > Aplicativos.
   Não são necessários direitos de administrador para instalar.


COMO USAR
---------
Guia Limpeza:
1. Feche os programas de que você não precisa, principalmente navegadores, aplicativos de chat, editores de código e jogos.
2. O aplicativo mede os tamanhos sozinho quando abre. Leva um ou dois minutos. Clique em "Parar" para parar
   ou em "Medir tamanhos" para medir de novo.
3. Cada grupo mostra seu tamanho total. Clique no título de um grupo para recolhê-lo ou abri-lo de novo;
   o aplicativo lembra quais grupos você recolheu.
4. Clique em qualquer item para ler o que é, onde fica e o que você mantém ou perde.
5. Marque a caixa de cada item que você quer limpar ou clique em "Selecionar seguros".
   O título de cada grupo mostra quantos dos seus itens estão selecionados.
6. Clique em "Limpar selecionados". Ele mostra a lista exata e pergunta antes de excluir qualquer coisa.

Guia Analisar disco:
1. Escolha uma unidade (ou "Só minha pasta de usuário") e clique em "Verificar". Uma unidade inteira pode levar alguns minutos.
2. Lado esquerdo: as maiores pastas. Clique duas vezes em uma pasta para ver o conteúdo e clique em "Subir" para voltar.
3. Lado direito: os maiores arquivos. Selecione arquivos e escolha "Mover para a Lixeira" ou "Excluir permanentemente".
   Os arquivos do Windows e de programas estão protegidos e não podem ser excluídos aqui.

Guia Saúde do disco:
- Mostra cada disco (com temperatura, desgaste e contagem de erros quando iniciado com direitos de administrador)
  e quão cheia está cada unidade.
- A temperatura é atualizada sozinha a cada poucos segundos enquanto a guia está aberta.
- Passe o ponteiro sobre um gráfico de histórico para ver o valor exato e quando ele foi salvo.
- "Verificar erros" executa a verificação somente leitura do próprio Windows na unidade do Windows.
- "Otimizar unidades" abre o otimizador de unidades do próprio Windows.

Guia Desempenho do disco:
- Mostra o quanto o disco escolhido está ocupado agora, com gráficos dos últimos dois minutos.
  Passe o ponteiro sobre um gráfico para ver os valores exatos.
- "Testar velocidade" mede a velocidade real de gravação e leitura do disco. O resultado é salvo,
  para você poder comparar depois.


O QUE SIGNIFICAM OS GRUPOS DE LIMPEZA
-------------------------------------
Seguro         Caches e arquivos temporários. Os programas os recriam sozinhos.
               A limpeza de caches de navegadores e aplicativos mantém suas senhas, favoritos, histórico e logins.
Admin          Arquivos de atualização antigos de programas instalados (mantém os 3 mais novos de cada)
               e a limpeza de componentes do Windows da própria Microsoft.
Você decide    Você perde algo pequeno: histórico de projetos antigos, músicas baixadas,
               prévias de vídeo, arquivos de despejo de falhas ou o que está na Lixeira.
Configuração   Opções do Windows que liberam espaço: hibernação, Armazenamento reservado, CompactOS
               e Sensor de Armazenamento.
Só informação  Coisas grandes que a ferramenta só mostra e abre, nunca exclui: instalação anterior
               do Windows, maiores programas, Restauração do Sistema, Docker, WSL, Downloads.

Só são listados os itens que existem no seu PC.


SEGURANÇA
---------
- Nada é excluído até você selecionar e confirmar.
- Os arquivos excluídos pela guia Limpeza não vão para a Lixeira e não podem ser restaurados.
- A guia Limpeza nunca mexe nos seus documentos, fotos, músicas, vídeos, área de trabalho ou downloads.
- Ele pede que você feche o navegador ou aplicativo antes de limpar o cache dele.
- Ele avisa você se o PC estiver com pouca memória antes de medir, verificar ou limpar.
- Ele nunca segue links de pastas para outras pastas.
- Nunca exclua manualmente arquivos nas pastas Windows, WinSxS ou Installer.


HISTÓRICO
---------
O que foi limpo é salvo somente neste PC, em:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Abra-o pela guia Sobre.


ATUALIZAÇÕES
------------
Ao iniciar, o aplicativo pergunta à sua página no GitHub se existe uma nova versão (no máximo uma vez por dia).
Uma nova versão é baixada em segundo plano e verificada (SHA256 e número de versão) antes de ser usada.
Depois, uma barra oferece "Reiniciar agora". Se você escolher "Mais tarde", a atualização é instalada quando você fechar o aplicativo.
Para desativar a verificação automática, desmarque a caixa na guia Sobre. "Verificar se há atualizações" continua funcionando lá.
Nada sobre o seu PC é enviado.


DESINSTALAR
-----------
Configurações > Aplicativos > Disk Cleanup Helper > Desinstalar, ou clique em "Desinstalar" na guia Sobre.
Se você nunca o instalou, basta excluir DiskCleanupHelper.exe.


DESFAZER UMA CONFIGURAÇÃO DO WINDOWS
------------------------------------
Abra o Prompt de Comando como administrador e execute a linha de que você precisa:
    powercfg /h on                                          (reativa a hibernação)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (reativa o Armazenamento reservado)
    compact.exe /compactos:never                            (para de compactar os arquivos do Windows)


REQUISITOS
----------
Windows 10 ou Windows 11. Nada mais para instalar: ele usa o .NET Framework que já vem no Windows.


LICENÇA E AUTOR
---------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Software livre e de código aberto: você pode usá-lo, modificá-lo e compartilhá-lo sob a GNU General Public License v3
com termos adicionais (seção 7): veja LICENSE.txt e NOTICE.txt incluídos no download, ou "Licença e termos" na guia Sobre.
Código-fonte: https://github.com/Philip-Droubi/disk-cleanup-helper
Contato: https://philip-droubi.github.io/Philip_Droubi_portfolio/ e https://www.linkedin.com/in/philip-droubi/


AVISO LEGAL
-----------
Este programa é fornecido no estado em que se encontra, sem garantia de qualquer tipo. Use por sua conta e risco.
Certifique-se de que os arquivos importantes tenham backup antes de limpar qualquer computador.
