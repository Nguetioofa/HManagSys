# Documentation de la Base de Données - Système de Gestion Hospitalière

## Vue d'Ensemble

Cette base de données constitue le cœur du système de gestion hospitalière, conçue pour gérer efficacement les opérations quotidiennes d'un réseau de petits hôpitaux. L'architecture privilégie la flexibilité, la traçabilité et la simplicité d'utilisation, tout en respectant les spécificités du contexte médical camerounais.

## Philosophie Architecturale

Le système est construit autour de trois principes fondamentaux :

**Flexibilité Multi-Centres** : Un même utilisateur peut exercer dans plusieurs centres avec des rôles différents. Un SuperAdmin peut également administrer des soins comme le Personnel Soignant.

**Traçabilité Complète** : Chaque table possède des champs d'audit (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt) permettant de suivre l'évolution de chaque donnée. Cette traçabilité est gérée entièrement dans le code C# pour un contrôle total.

**Tables de Rapports Matérialisées** : Les rapports et statistiques sont stockés dans des tables physiques avec ID et champs d'audit, permettant l'historisation et de meilleures performances.

## Structure de la Base de Données

### Section 1 : Tables Fondamentales

Ces tables forment les fondations du système, comparable aux infrastructures de base d'un hôpital.

#### HospitalCenters
Représente chaque centre hospitalier du réseau. Chaque centre conserve son autonomie tout en étant connecté au système global.

**Champs Clés :**
- `Name` : Nom unique du centre
- `IsActive` : Permet de désactiver temporairement un centre sans perdre l'historique

#### Users
Table centrale pour tous les utilisateurs (SuperAdmin et Personnel Soignant). L'authentification se fait par email unique.

**Spécificités :**
- `MustChangePassword` : Force le changement après réinitialisation par admin
- `LastLoginDate` : Tracking de sécurité
- Le système génère des mots de passe temporaires simples lors des réinitialisations

#### UserCenterAssignments
Table cruciale qui matérialise la flexibilité multi-centres. Un utilisateur peut avoir plusieurs affectations avec des rôles différents.

**Types de Rôles :**
- `SuperAdmin` : Droits complets sur le centre
- `MedicalStaff` : Personnel soignant avec droits opérationnels

#### UserSessions
Gère les sessions utilisateur avec durée de vie de 12 heures. Chaque session est associée à un centre spécifique.

**Fonctionnalités :**
- Tracking de sécurité (IP, durée de connexion)
- Gestion du contexte multi-centres
- Nettoyage automatique des sessions expirées

#### UserLastSelectedCenters
Mémorise le dernier centre sélectionné par chaque utilisateur pour améliorer l'expérience utilisateur.

### Section 2 : Gestion des Stocks

Cette section modélise le système circulatoire de l'hôpital - tout ce qui entre, sort et circule.

#### ProductCategories / Products
Définition hiérarchique des produits (médicaments, consommables, équipements) avec prix de vente intégré.

#### StockInventory
Stock actuel par produit et par centre, avec seuils d'alerte configurables (minimum/maximum).

#### StockMovements
Journal complet de tous les mouvements de stock. Utilise un système de référence croisée intelligent :
- `ReferenceType` et `ReferenceId` lient le mouvement à sa cause (vente, soin, transfert, etc.)
- Permet une traçabilité bidirectionnelle complète

**Types de Mouvements :**
- `Initial` : Stock de départ
- `Entry` : Entrée (achat, don)
- `Sale` : Vente directe
- `Transfer` : Transfert inter-centres
- `Adjustment` : Correction manuelle
- `Care` : Utilisation dans un soin

#### StockTransfers
Gestion formalisée des transferts inter-centres avec workflow d'approbation.

### Section 3 : Gestion des Patients

Modélise l'aspect humain et médical du système.

#### Patients
Dossier patient adapté au contexte local :
- Numéro de téléphone obligatoire (réalité camerounaise)
- Email optionnel
- Informations médicales de base (allergies, groupe sanguin)

#### Diagnoses
Chaque diagnostic est signé par un soignant, créant une responsabilité tracée essentielle pour l'audit médical.

### Section 4 : Soins et Traitements

Modélise le processus thérapeutique complet.

#### CareTypes
Types de soins standardisés avec tarification de base.

#### CareEpisodes
Représente un cycle de soins complet pour un patient :
- Lié à un diagnostic précis
- Soignant principal désigné
- Suivi financier intégré (coût total, montant payé, solde)

#### CareServices
Services individuels dans un épisode de soins. Chaque service peut être administré par un soignant différent.

#### CareServiceProducts
Lie automatiquement l'utilisation de produits aux soins, créant une décrémentation intelligente du stock.

### Section 5 : Prescriptions

#### Prescriptions / PrescriptionItems
Gestion complète des prescriptions médicales avec détails posologiques. Facilite la dispensation ultérieure.

### Section 6 : Examens Médicaux

#### ExaminationTypes
Types d'examens avec double tarification :
- `BasePrice` : Prix de vente
- `SubcontractorPrice` : Prix de sous-traitance (pour examens externalisés)

#### Examinations
Workflow complet : demande → planification → réalisation → résultats

#### ExaminationResults
Stockage des résultats avec possibilité d'attachements (radios, analyses).

### Section 7 : Ventes et Facturation

#### Sales / SaleItems
Système de facturation flexible :
- Ventes nominatives ou anonymes
- Gestion des remises
- Statuts de paiement progressifs

### Section 8 : Paiements et Encaissements

#### PaymentMethods
Méthodes adaptées au contexte (Espèces, Orange Money, MTN Money).

#### Payments
Système de paiement unifié utilisant les références croisées pour lier aux ventes, soins ou examens.

#### Financiers / CashHandovers
Gestion des remises aux financiers avec tracking complet des mouvements de caisse.

### Section 9 : Audit et Traçabilité

#### AuditLog
Journal centralisé des actions critiques du système. Complète les champs d'audit des tables avec une vue d'ensemble.

### Section 10 : Tables de Rapports

Contrairement aux vues traditionnelles, ces tables sont physiques avec ID et audit :

#### rpt_UserCenterDetails
Détails des affectations utilisateur-centre

#### rpt_ActiveSessions
Sessions en cours

#### rpt_StockStatus
État temps réel des stocks avec alertes

#### rpt_FinancialActivity
Synthèse financière par centre et par période

#### rpt_CaregiverPerformance
Performance des soignants

## Procédures Stockées

#### sp_CleanExpiredSessions
Nettoyage automatique des sessions expirées (> 12h)

#### sp_UpdateStockReports
Mise à jour des rapports de stock

## Contraintes et Règles Métier

### Contraintes d'Intégrité
- Unicité de l'email utilisateur
- Un produit = un seul stock par centre
- Validation des énumérations (statuts, types, etc.)

### Règles de Sécurité
- Mots de passe toujours hashés
- Tracking IP des sessions
- Audit obligatoire de toutes les modifications

### Règles Métier
- SuperAdmin peut tout modifier
- Sessions limitées à 12h
- Réinitialisation mot de passe par admin uniquement
- Multi-affectation possible pour tous les utilisateurs

## Index et Performances

Les index sont optimisés pour les requêtes fréquentes :
- Recherches utilisateur (email, nom)
- Consultation stocks par centre
- Génération rapports financiers
- Lookup patients par téléphone

## Évolutivité

L'architecture supporte facilement :
- Ajout de nouveaux types de soins/examens
- Extension multi-centres
- Intégration de documents numériques
- Modules spécialisés (laboratoire, radiologie)

## Points Techniques Importants

1. **Pas de Triggers** : Toute la logique d'audit est gérée dans le code C# pour un contrôle total
2. **Tables vs Vues** : Les rapports sont stockés physiquement pour historisation et performance
3. **Flexibilité des Rôles** : Un même utilisateur peut être SuperAdmin dans un centre et MedicalStaff dans un autre
4. **Références Croisées** : Système intelligent de liaison entre mouvements de stock et leurs causes

Cette architecture équilibre complexité fonctionnelle et simplicité d'utilisation, créant un système robuste et évolutif adapté aux besoins spécifiques des petits hôpitaux camerounais.