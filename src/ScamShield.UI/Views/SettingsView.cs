using ScamShield.UI.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ScamShield.UI.Views
{
    /// <summary>
    /// Écran des paramètres construit entièrement en code C# WPF (sans XAML).
    /// Pourquoi : Permet de gérer les dossiers surveillés, le niveau de sensibilité
    /// et la liste blanche avec une mise en page responsive en Grid (*) et des animations de 220 ms.
    /// </summary>
    public class SettingsView : UserControl
    {
        // Police unique imposée pour l'ensemble de l'interface.
        private static readonly FontFamily AppFontFamily = new("Segoe UI");

        // Ressources de couleurs partagées (thème sombre moderne + accent bleu-cyan).
        private static readonly Color BgWindowColor = ColorFromHex("#0B0F17");
        private static readonly Color BgSurfaceColor = ColorFromHex("#121826");
        private static readonly Color BgSurfaceElevatedColor = ColorFromHex("#182032");
        private static readonly Color BorderSubtleColor = ColorFromHex("#243049");
        private static readonly Color TextPrimaryColor = ColorFromHex("#F1F5F9");
        private static readonly Color TextSecondaryColor = ColorFromHex("#94A3B8");
        private static readonly Color TextMutedColor = ColorFromHex("#64748B");
        private static readonly Color AccentCyanColor = ColorFromHex("#06B6D4");
        private static readonly Color AccentCyanHoverColor = ColorFromHex("#22D3EE");
        private static readonly Color DangerColor = ColorFromHex("#EF4444");

        private static readonly Brush BgWindowBrush = FreezeBrush(new SolidColorBrush(BgWindowColor));
        private static readonly Brush BgSurfaceBrush = FreezeBrush(new SolidColorBrush(BgSurfaceColor));
        private static readonly Brush BgSurfaceElevatedBrush = FreezeBrush(new SolidColorBrush(BgSurfaceElevatedColor));
        private static readonly Brush BorderSubtleBrush = FreezeBrush(new SolidColorBrush(BorderSubtleColor));
        private static readonly Brush TextPrimaryBrush = FreezeBrush(new SolidColorBrush(TextPrimaryColor));
        private static readonly Brush TextSecondaryBrush = FreezeBrush(new SolidColorBrush(TextSecondaryColor));
        private static readonly Brush TextMutedBrush = FreezeBrush(new SolidColorBrush(TextMutedColor));
        private static readonly Brush AccentCyanBrush = FreezeBrush(new SolidColorBrush(AccentCyanColor));
        private static readonly Brush AccentCyanHoverBrush = FreezeBrush(new SolidColorBrush(AccentCyanHoverColor));
        private static readonly Brush DangerBrush = FreezeBrush(new SolidColorBrush(DangerColor));

        private StackPanel _watchedFoldersPanel = null!;
        private StackPanel _allowListPanel = null!;
        private SettingsViewModel? _viewModel;

        /// <summary>
        /// Initialise SettingsView et assemble l'interface de configuration en pur code C#.
        /// Pourquoi : Offre un écran complet relié à SettingsViewModel via Binding sans fichier XAML.
        /// </summary>
        public SettingsView()
        {
            FontFamily = AppFontFamily;
            Foreground = TextPrimaryBrush;
            Background = BgWindowBrush;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            DataContextChanged += OnSettingsViewDataContextChanged;
            Loaded += OnSettingsViewLoaded;

            Content = BuildRootLayout();

            if (DataContext is null)
            {
                DataContext = new SettingsViewModel();
            }
        }

        /// <summary>
        /// Construit la grille principale en étoiles (*) avec l'en-tête, la sensibilité et les deux listes.
        /// Pourquoi : S'adapte proprement de 1280x720 à 1920x1080 et en plein écran sans largeurs fixes.
        /// </summary>
        private UIElement BuildRootLayout()
        {
            var rootGrid = new Grid
            {
                Margin = new Thickness(24)
            };

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 250 });

            UIElement header = BuildHeaderSection();
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            Border sensitivityCard = BuildSensitivityCard();
            sensitivityCard.Margin = new Thickness(0, 16, 0, 16);
            Grid.SetRow(sensitivityCard, 1);
            rootGrid.Children.Add(sensitivityCard);

            Grid listsGrid = BuildListsSplitGrid();
            Grid.SetRow(listsGrid, 2);
            rootGrid.Children.Add(listsGrid);

            return rootGrid;
        }

        /// <summary>
        /// Construit l'en-tête avec le titre, le message d'état et les boutons Recharger / Enregistrer.
        /// Pourquoi : Permet d'enregistrer les modifications ou de recharger la configuration à tout moment.
        /// </summary>
        private UIElement BuildHeaderSection()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleBlock = new TextBlock
            {
                Text = "Paramètres de protection",
                FontFamily = AppFontFamily,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush
            };
            titleStack.Children.Add(titleBlock);

            var statusBlock = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            statusBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(SettingsViewModel.StatusMessage)));
            titleStack.Children.Add(statusBlock);

            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };

            Button reloadButton = CreateStyledButton(
                label: "Recharger",
                backgroundBrush: BgSurfaceElevatedBrush,
                foregroundBrush: TextPrimaryBrush,
                borderBrush: BorderSubtleBrush,
                commandBindingPath: nameof(SettingsViewModel.ReloadSettingsCommand));
            reloadButton.Margin = new Thickness(0, 0, 10, 0);

            Button saveButton = CreateStyledButton(
                label: "Enregistrer",
                backgroundBrush: AccentCyanBrush,
                foregroundBrush: BgWindowBrush,
                borderBrush: AccentCyanBrush,
                commandBindingPath: nameof(SettingsViewModel.SaveSettingsCommand));

            buttonsPanel.Children.Add(reloadButton);
            buttonsPanel.Children.Add(saveButton);

            Grid.SetColumn(buttonsPanel, 1);
            grid.Children.Add(buttonsPanel);

            return grid;
        }

        /// <summary>
        /// Construit la carte de sélection du niveau de sensibilité (Faible, Équilibrée, Élevée).
        /// Pourquoi : Permet de régler simplement la vigilance de l'analyse sans jargon technique.
        /// </summary>
        private Border BuildSensitivityCard()
        {
            Border card = CreateCardContainer();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var title = new TextBlock
            {
                Text = "Niveau de sensibilité",
                FontFamily = AppFontFamily,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush
            };
            textStack.Children.Add(title);

            var description = new TextBlock
            {
                Text = "Équilibrée convient à un usage quotidien. Élevée signale davantage de fichiers inhabituels.",
                FontFamily = AppFontFamily,
                FontSize = 12,
                Foreground = TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            textStack.Children.Add(description);

            Grid.SetColumn(textStack, 0);
            grid.Children.Add(textStack);

            var comboBox = new ComboBox
            {
                FontFamily = AppFontFamily,
                FontSize = 13,
                MinWidth = 170,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            comboBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(SettingsViewModel.SensitivityLevels)));
            comboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(SettingsViewModel.SelectedSensitivity))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            Grid.SetColumn(comboBox, 1);
            grid.Children.Add(comboBox);

            card.Child = grid;
            return card;
        }

        /// <summary>
        /// Construit la zone à deux colonnes (*) pour les dossiers surveillés et la liste blanche.
        /// Pourquoi : Organise clairement les deux listes configurables de manière responsive.
        /// </summary>
        private Grid BuildListsSplitGrid()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });

            Border watchedCard = BuildEditableListCard(
                title: "DOSSIERS SURVEILLÉS",
                subtitle: "Dossiers contrôlés dès l'arrivée ou la modification d'un fichier.",
                placeholderHint: @"Ex. : C:\Users\Public\Documents",
                inputBindingPath: nameof(SettingsViewModel.NewWatchedFolder),
                addCommandBindingPath: nameof(SettingsViewModel.AddWatchedFolderCommand),
                isWatchedFolders: true);

            Border allowCard = BuildEditableListCard(
                title: "LISTE BLANCHE",
                subtitle: "Chemins ou fichiers de confiance exclus des alertes répétées.",
                placeholderHint: @"Ex. : C:\Outils\MonOutilInterne.exe",
                inputBindingPath: nameof(SettingsViewModel.NewAllowListEntry),
                addCommandBindingPath: nameof(SettingsViewModel.AddAllowListCommand),
                isWatchedFolders: false);

            Grid.SetColumn(watchedCard, 0);
            Grid.SetColumn(allowCard, 2);

            grid.Children.Add(watchedCard);
            grid.Children.Add(allowCard);

            return grid;
        }

        /// <summary>
        /// Construit une carte d'édition de liste avec champ de saisie, bouton Ajouter et liste défilante.
        /// Pourquoi : Réutilise la même structure propre pour les dossiers surveillés et la liste blanche.
        /// </summary>
        private Border BuildEditableListCard(
            string title,
            string subtitle,
            string placeholderHint,
            string inputBindingPath,
            string addCommandBindingPath,
            bool isWatchedFolders)
        {
            Border card = CreateCardContainer();

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 140 });

            var headerStack = new StackPanel();
            headerStack.Children.Add(new TextBlock
            {
                Text = title,
                FontFamily = AppFontFamily,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = AccentCyanHoverBrush
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontFamily = AppFontFamily,
                FontSize = 12,
                Foreground = TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            });

            Grid.SetRow(headerStack, 0);
            layout.Children.Add(headerStack);

            var inputGrid = new Grid
            {
                Margin = new Thickness(0, 12, 0, 12)
            };
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                Foreground = TextPrimaryBrush,
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 7, 10, 7),
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = placeholderHint
            };
            textBox.SetBinding(TextBox.TextProperty, new Binding(inputBindingPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            Grid.SetColumn(textBox, 0);
            inputGrid.Children.Add(textBox);

            Button addButton = CreateStyledButton(
                label: "Ajouter",
                backgroundBrush: AccentCyanBrush,
                foregroundBrush: BgWindowBrush,
                borderBrush: AccentCyanBrush,
                commandBindingPath: addCommandBindingPath);
            addButton.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(addButton, 1);
            inputGrid.Children.Add(addButton);

            Grid.SetRow(inputGrid, 1);
            layout.Children.Add(inputGrid);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var listPanel = new StackPanel();
            scrollViewer.Content = listPanel;

            if (isWatchedFolders)
            {
                _watchedFoldersPanel = listPanel;
            }
            else
            {
                _allowListPanel = listPanel;
            }

            Grid.SetRow(scrollViewer, 2);
            layout.Children.Add(scrollViewer);

            card.Child = layout;
            return card;
        }

        /// <summary>
        /// Reconstruit la liste visuelle des dossiers surveillés.
        /// Pourquoi : Maintient l'affichage synchronisé avec WatchedFolders.
        /// </summary>
        private void RebuildWatchedFoldersList()
        {
            if (_watchedFoldersPanel is null || _viewModel is null)
            {
                return;
            }

            RebuildStringList(_watchedFoldersPanel, _viewModel.WatchedFolders, _viewModel.RemoveWatchedFolderCommand);
        }

        /// <summary>
        /// Reconstruit la liste visuelle de la liste blanche.
        /// Pourquoi : Maintient l'affichage synchronisé avec AllowList.
        /// </summary>
        private void RebuildAllowList()
        {
            if (_allowListPanel is null || _viewModel is null)
            {
                return;
            }

            RebuildStringList(_allowListPanel, _viewModel.AllowList, _viewModel.RemoveAllowListCommand);
        }

        /// <summary>
        /// Remplit un conteneur StackPanel avec les lignes d'une collection et un bouton Retirer.
        /// Pourquoi : Évite la duplication de code entre dossiers surveillés et liste blanche.
        /// </summary>
        private static void RebuildStringList(
            StackPanel targetPanel,
            ObservableCollection<string> items,
            ICommand removeCommand)
        {
            targetPanel.Children.Clear();

            if (items.Count == 0)
            {
                targetPanel.Children.Add(new TextBlock
                {
                    Text = "Aucun élément configuré pour le moment.",
                    FontFamily = AppFontFamily,
                    FontSize = 12,
                    Foreground = TextMutedBrush,
                    Margin = new Thickness(2, 4, 2, 4)
                });
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                string entry = items[i];
                var borderBrush = new SolidColorBrush(BorderSubtleColor);

                var row = new Border
                {
                    Background = BgSurfaceElevatedBrush,
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, i == items.Count - 1 ? 0 : 8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var textBlock = new TextBlock
                {
                    Text = entry,
                    FontFamily = AppFontFamily,
                    FontSize = 12.5,
                    Foreground = TextPrimaryBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = entry
                };
                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);

                var removeButton = new Button
                {
                    Content = "Retirer",
                    FontFamily = AppFontFamily,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = DangerBrush,
                    Background = Brushes.Transparent,
                    BorderBrush = DangerBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(10, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Command = removeCommand,
                    CommandParameter = entry,
                    Template = CreateRoundedButtonTemplate()
                };
                Grid.SetColumn(removeButton, 1);
                grid.Children.Add(removeButton);

                row.Child = grid;

                row.MouseEnter += (_, _) =>
                {
                    borderBrush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(AccentCyanColor, TimeSpan.FromMilliseconds(220)));
                };

                row.MouseLeave += (_, _) =>
                {
                    borderBrush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(BorderSubtleColor, TimeSpan.FromMilliseconds(220)));
                };

                targetPanel.Children.Add(row);
            }
        }

        /// <summary>
        /// Crée un bouton arrondi lié au ViewModel en pur code C#.
        /// Pourquoi : Maintient une apparence cohérente sans fichier XAML.
        /// </summary>
        private static Button CreateStyledButton(
            string label,
            Brush backgroundBrush,
            Brush foregroundBrush,
            Brush borderBrush,
            string commandBindingPath)
        {
            var button = new Button
            {
                Content = label,
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = foregroundBrush,
                Background = backgroundBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = Cursors.Hand,
                Effect = CreateSoftShadow(0.22, 10)
            };

            button.SetBinding(Button.CommandProperty, new Binding(commandBindingPath));
            button.Template = CreateRoundedButtonTemplate();
            return button;
        }

        /// <summary>
        /// Crée un conteneur de carte standard avec coins arrondis et ombre légère.
        /// Pourquoi : Harmonise l'esthétique sombre des sections de paramètres.
        /// </summary>
        private static Border CreateCardContainer()
        {
            return new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Effect = CreateSoftShadow()
            };
        }

        /// <summary>
        /// Crée un ControlTemplate arrondi en pur code C#.
        /// Pourquoi : Garantit des coins arrondis propres sans XAML.
        /// </summary>
        private static ControlTemplate CreateRoundedButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            borderFactory.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            borderFactory.SetBinding(Border.PaddingProperty, new Binding(nameof(Control.Padding))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            template.VisualTree = borderFactory;

            var disabledTrigger = new Trigger
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Crée une ombre portée légère pour structurer les cartes et boutons.
        /// Pourquoi : Ajoute du relief discret sur le fond sombre.
        /// </summary>
        private static DropShadowEffect CreateSoftShadow(double opacity = 0.28, double blurRadius = 14)
        {
            return new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = opacity,
                BlurRadius = blurRadius,
                ShadowDepth = 3,
                Direction = 270
            };
        }

        /// <summary>
        /// Synchronise les abonnements aux collections lorsque le DataContext change.
        /// Pourquoi : Maintient les listes visuelles à jour avec le SettingsViewModel actif.
        /// </summary>
        private void OnSettingsViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SettingsViewModel oldVm)
            {
                oldVm.WatchedFolders.CollectionChanged -= OnWatchedFoldersChanged;
                oldVm.AllowList.CollectionChanged -= OnAllowListChanged;
            }

            _viewModel = e.NewValue as SettingsViewModel;

            if (_viewModel is not null)
            {
                _viewModel.WatchedFolders.CollectionChanged += OnWatchedFoldersChanged;
                _viewModel.AllowList.CollectionChanged += OnAllowListChanged;
            }

            RebuildWatchedFoldersList();
            RebuildAllowList();
        }

        /// <summary>
        /// Reconstruit la liste des dossiers surveillés lorsqu'elle change.
        /// Pourquoi : Reflète immédiatement les ajouts et suppressions de dossiers.
        /// </summary>
        private void OnWatchedFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildWatchedFoldersList();
        }

        /// <summary>
        /// Reconstruit la liste blanche lorsqu'elle change.
        /// Pourquoi : Reflète immédiatement les ajouts et suppressions dans la liste blanche.
        /// </summary>
        private void OnAllowListChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildAllowList();
        }

        /// <summary>
        /// Synchronise les listes dès que le contrôle est chargé.
        /// Pourquoi : Garantit l'affichage des éléments chargés au démarrage.
        /// </summary>
        private void OnSettingsViewLoaded(object sender, RoutedEventArgs e)
        {
            RebuildWatchedFoldersList();
            RebuildAllowList();
        }

        /// <summary>
        /// Convertit une couleur hexadécimale (#RRGGBB) en Color WPF.
        /// Pourquoi : Centralise les couleurs de thème.
        /// </summary>
        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        /// <summary>
        /// Fige un Brush pour optimiser les performances et éviter les allocations répétées.
        /// Pourquoi : Améliore la fluidité de rendu de la vue.
        /// </summary>
        private static T FreezeBrush<T>(T brush) where T : Brush
        {
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }
            return brush;
        }
    }
}