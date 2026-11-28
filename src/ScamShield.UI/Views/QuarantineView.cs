using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ScamShield.UI.ViewModels;

namespace ScamShield.UI.Views
{
    /// <summary>
    /// Écran de quarantaine construit entièrement en code C# WPF (sans XAML).
    /// Pourquoi : Permet de consulter les fichiers isolés et de les restaurer ou les supprimer
    /// avec une mise en page responsive en Grid (*) et des animations discrètes de 220 ms.
    /// </summary>
    public class QuarantineView : UserControl
    {
        // Police unique imposée pour l'ensemble de l'interface.
        private static readonly FontFamily AppFontFamily = new("Segoe UI");

        // Ressources de couleurs partagées (thème sombre moderne + accent bleu-cyan + couleurs d'état).
        private static readonly Color BgWindowColor = ColorFromHex("#0B0F17");
        private static readonly Color BgSurfaceColor = ColorFromHex("#121826");
        private static readonly Color BgSurfaceElevatedColor = ColorFromHex("#182032");
        private static readonly Color BorderSubtleColor = ColorFromHex("#243049");
        private static readonly Color TextPrimaryColor = ColorFromHex("#F1F5F9");
        private static readonly Color TextSecondaryColor = ColorFromHex("#94A3B8");
        private static readonly Color TextMutedColor = ColorFromHex("#64748B");
        private static readonly Color AccentCyanColor = ColorFromHex("#06B6D4");
        private static readonly Color AccentCyanHoverColor = ColorFromHex("#22D3EE");
        private static readonly Color ActionRestoreColor = ColorFromHex("#10B981");
        private static readonly Color ActionDeleteColor = ColorFromHex("#EF4444");

        private static readonly Brush BgWindowBrush = FreezeBrush(new SolidColorBrush(BgWindowColor));
        private static readonly Brush BgSurfaceBrush = FreezeBrush(new SolidColorBrush(BgSurfaceColor));
        private static readonly Brush BgSurfaceElevatedBrush = FreezeBrush(new SolidColorBrush(BgSurfaceElevatedColor));
        private static readonly Brush BorderSubtleBrush = FreezeBrush(new SolidColorBrush(BorderSubtleColor));
        private static readonly Brush TextPrimaryBrush = FreezeBrush(new SolidColorBrush(TextPrimaryColor));
        private static readonly Brush TextSecondaryBrush = FreezeBrush(new SolidColorBrush(TextSecondaryColor));
        private static readonly Brush TextMutedBrush = FreezeBrush(new SolidColorBrush(TextMutedColor));
        private static readonly Brush AccentCyanBrush = FreezeBrush(new SolidColorBrush(AccentCyanColor));
        private static readonly Brush AccentCyanHoverBrush = FreezeBrush(new SolidColorBrush(AccentCyanHoverColor));
        private static readonly Brush ActionRestoreBrush = FreezeBrush(new SolidColorBrush(ActionRestoreColor));
        private static readonly Brush ActionDeleteBrush = FreezeBrush(new SolidColorBrush(ActionDeleteColor));

        private StackPanel _quarantineListPanel = null!;
        private QuarantineViewModel? _viewModel;

        /// <summary>
        /// Initialise QuarantineView et assemble l'interface de quarantaine en pur code C#.
        /// Pourquoi : Offre un écran prêt à l'emploi via Binding avec QuarantineViewModel.
        /// </summary>
        public QuarantineView()
        {
            FontFamily = AppFontFamily;
            Foreground = TextPrimaryBrush;
            Background = BgWindowBrush;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            DataContextChanged += OnQuarantineViewDataContextChanged;
            Loaded += OnQuarantineViewLoaded;

            Content = BuildRootLayout();

            if (DataContext is null)
            {
                DataContext = new QuarantineViewModel();
            }
        }

        /// <summary>
        /// Construit la grille principale en étoiles (*) avec l'en-tête, la bannière d'information et la liste des fichiers isolés.
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
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 240 });

            UIElement header = BuildHeaderSection();
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            Border infoBanner = BuildInfoBanner();
            infoBanner.Margin = new Thickness(0, 14, 0, 16);
            Grid.SetRow(infoBanner, 1);
            rootGrid.Children.Add(infoBanner);

            Border listCard = BuildQuarantineListCard();
            Grid.SetRow(listCard, 2);
            rootGrid.Children.Add(listCard);

            return rootGrid;
        }

        /// <summary>
        /// Construit l'en-tête avec le titre, le message d'état et le bouton Actualiser.
        /// Pourquoi : Permet de suivre l'état de la quarantaine et de recharger la liste.
        /// </summary>
        private UIElement BuildHeaderSection()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleBlock = new TextBlock
            {
                Text = "Quarantaine des fichiers",
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
            statusBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(QuarantineViewModel.StatusMessage)));
            titleStack.Children.Add(statusBlock);

            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            Button refreshBtn = CreateStyledButton(
                label: "Actualiser",
                backgroundBrush: AccentCyanBrush,
                foregroundBrush: BgWindowBrush,
                commandBindingPath: nameof(QuarantineViewModel.RefreshCommand),
                commandParameterBindingPath: null);
            refreshBtn.Margin = new Thickness(16, 0, 0, 0);
            refreshBtn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(refreshBtn, 1);
            grid.Children.Add(refreshBtn);

            return grid;
        }

        /// <summary>
        /// Construit une bannière explicative sobre sur le fonctionnement de la quarantaine.
        /// Pourquoi : Aide l'utilisateur non technique à comprendre qu'un fichier isolé ne peut pas s'exécuter par erreur
        /// et qu'il peut être restauré ou supprimé en toute clarté.
        /// </summary>
        private static Border BuildInfoBanner()
        {
            return new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 12),
                Effect = CreateSoftShadow(0.2, 10),
                Child = new TextBlock
                {
                    Text = "Les fichiers placés en quarantaine sont isolés en sécurité. Restaurez un fichier uniquement si vous faites confiance à sa source, ou supprimez-le définitivement.",
                    FontFamily = AppFontFamily,
                    FontSize = 12.5,
                    Foreground = TextSecondaryBrush,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        /// <summary>
        /// Construit la carte principale contenant la liste défilante des fichiers en quarantaine.
        /// Pourquoi : Fournit un espace lisible et adaptable quelle que soit la taille de la fenêtre.
        /// </summary>
        private Border BuildQuarantineListCard()
        {
            var card = new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Effect = CreateSoftShadow()
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _quarantineListPanel = new StackPanel();
            scrollViewer.Content = _quarantineListPanel;
            card.Child = scrollViewer;

            return card;
        }

        /// <summary>
        /// Reconstruit la liste des éléments de quarantaine avec leurs boutons Restaurer et Supprimer.
        /// Pourquoi : Maintient l'affichage synchronisé avec la collection Items du ViewModel.
        /// </summary>
        private void RebuildQuarantineList()
        {
            if (_quarantineListPanel is null || _viewModel is null)
            {
                return;
            }

            _quarantineListPanel.Children.Clear();

            if (_viewModel.Items.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "Aucun fichier en quarantaine pour le moment. Votre système est sain.",
                    FontFamily = AppFontFamily,
                    FontSize = 13.5,
                    Foreground = TextSecondaryBrush,
                    Margin = new Thickness(4, 8, 4, 8)
                };
                _quarantineListPanel.Children.Add(emptyText);
                return;
            }

            for (int i = 0; i < _viewModel.Items.Count; i++)
            {
                QuarantineDisplayItem item = _viewModel.Items[i];
                Border rowCard = CreateQuarantineItemCard(item, isLast: i == _viewModel.Items.Count - 1);
                _quarantineListPanel.Children.Add(rowCard);
            }
        }

        /// <summary>
        /// Crée une carte pour un fichier en quarantaine avec score, motif simple et boutons Restaurer / Supprimer.
        /// Pourquoi : Offre des actions directes, claires et animées au survol (220 ms) sur chaque élément.
        /// </summary>
        private Border CreateQuarantineItemCard(QuarantineDisplayItem item, bool isLast)
        {
            var borderBrush = new SolidColorBrush(BorderSubtleColor);
            var card = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, isLast ? 0 : 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var topLine = new StackPanel { Orientation = Orientation.Horizontal };

            var fileNameBlock = new TextBlock
            {
                Text = item.FileName,
                FontFamily = AppFontFamily,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            topLine.Children.Add(fileNameBlock);

            var scoreBadge = new Border
            {
                Background = FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 239, 68, 68))),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var scoreText = new TextBlock
            {
                Text = $"Risque {item.RiskScorePercent} %",
                FontFamily = AppFontFamily,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = ActionDeleteBrush
            };
            scoreBadge.Child = scoreText;
            topLine.Children.Add(scoreBadge);

            infoStack.Children.Add(topLine);

            var reasonBlock = new TextBlock
            {
                Text = item.SummaryReason,
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                Foreground = TextPrimaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 3)
            };
            infoStack.Children.Add(reasonBlock);

            var metaBlock = new TextBlock
            {
                Text = $"Origine : {item.OriginalPath} • Isolé le {item.QuarantinedAtText}",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextMutedBrush,
                TextWrapping = TextWrapping.Wrap
            };
            infoStack.Children.Add(metaBlock);

            Grid.SetColumn(infoStack, 0);
            grid.Children.Add(infoStack);

            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };

            Button restoreButton = new()
            {
                Content = "Restaurer",
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BgWindowBrush,
                Background = ActionRestoreBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Command = _viewModel?.RestoreCommand,
                CommandParameter = item,
                Template = CreateRoundedButtonTemplate()
            };

            Button deleteButton = new()
            {
                Content = "Supprimer",
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                Background = ActionDeleteBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = System.Windows.Input.Cursors.Hand,
                Command = _viewModel?.DeleteCommand,
                CommandParameter = item,
                Template = CreateRoundedButtonTemplate()
            };

            actionsPanel.Children.Add(restoreButton);
            actionsPanel.Children.Add(deleteButton);

            Grid.SetColumn(actionsPanel, 1);
            grid.Children.Add(actionsPanel);

            card.Child = grid;

            card.MouseEnter += (_, _) =>
            {
                if (_viewModel is not null)
                {
                    _viewModel.SelectedItem = item;
                }

                borderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(AccentCyanColor, TimeSpan.FromMilliseconds(220)));
            };

            card.MouseLeave += (_, _) =>
            {
                borderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(BorderSubtleColor, TimeSpan.FromMilliseconds(220)));
            };

            return card;
        }

        /// <summary>
        /// Crée un bouton stylisé lié au ViewModel en pur code C#.
        /// Pourquoi : Maintient une apparence cohérente et arrondie sans XAML.
        /// </summary>
        private static Button CreateStyledButton(
            string label,
            Brush backgroundBrush,
            Brush foregroundBrush,
            string commandBindingPath,
            string? commandParameterBindingPath)
        {
            var button = new Button
            {
                Content = label,
                FontFamily = AppFontFamily,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = foregroundBrush,
                Background = backgroundBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 9, 16, 9),
                Cursor = System.Windows.Input.Cursors.Hand,
                Effect = CreateSoftShadow(0.25, 10)
            };

            button.SetBinding(Button.CommandProperty, new Binding(commandBindingPath));
            if (!string.IsNullOrWhiteSpace(commandParameterBindingPath))
            {
                button.SetBinding(Button.CommandParameterProperty, new Binding(commandParameterBindingPath));
            }

            button.Template = CreateRoundedButtonTemplate();
            return button;
        }

        /// <summary>
        /// Crée un ControlTemplate arrondi pour les boutons en pur code C#.
        /// Pourquoi : Garantit des coins arrondis et un état désactivé lisible sans XAML.
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
        /// Crée une ombre portée légère pour les cartes et boutons.
        /// Pourquoi : Ajoute une profondeur sobre sur le thème sombre.
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
        /// Synchronise les abonnements aux événements lorsque le DataContext change.
        /// Pourquoi : Maintient la vue connectée au bon QuarantineViewModel.
        /// </summary>
        private void OnQuarantineViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is QuarantineViewModel oldVm)
            {
                oldVm.Items.CollectionChanged -= OnQuarantineItemsCollectionChanged;
            }

            _viewModel = e.NewValue as QuarantineViewModel;

            if (_viewModel is not null)
            {
                _viewModel.Items.CollectionChanged += OnQuarantineItemsCollectionChanged;
            }

            RebuildQuarantineList();
        }

        /// <summary>
        /// Reconstruit la liste lorsque la collection Items est modifiée.
        /// Pourquoi : Reflète immédiatement la restauration ou la suppression d'un fichier.
        /// </summary>
        private void OnQuarantineItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildQuarantineList();
        }

        /// <summary>
        /// Synchronise l'affichage une fois le UserControl chargé.
        /// Pourquoi : Assure que les éléments présents au démarrage sont affichés.
        /// </summary>
        private void OnQuarantineViewLoaded(object sender, RoutedEventArgs e)
        {
            RebuildQuarantineList();
        }

        /// <summary>
        /// Convertit une chaîne hexadécimale (#RRGGBB) en structure Color WPF.
        /// Pourquoi : Centralise la définition des couleurs de thème.
        /// </summary>
        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        /// <summary>
        /// Fige un Brush pour optimiser les performances et permettre la réutilisation sécurisée.
        /// Pourquoi : Réduit les allocations répétées lors du rendu de la liste.
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