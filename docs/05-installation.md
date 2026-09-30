# Installation et coexistence

## Inventaire sans modification

Détecter le dossier contenant `Gw2-64.exe`, puis recenser les DLL candidates à la racine, `bin64`, `addons` et les configurations de chargeurs. Ces noms sont des **indices**, pas des preuves de propriété. Lire métadonnées PE et SHA-256, manifestes connus et configurations ; ne pas charger une DLL inconnue pour l’identifier.

Nexus : croiser empreintes/version/manifestes issus de sa distribution et configuration du chargeur. Ne pas conclure « Nexus » à partir de la seule présence de `d3d11.dll`. L’emplacement courant exact et les règles de chainloading doivent être confirmés auprès de Raidcore et en installation réelle. Le dépôt de l’intégration inspecté expose des événements Nexus ; il ne suffit pas à prouver comment le chargeur actuel installe ArcDPS.

Le README BHUD et Mechanics Log mentionne encore `bin64`, alors que Healing Stats et Unofficial Extras décrivent la racine avec `d3d11.dll`. **Ne pas transformer ces indications hétérogènes en recette automatique universelle.** Chaque recette sera versionnée et liée à une topologie testée.

## Propriété

`owner = companion | nexus | otherManager | external | unknown` par composant/fichier. Un fichier inconnu ne devient pas propriété de l’application par simple reconnaissance de son nom. L’adoption d’une installation existante requiert inventaire vérifié, accord explicite et sauvegarde ; sinon lecture seule.

| Topologie | Politique initiale |
|---|---|
| GW2 propre | Recette autonome ArcDPS + bridge, après tests officiels du chargement |
| ArcDPS existant reconnu | Réutiliser en lecture seule ; proposer adoption séparée |
| Nexus reconnu | Laisser Nexus responsable de ses fichiers ; bridge seulement selon recette de coexistence validée |
| Autre chargeur / inconnue | Inventaire et diagnostic ; installation bloquée |
| Plusieurs gestionnaires sur le même composant | Conflit explicite ; aucune mise à jour concurrente |

Nexus n’est nécessaire que pour les candidats qui le déclarent, notamment WvW Fight Analysis selon son README. Aucun changement automatique du chargeur de l’utilisateur pour ajouter une statistique.

## Transaction de fichiers

1. Plan : résoudre les dépendances et leurs propriétaires ; vérifier l’espace, les droits et les chemins canoniques. Refuser traversées de chemins, points de réanalyse inattendus et cibles hors périmètre.
2. Télécharger dans un staging de l’application depuis les sources officielles en HTTPS. Manifeste de recettes signé par notre projet, hash attendu épinglé, versions et chaîne d’approvisionnement contrôlées. SHA-256 calculé seul ne prouve pas l’authenticité ; ne pas traiter MD5 comme signature.
3. Revue utilisateur : versions, fichiers ajoutés/remplacés, configurations préservées, redémarrage et niveau de compatibilité.
4. Si le jeu tourne : journaliser `pendingGameExit`. Avant application, refaire inventaire et hashes ; si un autre gestionnaire a modifié un fichier, invalider le plan.
5. Verrouiller notre transaction par installation. Recontrôler les processus et les verrous fichiers. Un jeu qui redémarre peut faire échouer la modification : ne pas forcer une DLL chargée.
6. Sauvegarder les fichiers possédés avec hash, emplacement et métadonnées ; écrire durablement le journal avant chaque mutation.
7. Remplacer via fichiers temporaires sur le même volume et opérations atomiques par fichier. Une opération multi-fichier n’est **pas** atomique : le journal permet reprise/compensation.
8. Vérifier les hashes finaux ; inscrire le manifeste de propriété et l’état committed. La validation après démarrage en jeu est distincte de la réussite de la copie.

Au redémarrage de l’application, parcourir les transactions inachevées. Restaurer seulement si le fichier courant est celui écrit par la transaction (hash post-image attendu). Si un tiers l’a changé, ne pas l’écraser : état `recoveryRequired` et diagnostic. Ne jamais déduire une compatibilité d’un rollback réussi.

## Désactivation et suppression

Désactiver après fermeture du jeu selon la recette (déplacer le fichier possédé vers un emplacement non chargé, par exemple), sauvegarder l’état. Réactivation : recontrôle versions/chargeur avant remise en place. Désinstaller : supprimer uniquement les fichiers encore possédés et dont le hash correspond au manifeste ; garder configurations et profils par défaut. Une dépendance partagée reste en place tant qu’un composant activé l’exige.

Un helper élevé éventuel exécute un plan validé et borné, pas une commande shell arbitraire ; l’UI reste en utilisateur standard. Il revalide chemins, hashes et identité du demandeur. UAC survient uniquement pour une opération qui le nécessite.

## Livraison et maintenance

Installateur de l’application distinct des addons. Proposition : installation par utilisateur, runtime .NET inclus, binaires x64 signés Authenticode lorsque certificat et processus de release sont disponibles. Mise à jour de l’application avec migration sauvegardée ; distribution du bridge depuis nos releases contrôlées. Catalogue extensible par manifestes déclaratifs, mais une nouvelle recette native demande revue et test avant activation.
