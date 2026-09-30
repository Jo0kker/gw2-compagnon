# Logs, sessions et WvW Insights

## Vérification manquante et frontière d’implémentation

La consultation de <https://parser.rethl.net/api-docs.html> a été refusée par le proxy de l’environnement le 2026-09-30 (HTTP 403 au tunnel). Les éléments du brief — `.zevtc` McM, minimum deux fichiers, maximum 1 Go, traitement asynchrone, jeton d’historique, résultats JSON — sont **des exigences provisoires à revérifier**. Aucun endpoint HTTP, nom de champ, statut, quota ou méthode d’authentification n’est inventé ici.

Avant d’écrire l’adaptateur réel, relever et dater : endpoints et méthodes, multipart/chunks, limites exactes en octets et taille de requête, critères McM, création/réutilisation du jeton, authentification, options, réponses/jobs, polling et rate limits, TTL, annulation, idempotence, URLs retournées, visibilité/rétention, JSON de résultat et erreurs. Enregistrer des fixtures de réponses expurgées. Tester ensuite un petit lot avec consentement de publication.

## Index local

Détection du dossier Documents réel via Known Folders Windows ; proposer `Guild Wars 2/addons/arcdps/arcdps.cbtlogs` selon Elite Insights, accepter un chemin personnalisé. Watcher avec debounce + balayage périodique pour récupérer les événements perdus.

États fichier : discovered → settling → ready → parsing → indexed, ou invalid / missing / retryableError. Proposition de stabilité : taille et date inchangées lors de trois observations espacées de deux secondes, ouverture en lecture avec partage compatible, validation archive et en-tête. La stabilité de taille seule ne prouve pas la fin d’écriture. Recontrôler avant et après hash/lecture ; reprendre si le fichier change. Limiter mémoire, taille décompressée et nombre d’entrées pour éviter les archives pathologiques.

Calcul SHA-256 après stabilisation ; doublons par contenu, pas par nom. Plusieurs chemins peuvent référencer le même log. Un déplacement met à jour la référence lors du scan sans perdre session/rapport. Un fichier absent reste indexé avec état manquant. Les métadonnées inconnues (carte, mode, durée) restent null ; ne pas décider McM sur le seul nom du fichier.

Jobs de parsing persistants avec bail ; un job interrompu est repris après expiration du bail, sans produire deux analyses valides. Résultat versionné par hash du fichier + version du parseur + options. Session nommée : sélection de logIds, exclusions persistées, ordre chronologique si dates connues.

## Ports applicatifs proposés

Interfaces métier, **pas noms de l’API distante** :

```text
ILogIndexer.Scan(directory) -> discovered files and changes
ILogInspector.Inspect(stableFile) -> validated metadata or rejection
IPublicationConnector.Validate(batch, verifiedPolicy) -> per-file issues
IPublicationConnector.Submit(batch, credentialRef) -> remoteJobRef
IPublicationConnector.GetStatus(remoteJobRef, credentialRef) -> status
IPublicationConnector.FindExisting(fingerprint, credentialRef) -> match/unknown
ISecretStore.GetOrCreate(connectorId) -> protected credential reference
```

Si le service ne permet pas FindExisting ou un token d’idempotence, l’adaptateur retourne unknown et demande une résolution manuelle ; ne pas prétendre dédupliquer côté serveur.

## Machine d’états persistée

```text
draft → validating → ready → uploading → accepted → queued → processing → succeeded
                                  └→ submissionUnknown
Toute étape → failed / expired selon réponse vérifiée
Annulation locale avant acceptation : canceled
Après acceptation : abandon du suivi ≠ annulation du calcul distant
```

Les transitions exactes distantes sont mappées au modèle à partir de l’API documentée. Sauvegarder l’identifiant distant dès réception, avant de poursuivre le polling. Après crash, reprendre le suivi par ID, jamais recommencer automatiquement l’upload. Timeout après soumission sans ID = `submissionUnknown`.

Empreinte locale = SHA-256(version connecteur + hashes de fichiers triés et uniques + options canoniques + identité locale du compte/destination). Verrou et contrainte d’unicité empêchent deux travaux actifs identiques. Pour un lot déjà réussi, montrer le rapport précédent ; nouvel envoi seulement sur demande explicite. Le jeton lui-même n’entre ni dans l’empreinte journalisée ni dans les logs.

Prévalidation provisoire : deux fichiers McM distincts au moins, `.zevtc`, poids total au plus 1 000 000 000 octets par prudence en attendant la définition exacte de « 1 Go ». Cette règle est configurable et **ne doit pas activer le connecteur réel avant vérification du contrat**. Les fichiers invalides, manquants, en écriture ou dont le mode n’est pas confirmé sont exclus avec motifs.

HTTP : streaming depuis fichiers stables, limites de concurrence, cancellation, délais explicites. Retry avec backoff/jitter pour requêtes de lecture et réponses admissibles, respect de Retry-After. Pas de retry aveugle d’une soumission non idempotente. Progression upload basée sur octets effectivement transmis ; traitement distant affiche l’état réel ou un indicateur indéterminé.

## Jetons et résultats

Réutiliser un jeton existant dans DPAPI CurrentUser ; création seulement selon API vérifiée. En cas de changement de compte ou jeton perdu, ne pas promettre récupération de l’historique. Expurger headers/body sensibles des diagnostics.

Conserver identifiant de job, liens de rapports, état et timestamps avec la session. Valider scheme HTTPS et origine autorisée avant ouverture d’un lien externe. Ne pas embarquer arbitrairement du HTML distant dans l’application. Les résultats JSON alimenteront les widgets P1 via un adaptateur versionné, avec source « Rapport WvW Insights », période historique et champs absents null.

PvE : connecteur séparé, non envoyé à WvW Insights. Automatisation P1 désactivée par défaut ; activation explicite avec portée, regroupement, seuils, destination et délai d’annulation. Aucun envoi de logs réels pendant les tests unitaires.
