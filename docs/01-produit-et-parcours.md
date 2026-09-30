# Produit et parcours

## Statut des décisions

**Acquis** : Windows pour cette première application, autonome, ArcDPS comme source, bridge dédié géré par l’application, profils libres, widgets indépendants, WvW Insights dès P0, publication manuelle par défaut, aucune automatisation du jeu.

**Retenu pour l’interface** : maquette 01 simplifiée, navigation supérieure compacte sans menu latéral, deux tableaux par défaut. Réglages visibles pendant l’édition. [Variante simplifiée](maquettes/01-dashboard-simplifie.png).

**Proposé** : interface française, .NET/WPF, une fenêtre dashboard en P0, panneau contextuel unique à terme, installation par utilisateur de l’application. Nom non décidé ; « Compagnon GW2 » reste un libellé de travail.

**À éprouver** : exactitude et latence des statistiques, compatibilité des chargeurs et des versions, règles de combat McM, intégrité des exports, comportement à 150/200 % de mise à l’échelle, performances sur une machine de référence.

## Périmètre

| Livraison | Résultat pour l’utilisateur | Conditions de sortie |
|---|---|---|
| Socle / 0.1 | Composer un dashboard et rejouer un combat fictif sans GW2 | Profils persistants, deux instances indépendantes, source toujours explicite |
| P0 / 0.2 | Installer les composants d’une topologie vérifiée, suivre ses dégâts, publier une session McM | Essais Windows/GW2 et service WvW Insights ; ne pas appeler 0.1 une P0 complète |
| P1 | Soins partagés si accessibles, avantages, bilans JSON, import/export complet, raccourcis | Adaptateurs et définitions validés séparément |
| P2 | Profils contextuels, connecteurs supplémentaires, widgets tiers, partage de modèles | Isolation, versionnement et politique de confiance des extensions |

La publication WvW Insights se développe parallèlement au direct. Elle ne dépend pas de la disponibilité du bridge : des logs locaux admissibles doivent suffire.

## Organisation des écrans

| Espace | Contenu et action principale |
|---|---|
| Configuration initiale | Dossier GW2, inventaire existant, responsabilités, plan de modifications ; « Configurer » après revue |
| Dashboard | Profil et page, widgets ; « Modifier » puis « Terminer l’édition » |
| Profils | Créer vide/depuis modèle, renommer, icône, dupliquer, supprimer, pages ; import/export à terme |
| Bibliothèque | Intégration, source, capacités et aperçu ; « Ajouter à cette page » |
| Intégrations | Catalogue et fiche : auteur, source officielle, versions, dépendances, données, widgets, compatibilité et responsable des mises à jour |
| Combats et sessions | Index des fichiers locaux, filtres, sélection, exclusion, session nommée ; « Préparer la publication » |
| Rapports | Travaux en cours, liens hébergés et bilans ; liens distincts des analyses locales |
| Paramètres / Diagnostic | Chemins, densité, secrets protégés, versions, connexion, pertes, opérations et export de diagnostic expurgé |

Une barre d’état distingue toujours **jeu**, **ArcDPS**, **bridge** et **source affichée**. « Jeu ouvert » ne signifie pas « collecte fonctionnelle ». Les installations et publications continuent dans les services de l’application lorsque l’utilisateur change de page.

## Parcours détaillés

### Première ouverture

1. Présenter le bénéfice et proposer « Configurer GW2 » ou « Explorer avec un combat fictif ».
2. Lire les emplacements Windows connus, les bibliothèques Steam quand elles sont accessibles ; le choix manuel du dossier reste disponible. Vérifier `Gw2-64.exe` sans l’exécuter. Documents peut être redirigé : utiliser les Known Folders Windows.
3. Inspecter fichiers, versions, empreintes, chargeurs et manifestes. Ne rien modifier pendant l’inventaire.
4. Afficher les composants requis, ceux déjà présents, leur propriétaire et les incertitudes. Une DLL inconnue ou une topologie non testée empêche l’installation automatique et ouvre un diagnostic.
5. Montrer les fichiers qui seraient ajoutés/remplacés, les sauvegardes et l’éventuelle élévation de droits. L’utilisateur lance l’opération.
6. Si GW2 tourne, enregistrer une opération en attente ; proposer d’attendre ou d’annuler, jamais tuer le jeu. Refaire l’inventaire à sa fermeture.
7. Installer transactionnellement ; en cas d’échec proposer la reprise ou la restauration applicable.
8. Créer un profil vide ou dérivé du modèle McM/PvE/Raid/Heal. Les modèles sont des copies modifiables, pas des types de profil.
9. Ouvrir la bibliothèque, ajouter Dégâts, indiquer comment déplacer la fenêtre sur le deuxième écran. Si le bridge n’est pas connecté, expliquer que le jeu doit démarrer et afficher « En attente de données ».

### Consultation et édition

Consultation : changer librement de profil/page sans couper la collecte ; chaque widget annonce source, période, portée et qualité. Sans données, afficher « — » et une explication. Une vraie valeur nulle n’apparaît qu’après une observation valide.

Édition : grille en unités logiques de 16 DIP, poignées visibles, glisser-déposer et redimensionnement, ajout, duplication, masquage, retrait et réglages dans un panneau latéral. Prévoir déplacement au clavier et champs numériques pour position/taille. Une instance sélectionnée est surlignée sans masquer les autres. Un verrou de disposition interdit les gestes involontaires ; Terminer revient en consultation. Les opérations sont sauvegardées avec un court délai, fermeture incluse. Une erreur de sauvegarde reste affichée jusqu’à résolution.

Les widgets masqués apparaissent dans la liste d’édition avec « Afficher ». Une intégration désactivée laisse les emplacements et paramètres intacts. Supprimer un profil demande confirmation et sélectionne un profil restant ; le dernier profil peut être remplacé par un profil vide. Duplication régénère tous les identifiants internes.

### Combats → publication

1. Ouvrir une session ou sélectionner plusieurs combats ; distinguer fichier local, analyse locale et rapport distant.
2. Exclure les fichiers non admissibles avec un motif par ligne. Afficher nombre, poids total, destination et options.
3. Présenter « Ces fichiers seront envoyés à WvW Insights » et la visibilité connue du rapport. Ne pas promettre la confidentialité d’un service dont les conditions ne sont pas vérifiées.
4. Déclencher manuellement l’envoi. Distinguer progression d’upload et état de traitement distant ; aucun pourcentage de calcul inventé.
5. Si la connexion échoue après acceptation possible, rechercher le travail existant avant de proposer un nouvel envoi.
6. Afficher les liens reçus, les rattacher à la session et les retrouver dans Rapports. Un lot expiré conserve son historique et nécessite une nouvelle action explicite.

### Intégrations

Fiche → dépendances et widgets réellement disponibles → plan d’installation → activation → bibliothèque. Les mentions « installation disponible », « données accessibles » et « widget implémenté » sont distinctes. Un candidat étudié n’est pas affiché comme intégration vérifiée. Une désinstallation conserve les réglages par défaut et signale les composants encore utilisés ailleurs.

## États et actions

| Situation | Message utilisateur | Action |
|---|---|---|
| Jeu non détecté | « Le dossier de Guild Wars 2 n’a pas été trouvé. » | Choisir un dossier / Démonstration |
| Jeu fermé | « Ouvrez GW2 pour recevoir les combats. » | Rejouer un combat enregistré |
| ArcDPS absent | « La collecte nécessite ArcDPS. » | Examiner l’installation |
| Bridge absent | « Le composant de connexion n’est pas installé. » | Configurer les composants |
| Bridge déconnecté | « Connexion interrompue ; dernières données reçues à … » | Diagnostic / Reconnexion automatique |
| Incompatibilité connue | « Cette combinaison de versions présente un problème connu. » | Lire le détail / Désactiver après fermeture du jeu |
| Compatibilité inconnue | « Cette version n’a pas été vérifiée avec votre version de GW2. » | Voir les versions et preuves ; aucune coche verte |
| Installation en conflit | « Un fichier existant n’appartient pas à notre gestionnaire. » | Inventaire et aide ; aucune écriture |
| Droits insuffisants | « Windows empêche la modification de ce dossier. » | Relancer uniquement l’opération avec élévation |
| Jeu en cours | « Installation en attente de fermeture de GW2. » | Annuler l’attente |
| Intégration désactivée | « Cette source est désactivée. Vos réglages sont conservés. » | Ouvrir l’intégration |
| Aucun combat | « Aucun fichier de combat trouvé dans ce dossier. » | Choisir le dossier / Voir comment activer les logs |
| Données partielles | « Des événements manquent. Ces statistiques sont partielles. » | Voir la période et les pertes |
| Service indisponible | « WvW Insights ne répond pas. Les fichiers restent locaux. » | Réessayer la vérification / Annuler |
| Fichier rejeté | « Ce fichier ne peut pas être publié : [motif]. » | Exclure / Voir le détail |
| Publication terminée | « Rapport disponible. » | Ouvrir / Copier le lien |
| Écran débranché | « La fenêtre a été replacée sur un écran disponible. » | Choisir son emplacement |
| Opération interrompue | « Une installation n’a pas été terminée. » | Examiner la reprise ou la restauration |

## Direction et maquettes

Trois maquettes de direction sont proposées dans la conversation : tableau de commandement, lecture à distance et atelier d’édition. Elles sont des illustrations, pas des captures d’une application fonctionnelle. Les chiffres sont fictifs et la connexion au jeu n’est jamais simulée comme réelle.

Fichiers initiaux conservés dans l’ordre de présentation : [maquette 1](maquettes/01-dashboard.png), [maquette 2](maquettes/02-dashboard.png), [maquette 3](maquettes/03-edition.png). La direction 1 est choisie et simplifiée dans la variante ci-dessus. Les labels ou graphiques supplémentaires des images ne constituent pas un engagement fonctionnel : le modèle et le périmètre de ce dossier font référence.

Structure des écrans complémentaires à réaliser dans la direction retenue :

```text
CONFIGURATION                     COMBATS ET SESSIONS
Dossier GW2 [Choisir]              Session [Soirée McM] [Créer]
Composant | État | Gestionnaire    [x] Combat | Date | Durée | Fichier
ArcDPS    | …    | …              [ ] Rejeté | motif | [Exclure]
Bridge    | …    | …              3 fichiers · 120 Mo · WvW Insights
Conflits / sauvegarde             [Préparer la publication]
[Voir les changements]            Panneau de revue puis envoi explicite

INTÉGRATION                       RAPPORTS
Nom · auteur · source             Session | État | Destination
Installée / disponible            Envoi … / En attente / Traitement …
Compatibilité + preuve            Rapport terminé [Ouvrir]
Dépendances / responsable         Erreur [Diagnostic] [Reprendre]
Données direct / après combat     Historique conservé même si log déplacé
Widgets réellement fournis
[Installer / Activer / Désactiver]
```

Les maquettes détaillées de ces écrans secondaires restent à produire après le choix visuel. Aucun asset officiel GW2 n’est repris sans examen des droits. Contraste cible WCAG AA, focus visible, navigation clavier, libellés d’état non limités à la couleur, densité confortable par défaut et animations désactivables.
