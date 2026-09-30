# Réalisation et critères de sortie

## État de cette livraison

Réalisé : recherche documentaire, conception, trois maquettes puis variante 01 simplifiée. Premier code : cœur C#, stockage JSON validé avec sauvegarde, rejeu synthétique, modèle de présentation, shell WPF et CI. Sept scénarios exécutés sous Linux passent. Compilation WPF bloquée par le refus réseau NuGet ; aucun lancement Windows ni essai en jeu. SQLite, bridge, installations et publication réelle restent à réaliser.

Le choix visuel est acquis : 01 simplifiée sans sidebar. Le README décrit le périmètre exact du premier incrément ; les lots ci-dessous décrivent toujours la cible complète. Le nom du produit et le certificat de signature ne bloquent pas le domaine ni le rejeu.

## Lots ordonnés

| Lot | Dépendances | Livrable concret | Critères de sortie |
|---|---|---|---|
| A — Socle | .NET 10 SDK, stack retenue | Solution C#, Domain/Application/Infrastructure, tests et CI Linux/Windows | Build domaine Linux et solution Windows ; aucune référence WPF dans Domain |
| B — Profils et widgets | A, maquette retenue pour l’UI | Shell WPF, pages, grille, bibliothèque, duplication indépendante, persistance SQLite | Scénarios 3–6, redémarrage, sauvegarde échouée visible, navigation clavier |
| C — Rejeu et métriques | A, définition commune dégâts | Fixtures synthétiques, moteur déterministe, widget dégâts, qualité/déconnexion | Résultats attendus exacts ; null distinct de zéro ; trou et coupure marquent partiel |
| D — Inventaire et transactions | A, documentation chargement à jour | Détection GW2, planificateur, staging, propriété et journal ; tests sur faux dossier | Scénarios 2 et 9, hash concurrent modifié, crash injecté à chaque étape ; aucune DLL inconnue écrasée |
| E — Bridge natif | C, ABI officielle ArcDPS, MSVC x64 | DLL minimale, queue bornée, pipe, handshake, détection de pertes | Scénarios 7–8 ; stress sans UI, arrêt processus, saturation, reconnexion et ABI validés |
| F — Installation réelle | D + E, topologie certifiée | ArcDPS + bridge sur GW2 propre puis coexistences | Scénario 1, aucun déplacement DLL manuel sur configurations officiellement prises en charge |
| G — Logs et sessions | A, règles de formats | Watcher + rescan, stabilité/validation, SHA-256, index, sessions et exclusions | Fichier en écriture, faux zip, doublon, déplacement, job interrompu et métadonnées absentes |
| H — Publication WvW Insights | G, contrat API officiel relu | Coffre DPAPI, revue de lot, upload, reprise de jobs et rapports | Scénario 10 ; fake HTTP puis petit lot réel consenti, aucun POST répété après timeout ambigu |
| I — Windows et distribution | B + F + H | Multi-écran, DPI, paquet autonome, diagnostic et guide d’installation | Scénario 12 ; installation sur Windows propre sans SDK ; budgets mesurés |
| J — P1 | P0 éprouvée | Export/import complet, soins/avantages, résultats JSON et nouvelles intégrations | Scénario 11, secrets absents, schémas migrés, validation séparée de chaque capacité |

G/H peuvent avancer en parallèle de D/E/F au niveau du planning : WvW Insights est une branche de P0, pas un supplément facultatif. Cela ne suppose pas des agents de développement parallèles.

## Matrice d’acceptation du brief

Tous les scénarios ci-dessous sont **à exécuter**, aucun n’est déclaré réussi dans cette livraison.

| # | Scénario | Méthode / oracle | Lot |
|---|---|---|---|
| 1 | Configuration sans déplacement manuel de DLL | Machine Windows propre, dossier GW2 compatible, comparaison inventaire avant/après | F/I |
| 2 | Installation existante préservée | DLL inconnue sentinelle et chargeur externe ; hashes inchangés après refus | D/F |
| 3 | Disposition retrouvée au redémarrage | Créer profil/pages, déplacer/redimensionner/masquer, relancer, comparaison état | B |
| 4 | Filtres indépendants | Deux instances puis duplication profonde ; modifier groupe/colonnes sur une seule | B/C |
| 5 | Activation enrichit la bibliothèque | Capacités fournies par manifeste validé, pas par présence DLL seule | B |
| 6 | Désactivation conserve les réglages | Snapshot avant/après ; widget placeholder puis restauration identique | B |
| 7 | Déconnexion visible | Couper le pipe et attendre seuil ; lastReceived visible, combat partiel, pas de faux zéro | C/E |
| 8 | Dashboard absent sans blocage GW2 | Tuer l’application pendant une rafale ; mesurer callback p99 et réactivité jeu | E |
| 9 | Installation interrompue récupérable | Injection crash après chaque mutation, reprise puis hash final ; conflit tiers → diagnostic | D |
| 10 | Session admissible publiée et rapport retrouvé | Lot réel autorisé, suivi asynchrone, relance app, lien conservé ; tests réseau dégradé | H |
| 11 | Export sans secret | Placer valeurs sentinelles dans coffre/règles ; scanner export y compris settings inconnus | J |
| 12 | Écran absent, fenêtre accessible | Écran à coordonnées négatives, débranchement, DPI changé, titlebar visible | I |

## Vérifications supplémentaires

- Parser du pipe : fuzz des tailles, fragmentation, payload tronqué, type inconnu, version incompatible ; aucun buffer illimité.
- File native : stress multi-producteurs, consommateur lent, compte exact des pertes, absence de deadlock, arrêt propre hors loader lock.
- Métriques : dégâts directs/altérations séparés puis total, propriétaires d’invocations, cibles, sous-groupes inconnus, durée zéro, double comptage area/local.
- Données : migration SQLite interrompue, backup/restauration, disque plein, corruption, profil importé trop gros ou futur.
- Publication : lot < 2 fichiers distincts, limite exacte selon API, fichier modifié depuis validation, mode PvE/inconnu, 429, 5xx, timeout avant/après acceptation, expiration, jeton indisponible.
- Installations : démarrage GW2 entre contrôle et commit, fichier verrouillé, élévation refusée, antivirus, liens/réanalyses et dépendance partagée.
- UX : contraste, clavier et lecteurs d’écran, texte à 200 %, tableau rempli, source absente, panneau d’édition avec plusieurs pages.

## Mesures à collecter

Machine de référence à renseigner : version Windows, CPU, RAM, GPU, résolutions/DPI, build GW2, ArcDPS, bridge et chargeur. Enregistrer baseline GW2 sans bridge, GW2 avec bridge seul puis application connectée. Mesurer trois sessions répétées, idle/direct/rafale/parse local, RSS/private bytes, CPU total, p95/p99 callback, latence et pertes. Publier les conditions avec les résultats.

Configuration initiale : chronométrer du lancement au premier widget connecté, compter les actions et déplacements manuels de fichiers (cible 0 pour les topologies supportées). Exactitude : comparer mêmes joueurs/cibles/périodes et définitions aux fixtures puis logs ; les écarts dus à couverture/délai doivent être expliqués, pas masqués.

## Registre des points ouverts

| Point | Effet | Décision provisoire |
|---|---|---|
| API officielle ArcDPS et chargement | Bloque la DLL réelle et la recette d’installation | Rejeu synthétique disponible dans le premier incrément |
| API WvW Insights, limites/TTL/auth | Bloque le connecteur HTTP réel | Ports applicatifs et faux transport testables, pas d’endpoint deviné |
| Coexistence Nexus | Bloque automatisation sur cette topologie | Lecture seule et propriétaire externe par défaut |
| Données Healing Stats | Bloque widget soins partagés promis | P1, audit du protocole et cas « partage absent » |
| Règles ArenaNet | Bloque une affirmation de conformité vérifiée | Pas d’automatisation du jeu ; revue officielle avant distribution |
| Licence projet et signature | Affecte distribution publique | Ne pas imposer de licence ou publier de binaires sans décision projet |
| Nom et direction visuelle | Affecte identité/UI, pas les métriques | Libellé de travail ; sélectionner une maquette |
| Plusieurs fenêtres | Complexifie édition et cycle de vie | Une fenêtre P0, modèle extensible, décision P1 |

P0 ne sera pas annoncée « utilisable en jeu » tant que F, H et I n’auront pas leurs preuves. Une maquette, un build Windows ou un test de domaine ne suffisent pas à eux seuls.
