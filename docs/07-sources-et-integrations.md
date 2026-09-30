# Sources et matrice des intégrations

Inspection documentaire du **30 septembre 2026**. Les dépôts ont été clonés en lecture seule, leurs README/licences examinés et les liens GitHub `releases/latest` résolus. Les hashes complets consultés figurent dans [sources.json](sources.json). Les dates de commit ne sont pas des garanties de maintenance ; les releases observées ne sont pas des garanties de compatibilité. **Aucune DLL téléchargée, chargée ou testée dans GW2.** Aucun code tiers incorporé dans ce dépôt.

Niveaux : **D** = décrit dans la documentation ; **S** = observé dans le code source inspecté ; **J** = testé dans le jeu. Aucun candidat n’a le niveau J. Une source marquée D/S ne doit pas devenir un badge « compatible » dans le catalogue.

## Versions et conditions

| Candidat / auteur | Release observée | Dernier commit du checkout | Licence observée et précautions | Dépendances |
|---|---|---|---|---|
| [ArcDPS / deltaconnected](https://www.deltaconnected.com/arcdps/) | Non vérifiable ici | Non applicable | Conditions officielles non consultées ; ne pas supposer open source ou redistribution permise | GW2 x64 et compatibilité du build |
| [Healing Stats / Krappa322](https://github.com/Krappa322/arcdps_healing_stats) | [v2.20.1](https://github.com/Krappa322/arcdps_healing_stats/releases/tag/v2.20.1) | 2026-09-05, fc36044 | MIT pour le code ; conserver notices, examiner dépendances si réutilisation | ArcDPS ; partage activé chez les participants concernés |
| [Boon Table / knoxfighter](https://github.com/knoxfighter/GW2-ArcDPS-Boon-Table) | [3.1.0](https://github.com/knoxfighter/GW2-ArcDPS-Boon-Table/releases/tag/3.1.0) | 2026-08-22, 156bba5 | MIT ; assets du jeu soumis à droits distincts | ArcDPS ; intégration Unofficial Extras à examiner pour fonctions complémentaires |
| [Know Thy Enemy / typedeck0](https://github.com/typedeck0/Know-thy-enemy) | [4.11.4](https://github.com/typedeck0/Know-thy-enemy/releases/tag/4.11.4) | 2024-08-21, a6d5df5 | CC0 dans le dépôt public, mais README signale des parties fermées ; ne pas extrapoler au binaire complet | ArcDPS |
| [WvW Fight Analysis / jake-greygoose](https://github.com/jake-greygoose/WvW-Fight-Analysis-Addon) | [v2026.9.16.927](https://github.com/jake-greygoose/WvW-Fight-Analysis-Addon/releases/tag/v2026.9.16.927) | 2026-09-16, 171086b | MIT dans le dépôt (copyright Raidcore.GG) | **Nexus**, logs McM ArcDPS |
| [Mechanics Log / knoxfighter](https://github.com/knoxfighter/GW2-ArcDPS-Mechanics-Log) | [v3.0.1](https://github.com/knoxfighter/GW2-ArcDPS-Mechanics-Log/releases/tag/v3.0.1) | 2026-07-12, dded3f1 | MIT dans LICENSE (copyright baaron4) ; conserver notices si réutilisation | ArcDPS, runtime VC++ selon README |
| [Killproof.me / knoxfighter](https://github.com/knoxfighter/arcdps-killproof.me-plugin) | [v3.1.0](https://github.com/knoxfighter/arcdps-killproof.me-plugin/releases/tag/v3.1.0) | 2026-08-15, 2148a9f | MIT du plugin ; service externe et assets ont leurs propres conditions | ArcDPS, service killproof.me ; Unofficial Extras pour suivi hors instance |
| [Unofficial Extras / Krappa322](https://github.com/Krappa322/arcdps_unofficial_extras_releases) | [v2.6.1](https://github.com/Krappa322/arcdps_unofficial_extras_releases/releases/tag/v2.6.1) | 2026-08-14, 33bd9ac | API publique MIT ; **implémentation fermée**, ne pas attribuer MIT à toute la DLL | ArcDPS ; addon disposant de sa propre mise à jour |
| [WvW Insights](https://parser.rethl.net/) | Service, version inconnue | Non applicable | Conditions API/service non consultables ici ; droits/rétention à vérifier | HTTPS, logs admissibles, jeton selon API à relire |

## Capacités et effort réel

| Candidat | Capacité démontrée par les sources | Direct / après combat | Accès depuis notre application et travail nécessaire |
|---|---|---|---|
| ArcDPS | Les références BHUD et Nexus utilisent callbacks area/local (S) ; API officielle bloquée | Direct et logs selon brief/références | Bridge dédié, normalisation et agrégation ; relire ABI officielle et valider les métriques |
| Healing Stats | Soins locaux, partage volontaire, enregistrement de soins dans EVTC (D) | Direct dans l’addon, historique via logs | Aucune API d’export vers notre app validée ; auditer le protocole de partage ou décoder l’extension de logs. Ne pas lire sa fenêtre ni promettre des soins d’escouade complets |
| Boon Table | Table d’avantages, filtres joueurs/sous-groupes, statistiques agrégées (D) | Direct dans l’addon | Aucun endpoint public d’agrégats vérifié ; recalcul dans notre domaine à partir d’événements ArcDPS ou adaptateur explicitement validé |
| Know Thy Enemy | Adversaires touchés par l’escouade ou ayant touché l’escouade, remis à zéro par combat (D) | Direct et historique interne | Export non vérifié ; widget indépendant à calculer depuis observations disponibles. Libellé obligatoire « ennemis observés », jamais recensement de proximité |
| WvW Fight Analysis | Parse des logs EVTC et effectifs par équipe (D) | Après combat | Pas d’export IPC validé. On peut étudier un parseur local indépendant pour le même besoin ; installer sa DLL Nexus ne fournit pas ses résultats |
| Mechanics Log | Événements de mécaniques identifiés par skill ID et tables de rencontres (D) | Direct dans l’addon, événements présents dans EVTC selon README | Implémenter règles versionnées et tests des rencontres ; pas de gain automatique à installer la DLL. Pas de McM déclaré dans README |
| Killproof.me | Profils des joueurs accessibles/publics ; absence si privé/non inscrit (D) | Requête externe, pas métrique de combat | Connecteur direct au service à concevoir selon API et quotas ; pas besoin démontré du plugin pour ce widget. Les informations indisponibles restent inconnues |
| Unofficial Extras | Callbacks escouade, langue, raccourcis et chat (D/S) | Direct | Adapter uniquement les capacités utiles et versions supportées. Copier chaînes pendant callback ; chat non collecté par défaut. Dépendance facultative pour informations absentes d’ArcDPS |
| WvW Insights | Lot asynchrone et bilan JSON décrits dans le brief uniquement | Après combat | Connecteur P0 publication ; widgets de bilan JSON P1. Contrat distant à relire avant toute implémentation HTTP réelle |

## Installation / retrait documentaire

Ces recettes sont **des indications de sources**, pas des actions autorisées dans toutes les topologies. Toute suppression attend l’arrêt du jeu, respecte le manifeste de propriété et conserve la configuration.

| Projet | Indication de la source | Retrait / incertitude |
|---|---|---|
| ArcDPS | Le catalogue Approved-Addons référence le téléchargement officiel `gw2addon_arcdps.dll` et une empreinte `x64/d3d11.dll.md5sum` | Destination actuelle et chainloading à confirmer depuis ArcDPS ; ne pas écraser d3d11.dll |
| Healing Stats | `arcdps_healing_stats.dll` dans le dossier d’ArcDPS (`d3d11.dll`) | Suppression de la DLL possédée proposée ; pas de procédure de retrait complète vérifiée |
| Boon Table | README consulté décrit usage et configuration, pas une recette complète d’installation | Localisation du binaire à valider par release/topologie avant automatisation |
| Know Thy Enemy | `know_thy_enemy.dll` à côté de l’exécutable GW2 ou dans bin64 selon README | Ambiguïté d’emplacement à résoudre en test ; préserver réglages |
| WvW Fight Analysis | `WvWFightAnalysis.dll` dans `<GW2>/addons`, chargé par Nexus | Nexus reste propriétaire du cycle de vie ; ne pas supprimer une dépendance utilisée par d’autres addons |
| Mechanics Log | Même dossier qu’ArcDPS, README cite encore `bin64` ; retrait par suppression/déplacement | Documentation vraisemblablement dépendante de topologie ; ne pas appliquer aveuglément les indications DX9/bin64 |
| Killproof.me | DLL dans dossier ArcDPS ou racine GW2 ; retrait par suppression DLL | Préserver `<GW2>/addons/arcdps/arcdps_killproof.me.json` |
| Unofficial Extras | `arcdps_unofficial_extras.dll` à côté du `d3d11.dll` ArcDPS | Auto-update configurable : résoudre le propriétaire avant toute prise en charge |
| WvW Insights | Service distant, aucune DLL requise par ce parcours | Désactivation du connecteur conserve les rapports ; révocation/suppression du jeton selon API |

## Références d’architecture supplémentaires

- [BHUD bridge, README au commit consulté](https://github.com/blish-hud/arcdps-bhud/blob/0daaffd64cac647389d14468efc7a0f09430fa8d/README.md), [pubsub.rs](https://github.com/blish-hud/arcdps-bhud/blob/0daaffd64cac647389d14468efc7a0f09430fa8d/src/pubsub.rs), [exports/mod.rs](https://github.com/blish-hud/arcdps-bhud/blob/0daaffd64cac647389d14468efc7a0f09430fa8d/src/exports/mod.rs) : Apache-2.0, release [v2.0.1](https://github.com/blish-hud/arcdps-bhud/releases/tag/v2.0.1). TCP loopback, file de 1 000 éléments, timeout d’écriture de 5 s. Le trait Message a BLOCK=true par défaut, combat utilise ce défaut ; imgui le désactive. Référence à comprendre, pas à recopier pour notre exigence non bloquante.
- [Nexus ArcDPS Integration, entry.cpp](https://github.com/RaidcoreGG/GW2-Arcdps-Integration/blob/c5a2c5a9c7450cb9a9e431137d12d54e14536537/src/entry.cpp) et [ArcEvents.cpp](https://github.com/RaidcoreGG/GW2-Arcdps-Integration/blob/c5a2c5a9c7450cb9a9e431137d12d54e14536537/src/ArcEvents.cpp) : MIT, release [2024-04-16](https://github.com/RaidcoreGG/GW2-Arcdps-Integration/releases/tag/2024-04-16), commit 2025-04-22. Relaie les événements bruts area/local dans Nexus ; ne constitue pas à lui seul un serveur IPC externe.
- [Approved-Addons](https://github.com/gw2-addon-loader/Approved-Addons) : MIT, commit 754d888 du 2026-01-14 ; décrit des métadonnées `update.yaml` pour les gestionnaires. Les fichiers présents sont nommés `update-placeholder.yaml` : catalogue documentaire, pas preuve de compatibilité actuelle.
- [Elite Insights](https://github.com/baaron4/GW2-Elite-Insights-Parser), MIT, release [v3.30.1.0](https://github.com/baaron4/GW2-Elite-Insights-Parser/releases/tag/v3.30.1.0), commit 6470c0d du 2026-09-30 : CLI, `.evtc`/`.evtc.zip`/`.zevtc`, sortie JSON, parsing d’extensions et McM détaillé. Candidat pour analyse locale isolée en sous-processus ; options de publication externe à laisser désactivées. Le README annonce .NET 8 puis migration .NET 10 en novembre 2026, à revérifier pour le binaire retenu.

## Sources non consultées et conséquences

| Source | État | Conséquence |
|---|---|---|
| [API ArcDPS](https://www.deltaconnected.com/arcdps/api/README.txt) | Tunnel HTTPS refusé (403) | ABI, restrictions et recette native non validées |
| [API WvW Insights](https://parser.rethl.net/api-docs.html) | Tunnel HTTPS refusé (403) | Aucun endpoint implémentable à partir de cette recherche seule |
| [Règles ArenaNet](https://help.guildwars2.com/hc/en-us/articles/360013625034-Policy-Third-Party-Programs) | Tunnel HTTPS refusé (403) | Pas d’affirmation d’approbation officielle ; relire avant distribution |
| [Catalogue Raidcore](https://raidcore.gg/gw2/addons) | Domaine absent de la liste réseau autorisée ; non consulté | Détection et propriété Nexus à valider dans documentation officielle actuelle |

Les tests devront utiliser les versions exactes, le build GW2, le chargeur et l’architecture documentés. La présence d’un README, d’un tag récent ou d’une licence permissive n’est pas un test d’intégration.
