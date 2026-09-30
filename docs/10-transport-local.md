# Transport local : premier incrément implémenté

## Ce qui fonctionne et ce qui manque

Implémenté : récepteur Named Pipe asynchrone, framing borné, validation des messages, sessions, agrégation incrémentale, qualité des données et simulateur séparé. Le dashboard WPF est raccordé au récepteur et lit un snapshot au plus quatre fois par seconde. Les tests de transport utilisent de vrais pipes sous Linux ; WPF et les permissions des pipes sous Windows restent à éprouver.

**Ce n’est pas la DLL ArcDPS.** Le simulateur est un programme console .NET envoyant des dégâts fictifs. Aucun callback du jeu n’est branché ; aucune ABI ArcDPS n’est implémentée ; aucune DLL ni aucun fichier du jeu n’est modifié. Le programme de simulation peut attendre l’écriture du pipe : il ne démontre donc pas encore la propriété non bloquante requise pour les callbacks natifs du futur bridge.

## Essai Windows

Depuis la racine du dépôt, dans deux terminaux avec le SDK .NET 10 :

```powershell
dotnet run --project src/Companion.Desktop
dotnet run --project tools/Companion.BridgeSimulator
```

Dans l’application, ouvrir Intégrations et cocher **Utiliser le simulateur local à la place du rejeu intégré**, puis revenir au dashboard. Le combat fictif dure dix secondes. Les tableaux restent visibles après sa fin et indiquent la fermeture du canal. La source choisie pour ce test n’est pas persistée ; au prochain démarrage, l’application revient à son réglage de rejeu intégré.

Si les artefacts CI sont disponibles, extraire entièrement les archives application et simulateur dans deux dossiers distincts. Ouvrir l’application puis `Companion.BridgeSimulator.exe` ; aucun SDK requis pour ces paquets autonomes. Les deux programmes doivent fonctionner sous le même utilisateur.

```powershell
# Omettre un événement et annoncer sa perte
dotnet run --project tools/Companion.BridgeSimulator -- --drop

# Fermer la connexion au milieu du combat
dotnet run --project tools/Companion.BridgeSimulator -- --interrupt

# Garder le processus vivant mais cesser d’envoyer des messages
dotnet run --project tools/Companion.BridgeSimulator -- --stall
```

Résultat attendu : dernières valeurs conservées, données partielles signalées, heure de réception visible dans chaque widget. Une coupure peut rendre le combat incomplet même lorsque le compteur de pertes explicites vaut zéro. Chaque nouveau lancement du simulateur produit une nouvelle session et ne cumule pas les anciens dégâts. Ctrl+C arrête la simulation ; l’absence de l’application produit une erreur lisible après cinq secondes.

## Protocole expérimental de simulation v1

Ce format constitue un outil de développement séparé de la proposition native de [04-bridge.md](04-bridge.md), qui n’est pas encore figée. Ne pas considérer ce format comme l’ABI ArcDPS ni un contrat de production accepté par une DLL.

- Canal `gw2-companion-simulator-<hash utilisateur>`, limité à l’utilisateur courant par `PipeOptions.CurrentUserOnly`. Aucun serveur HTTP ou socket TCP applicatif. Le refus des clients Windows distants et la vérification du PID du futur processus GW2 restent à implémenter pour le transport natif ; aucune garantie d’authenticité du jeu n’est revendiquée ici.
- Prefixe de longueur 4 octets little-endian, puis objet JSON UTF-8 ; longueur de corps de 2 à 65 536 octets. Les lectures peuvent être fragmentées. Longueur invalide rejetée avant allocation du corps.
- Champs : `Version`, `SessionId`, `Sequence`, `Kind`, `TimeMs`, `Player`, `Subgroup`, `Damage`, `DroppedEvents`, `Synthetic`. Les quatre premiers paramètres sont requis ; les valeurs par défaut des autres sont celles du record `BridgeFrame`.
- Version supportée : 1. `Kind` numérique : Hello=0, Damage=1, Heartbeat=2, End=3. Types/champs inconnus refusés. `Synthetic=false` refusé : toute source acceptée reste explicitement présentée comme fictive.
- Première frame : Hello avec session UUID non vide, séquence 0 et temps 0. Flux unidirectionnel : validation de version, mais pas encore de HelloAck ni de négociation de capacités.
- Frames suivantes : même session, séquence positive, temps relatif de 0 à 24 h, pertes cumulées croissantes. Damage contient un nom borné à 128 caractères, un sous-groupe nullable 1–15 et des dégâts positifs ou nuls. Les noms servent d’identité **uniquement dans cette simulation** ; le vrai bridge nécessitera des IDs d’agents stables et les propriétaires d’invocations.
- Séquence dupliquée ignorée ; séquence rétrograde ou trou marque le combat partiel. Perte explicite, coupure ou timeout en combat marque également partiel. Les nombres invalides, horloges rétrogrades, sessions différentes et débordements sont refusés.
- L’agrégateur conserve au maximum 512 couples joueur/sous-groupe, sans conserver une liste illimitée d’événements. DPS = dégâts / temps écoulé déclaré ; durée nulle → valeur null.
- End clôt le combat ; une fermeture après End ne le rend pas artificiellement incomplet. Reconnexion à la même session conserve compteurs et qualité ; une nouvelle session remet l’agrégateur à neuf. Il n’y a pas encore d’historique des anciennes sessions.
- Timeout de trois secondes par frame complète, handshake compris. Client bloqué ou silencieux : fermeture de sa connexion, statut périmé et nouvelle écoute. L’annulation arrête aussi bien l’attente d’un client que la lecture en cours.

## Validation exécutée

17 scénarios du domaine, du modèle de présentation et du transport passent sous Linux. Couverture supplémentaire : fragmentation/concaténation, EOF, taille excessive, JSON invalide, mauvaise version, source non synthétique, agrégation bornée, doublons/pertes, sessions, pipe réel, timeout, rejet du handshake, reconnexion, arrêt annulable et affichage de la qualité dans le modèle du dashboard.

Compilation du simulateur et des tests réussie avec zéro avertissement. Aucune compilation/régression visuelle Windows revendiquée ; l’environnement n’a pas les références WPF disponibles. La prochaine étape native reste l’implémentation et l’essai de la queue C++ et de l’adaptateur callbacks ArcDPS, après vérification de l’ABI officielle.
