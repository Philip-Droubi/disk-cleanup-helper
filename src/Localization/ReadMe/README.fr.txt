DISK CLEANUP HELPER
===================

Un assistant gratuit et open source pour Windows 10 et Windows 11.
Il montre ce qui remplit vos disques et libère de l'espace en toute sécurité.
C'est un seul petit fichier programme. Il n'envoie rien concernant votre PC ; il demande seulement à GitHub si une nouvelle version existe.


CE QU'IL FAIT
-------------
Nettoyage     Nettoyage sans risque des caches des navigateurs et des applications, des fichiers temporaires,
              des anciens fichiers de mise à jour, des restes de Windows et plus encore.
              Chaque élément explique ce que vous gardez et ce que vous perdez.
Analyser le disque  Affiche les plus gros dossiers (double-clic pour voir leur contenu) et les plus gros
              fichiers de n'importe quel lecteur, pour trouver ce qui prend votre espace.
Santé du disque  Indique si chaque disque et chaque lecteur est en bon état et à quel point il est plein,
              avec la température en direct et des graphiques d'historique, et lance la recherche d'erreurs de Windows.
Détails SMART Toutes les données d'autosurveillance de chaque disque.
Performances du disque  Activité du disque en direct et un test de vitesse.
Détails de l'appareil   Windows, processeur, mémoire, graphiques, écrans, stockage, batterie, réseau
                  et son, comme l'Outil de diagnostic DirectX de Windows. Copiez-les ou enregistrez-les en fichier texte.
Conseils et FAQ  De bonnes habitudes simples pour éviter que votre disque se remplisse, et des réponses aux questions fréquentes.
À propos      Installez-le sur votre PC avec une entrée dans le menu Démarrer et un raccourci sur le bureau, ou désinstallez-le.
              Rechercher des mises à jour.


POUR COMMENCER
--------------
1. Double-cliquez sur DiskCleanupHelper.exe.
2. Windows demande « Voulez-vous autoriser cette application à apporter des modifications à votre appareil ? ». Cliquez sur Oui.
   Si vous cliquez sur Non, il s'ouvre quand même, mais les éléments qui nécessitent les droits d'administrateur sont grisés.
3. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur « Informations complémentaires », puis sur « Exécuter quand même ».
   Cela peut apparaître pour les nouveaux programmes qui ne sont pas encore connus de Microsoft.
4. Langue : cliquez sur le bouton de langue (le globe) en haut à droite et choisissez une langue. La fenêtre passe
   tout de suite à cette langue et retient votre choix.
5. Clair ou sombre : cliquez sur le bouton « Mode sombre » / « Mode clair » en haut à droite. L'application retient votre choix.
6. Pour le garder : ouvrez l'onglet À propos et cliquez sur « Installer sur ce PC ».
   Il apparaît alors dans le menu Démarrer, sur le bureau et dans Paramètres > Applications.
   Aucun droit d'administrateur n'est nécessaire pour l'installer.


UTILISATION
-----------
Onglet Nettoyage :
1. Fermez les programmes dont vous n'avez pas besoin, surtout les navigateurs, les applications de discussion, les éditeurs de code et les jeux.
2. Les tailles sont mesurées automatiquement à l'ouverture de l'application. Cela prend une minute ou deux. Cliquez sur « Arrêter » pour arrêter,
   ou sur « Mesurer les tailles » pour mesurer à nouveau.
3. Chaque groupe affiche sa taille totale. Cliquez sur le titre d'un groupe pour le replier ou le rouvrir ;
   l'application retient les groupes que vous avez repliés.
4. Cliquez sur un élément pour lire ce que c'est, où il se trouve, et ce que vous gardez ou perdez.
5. Cochez la case de chaque élément que vous voulez nettoyer, ou cliquez sur « Sélectionner les sûrs ».
   Le titre de chaque groupe indique combien de ses éléments sont sélectionnés.
6. Cliquez sur « Nettoyer la sélection ». La liste exacte s'affiche et une confirmation est demandée avant toute suppression.

Onglet Analyser le disque :
1. Choisissez un lecteur (ou « Mon dossier utilisateur seulement ») et cliquez sur « Analyser ». Un lecteur entier peut prendre quelques minutes.
2. À gauche : les plus gros dossiers. Double-cliquez sur un dossier pour voir son contenu, cliquez sur « Remonter » pour revenir.
3. À droite : les plus gros fichiers. Sélectionnez des fichiers et choisissez « Déplacer vers la Corbeille » ou « Supprimer définitivement ».
   Les fichiers Windows et des programmes sont protégés et ne peuvent pas être supprimés ici.

Onglet Santé du disque :
- Affiche chaque disque (avec la température, l'usure et le nombre d'erreurs quand l'outil est lancé avec les droits d'administrateur)
  et le taux de remplissage de chaque lecteur.
- La température est mise à jour automatiquement toutes les quelques secondes tant que l'onglet est ouvert.
- Pointez sur un graphique d'historique pour voir la valeur exacte et le moment où elle a été enregistrée.
- « Rechercher des erreurs » lance la vérification en lecture seule de Windows sur le lecteur Windows.
- « Optimiser les lecteurs » ouvre l'outil d'optimisation des lecteurs de Windows.

Onglet Performances du disque :
- Indique à quel point le disque choisi est occupé en ce moment, avec des graphiques des deux dernières minutes.
  Pointez sur un graphique pour voir les valeurs exactes.
- « Lancer le test de vitesse » mesure à quelle vitesse le disque peut vraiment écrire et lire. Le résultat est enregistré
  pour que vous puissiez comparer plus tard.


SIGNIFICATION DES GROUPES DE NETTOYAGE
--------------------------------------
Sûr           Caches et fichiers temporaires. Les programmes les recréent d'eux-mêmes.
              Le nettoyage des caches des navigateurs et des applications garde vos mots de passe, favoris, historique et connexions.
Admin         Anciens fichiers de mise à jour des programmes installés (garde les 3 plus récents de chacun),
              et le nettoyage des composants Windows propre à Microsoft.
À vous de voir  Vous perdez quelque chose de mineur : ancien historique de projets, titres téléchargés,
              aperçus vidéo, fichiers de vidage sur incident, ou ce qui est dans la Corbeille.
Paramètre     Réglages Windows qui libèrent de l'espace : veille prolongée, Stockage réservé, CompactOS
              et Assistant Stockage.
Info seulement  Gros éléments que l'outil affiche et ouvre seulement, sans jamais les supprimer : installation précédente
              de Windows, plus gros programmes, Restauration du système, Docker, WSL, Téléchargements.

Seuls les éléments présents sur votre PC sont listés.


SÉCURITÉ
--------
- Rien n'est supprimé tant que vous ne l'avez pas sélectionné et confirmé.
- Les fichiers supprimés par l'onglet Nettoyage ne vont pas dans la Corbeille et ne peuvent pas être restaurés.
- L'onglet Nettoyage ne touche jamais à vos documents, photos, musiques, vidéos, bureau ou téléchargements.
- Il vous demande de fermer un navigateur ou une application avant de nettoyer son cache.
- Il vous prévient si le PC manque de mémoire avant de mesurer, d'analyser ou de nettoyer.
- Il ne suit jamais les liens de dossier vers d'autres dossiers.
- Ne supprimez jamais à la main des fichiers dans les dossiers Windows, WinSxS ou Installer.


HISTORIQUE
----------
Ce qui a été nettoyé est enregistré sur ce PC uniquement, dans :
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Ouvrez-le depuis l'onglet À propos.


MISES À JOUR
------------
Au démarrage, l'application demande à sa page GitHub si une nouvelle version existe (au plus une fois par jour).
Une nouvelle version est téléchargée en arrière-plan et vérifiée (SHA256 et numéro de version) avant d'être utilisée.
Une barre propose ensuite « Redémarrer maintenant ». Si vous choisissez « Plus tard », la mise à jour est installée quand vous fermez l'application.
Pour désactiver la vérification automatique, décochez-la dans l'onglet À propos. « Rechercher des mises à jour » y fonctionne toujours.
Rien concernant votre PC n'est envoyé.


DÉSINSTALLATION
---------------
Paramètres > Applications > Disk Cleanup Helper > Désinstaller, ou cliquez sur « Désinstaller » dans l'onglet À propos.
Si vous ne l'avez jamais installé, supprimez simplement DiskCleanupHelper.exe.


ANNULER UN PARAMÈTRE WINDOWS
----------------------------
Ouvrez l'Invite de commandes en tant qu'administrateur et exécutez la ligne dont vous avez besoin :
    powercfg /h on                                          (réactive la veille prolongée)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (réactive le Stockage réservé)
    compact.exe /compactos:never                            (arrête de compresser les fichiers Windows)


CONFIGURATION REQUISE
---------------------
Windows 10 ou Windows 11. Rien d'autre à installer : il utilise le .NET Framework intégré à Windows.


LICENCE ET AUTEUR
-----------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Logiciel libre et open source : vous pouvez l'utiliser, le modifier et le partager sous la GNU General Public License v3
avec des conditions supplémentaires (section 7) : voir LICENSE.txt et NOTICE.txt inclus dans le téléchargement, ou « Licence et conditions » dans l'onglet À propos.
Code source : https://github.com/Philip-Droubi/disk-cleanup-helper
Contact : https://philip-droubi.github.io/Philip_Droubi_portfolio/ et https://www.linkedin.com/in/philip-droubi/


LIMITATION DE RESPONSABILITÉ
----------------------------
Ce programme est fourni tel quel, sans garantie d'aucune sorte. Utilisez-le à vos propres risques.
Assurez-vous que les fichiers importants sont sauvegardés avant de nettoyer un ordinateur.
