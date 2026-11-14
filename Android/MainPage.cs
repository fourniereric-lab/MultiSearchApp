using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace MultiSearchApp.Android;

public sealed class MainPage : ContentPage
{
    private readonly List<SearchEngine> _engines =
    [
        new("Google", "https://www.google.com/search?q={0}", "Web"),
        new("Bing", "https://www.bing.com/search?q={0}", "Web"),
        new("DuckDuckGo", "https://duckduckgo.com/?q={0}", "Web"),
        new("Qwant", "https://www.qwant.com/?q={0}&t=web", "Web"),
        new("YouTube", "https://www.youtube.com/results?search_query={0}", "Vidéos"),
        new("Google Images", "https://www.google.com/search?q={0}&tbm=isch", "Images"),
        new("Google News", "https://news.google.com/search?q={0}", "Actualités"),
        new("Ecosia", "https://www.ecosia.org/search?q={0}", "Privé"),
        new("Startpage", "https://www.startpage.com/sp/search?query={0}", "Privé")
    ];

    private readonly HashSet<SearchEngine> _selectedEngines = [];
    private readonly List<string> _searchHistory = [];
    private readonly List<string> _favoriteQueries = [];
    private readonly Entry _searchEntry = new() { Placeholder = "Que souhaitez-vous rechercher ?", ReturnType = ReturnType.Search };
    private readonly Picker _enginePicker = new() { Title = "Choisir un moteur" };
    private readonly WebView _resultsView = new();
    private readonly Label _statusLabel = new() { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _favoriteButton = new() { Text = "☆", FontSize = 22, Padding = new Thickness(10, 0) };
    private readonly FlexLayout _enginePanel = new() { Wrap = FlexWrap.Wrap, Direction = FlexDirection.Row };
    private readonly Button _selectAllButton = new() { Text = "Tout sélectionner", FontSize = 12, Padding = new Thickness(8, 4) };

    public MainPage()
    {
        Title = "MultiSearch";
        BackgroundColor = Colors.White;

        foreach (var engine in _engines)
        {
            _selectedEngines.Add(engine);
            _enginePanel.Children.Add(CreateEngineButton(engine));
        }

        _enginePicker.ItemDisplayBinding = new Binding(nameof(SearchEngine.Name));
        _enginePicker.SelectedIndexChanged += (_, _) => DisplaySelectedEngine();
        _searchEntry.Completed += async (_, _) => await RunSearchAsync();
        _searchEntry.TextChanged += (_, _) => UpdateSelectionControls();
        _favoriteButton.Clicked += (_, _) => ToggleFavorite();
        _selectAllButton.Clicked += (_, _) => ToggleAllEngines();

        var layout = new Grid
        {
            Padding = new Thickness(12),
            RowSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var historyButton = new Button { Text = "Historique", FontSize = 12, Padding = new Thickness(8, 5) };
        historyButton.Clicked += async (_, _) => await ShowQueryListAsync(_searchHistory, "Historique");
        var favoritesButton = new Button { Text = "Favoris", FontSize = 12, Padding = new Thickness(8, 5) };
        favoritesButton.Clicked += async (_, _) => await ShowQueryListAsync(_favoriteQueries, "Favoris");

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 4
        };
        header.Add(new Label { Text = "MultiSearch", FontSize = 22, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0);
        header.Add(historyButton, 1);
        header.Add(favoritesButton, 2);
        layout.Add(header, 0);

        var searchBar = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 6
        };
        var searchButton = new Button { Text = "Rechercher", Padding = new Thickness(12, 0), BackgroundColor = Color.FromArgb("#2563EB"), TextColor = Colors.White };
        searchButton.Clicked += async (_, _) => await RunSearchAsync();
        searchBar.Add(_searchEntry, 0);
        searchBar.Add(_favoriteButton, 1);
        searchBar.Add(searchButton, 2);
        layout.Add(searchBar, 1);

        var enginesHeader = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        enginesHeader.Add(new Label { Text = "Moteurs de recherche", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0);
        enginesHeader.Add(_selectAllButton, 1);
        layout.Add(enginesHeader, 2);

        var engineScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            HeightRequest = 94,
            Content = _enginePanel
        };
        layout.Add(engineScroll, 3);
        layout.Add(_enginePicker, 4);
        layout.Add(_resultsView, 5);
        layout.Add(_statusLabel, 6);
        Content = layout;
        UpdateSelectionControls();
        SetStatus("Sélectionnez des moteurs et lancez une recherche.");
    }

    private Button CreateEngineButton(SearchEngine engine)
    {
        var button = new Button
        {
            Text = engine.Name,
            FontSize = 12,
            Padding = new Thickness(10, 5),
            CornerRadius = 16,
            Margin = new Thickness(2),
            CommandParameter = engine
        };
        button.Clicked += (_, _) =>
        {
            if (!_selectedEngines.Add(engine))
            {
                _selectedEngines.Remove(engine);
            }

            UpdateSelectionControls();
        };
        UpdateEngineButton(button, engine);
        return button;
    }

    private void UpdateSelectionControls()
    {
        foreach (var child in _enginePanel.Children.OfType<Button>())
        {
            if (child.CommandParameter is SearchEngine engine)
            {
                UpdateEngineButton(child, engine);
            }
        }

        _selectAllButton.Text = _selectedEngines.Count == _engines.Count ? "Tout désélectionner" : "Tout sélectionner";
        _favoriteButton.Text = IsFavorite(_searchEntry.Text) ? "★" : "☆";
    }

    private void UpdateEngineButton(Button button, SearchEngine engine)
    {
        var selected = _selectedEngines.Contains(engine);
        button.BackgroundColor = selected ? Color.FromArgb("#DBEAFE") : Colors.LightGray;
        button.TextColor = selected ? Color.FromArgb("#1D4ED8") : Colors.DarkSlateGray;
    }

    private void ToggleAllEngines()
    {
        if (_selectedEngines.Count == _engines.Count)
        {
            _selectedEngines.Clear();
        }
        else
        {
            _selectedEngines.UnionWith(_engines);
        }

        UpdateSelectionControls();
        SetStatus($"{_selectedEngines.Count}/{_engines.Count} moteurs sélectionnés.");
    }

    private async Task RunSearchAsync()
    {
        var query = _searchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            SetStatus("Veuillez saisir un texte de recherche.");
            return;
        }

        if (_selectedEngines.Count == 0)
        {
            SetStatus("Sélectionnez au moins un moteur.");
            return;
        }

        _searchHistory.RemoveAll(item => string.Equals(item, query, StringComparison.OrdinalIgnoreCase));
        _searchHistory.Insert(0, query);
        if (_searchHistory.Count > 20)
        {
            _searchHistory.RemoveAt(_searchHistory.Count - 1);
        }

        _enginePicker.ItemsSource = _engines.Where(_selectedEngines.Contains).ToList();
        _enginePicker.SelectedIndex = 0;
        UpdateSelectionControls();
        DisplaySelectedEngine();
        SetStatus($"Recherche lancée sur {_selectedEngines.Count} moteur(s). Choisissez un moteur pour afficher ses résultats.");
        await Task.CompletedTask;
    }

    private void DisplaySelectedEngine()
    {
        if (_enginePicker.SelectedItem is not SearchEngine engine || string.IsNullOrWhiteSpace(_searchEntry.Text))
        {
            return;
        }

        var encodedQuery = Uri.EscapeDataString(_searchEntry.Text.Trim());
        var searchUrl = string.Format(engine.SearchUrlTemplate, encodedQuery);
        _resultsView.Source = new UrlWebViewSource { Url = searchUrl };
    }

    private void ToggleFavorite()
    {
        var query = _searchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            SetStatus("Saisissez une recherche avant d’ajouter un favori.");
            return;
        }

        if (IsFavorite(query))
        {
            _favoriteQueries.RemoveAll(item => string.Equals(item, query, StringComparison.OrdinalIgnoreCase));
            SetStatus("Favori retiré.");
        }
        else
        {
            _favoriteQueries.Add(query);
            SetStatus("Favori ajouté.");
        }

        UpdateSelectionControls();
    }

    private async Task ShowQueryListAsync(List<string> entries, string title)
    {
        if (entries.Count == 0)
        {
            await DisplayAlertAsync(title, title == "Historique" ? "Aucune recherche récente." : "Aucun favori enregistré.", "Fermer");
            return;
        }

        var selectedQuery = await DisplayActionSheetAsync(title, "Annuler", null, entries.ToArray());
        if (selectedQuery is null or "Annuler" || !entries.Contains(selectedQuery))
        {
            return;
        }

        _searchEntry.Text = selectedQuery;
        UpdateSelectionControls();
        await RunSearchAsync();
    }

    private bool IsFavorite(string? query)
    {
        return !string.IsNullOrWhiteSpace(query) &&
            _favoriteQueries.Any(item => string.Equals(item, query.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private sealed record SearchEngine(string Name, string SearchUrlTemplate, string Category);
}
