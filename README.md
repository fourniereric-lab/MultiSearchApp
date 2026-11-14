# 🔍 MultiSearch v2.0 — Application de Recherche Multi-Moteur

Application de recherche multi-moteur disponible sur **Windows 10/11** (WinUI 3) et **Android** (.NET MAUI). L’application Android affiche les résultats dans une WebView intégrée et permet de passer d’un moteur sélectionné à un autre.

## 📱 Android

Le projet Android est séparé du projet WinUI afin de conserver la version Windows inchangée. Il reprend les moteurs, la sélection multiple, l’historique et les favoris (conservés en mémoire pendant la session).

Prérequis : .NET SDK 10 avec la charge de travail **.NET MAUI / Android**, et un émulateur Android ou un appareil connecté.

```bash
dotnet workload install maui-android
dotnet build Android/MultiSearchApp.Android.csproj -f net10.0-android
```

Dans Visual Studio, ouvrez `Android/MultiSearchApp.Android.csproj`, choisissez un appareil ou un émulateur Android, puis lancez l’application. La permission d’accès Internet est déclarée dans le manifeste Android.

---

## ✨ Nouveautés v2.0

| Amélioration | Détail |
|---|---|
| **Fenêtre centrée** | Démarrage centré automatiquement sur l'écran principal |
| **Bouton Tout sélectionner** | Bascule rapide tous moteurs ON/OFF |
| **Compteur moteurs amélioré** | Affichage coloré en bleu accent |
| **Historique enrichi** | Affiche date + moteurs utilisés |
| **Statut dynamique** | Icône + message contextuel dans la barre de statut |
| **Onglet accueil v2** | Guide en 4 étapes, mise en page améliorée |
| **Toolbar par onglet** | Recharger · Ouvrir navigateur · Ajouter favoris |
| **Config NuGet stable** | Toutes les erreurs de packages corrigées |

---

## 🌐 27 Moteurs — 6 Catégories

| Catégorie | Moteurs |
|---|---|
| **Général** | Google · Bing · DuckDuckGo · Yahoo · Ecosia · Brave · Qwant · Startpage |
| **Académique** | Google Scholar · PubMed · Semantic Scholar · arXiv |
| **Dev** | GitHub · Stack Overflow · MDN · NuGet |
| **Médias** | YouTube · Wikipedia FR · Reddit · Twitter/X |
| **Shopping** | Amazon FR · eBay FR · Fnac |
| **Images** | Google Images · Unsplash · Flickr |

---

## 🛠️ Prérequis

| Outil | Version |
|---|---|
| Windows | 10 (1809 min) ou Windows 11 |
| .NET SDK | **8.0+** |
| Visual Studio | **2022 ou 2026** |
| Windows App SDK | **1.6** (installé via NuGet) |
| WebView2 Runtime | Inclus dans Windows 11 / téléchargeable séparément |

---

## 🚀 Démarrage rapide

### 1. Ouvrir le projet
```
Fichier → Ouvrir → Projet/Solution → MultiSearchApp.csproj
```

### 2. Restaurer les packages NuGet
```bash
dotnet restore
```

### 3. Sélectionner la plateforme x64 dans Visual Studio

### 4. Lancer
```
F5  ou  Déboguer → Démarrer le débogage
```

---

## 📦 Packages NuGet (configuration finale stable)

```xml
<WindowsSdkPackageVersion>10.0.22621.56</WindowsSdkPackageVersion>
<EnableDefaultPageItems>false</EnableDefaultPageItems>

Microsoft.WindowsAppSDK          1.6.250228000
Microsoft.Windows.SDK.BuildTools 10.0.22621.756
Microsoft.Web.WebView2           1.0.2849.39
CommunityToolkit.Mvvm            8.3.2
CommunityToolkit.WinUI           7.1.2
```

### Erreurs corrigées dans cette configuration

| Erreur | Correction appliquée |
|---|---|
| `WindowsSdkPackageVersion` trop ancienne | → `10.0.22621.56` |
| `NETSDK1022` doublons XAML | → `EnableDefaultPageItems=false` |
| `CommunityToolkit.WinUI.Controls` introuvable | → `CommunityToolkit.WinUI` 7.1.2 |
| MVVM Toolkit exige SDK.NET.Ref `.56` | → `WindowsSdkPackageVersion` mis à jour |

---

## 🎨 Personnalisation

### Ajouter un moteur de recherche
Dans `MainWindow.xaml.cs`, méthode `BuildEngineList()` :
```csharp
new() {
    Name       = "MonMoteur",
    Icon       = "🔎",
    UrlPattern = "https://monmoteur.com/search?q={0}",
    Color      = "#FF5733",
    IsSelected = false,
    Category   = "Général"
},
```

### Modifier la taille de la fenêtre
Dans `App.xaml.cs` :
```csharp
appWindow.Resize(new SizeInt32(1600, 1000));
```

### Activer le thème sombre par défaut
Dans `MainWindow.xaml.cs`, constructeur :
```csharp
if (this.Content is FrameworkElement root)
    root.RequestedTheme = ElementTheme.Dark;
_isDarkTheme = true;
ThemeIcon.Glyph = "\uE706";
```

---

## 📐 Architecture

```
MultiSearchApp/
├── App.xaml / App.xaml.cs       → Point d'entrée, taille & centrage fenêtre
├── MainWindow.xaml               → Interface XAML (layout, contrôles)
├── MainWindow.xaml.cs            → Logique C# complète
├── MultiSearchApp.csproj         → Projet .NET 8, packages NuGet stables
├── app.manifest                  → DPI PerMonitorV2, Windows 10/11
└── README.md                     → Cette documentation
```

### Classes internes

| Classe | Rôle |
|---|---|
| `SearchEngine` | Modèle moteur (nom, icône, URL pattern, catégorie) |
| `SearchHistoryItem` | Entrée historique avec propriétés d'affichage |
| `FavoriteItem` | Favori (titre, URL, moteur source) |

### Méthodes clés

| Méthode | Rôle |
|---|---|
| `BuildEngineList()` | Définit les 27 moteurs |
| `BuildEngineButtons()` | Génère les ToggleButtons par catégorie |
| `ExecuteSearch()` | Lance la recherche multi-moteurs |
| `CreateEngineTab()` | Crée un onglet WebView2 avec toolbar |
| `SetStatus()` | Met à jour la barre de statut |

---

## 🔒 Confidentialité

- Aucune donnée transmise à des serveurs tiers par l'application.
- Requêtes envoyées directement aux moteurs via WebView2.
- Historique et favoris stockés **en mémoire uniquement** (non persistés).

---

## 📋 Roadmap

- [ ] Persistance historique/favoris (JSON local)
- [ ] Raccourcis clavier (Ctrl+T, Ctrl+W, Ctrl+L)
- [ ] Barre de progression de chargement par onglet
- [ ] Profils de moteurs sauvegardables
- [ ] Export résultats en PDF
- [ ] Recherche vocale (Windows Speech API)

---

## 📄 Licence

MIT — Libre d'utilisation, modification et distribution.