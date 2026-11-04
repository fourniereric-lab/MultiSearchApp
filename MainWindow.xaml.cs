using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;

namespace MultiSearchApp;

public sealed partial class MainWindow : Window
{
    private readonly List<SearchEngine> _searchEngines =
    [
        new SearchEngine("Google", "https://www.google.com/search?q={0}", "Web"),
        new SearchEngine("Bing", "https://www.bing.com/search?q={0}", "Web"),
        new SearchEngine("DuckDuckGo", "https://duckduckgo.com/?q={0}", "Web"),
        new SearchEngine("Qwant", "https://www.qwant.com/?q={0}&t=web", "Web"),
        new SearchEngine("YouTube", "https://www.youtube.com/results?search_query={0}", "Vidéos"),
        new SearchEngine("Google Images", "https://www.google.com/search?q={0}&tbm=isch", "Images"),
        new SearchEngine("Google News", "https://news.google.com/search?q={0}", "Actualités"),
        new SearchEngine("Ecosia", "https://www.ecosia.org/search?q={0}", "Privé"),
        new SearchEngine("Startpage", "https://www.startpage.com/sp/search?query={0}", "Privé")
    ];

    private readonly List<string> _searchHistory = [];
    private readonly List<string> _favoriteQueries = [];
    private bool _areAllEnginesSelected = true;

    public MainWindow()
    {
        InitializeComponent();
        (AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)?.Maximize();
        InitializeSearchEngines();
        UpdateFavoriteToggleState();
        UpdateStatus("Prêt — Entrez une requête et lancez une recherche");
    }

    private void InitializeSearchEngines()
    {
        EnginePanel.Children.Clear();

        foreach (var group in GetEngineGroups())
        {
            var categoryColor = GetCategoryColor(group.Key);

            var groupBorder = new Border
            {
                BorderBrush = new SolidColorBrush(categoryColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 12, 0)
            };

            var groupPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            var badge = new Border
            {
                Background = new SolidColorBrush(categoryColor),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = group.Key,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 11
                }
            };

            groupPanel.Children.Add(badge);

            foreach (var engine in group)
            {
                var checkBox = new CheckBox
                {
                    Content = engine.Name,
                    IsChecked = true,
                    Margin = new Thickness(0),
                    MinWidth = 90,
                    Padding = new Thickness(6, 2, 6, 2),
                    Tag = engine
                };

                checkBox.Checked += (_, _) => UpdateEngineCountText();
                checkBox.Unchecked += (_, _) => UpdateEngineCountText();

                groupPanel.Children.Add(checkBox);
            }

            groupBorder.Child = groupPanel;
            EnginePanel.Children.Add(groupBorder);
        }

        UpdateEngineCountText();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder non-bloquant: la recherche reste fonctionnelle.
    }

    private async void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowQueryListDialogAsync(_searchHistory, "Aucun historique pour le moment.", "Historique de recherche");
    }

    private async void FavoritesButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowQueryListDialogAsync(_favoriteQueries, "Aucun favori pour le moment.", "Mes favoris");
    }

    private async void FavoriteToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            UpdateStatus("Saisissez une recherche avant d’ajouter un favori.");
            return;
        }

        var isFavorite = _favoriteQueries.Any(existing => string.Equals(existing, query, StringComparison.OrdinalIgnoreCase));
        if (isFavorite)
        {
            _favoriteQueries.RemoveAll(x => string.Equals(x, query, StringComparison.OrdinalIgnoreCase));
            UpdateStatus($"Le favori \"{query}\" a été retiré.");
        }
        else
        {
            _favoriteQueries.Add(query);
            UpdateStatus($"Le favori \"{query}\" a été ajouté.");
        }

        UpdateFavoriteToggleState();
    }

    private async Task ShowQueryListDialogAsync(List<string> entries, string emptyMessage, string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            CloseButtonText = "Fermer",
            XamlRoot = this.Content is FrameworkElement root ? root.XamlRoot : null
        };

        if (entries.Count == 0)
        {
            dialog.Content = new TextBlock
            {
                Text = emptyMessage,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            await dialog.ShowAsync();
            return;
        }

        var listView = new ListView
        {
            ItemsSource = entries,
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 420
        };

        listView.SelectionChanged += (_, args) =>
        {
            if (args.AddedItems.Count == 0 || args.AddedItems[0] is not string selectedQuery)
            {
                return;
            }

            SearchBox.Text = selectedQuery;
            _ = RunSearchAsync();
            dialog.Hide();
        };

        dialog.Content = listView;
        await dialog.ShowAsync();
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await RunSearchAsync();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            UpdateStatus("Saisie en cours...");
        }

        UpdateFavoriteToggleState();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            UpdateStatus("Veuillez saisir un texte de recherche.");
            return;
        }

        AddToHistory(query);
        UpdateFavoriteToggleState();

        var selectedEngines = GetSelectedEngines();
        if (selectedEngines.Count == 0)
        {
            UpdateStatus("Sélectionnez au moins un moteur.");
            return;
        }

        ResultsTabView.TabItems.Clear();

        var initializedEnginesCount = 0;

        foreach (var engine in selectedEngines)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = string.Format(engine.SearchUrlTemplate, encodedQuery);

            var webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                DefaultBackgroundColor = Microsoft.UI.Colors.White
            };

            var diagnosticText = new TextBlock
            {
                Text = $"Chargement de {engine.Name}...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.WrapWholeWords,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray)
            };

            var webViewHost = new Grid
            {
                MinWidth = 200,
                MinHeight = 200,
                Background = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
            webViewHost.Children.Add(webView);
            webViewHost.Children.Add(diagnosticText);

            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.NavigationCompleted += (_, args) =>
                {
                    if (args.IsSuccess)
                    {
                        diagnosticText.Visibility = Visibility.Collapsed;
                        return;
                    }

                    webView.Visibility = Visibility.Collapsed;
                    diagnosticText.Visibility = Visibility.Visible;
                    diagnosticText.Text = $"Erreur sur {engine.Name} : {args.WebErrorStatus}";
                    UpdateStatus($"{engine.Name} : navigation échouée ({args.WebErrorStatus}).");
                };

                webView.Source = new Uri(searchUrl);
            }
            catch (Exception ex) when (ex is InvalidOperationException or COMException)
            {
                UpdateStatus("Impossible d'initialiser WebView2. Vérifiez que Microsoft Edge WebView2 Runtime est installé.");
                continue;
            }

            var tab = new TabViewItem
            {
                Header = engine.Name,
                Content = webViewHost,
                IsClosable = true
            };

            ResultsTabView.TabItems.Add(tab);
            initializedEnginesCount++;
        }

        if (ResultsTabView.TabItems.Count > 0)
        {
            ResultsTabView.SelectedIndex = 0;
            UpdateStatus($"Recherche lancée pour \"{query}\" sur {initializedEnginesCount} moteur(s).");
            return;
        }

        UpdateStatus("Aucun résultat n'a pu être affiché : WebView2 n'est pas disponible.");
    }

    private List<SearchEngine> GetSelectedEngines()
    {
        var selected = new List<SearchEngine>();

        foreach (var child in EnginePanel.Children)
        {
            if (child is not Border groupBorder || groupBorder.Child is not StackPanel groupPanel)
            {
                continue;
            }

            foreach (var innerChild in groupPanel.Children)
            {
                if (innerChild is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is SearchEngine engine)
                {
                    selected.Add(engine);
                }
            }
        }

        return selected;
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        _areAllEnginesSelected = !_areAllEnginesSelected;

        foreach (var child in EnginePanel.Children)
        {
            if (child is not Border groupBorder || groupBorder.Child is not StackPanel groupPanel)
            {
                continue;
            }

            foreach (var innerChild in groupPanel.Children)
            {
                if (innerChild is CheckBox checkBox)
                {
                    checkBox.IsChecked = _areAllEnginesSelected;
                }
            }
        }

        UpdateEngineCountText();
        UpdateStatus(_areAllEnginesSelected ? "Tous les moteurs sont sélectionnés." : "Tous les moteurs sont désélectionnés.");
    }

    private async void ResultsTabView_AddTabButtonClick(TabView sender, object args)
    {
        await RunSearchAsync();
    }

    private void ResultsTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
        UpdateStatus(sender.TabItems.Count == 0 ? "Aucun onglet de résultat ouvert." : "Onglet fermé.");
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        ResultsTabView.TabItems.Clear();
        UpdateStatus("Tous les onglets ont été fermés.");
    }

    private void AddToHistory(string query)
    {
        if (_searchHistory.Any(existing => string.Equals(existing, query, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _searchHistory.Insert(0, query);
        if (_searchHistory.Count > 20)
        {
            _searchHistory.RemoveAt(_searchHistory.Count - 1);
        }
    }

    private void UpdateFavoriteToggleState()
    {
        var query = SearchBox.Text?.Trim();
        var isFavorite = !string.IsNullOrWhiteSpace(query) &&
            _favoriteQueries.Any(existing => string.Equals(existing, query, StringComparison.OrdinalIgnoreCase));

        FavoriteToggleText.Text = isFavorite ? "Favori ✓" : "Favori";
        FavoriteToggleButton.Background = isFavorite
            ? new SolidColorBrush(Microsoft.UI.Colors.Gold)
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void UpdateEngineCountText()
    {
        var selectedCount = 0;

        foreach (var child in EnginePanel.Children)
        {
            if (child is not Border groupBorder || groupBorder.Child is not StackPanel groupPanel)
            {
                continue;
            }

            foreach (var innerChild in groupPanel.Children)
            {
                if (innerChild is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    selectedCount++;
                }
            }
        }

        EngineCountText.Text = $"{selectedCount}/{_searchEngines.Count} moteurs";
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = message;
    }

    private static Windows.UI.Color GetCategoryColor(string category) => category switch
    {
        "Web" => Windows.UI.Color.FromArgb(255, 37, 99, 235),
        "Vidéos" => Windows.UI.Color.FromArgb(255, 220, 38, 38),
        "Images" => Windows.UI.Color.FromArgb(255, 147, 51, 234),
        "Actualités" => Windows.UI.Color.FromArgb(255, 217, 119, 6),
        "Privé" => Windows.UI.Color.FromArgb(255, 22, 163, 74),
        _ => Windows.UI.Color.FromArgb(255, 100, 116, 139)
    };

    private List<IGrouping<string, SearchEngine>> GetEngineGroups()
    {
        return _searchEngines
            .GroupBy(engine => engine.Category)
            .OrderBy(group => group.Key switch
            {
                "Web" => 0,
                "Vidéos" => 1,
                "Images" => 2,
                "Actualités" => 3,
                "Privé" => 4,
                _ => 5
            })
            .ToList();
    }

    private sealed record SearchEngine(string Name, string SearchUrlTemplate, string Category);
}
