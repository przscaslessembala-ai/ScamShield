using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ScamShield.UI.Services;
using ScamShield.UI.ViewModels;

namespace ScamShield.UI.Views
{
    /// <summary>
    /// Vue WPF construite entièrement en code C# pour afficher les alertes par zone (Sain, Suspect, Dangereux),
    /// le score de risque et les raisons en français simple.
    /// Pourquoi : Permet à l'utilisateur de comprendre en moins de 10 secondes pourquoi un fichier est signalé
    /// et quelle action adopter, tout en conservant une mise en page responsive sans XAML.
    /// </summary>
    public class AlertsView : UserControl
    {
        // Police unique imposée sur l'interface.
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
        private static readonly Color ZoneSafeColor = ColorFromHex("#10B981");
        private static readonly Color ZoneSuspectColor = ColorFromHex("#F59E0B");
        private static readonly Color ZoneDangerColor = ColorFromHex("#EF4444");

        private static readonly Brush BgWindowBrush = FreezeBrush(new SolidColorBrush(BgWindowColor));
        private static readonly Brush BgSurfaceBrush = FreezeBrush(new SolidColorBrush(BgSurfaceColor));
        private static readonly Brush BgSurfaceElevatedBrush = FreezeBrush(new SolidColorBrush(BgSurfaceElevatedColor));
        private static readonly Brush BorderSubtleBrush = FreezeBrush(new SolidColorBrush(BorderSubtleColor));
        private static readonly Brush TextPrimaryBrush = FreezeBrush(new SolidColorBrush(TextPrimaryColor));
        private static readonly Brush TextSecondaryBrush = FreezeBrush(new SolidColorBrush(TextSecondaryColor));
        private static readonly Brush TextMutedBrush = FreezeBrush(new SolidColorBrush(TextMutedColor));
        private static readonly Brush AccentCyanBrush = FreezeBrush(new SolidColorBrush(AccentCyanColor));
        private static readonly Brush AccentCyanHoverBrush = FreezeBrush(new SolidColorBrush(AccentCyanHoverColor));
        private static readonly Brush ZoneSafeBrush = FreezeBrush(new SolidColorBrush(ZoneSafeColor));
        private static readonly Brush ZoneSuspectBrush = FreezeBrush(new SolidColorBrush(ZoneSuspectColor));
        private static readonly Brush ZoneDangerBrush = FreezeBrush(new SolidColorBrush(ZoneDangerColor));

        private StackPanel _alertsListPanel = null!;
        private Border _detailCard = null!;
        private Border _detailZoneBadge = null!;
        private TextBlock _detailZoneBadgeText = null!;
        private TextBlock _detailFileName = null!;
        private TextBlock _detailFilePath = null!;
        private TextBlock _detailScoreText = null!;
        private TextBlock _detailActionText = null!;
        private StackPanel _detailReasonsPanel = null!;

        private readonly Dictionary<AlertZoneFilter, (Border Card, SolidColorBrush BorderBrush)> _filterCards = [];
        private AlertsViewModel? _viewModel;

        /// <summary>
        /// Initialise AlertsView et construit la mise en page responsive en pur code C#.
        /// Pourquoi : Permet d'afficher immédiatement les alertes par zone via Binding sans fichier XAML.
        /// </summary>
        public AlertsView()
        {
            FontFamily = AppFontFamily;
            Foreground = TextPrimaryBrush;
            Background = BgWindowBrush;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            DataContextChanged += OnAlertsViewDataContextChanged;
            Loaded += OnAlertsViewLoaded;

            Content = BuildRootLayout();

            if (DataContext is null)
            {
                DataContext = new AlertsViewModel();
            }
        }

        /// <summary>
        /// Construit la grille principale en étoiles (*) avec l'en-tête, les cartes de zones et la zone liste + détail.
        /// Pourquoi : Assure un affichage propre de 1280x720 à 1920x1080 et en plein écran sans largeurs fixes.
        /// </summary>
        private UIElement BuildRootLayout()
        {
            var rootGrid = new Grid
            {
                Margin = new Thickness(24)
            };

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 260 });

            UIElement header = BuildHeaderSection();
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            Grid zonesGrid = BuildZoneFilterRow();
            zonesGrid.Margin = new Thickness(0, 16, 0, 16);
            Grid.SetRow(zonesGrid, 1);
            rootGrid.Children.Add(zonesGrid);

            Grid contentGrid = BuildContentSplitRow();
            Grid.SetRow(contentGrid, 2);
            rootGrid.Children.Add(contentGrid);

            return rootGrid;
        }

        /// <summary>
        /// Construit l'en-tête de la vue des alertes avec le titre, le message d'état et le bouton Actualiser.
        /// Pourquoi : Donne une vision immédiate de l'état de la liste et permet de recharger les alertes.
        /// </summary>
        private UIElement BuildHeaderSection()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleBlock = new TextBlock
            {
                Text = "Alertes par zone de sécurité",
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
            statusBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(AlertsViewModel.StatusMessage)));
            titleStack.Children.Add(statusBlock);

            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            Button refreshButton = CreateActionButton("Actualiser", nameof(AlertsViewModel.RefreshAlertsCommand));
            refreshButton.Margin = new Thickness(16, 0, 0, 0);
            refreshButton.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(refreshButton, 1);
            grid.Children.Add(refreshButton);

            return grid;
        }

        /// <summary>
        /// Construit les cartes cliquables de filtrage par zone : Dangereux, Suspect, Sain et Toutes.
        /// Pourquoi : Permet de filtrer instantanément les fichiers selon leur niveau de risque.
        /// </summary>
        private Grid BuildZoneFilterRow()
        {
            var grid = new Grid();
            for (int i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            Border dangerCard = CreateZoneFilterCard(
                filter: AlertZoneFilter.Dangereux,
                title: "ZONE DANGEREUX",
                subtitle: "Menaces bloquées ou à isoler",
                countBindingPath: nameof(AlertsViewModel.DangerousCount),
                accentBrush: ZoneDangerBrush,
                accentColor: ZoneDangerColor,
                margin: new Thickness(0, 0, 8, 0));

            Border suspectCard = CreateZoneFilterCard(
                filter: AlertZoneFilter.Suspect,
                title: "ZONE SUSPECT",
                subtitle: "Comportement à vérifier",
                countBindingPath: nameof(AlertsViewModel.SuspectCount),
                accentBrush: ZoneSuspectBrush,
                accentColor: ZoneSuspectColor,
                margin: new Thickness(8, 0, 8, 0));

            Border safeCard = CreateZoneFilterCard(
                filter: AlertZoneFilter.Sain,
                title: "ZONE SAIN",
                subtitle: "Fichiers sans risque repéré",
                countBindingPath: nameof(AlertsViewModel.SafeCount),
                accentBrush: ZoneSafeBrush,
                accentColor: ZoneSafeColor,
                margin: new Thickness(8, 0, 8, 0));

            Border allCard = CreateZoneFilterCard(
                filter: AlertZoneFilter.Toutes,
                title: "TOUTES LES ALERTES",
                subtitle: "Historique complet des analyses",
                countBindingPath: nameof(AlertsViewModel.VisibleAlerts.Count),
                accentBrush: AccentCyanHoverBrush,
                accentColor: AccentCyanColor,
                margin: new Thickness(8, 0, 0, 0));

            Grid.SetColumn(dangerCard, 0);
            Grid.SetColumn(suspectCard, 1);
            Grid.SetColumn(safeCard, 2);
            Grid.SetColumn(allCard, 3);

            grid.Children.Add(dangerCard);
            grid.Children.Add(suspectCard);
            grid.Children.Add(safeCard);
            grid.Children.Add(allCard);

            return grid;
        }

        /// <summary>
        /// Crée une carte interactive de zone avec compteur et animation de bordure au survol (220 ms).
        /// Pourquoi : Rend chaque zone visuellement distincte et facile à sélectionner.
        /// </summary>
        private Border CreateZoneFilterCard(
            AlertZoneFilter filter,
            string title,
            string subtitle,
            string countBindingPath,
            Brush accentBrush,
            Color accentColor,
            Thickness margin)
        {
            var borderBrush = new SolidColorBrush(BorderSubtleColor);
            var card = new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = margin,
                Cursor = Cursors.Hand,
                Effect = CreateSoftShadow(0.22, 10)
            };

            var stack = new StackPanel();

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontFamily = AppFontFamily,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);
            topGrid.Children.Add(titleBlock);

            var countBlock = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            countBlock.SetBinding(TextBlock.TextProperty, new Binding(countBindingPath));
            Grid.SetColumn(countBlock, 1);
            topGrid.Children.Add(countBlock);

            stack.Children.Add(topGrid);

            var subtitleBlock = new TextBlock
            {
                Text = subtitle,
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            stack.Children.Add(subtitleBlock);

            card.Child = stack;

            card.MouseLeftButtonUp += (_, _) =>
            {
                if (_viewModel is not null && _viewModel.SelectFilterCommand.CanExecute(filter))
                {
                    _viewModel.SelectFilterCommand.Execute(filter);
                }
            };

            card.MouseEnter += (_, _) =>
            {
                borderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(accentColor, TimeSpan.FromMilliseconds(220)));
            };

            card.MouseLeave += (_, _) =>
            {
                Color target = _viewModel?.SelectedFilter == filter ? accentColor : BorderSubtleColor;
                borderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(target, TimeSpan.FromMilliseconds(220)));
            };

            _filterCards[filter] = (card, borderBrush);
            return card;
        }

        /// <summary>
        /// Construit la zone inférieure à deux colonnes (*) : liste des alertes à gauche et détail clair à droite.
        /// Pourquoi : Permet de parcourir les alertes et de lire immédiatement le score, les raisons et l'action conseillée.
        /// </summary>
        private Grid BuildContentSplitRow()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star), MinWidth = 320 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });

            Border listCard = CreateCardContainer();
            var listOuterGrid = new Grid();
            listOuterGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            listOuterGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var listHeader = new TextBlock
            {
                Text = "LISTE DES FICHIERS ANALYSÉS",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(listHeader, 0);
            listOuterGrid.Children.Add(listHeader);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _alertsListPanel = new StackPanel();
            scrollViewer.Content = _alertsListPanel;

            Grid.SetRow(scrollViewer, 1);
            listOuterGrid.Children.Add(scrollViewer);
            listCard.Child = listOuterGrid;

            Grid.SetColumn(listCard, 0);
            grid.Children.Add(listCard);

            _detailCard = BuildAlertDetailCard();
            Grid.SetColumn(_detailCard, 2);
            grid.Children.Add(_detailCard);

            return grid;
        }

        /// <summary>
        /// Construit le panneau de détail affichant le score de risque, les raisons en français simple et l'action conseillée.
        /// Pourquoi : Aide un utilisateur non technique à comprendre en moins de 10 secondes pourquoi un fichier est signalé.
        /// </summary>
        private Border BuildAlertDetailCard()
        {
            Border card = CreateCardContainer();

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var stack = new StackPanel();

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var detailCaption = new TextBlock
            {
                Text = "EXPLICATION ET SCORE DE RISQUE",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(detailCaption, 0);
            topGrid.Children.Add(detailCaption);

            _detailZoneBadge = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 3, 10, 3)
            };

            _detailZoneBadgeText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            };
            _detailZoneBadge.Child = _detailZoneBadgeText;

            Grid.SetColumn(_detailZoneBadge, 1);
            topGrid.Children.Add(_detailZoneBadge);
            stack.Children.Add(topGrid);

            _detailFileName = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush,
                Margin = new Thickness(0, 12, 0, 2),
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(_detailFileName);

            _detailFilePath = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextMutedBrush,
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(_detailFilePath);

            var scoreBox = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 14, 0, 14)
            };

            _detailScoreText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                TextWrapping = TextWrapping.Wrap
            };
            scoreBox.Child = _detailScoreText;
            stack.Children.Add(scoreBox);

            var reasonsTitle = new TextBlock
            {
                Text = "Pourquoi ce fichier est signalé :",
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = AccentCyanHoverBrush,
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(reasonsTitle);

            _detailReasonsPanel = new StackPanel();
            stack.Children.Add(_detailReasonsPanel);

            var actionBox = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 14, 0, 0)
            };

            var actionStack = new StackPanel();
            var actionTitle = new TextBlock
            {
                Text = "Que faire ?",
                FontFamily = AppFontFamily,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush
            };
            actionStack.Children.Add(actionTitle);

            _detailActionText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                Foreground = TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            actionStack.Children.Add(_detailActionText);

            actionBox.Child = actionStack;
            stack.Children.Add(actionBox);

            scroll.Content = stack;
            card.Child = scroll;
            return card;
        }

        /// <summary>
        /// Reconstruit la liste visuelle des alertes filtrées selon la zone active.
        /// Pourquoi : Synchronise l'affichage avec VisibleAlerts dans AlertsViewModel.
        /// </summary>
        private void RebuildAlertsList()
        {
            if (_alertsListPanel is null || _viewModel is null)
            {
                return;
            }

            _alertsListPanel.Children.Clear();

            if (_viewModel.VisibleAlerts.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "Aucune alerte dans cette zone pour le moment.",
                    FontFamily = AppFontFamily,
                    FontSize = 13,
                    Foreground = TextSecondaryBrush,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                _alertsListPanel.Children.Add(emptyText);
                return;
            }

            for (int i = 0; i < _viewModel.VisibleAlerts.Count; i++)
            {
                AlertDisplayItem item = _viewModel.VisibleAlerts[i];
                Border row = CreateAlertRowCard(item, isLast: i == _viewModel.VisibleAlerts.Count - 1);
                _alertsListPanel.Children.Add(row);
            }
        }

        /// <summary>
        /// Crée une ligne d'alerte indiquant le nom du fichier, sa zone, son score et un aperçu des raisons.
        /// Pourquoi : Permet de repérer rapidement les fichiers Sain, Suspect ou Dangereux et de cliquer pour voir le détail.
        /// </summary>
        private Border CreateAlertRowCard(AlertDisplayItem item, bool isLast)
        {
            (Brush zoneBrush, Brush zoneBgBrush) = GetZoneBrushes(item.Zone);
            bool isSelected = _viewModel?.SelectedAlert == item;

            var row = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = isSelected ? zoneBrush : BorderSubtleBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, isLast ? 0 : 10),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var fileTitle = new TextBlock
            {
                Text = item.FileName,
                FontFamily = AppFontFamily,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(fileTitle, 0);
            topGrid.Children.Add(fileTitle);

            var badge = new Border
            {
                Background = zoneBgBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 2, 9, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var badgeText = new TextBlock
            {
                Text = $"{item.ZoneLabel} • {item.RiskScorePercent} %",
                FontFamily = AppFontFamily,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = zoneBrush
            };
            badge.Child = badgeText;
            Grid.SetColumn(badge, 1);
            topGrid.Children.Add(badge);

            Grid.SetRow(topGrid, 0);
            grid.Children.Add(topGrid);

            var summaryBlock = new TextBlock
            {
                Text = $"{item.DetectedAtText} — {item.Reasons[0]}",
                FontFamily = AppFontFamily,
                FontSize = 12,
                Foreground = TextSecondaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(summaryBlock, 1);
            grid.Children.Add(summaryBlock);

            row.Child = grid;

            row.MouseLeftButtonUp += (_, _) =>
            {
                if (_viewModel is not null)
                {
                    _viewModel.SelectedAlert = item;
                }
            };

            return row;
        }

        /// <summary>
        /// Met à jour le panneau de détail avec une transition discrète (250 ms) selon l'alerte sélectionnée.
        /// Pourquoi : Affiche clairement le score, les raisons simples et la marche à suivre.
        /// </summary>
        private void UpdateSelectedAlertDetail()
        {
            if (_detailCard is null || _viewModel is null)
            {
                return;
            }

            AlertDisplayItem? alert = _viewModel.SelectedAlert;
            _detailReasonsPanel.Children.Clear();

            if (alert is null)
            {
                _detailZoneBadgeText.Text = "Aucune";
                _detailZoneBadge.Background = BgSurfaceElevatedBrush;
                _detailZoneBadgeText.Foreground = TextSecondaryBrush;
                _detailFileName.Text = "Aucun fichier sélectionné";
                _detailFilePath.Text = "Sélectionnez une alerte dans la liste pour voir les explications.";
                _detailScoreText.Text = "Score de risque : —";
                _detailActionText.Text = "Aucune action requise.";
                return;
            }

            (Brush zoneBrush, Brush zoneBgBrush) = GetZoneBrushes(alert.Zone);

            _detailZoneBadge.Background = zoneBgBrush;
            _detailZoneBadgeText.Text = alert.ZoneLabel;
            _detailZoneBadgeText.Foreground = zoneBrush;

            _detailFileName.Text = alert.FileName;
            _detailFilePath.Text = $"{alert.FilePath} • Détecté le {alert.DetectedAtText}";
            _detailScoreText.Text = $"Score de risque : {alert.RiskScorePercent} % ({alert.RiskScore:0.00} / 1.00) — Zone {alert.ZoneLabel}";
            _detailActionText.Text = alert.RecommendedAction;

            foreach (string reason in alert.Reasons)
            {
                var reasonLine = new TextBlock
                {
                    Text = $"• {reason}",
                    FontFamily = AppFontFamily,
                    FontSize = 12.5,
                    Foreground = TextPrimaryBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                _detailReasonsPanel.Children.Add(reasonLine);
            }

            // Fondu discret de 250 ms lors du changement d'alerte sélectionnée.
            _detailCard.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation(0.55, 1.0, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        /// <summary>
        /// Met en surbrillance la carte de zone active selon SelectedFilter.
        /// Pourquoi : Indique visuellement quel filtre de zone (Dangereux, Suspect, Sain, Toutes) est actif.
        /// </summary>
        private void UpdateActiveFilterVisual()
        {
            if (_viewModel is null)
            {
                return;
            }

            foreach (KeyValuePair<AlertZoneFilter, (Border Card, SolidColorBrush BorderBrush)> entry in _filterCards)
            {
                bool active = entry.Key == _viewModel.SelectedFilter;
                Color targetColor = active
                    ? entry.Key switch
                    {
                        AlertZoneFilter.Dangereux => ZoneDangerColor,
                        AlertZoneFilter.Suspect => ZoneSuspectColor,
                        AlertZoneFilter.Sain => ZoneSafeColor,
                        _ => AccentCyanColor
                    }
                    : BorderSubtleColor;

                entry.Value.BorderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(targetColor, TimeSpan.FromMilliseconds(220)));
            }
        }

        /// <summary>
        /// Retourne les pinceaux de premier plan et de fond associés à une zone (Sain, Suspect, Dangereux).
        /// Pourquoi : Centralise les couleurs sémantiques des trois zones.
        /// </summary>
        private static (Brush Foreground, Brush Background) GetZoneBrushes(AgentThreatZone zone)
        {
            return zone switch
            {
                AgentThreatZone.Dangereux => (ZoneDangerBrush, FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)))),
                AgentThreatZone.Suspect => (ZoneSuspectBrush, FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)))),
                _ => (ZoneSafeBrush, FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 16, 185, 129))))
            };
        }

        /// <summary>
        /// Crée un bouton d'action sobre avec coins arrondis en pur code C#.
        /// Pourquoi : Assure la cohérence visuelle avec le reste de l'interface.
        /// </summary>
        private static Button CreateActionButton(string label, string commandBindingPath)
        {
            var button = new Button
            {
                Content = label,
                FontFamily = AppFontFamily,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = BgWindowBrush,
                Background = AccentCyanBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 9, 16, 9),
                Cursor = Cursors.Hand,
                Effect = CreateSoftShadow(0.25, 10)
            };

            button.SetBinding(Button.CommandProperty, new Binding(commandBindingPath));
            button.Template = CreateRoundedButtonTemplate();
            return button;
        }

        /// <summary>
        /// Crée un conteneur de carte standard (fond sombre, bordure subtile, coins arrondis, ombre légère).
        /// Pourquoi : Harmonise le design des cartes sur toute la vue.
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
        /// Crée une ombre portée légère pour structurer les cartes.
        /// Pourquoi : Apporte de la profondeur sans alourdir le thème sombre.
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
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            borderFactory.SetBinding(Border.PaddingProperty, new Binding(nameof(Control.Padding))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            template.VisualTree = borderFactory;
            return template;
        }

        /// <summary>
        /// Gère le changement de DataContext pour synchroniser les abonnements aux événements.
        /// Pourquoi : Garde la vue alignée avec l'instance courante de AlertsViewModel.
        /// </summary>
        private void OnAlertsViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AlertsViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                oldVm.VisibleAlerts.CollectionChanged -= OnVisibleAlertsCollectionChanged;
            }

            _viewModel = e.NewValue as AlertsViewModel;

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.VisibleAlerts.CollectionChanged += OnVisibleAlertsCollectionChanged;
            }

            RebuildAlertsList();
            UpdateSelectedAlertDetail();
            UpdateActiveFilterVisual();
        }

        /// <summary>
        /// Réagit aux changements de propriétés d'AlertsViewModel.
        /// Pourquoi : Met à jour le filtre actif et le panneau de détail lors d'une sélection.
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlertsViewModel.SelectedFilter))
            {
                UpdateActiveFilterVisual();
            }
            else if (e.PropertyName == nameof(AlertsViewModel.SelectedAlert))
            {
                RebuildAlertsList();
                UpdateSelectedAlertDetail();
            }
        }

        /// <summary>
        /// Reconstruit la liste d'alertes lorsque VisibleAlerts change.
        /// Pourquoi : Affiche immédiatement les alertes filtrées ou nouvellement reçues.
        /// </summary>
        private void OnVisibleAlertsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildAlertsList();
            UpdateSelectedAlertDetail();
        }

        /// <summary>
        /// Synchronise l'affichage une fois le contrôle chargé.
        /// Pourquoi : Garantit que les éléments visuels reflètent l'état initial du ViewModel.
        /// </summary>
        private void OnAlertsViewLoaded(object sender, RoutedEventArgs e)
        {
            RebuildAlertsList();
            UpdateSelectedAlertDetail();
            UpdateActiveFilterVisual();
        }

        /// <summary>
        /// Convertit une couleur hexadécimale (#RRGGBB) en Color WPF.
        /// Pourquoi : Centralise la déclaration des couleurs sombres et d'accent.
        /// </summary>
        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        /// <summary>
        /// Fige un Brush pour optimiser les performances et permettre la réutilisation sécurisée.
        /// Pourquoi : Réduit l'allocation mémoire sur les éléments répétés de la liste.
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