# Livraison Windows et auto-update

Exigence ajoutée : permettre à l’utilisateur de tester une première version Windows puis de recevoir les mises à jour depuis l’application. Ce document décrit la cible ; **aucun moteur d’auto-update n’est encore implémenté**.

## Première version testable

La CI Windows doit compiler, exécuter les tests puis publier un paquet `win-x64` autonome incluant le runtime .NET. Le testeur télécharge l’artefact du workflow, extrait tout le dossier et lance `Companion.Desktop.exe`, sans installer le SDK. Cette distribution portable initiale ne propose pas encore de mise à jour automatique et n’est pas signée. Un build réussi ne valide pas l’affichage, le bridge ou le fonctionnement dans GW2.

Le workflow peut aussi être déclenché manuellement dans GitHub Actions. Les artefacts de test expirent après 14 jours : ce ne sont pas des URLs de distribution pour l’updater. La première livraison publique installable doit être produite par un pipeline de release séparé, avec version et notes de version.

## Parcours de mise à jour de l’application

1. Vérification en arrière-plan au démarrage, au plus une fois par jour, et bouton manuel dans Paramètres. Une panne réseau ne bloque jamais le démarrage.
2. Canal `stable` par défaut. Canal `preview` uniquement après activation explicite ; les builds de test de la première version sont identifiés comme tels.
3. Afficher version installée, version proposée, notes et éventuelles contraintes Windows/bridge. Ne pas sélectionner une version seulement à partir d’un nom de fichier ou d’un tri lexicographique.
4. Télécharger en staging sans interrompre le dashboard. Téléchargement automatique activable ; bouton « Installer et redémarrer » explicite par défaut.
5. Vérifier origine autorisée, intégrité et authenticité avant toute exécution. Un hash téléchargé depuis le même serveur ne suffit pas à authentifier un paquet.
6. Sauvegarder les profils et les données avant migration, arrêter proprement les services, puis laisser un updater séparé remplacer l’application après sa fermeture. Ne pas se remplacer pendant son exécution.
7. Au redémarrage, vérifier chargement des données et démarrage des services. Confirmer la nouvelle version seulement après ce contrôle.
8. Si échec, proposer reprise ou restauration compatible. Conserver les profils, les rapports et le diagnostic de l’opération. Ne pas annoncer un rollback des binaires comme suffisant après une migration de données irréversible.

Pendant une session de jeu ou une publication en cours, différer le redémarrage proposé. Le bridge doit tolérer l’absence temporaire de l’application ; tout combat interrompu conserve son indicateur de données partielles. Aucun redémarrage imposé pendant le jeu.

## Choix technique à valider

Étudier un updater .NET maintenu, tel que **Velopack**, avant d’écrire notre propre remplacement d’exécutables. Alternative : MSIX/App Installer, à comparer selon les contraintes de signature, installation par utilisateur et intégration aux opérations sur le dossier du jeu. Leurs documentations et versions n’ont pas encore été vérifiées dans cette tâche : ce sont des candidats, pas des dépendances installées.

Critères de sélection : WPF/.NET 10, Windows x64, paquet autonome, installation par utilisateur, canaux, intégration CI, vérification cryptographique avec clé de confiance distribuée dans le client, contrôle du moment d’installation, reprise et support des migrations. Vérifier ce que l’outil fournit réellement avant de lui attribuer ces garanties.

Publier des releases immuables et un index par canal. Prévoir signatures des paquets/manifeste et signature Authenticode des exécutables/installateurs, avec procédure de rotation des clés. Clés privées et certificat de signature dans le coffre de CI, jamais dans Git ou l’application. Aucun jeton GitHub embarqué ; la distribution depuis un dépôt privé nécessite un mécanisme adapté à concevoir séparément.

Le pipeline de release exécutera les tests, la compilation Windows, le packaging, la signature, la vérification des signatures puis un test d’installation/mise à jour sur machine propre. La promotion de preview vers stable est une action explicite. Le workflow de build actuel ne publie pas automatiquement de release stable.

## Application, bridge et addons

Trois cycles de version distincts :

- **Application** : moteur de mise à jour du logiciel et migration de ses données.
- **Bridge** : DLL gérée par le gestionnaire de composants, remplacée uniquement après fermeture de GW2 ; protocole compatible avec l’application installée.
- **ArcDPS/extensions** : sources officielles, propriété des fichiers et compatibilité vérifiées ; jamais modifiés par l’updater de l’application.

Une version d’application annonce la plage de protocoles bridge acceptée. Si un bridge plus récent est nécessaire pendant que GW2 tourne, conserver la connexion compatible ou déclarer la collecte indisponible et proposer une opération différée. Ne pas écraser la DLL chargée. La mise à jour d’ArcDPS reste soumise aux règles de coexistence Nexus et autres gestionnaires.

## Validation avant activation

Tester N→N+1 avec profils existants, canal preview/stable, version future/incompatible, téléchargement interrompu, paquet corrompu, signature invalide, disque plein, antivirus/verrou fichier, refus de droits, fermeture brutale à chaque étape, premier démarrage échoué et migration interrompue. Vérifier absence de redémarrage en jeu et conservation des logs, rapports et jetons.

Statuts utilisateur : à jour, vérification impossible, mise à jour disponible, téléchargement en cours, prête à installer, installation différée, erreur récupérable. Afficher une progression issue du téléchargement réel ; ne pas présenter une erreur réseau comme « à jour ».
