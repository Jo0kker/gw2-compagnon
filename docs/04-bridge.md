# Contrat du bridge — proposition v0

Ce document décrit **notre protocole**, pas l’ABI officielle ArcDPS. L’ABI exacte, ses structures, conventions d’appel, callbacks concurrents et règles de chargement devront être relues et figées avant de compiler une DLL. Aucun pointeur brut du jeu ne traverse le transport.

## Responsabilités et cycle de vie

Bridge C++ x64 chargé par ArcDPS. Initialisation légère ; travail de connexion et transport sur un worker créé hors des contraintes du loader Windows. Copier les champs et chaînes utiles pendant le callback. Pas de disque, réseau externe, attente de l’UI, allocation non bornée ou calcul de statistiques dans les callbacks. Exceptions contenues à la frontière ABI.

Pipe proposé : `\\.\pipe\GW2Companion.<user-id-hash>.<game-pid>` ; nom définitif après choix du produit. Serveur côté application, client côté bridge. L’application détecte le PID GW2 et crée le pipe ACL CurrentUser + local uniquement ; confirmer les scénarios jeu élevé/application non élevée avant de les déclarer supportés.

## Encodage proposé

Framing little-endian : préfixe longueur u32 (maximum 64 Kio), magic `GWCP`, version majeure/mineure u16, type u16, taille d’en-tête u16, session UUID 16 octets, numéro de séquence u64, temps monotone µs u64, corps sérialisé champ par champ selon le type. Jamais de `memcpy` d’une structure C++ entière : padding/ABI non portables. Un décodeur incrémental gère lectures partielles, plusieurs frames et fin de flux.

| Message | Contenu |
|---|---|
| Hello | Version bridge, build ArcDPS/GW2 si connu, PID, session UUID, capacités, fréquence du compteur monotone |
| HelloAck | Version retenue, capacités acceptées, limites ; version majeure incompatible → fermeture expliquée |
| AgentUpsert | Identifiant stable dans la session, nom copié, catégorie, profession/sous-groupe si connus, provenance |
| CombatRaw | Canal area/local, id et revision ArcDPS, champs scalaires explicitement documentés, agents copiés/référencés |
| CombatBoundary | Entrée/sortie ou marqueur disponible ; une heuristique application sera étiquetée comme telle |
| Health | Séquence produite, nombre cumulé de pertes, occupation file, version, horloge |
| SessionEnd | Raison de fin si arrêt propre ; l’absence de ce message reste possible |

Réserver des types et négocier les capacités optionnelles. Major inconnue : aucun décodage. Minor avec champs additionnels : ignorer seulement les champs explicitement extensibles et bornés. L’application ne demande jamais une action de gameplay au bridge.

## File bornée et saturation

Proposition : queue MPSC préallouée de 8 192 slots × maximum 1 Kio = environ 8 Mio, worker consommateur unique. Vérifier la concurrence réelle des callbacks. Chaînes UTF-8 bornées avec indicateur de troncature ; agent invalide/donnée trop grande → perte signalée, pas de lecture mémoire prolongée.

La séquence est attribuée dans l’ordre de réservation de la queue, avant admission, y compris aux événements abandonnés ; le worker émet les slots admis dans cet ordre. Éviter l’attribution de séquences par des producteurs indépendants sans ordre de publication : cela produirait de faux trous. La conception MPSC et son ordering nécessitent tests de concurrence et benchmark.

Saturation : **abandonner le nouvel événement**, incrémenter un compteur atomique, retourner immédiatement. Les métadonnées Health sont construites par le worker depuis des compteurs, sans dépendre d’une place dans la queue saturée ; elles annoncent le total cumulatif de pertes. Aucun faux retour à « complet » dans le combat touché.

Application absente : worker tente une reconnexion avec délai borné 250 ms→5 s et jitter. Pas de backlog illimité ; vider/abandonner les événements selon la même comptabilité de pertes. L’absence de l’application ne doit jamais faire attendre le callback. Les écritures du pipe sont asynchrones et annulables. À la déconnexion, interrompre l’écriture et libérer les buffers. Ne pas attendre indéfiniment un worker lors du déchargement ; établir l’ordre d’arrêt garantissant qu’aucun thread n’utilise du code déchargé, avec validation spécifique Windows.

## Reprise et qualité

Le sessionId change à chaque nouvelle instance du bridge. Une connexion transport ne redémarre pas les séquences. Heartbeat proposé chaque seconde ; après 3 s sans réception, l’UI indique données périmées, délai à ajuster par essais.

Après reconnexion : handshake, état Health avec séquence et pertes cumulées, snapshot des agents si disponible. L’application clôture les périodes ouvertes comme interrompues. Elle conserve les derniers chiffres avec leur timestamp, ne les remet pas à zéro. Elle ne reconstitue pas des événements absents. Même sans compteur de pertes reçu, une coupure en combat suffit à marquer celui-ci incomplet.

Une nouvelle session ne fusionne pas ses compteurs avec l’ancienne. Une séquence dupliquée est ignorée ; une séquence rétrograde ou un saut est diagnostiqué selon le contrat d’ordre. Les doublons s’évaluent par session+séquence. Un marqueur d’incomplétude reste attaché à la rencontre après reconnexion et jusqu’à son archivage.

## Rejeu et tests

Enregistrer des traces normalisées dans l’application, jamais écrire les logs sur le thread du jeu. Format de fixtures NDJSON avec header de version et `synthetic=true`. Rejeu déterministe sans délai réel pour les tests ; vitesse configurable pour l’UI. Un `.zevtc` n’est pas une trace du bridge : il nécessite un parseur distinct.

Fixtures indispensables : deux joueurs et propriétaires d’invocations, double flux area/local, dégâts d’altérations, agent réutilisé, filtre sous-groupe, séquence trouée, doublon, saturation, message malformé, version future, fermeture en cours de combat, nouvelle session et durée nulle. Comparer aux définitions du domaine, puis à ArcDPS/Elite Insights sur des combats autorisés. Un succès de rejeu ne prouve pas la validité du collecteur réel.
