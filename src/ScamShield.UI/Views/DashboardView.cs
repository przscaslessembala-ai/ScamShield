using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using ScamShield.UI.ViewModels;

namespace ScamShield.UI.Views
{
    /// <summary>
    /// Vue principale du tableau de bord construite entièrement en code C# WPF (sans XAML).
    /// Pourquoi : Garantit une mise en page responsive en Grid (*), un thème sombre cohérent
    /// et une séparation claire avec DashboardViewModel via les Bindings.
    /// </summary>
    public class DashboardView : UserControl
    {
        // Police unique imposée pour l'ensemble du tableau de bord.
        private static readonly FontFamily AppFontFamily = new("Segoe UI");

        // Ressources de couleurs et pinceaux partagés (thème sombre moderne + accent bleu-cyan).
        private static readonly Color BgWindowColor = ColorFromHex("#0B0F17");
        private static readonly Color BgSurfaceColor = ColorFromHex("#121826");
        private static readonly Color BgSurfaceElevatedColor = ColorFromHex("#182032");
        private static readonly Color BorderSubtleColor = ColorFromHex("#243049");
        private static readonly Color TextPrimaryColor = ColorFromHex("#F1F5F9");
        private static readonly Color TextSecondaryColor = ColorFromHex("#94A3B8");
        private static readonly Color TextMutedColor = ColorFromHex("#64748B");
        private static readonly Color AccentCyanColor = ColorFromHex("#06B6D4");
        private static readonly Color AccentCyanHoverColor = ColorFromHex("#22D3EE");
        private static readonly Color AccentCyanSoftColor = ColorFromHex("#164E63");
        private static readonly Color StatusSuccessColor = ColorFromHex("#10B981");
        private static readonly Color StatusWarningColor = ColorFromHex("#F59E0B");
        private static readonly Color StatusDangerColor = ColorFromHex("#EF4444");

        private static readonly Brush BgWindowBrush = FreezeBrush(new SolidColorBrush(BgWindowColor));
        private static readonly Brush BgSurfaceBrush = FreezeBrush(new SolidColorBrush(BgSurfaceColor));
        private static readonly Brush BgSurfaceElevatedBrush = FreezeBrush(new SolidColorBrush(BgSurfaceElevatedColor));
        private static readonly Brush BorderSubtleBrush = FreezeBrush(new SolidColorBrush(BorderSubtleColor));
        private static readonly Brush TextPrimaryBrush = FreezeBrush(new SolidColorBrush(TextPrimaryColor));
        private static readonly Brush TextSecondaryBrush = FreezeBrush(new SolidColorBrush(TextSecondaryColor));
        private static readonly Brush TextMutedBrush = FreezeBrush(new SolidColorBrush(TextMutedColor));
        private static readonly Brush AccentCyanBrush = FreezeBrush(new SolidColorBrush(AccentCyanColor));
        private static readonly Brush AccentCyanHoverBrush = FreezeBrush(new SolidColorBrush(AccentCyanHoverColor));
        private static readonly Brush AccentCyanSoftBrush = FreezeBrush(new SolidColorBrush(AccentCyanSoftColor));
        private static readonly Brush StatusSuccessBrush = FreezeBrush(new SolidColorBrush(StatusSuccessColor));
        private static readonly Brush StatusWarningBrush = FreezeBrush(new SolidColorBrush(StatusWarningColor));
        private static readonly Brush StatusDangerBrush = FreezeBrush(new SolidColorBrush(StatusDangerColor));

        // Éléments visuels dynamiques mis à jour selon l'état du ViewModel ou le redimensionnement.
        private Ellipse _protectionIndicatorEllipse = null!;
        private SolidColorBrush _protectionIndicatorBrush = null!;
        private ScaleTransform _protectionPulseScale = null!;
        private Border _aiConfidenceBarFill = null!;
        private Grid _aiConfidenceTrack = null!;
        private Canvas _chartCanvas = null!;
        private Border _chartTooltipBorder = null!;
        private TextBlock _chartTooltipTitle = null!;
        private TextBlock _chartTooltipSubtitle = null!;
        private StackPanel _eventsStackPanel = null!;
        private Button _quickScanButton = null!;
        private SolidColorBrush _quickScanButtonBrush = null!;
        private ScaleTransform _quickScanButtonScale = null!;

        // Référence typée vers le ViewModel courant lorsque DataContext est un DashboardViewModel.
        private DashboardViewModel? _viewModel;

        /// <summary>
        /// Initialise le UserControl DashboardView et assemble toute l'interface en code C#.
        /// Pourquoi : Permet d'instancier directement DashboardView dans n'importe quel conteneur WPF
        /// sans fichier .xaml et avec un DataContext par défaut prêt à l'emploi.
        /// </summary>
        public DashboardView()
        {
            FontFamily = AppFontFamily;
            Foreground = TextPrimaryBrush;
            Background = BgWindowBrush;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            DataContextChanged += OnDashboardViewDataContextChanged;
            Loaded += OnDashboardViewLoaded;
            SizeChanged += OnDashboardViewSizeChanged;

            Content = BuildRootLayout();

            // Initialise un ViewModel par défaut si aucun DataContext n'a encore été assigné.
            if (DataContext is null)
            {
                DataContext = new DashboardViewModel();
            }
        }

        /// <summary>
        /// Construit la grille principale responsive en étoiles (*) du tableau de bord.
        /// Pourquoi : Répartit harmonieusement l'en-tête, la ligne supérieure (Protection + IA),
        /// les 4 cartes de statistiques et la ligne inférieure (Courbe 24h + Événements récents).
        /// </summary>
        private UIElement BuildRootLayout()
        {
            var rootGrid = new Grid
            {
                Margin = new Thickness(24)
            };

            // Lignes responsives : en-tête compact, zone supérieure flexible, 4 stats compactes, zone inférieure large.
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.05, GridUnitType.Star), MinHeight = 175 });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.45, GridUnitType.Star), MinHeight = 230 });

            UIElement header = BuildHeaderSection();
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            Grid topRowGrid = new() { Margin = new Thickness(0, 18, 0, 16) };
            topRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star), MinWidth = 300 });
            topRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            topRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });

            Border protectionCard = BuildProtectionStatusCard();
            Grid.SetColumn(protectionCard, 0);
            topRowGrid.Children.Add(protectionCard);

            Border aiCard = BuildAiPanelCard();
            Grid.SetColumn(aiCard, 2);
            topRowGrid.Children.Add(aiCard);

            Grid.SetRow(topRowGrid, 1);
            rootGrid.Children.Add(topRowGrid);

            Grid statsGrid = BuildStatsRow();
            statsGrid.Margin = new Thickness(0, 0, 0, 16);
            Grid.SetRow(statsGrid, 2);
            rootGrid.Children.Add(statsGrid);

            Grid bottomRowGrid = new();
            bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star), MinWidth = 340 });
            bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 320 });

            Border chartCard = BuildActivityChartCard();
            Grid.SetColumn(chartCard, 0);
            bottomRowGrid.Children.Add(chartCard);

            Border eventsCard = BuildRecentEventsCard();
            Grid.SetColumn(eventsCard, 2);
            bottomRowGrid.Children.Add(eventsCard);

            Grid.SetRow(bottomRowGrid, 3);
            rootGrid.Children.Add(bottomRowGrid);

            return rootGrid;
        }

        /// <summary>
        /// Construit l'en-tête contenant le nom de l'application, l'horloge en direct et le bouton "Analyse rapide".
        /// Pourquoi : Offre un accès immédiat à l'identité du produit, à l'heure courante et à l'action principale.
        /// </summary>
        private UIElement BuildHeaderSection()
        {
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };

            var accentDot = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = AccentCyanBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            titleRow.Children.Add(accentDot);

            var appNameText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            appNameText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.AppName)));
            titleRow.Children.Add(appNameText);

            var clockBadge = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(14, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var clockText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = AccentCyanHoverBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            clockText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.LiveClockText)));
            clockBadge.Child = clockText;
            titleRow.Children.Add(clockBadge);

            titleStack.Children.Add(titleRow);

            var taglineText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 13,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(20, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            taglineText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.AppTagline)));
            titleStack.Children.Add(taglineText);

            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            _quickScanButton = BuildQuickScanButton();
            _quickScanButton.Margin = new Thickness(16, 0, 0, 0);
            _quickScanButton.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(_quickScanButton, 1);
            headerGrid.Children.Add(_quickScanButton);

            return headerGrid;
        }

        /// <summary>
        /// Construit le bouton principal "Analyse rapide" avec animations fluides de 250 ms au survol.
        /// Pourquoi : Met en valeur l'action principale du tableau de bord tout en restant réactif et discret.
        /// </summary>
        private Button BuildQuickScanButton()
        {
            _quickScanButtonBrush = new SolidColorBrush(AccentCyanColor);
            _quickScanButtonScale = new ScaleTransform(1.0, 1.0);

            var button = new Button
            {
                FontFamily = AppFontFamily,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = BgWindowBrush,
                Background = _quickScanButtonBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 12, 20, 12),
                Cursor = Cursors.Hand,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _quickScanButtonScale,
                Effect = CreateSoftShadow()
            };

            button.SetBinding(Button.CommandProperty, new Binding(nameof(DashboardViewModel.QuickScanCommand)));

            var contentStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var labelText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = BgWindowBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            labelText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.IsScanning))
            {
                Converter = new BooleanToScanLabelConverter()
            });

            contentStack.Children.Add(labelText);
            button.Content = contentStack;

            // Applique un modèle de bouton arrondi pour garantir le rendu sur tous les thèmes Windows.
            button.Template = CreateRoundedButtonTemplate();

            // Animation fluide de 250 ms lors du survol et de la sortie du curseur.
            button.MouseEnter += (_, _) => AnimateQuickScanHover(isHovered: true);
            button.MouseLeave += (_, _) => AnimateQuickScanHover(isHovered: false);

            return button;
        }

        /// <summary>
        /// Anime la couleur d'accent et une légère mise à l'échelle sur le bouton "Analyse rapide".
        /// Pourquoi : Donne un retour tactile discret (250 ms) sans perturber la lecture.
        /// </summary>
        private void AnimateQuickScanHover(bool isHovered)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(250));
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            var targetColor = isHovered ? AccentCyanHoverColor : AccentCyanColor;
            var targetScale = isHovered ? 1.02 : 1.0;

            _quickScanButtonBrush.BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(targetColor, duration) { EasingFunction = easing });

            _quickScanButtonScale.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                new DoubleAnimation(targetScale, duration) { EasingFunction = easing });

            _quickScanButtonScale.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                new DoubleAnimation(targetScale, duration) { EasingFunction = easing });
        }

        /// <summary>
        /// Construit la carte d'état de protection avec indicateur circulaire (Protégé, Attention, Menace).
        /// Pourquoi : Synthétise visuellement l'état de sécurité de la machine et du module de récupération d'accès.
        /// </summary>
        private Border BuildProtectionStatusCard()
        {
            Border card = CreateCardContainer();

            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _protectionIndicatorBrush = new SolidColorBrush(StatusSuccessColor);
            _protectionPulseScale = new ScaleTransform(1.0, 1.0);

            var indicatorHost = new Grid
            {
                Width = 76,
                Height = 76,
                Margin = new Thickness(0, 0, 18, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Halo extérieur discret animé.
            var outerHalo = new Ellipse
            {
                Width = 72,
                Height = 72,
                Stroke = _protectionIndicatorBrush,
                StrokeThickness = 2,
                Opacity = 0.35,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _protectionPulseScale
            };
            indicatorHost.Children.Add(outerHalo);

            // Cercle principal de l'état de protection.
            _protectionIndicatorEllipse = new Ellipse
            {
                Width = 54,
                Height = 54,
                Fill = _protectionIndicatorBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = CreateSoftShadow(0.35, 10)
            };
            indicatorHost.Children.Add(_protectionIndicatorEllipse);

            Grid.SetColumn(indicatorHost, 0);
            contentGrid.Children.Add(indicatorHost);

            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var captionText = new TextBlock
            {
                Text = "ÉTAT DE LA PROTECTION",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondaryBrush
            };
            textStack.Children.Add(captionText);

            var stateTitleText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush,
                Margin = new Thickness(0, 4, 0, 4)
            };
            stateTitleText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.ProtectionTitle)));
            textStack.Children.Add(stateTitleText);

            var stateSubtitleText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 13,
                Foreground = TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap
            };
            stateSubtitleText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.ProtectionSubtitle)));
            textStack.Children.Add(stateSubtitleText);

            Grid.SetColumn(textStack, 1);
            contentGrid.Children.Add(textStack);

            card.Child = contentGrid;
            return card;
        }

        /// <summary>
        /// Construit le panneau IA présentant le statut, le score de confiance et la dernière décision.
        /// Pourquoi : Met en avant la supervision intelligente et l'aide à la récupération d'accès de façon lisible.
        /// </summary>
        private Border BuildAiPanelCard()
        {
            Border card = CreateCardContainer();

            var mainStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var aiTitle = new TextBlock
            {
                Text = "PANNEAU DE L'IA",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = AccentCyanHoverBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(aiTitle, 0);
            headerGrid.Children.Add(aiTitle);

            var aiStatusBadge = new Border
            {
                Background = AccentCyanSoftBrush,
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(10, 3, 10, 3)
            };

            var aiStatusText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = AccentCyanHoverBrush
            };
            aiStatusText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.AiStatus)));
            aiStatusBadge.Child = aiStatusText;

            Grid.SetColumn(aiStatusBadge, 1);
            headerGrid.Children.Add(aiStatusBadge);
            mainStack.Children.Add(headerGrid);

            // Ligne du score de confiance IA avec jauge animée.
            var scoreRow = new Grid { Margin = new Thickness(0, 12, 0, 6) };
            scoreRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scoreRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var confidenceLabel = new TextBlock
            {
                Text = "Score de confiance du modèle",
                FontFamily = AppFontFamily,
                FontSize = 13,
                Foreground = TextSecondaryBrush
            };
            Grid.SetColumn(confidenceLabel, 0);
            scoreRow.Children.Add(confidenceLabel);

            var confidenceValueText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush
            };
            confidenceValueText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.AiConfidenceScore))
            {
                StringFormat = "{0} %"
            });
            Grid.SetColumn(confidenceValueText, 1);
            scoreRow.Children.Add(confidenceValueText);
            mainStack.Children.Add(scoreRow);

            _aiConfidenceTrack = new Grid
            {
                Height = 8,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var trackBg = new Border
            {
                Background = BgSurfaceElevatedBrush,
                CornerRadius = new CornerRadius(4)
            };
            _aiConfidenceTrack.Children.Add(trackBg);

            _aiConfidenceBarFill = new Border
            {
                Background = AccentCyanBrush,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 120
            };
            _aiConfidenceTrack.Children.Add(_aiConfidenceBarFill);
            mainStack.Children.Add(_aiConfidenceTrack);

            var decisionLabel = new TextBlock
            {
                Text = "Dernière décision IA",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextMutedBrush
            };
            mainStack.Children.Add(decisionLabel);

            var decisionText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 13,
                Foreground = TextPrimaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            decisionText.SetBinding(TextBlock.TextProperty, new Binding(nameof(DashboardViewModel.AiLastDecision)));
            mainStack.Children.Add(decisionText);

            card.Child = mainStack;
            return card;
        }

        /// <summary>
        /// Construit la rangée des 4 cartes de statistiques en colonnes égales (*).
        /// Pourquoi : Présente de façon équilibrée les 4 indicateurs clés sans largeur fixe.
        /// </summary>
        private Grid BuildStatsRow()
        {
            var grid = new Grid();
            for (int i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            Border card1 = CreateStatCard(
                title: "FICHIERS ANALYSÉS",
                bindingPath: nameof(DashboardViewModel.FilesScannedCount),
                stringFormat: "{0:N0}",
                accentBrush: AccentCyanHoverBrush,
                margin: new Thickness(0, 0, 8, 0));

            Border card2 = CreateStatCard(
                title: "MENACES BLOQUÉES",
                bindingPath: nameof(DashboardViewModel.ThreatsBlockedCount),
                stringFormat: "{0:N0}",
                accentBrush: StatusDangerBrush,
                margin: new Thickness(8, 0, 8, 0));

            Border card3 = CreateStatCard(
                title: "SIGNATURES À JOUR",
                bindingPath: nameof(DashboardViewModel.SignaturesStatus),
                stringFormat: null,
                accentBrush: StatusSuccessBrush,
                margin: new Thickness(8, 0, 8, 0));

            Border card4 = CreateStatCard(
                title: "DERNIER SCAN",
                bindingPath: nameof(DashboardViewModel.LastScanText),
                stringFormat: null,
                accentBrush: TextPrimaryBrush,
                margin: new Thickness(8, 0, 0, 0));

            Grid.SetColumn(card1, 0);
            Grid.SetColumn(card2, 1);
            Grid.SetColumn(card3, 2);
            Grid.SetColumn(card4, 3);

            grid.Children.Add(card1);
            grid.Children.Add(card2);
            grid.Children.Add(card3);
            grid.Children.Add(card4);

            return grid;
        }

        /// <summary>
        /// Crée une carte statistique individuelle avec effet de survol subtil de 220 ms.
        /// Pourquoi : Évite la répétition du code de carte tout en offrant un rendu homogène et responsive.
        /// </summary>
        private static Border CreateStatCard(string title, string bindingPath, string? stringFormat, Brush accentBrush, Thickness margin)
        {
            var borderBrush = new SolidColorBrush(BorderSubtleColor);
            var card = new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = margin,
                Effect = CreateSoftShadow(0.22, 10)
            };

            var stack = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = title,
                FontFamily = AppFontFamily,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondaryBrush
            };
            stack.Children.Add(titleBlock);

            var valueBlock = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 6, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var binding = new Binding(bindingPath);
            if (!string.IsNullOrEmpty(stringFormat))
            {
                binding.StringFormat = stringFormat;
            }
            valueBlock.SetBinding(TextBlock.TextProperty, binding);

            stack.Children.Add(valueBlock);
            card.Child = stack;

            // Transition discrète de bordure au survol (220 ms).
            card.MouseEnter += (_, _) =>
            {
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
        /// Construit la carte contenant la courbe d'activité des 24 dernières heures et son infobulle au survol.
        /// Pourquoi : Permet d'inspecter l'évolution de l'activité heure par heure de façon interactive.
        /// </summary>
        private Border BuildActivityChartCard()
        {
            Border card = CreateCardContainer();

            var containerGrid = new Grid();
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "ACTIVITÉ DES 24 DERNIÈRES HEURES",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var hintText = new TextBlock
            {
                Text = "Survolez la courbe",
                FontFamily = AppFontFamily,
                FontSize = 12,
                Foreground = TextSecondaryBrush
            };
            Grid.SetColumn(hintText, 1);
            headerGrid.Children.Add(hintText);

            Grid.SetRow(headerGrid, 0);
            containerGrid.Children.Add(headerGrid);

            var plotHost = new Grid { Margin = new Thickness(0, 4, 0, 6) };
            _chartCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                ClipToBounds = false
            };
            _chartCanvas.MouseMove += OnChartCanvasMouseMove;
            _chartCanvas.MouseLeave += OnChartCanvasMouseLeave;
            plotHost.Children.Add(_chartCanvas);

            // Infobulle flottante affichée uniquement lorsqu'un point est survolé.
            _chartTooltipBorder = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = AccentCyanBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Effect = CreateSoftShadow(0.35, 12)
            };

            var tooltipStack = new StackPanel();
            _chartTooltipTitle = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = AccentCyanHoverBrush
            };
            _chartTooltipSubtitle = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextPrimaryBrush,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            tooltipStack.Children.Add(_chartTooltipTitle);
            tooltipStack.Children.Add(_chartTooltipSubtitle);
            _chartTooltipBorder.Child = tooltipStack;
            plotHost.Children.Add(_chartTooltipBorder);

            Grid.SetRow(plotHost, 1);
            containerGrid.Children.Add(plotHost);

            var axisLabelsGrid = new Grid();
            axisLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            axisLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            axisLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftAxis = new TextBlock
            {
                Text = "-24h",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextMutedBrush
            };
            var midAxis = new TextBlock
            {
                Text = "-12h",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextMutedBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var rightAxis = new TextBlock
            {
                Text = "Maintenant",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextMutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(leftAxis, 0);
            Grid.SetColumn(midAxis, 1);
            Grid.SetColumn(rightAxis, 2);

            axisLabelsGrid.Children.Add(leftAxis);
            axisLabelsGrid.Children.Add(midAxis);
            axisLabelsGrid.Children.Add(rightAxis);

            Grid.SetRow(axisLabelsGrid, 2);
            containerGrid.Children.Add(axisLabelsGrid);

            card.Child = containerGrid;
            return card;
        }

        /// <summary>
        /// Construit la carte des événements récents avec badges de gravité (Info, Avertissement, Critique).
        /// Pourquoi : Permet de visualiser rapidement les dernières alertes, vérifications et actions d'accès.
        /// </summary>
        private Border BuildRecentEventsCard()
        {
            Border card = CreateCardContainer();

            var outerGrid = new Grid();
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerText = new TextBlock
            {
                Text = "ÉVÉNEMENTS RÉCENTS",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerText, 0);
            outerGrid.Children.Add(headerText);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _eventsStackPanel = new StackPanel();
            scrollViewer.Content = _eventsStackPanel;

            Grid.SetRow(scrollViewer, 1);
            outerGrid.Children.Add(scrollViewer);

            card.Child = outerGrid;
            return card;
        }

        /// <summary>
        /// Reconstruit la liste des événements récents avec des badges colorés selon leur gravité.
        /// Pourquoi : Assure une synchronisation fidèle avec la collection RecentEvents du ViewModel.
        /// </summary>
        private void RebuildRecentEventsList()
        {
            if (_eventsStackPanel is null)
            {
                return;
            }

            _eventsStackPanel.Children.Clear();

            if (_viewModel is null)
            {
                return;
            }

            for (int i = 0; i < _viewModel.RecentEvents.Count; i++)
            {
                RecentSecurityEvent item = _viewModel.RecentEvents[i];
                Border rowCard = CreateEventRow(item, isLast: i == _viewModel.RecentEvents.Count - 1);
                _eventsStackPanel.Children.Add(rowCard);
            }
        }

        /// <summary>
        /// Crée une ligne d'événement récent dotée d'un badge de gravité (Info, Avertissement, Critique).
        /// Pourquoi : Rend la gravité immédiatement identifiable d'un simple coup d'œil.
        /// </summary>
        private static Border CreateEventRow(RecentSecurityEvent securityEvent, bool isLast)
        {
            (string badgeLabel, Brush badgeBg, Brush badgeFg) = securityEvent.Severity switch
            {
                EventSeverity.Critique => ("Critique", FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 239, 68, 68))), StatusDangerBrush),
                EventSeverity.Avertissement => ("Avertissement", FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))), StatusWarningBrush),
                _ => ("Info", FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 16, 185, 129))), StatusSuccessBrush)
            };

            var rowBorder = new Border
            {
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, isLast ? 0 : 8)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = securityEvent.Title,
                FontFamily = AppFontFamily,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);
            topGrid.Children.Add(titleBlock);

            var badge = new Border
            {
                Background = badgeBg,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var badgeText = new TextBlock
            {
                Text = badgeLabel,
                FontFamily = AppFontFamily,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = badgeFg
            };
            badge.Child = badgeText;

            Grid.SetColumn(badge, 1);
            topGrid.Children.Add(badge);

            Grid.SetRow(topGrid, 0);
            grid.Children.Add(topGrid);

            var contextBlock = new TextBlock
            {
                Text = $"{securityEvent.Timestamp} • {securityEvent.Context}",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextSecondaryBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(contextBlock, 1);
            grid.Children.Add(contextBlock);

            rowBorder.Child = grid;
            return rowBorder;
        }

        /// <summary>
        /// Redessine la courbe d'activité des 24 dernières heures en fonction de la taille disponible du Canvas.
        /// Pourquoi : Maintient une courbe fluide et proportionnelle sans largeur fixe quelle que soit la résolution.
        /// </summary>
        private void RedrawActivityChart()
        {
            if (_chartCanvas is null || _viewModel is null)
            {
                return;
            }

            _chartCanvas.Children.Clear();

            double width = _chartCanvas.ActualWidth;
            double height = _chartCanvas.ActualHeight;

            if (width < 40 || height < 30 || _viewModel.ActivityPoints24h.Count == 0)
            {
                return;
            }

            const double padLeft = 10;
            const double padRight = 10;
            const double padTop = 14;
            const double padBottom = 10;

            double plotWidth = Math.Max(10, width - padLeft - padRight);
            double plotHeight = Math.Max(10, height - padTop - padBottom);

            // Lignes horizontales discrètes de repère.
            for (int r = 0; r <= 2; r++)
            {
                double y = padTop + (plotHeight / 2.0) * r;
                var gridLine = new Line
                {
                    X1 = padLeft,
                    Y1 = y,
                    X2 = padLeft + plotWidth,
                    Y2 = y,
                    Stroke = BorderSubtleBrush,
                    StrokeThickness = 1,
                    StrokeDashArray = [4, 4],
                    Opacity = 0.65
                };
                _chartCanvas.Children.Add(gridLine);
            }

            int count = _viewModel.ActivityPoints24h.Count;
            double stepX = count > 1 ? plotWidth / (count - 1) : 0;

            var linePoints = new PointCollection(count);
            var areaPoints = new PointCollection(count + 2);

            areaPoints.Add(new Point(padLeft, padTop + plotHeight));

            for (int i = 0; i < count; i++)
            {
                double val = Math.Clamp(_viewModel.ActivityPoints24h[i].ActivityValue, 0, 100);
                double x = padLeft + i * stepX;
                double y = padTop + plotHeight - ((val / 100.0) * plotHeight);

                var pt = new Point(x, y);
                linePoints.Add(pt);
                areaPoints.Add(pt);
            }

            areaPoints.Add(new Point(padLeft + plotWidth, padTop + plotHeight));

            // Dégradé doux sous la courbe d'activité.
            var areaFill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                [
                    new GradientStop(Color.FromArgb(95, AccentCyanColor.R, AccentCyanColor.G, AccentCyanColor.B), 0.0),
                    new GradientStop(Color.FromArgb(8, AccentCyanSoftColor.R, AccentCyanSoftColor.G, AccentCyanSoftColor.B), 1.0)
                ]
            };

            var areaPolygon = new Polygon
            {
                Points = areaPoints,
                Fill = areaFill,
                IsHitTestVisible = false
            };
            _chartCanvas.Children.Add(areaPolygon);

            // Courbe principale en bleu-cyan.
            var curvePolyline = new Polyline
            {
                Points = linePoints,
                Stroke = AccentCyanHoverBrush,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false
            };
            _chartCanvas.Children.Add(curvePolyline);

            // Marqueurs discrets sur les points pour faciliter la lecture visuelle.
            for (int i = 0; i < linePoints.Count; i++)
            {
                Point pt = linePoints[i];
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = BgSurfaceBrush,
                    Stroke = AccentCyanHoverBrush,
                    StrokeThickness = 1.8,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dot, pt.X - 3);
                Canvas.SetTop(dot, pt.Y - 3);
                _chartCanvas.Children.Add(dot);
            }
        }

        /// <summary>
        /// Met à jour le point survolé et positionne l'infobulle détaillée lors du déplacement de la souris sur la courbe.
        /// Pourquoi : Permet de consulter la valeur exacte d'une heure donnée avec une transition fluide.
        /// </summary>
        private void OnChartCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel is null || _viewModel.ActivityPoints24h.Count == 0 || _chartCanvas.ActualWidth <= 20)
            {
                return;
            }

            Point mousePos = e.GetPosition(_chartCanvas);
            const double padLeft = 10;
            const double padRight = 10;
            double plotWidth = Math.Max(10, _chartCanvas.ActualWidth - padLeft - padRight);

            double clampedX = Math.Clamp(mousePos.X - padLeft, 0, plotWidth);
            int count = _viewModel.ActivityPoints24h.Count;
            int index = count > 1
                ? (int)Math.Round((clampedX / plotWidth) * (count - 1))
                : 0;
            index = Math.Clamp(index, 0, count - 1);

            HourlyActivityPoint point = _viewModel.ActivityPoints24h[index];
            _viewModel.HoveredActivityPoint = point;

            _chartTooltipTitle.Text = $"{point.HourLabel} • Indice d'activité : {point.ActivityValue:0}/100";
            _chartTooltipSubtitle.Text = point.Details;

            if (_chartTooltipBorder.Visibility != Visibility.Visible)
            {
                _chartTooltipBorder.Visibility = Visibility.Visible;
                _chartTooltipBorder.BeginAnimation(
                    UIElement.OpacityProperty,
                    new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)));
            }

            // Positionne l'infobulle près du curseur sans déborder hors du graphique.
            _chartTooltipBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tooltipWidth = Math.Min(300, _chartTooltipBorder.DesiredSize.Width > 0 ? _chartTooltipBorder.DesiredSize.Width : 240);
            double tooltipHeight = _chartTooltipBorder.DesiredSize.Height > 0 ? _chartTooltipBorder.DesiredSize.Height : 54;

            double targetX = mousePos.X + 12;
            if (targetX + tooltipWidth > _chartCanvas.ActualWidth - 8)
            {
                targetX = Math.Max(8, mousePos.X - tooltipWidth - 12);
            }

            double targetY = Math.Max(6, Math.Min(mousePos.Y - tooltipHeight - 10, _chartCanvas.ActualHeight - tooltipHeight - 6));

            _chartTooltipBorder.Margin = new Thickness(targetX, targetY, 0, 0);
        }

        /// <summary>
        /// Masque l'infobulle de la courbe lorsque le curseur quitte la zone du graphique.
        /// Pourquoi : Garde la courbe propre et dégagée lorsque l'utilisateur n'inspecte plus un point.
        /// </summary>
        private void OnChartCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            if (_viewModel is not null)
            {
                _viewModel.HoveredActivityPoint = null;
            }

            if (_chartTooltipBorder is not null && _chartTooltipBorder.Visibility == Visibility.Visible)
            {
                var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (_, _) =>
                {
                    if (_viewModel?.HoveredActivityPoint is null)
                    {
                        _chartTooltipBorder.Visibility = Visibility.Collapsed;
                    }
                };
                _chartTooltipBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }

        /// <summary>
        /// Met à jour les indicateurs visuels liés à l'état de protection et au score de confiance IA.
        /// Pourquoi : Synchronise les couleurs d'état (Protégé, Attention, Menace) et la largeur de la jauge IA.
        /// </summary>
        private void UpdateVisualIndicators()
        {
            if (_viewModel is null)
            {
                return;
            }

            Color targetStateColor = _viewModel.ProtectionState switch
            {
                ProtectionLevel.Menace => StatusDangerColor,
                ProtectionLevel.Attention => StatusWarningColor,
                _ => StatusSuccessColor
            };

            _protectionIndicatorBrush.BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(targetStateColor, TimeSpan.FromMilliseconds(300)));

            // Animation discrète de pulsation (350 ms) sur le halo de protection.
            var pulseAnim = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(350)
            };
            pulseAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0.0)));
            pulseAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.14, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
            pulseAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseIn }));

            _protectionPulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnim);
            _protectionPulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnim);

            // Met à jour la largeur de la jauge IA proportionnellement au conteneur.
            if (_aiConfidenceTrack is not null && _aiConfidenceBarFill is not null && _aiConfidenceTrack.ActualWidth > 0)
            {
                double ratio = Math.Clamp(_viewModel.AiConfidenceScore / 100.0, 0.0, 1.0);
                double targetWidth = _aiConfidenceTrack.ActualWidth * ratio;
                _aiConfidenceBarFill.BeginAnimation(
                    FrameworkElement.WidthProperty,
                    new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
            }
        }

        /// <summary>
        /// Gère le changement de DataContext pour s'abonner ou se désabonner proprement des événements du ViewModel.
        /// Pourquoi : Évite les fuites mémoire et maintient la vue synchronisée avec le ViewModel actif.
        /// </summary>
        private void OnDashboardViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is DashboardViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                oldVm.RecentEvents.CollectionChanged -= OnRecentEventsCollectionChanged;
                oldVm.ActivityPoints24h.CollectionChanged -= OnActivityPointsCollectionChanged;
            }

            _viewModel = e.NewValue as DashboardViewModel;

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.RecentEvents.CollectionChanged += OnRecentEventsCollectionChanged;
                _viewModel.ActivityPoints24h.CollectionChanged += OnActivityPointsCollectionChanged;
            }

            RebuildRecentEventsList();
            RedrawActivityChart();
            UpdateVisualIndicators();
        }

        /// <summary>
        /// Réagit aux changements de propriétés du DashboardViewModel.
        /// Pourquoi : Met à jour les éléments visuels customisés (indicateur circulaire, jauge IA, courbe).
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DashboardViewModel.ProtectionState)
                or nameof(DashboardViewModel.AiConfidenceScore)
                or nameof(DashboardViewModel.IsScanning))
            {
                UpdateVisualIndicators();
            }
        }

        /// <summary>
        /// Reconstruit la liste des événements lorsque la collection RecentEvents change.
        /// Pourquoi : Affiche immédiatement les nouveaux événements ajoutés lors d'une analyse rapide.
        /// </summary>
        private void OnRecentEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildRecentEventsList();
        }

        /// <summary>
        /// Redessine la courbe des 24 heures lorsque les points d'activité sont modifiés.
        /// Pourquoi : Maintient le graphique cohérent avec les données fictives mises à jour.
        /// </summary>
        private void OnActivityPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RedrawActivityChart();
        }

        /// <summary>
        /// Synchronise le rendu initial une fois le contrôle chargé dans l'arbre visuel WPF.
        /// Pourquoi : Les dimensions réelles (ActualWidth / ActualHeight) sont disponibles après le chargement.
        /// </summary>
        private void OnDashboardViewLoaded(object sender, RoutedEventArgs e)
        {
            RebuildRecentEventsList();
            RedrawActivityChart();
            UpdateVisualIndicators();
        }

        /// <summary>
        /// Adapte le graphique et la jauge IA lorsque la taille du UserControl change.
        /// Pourquoi : Garantit une mise en page propre de 1280x720 à 1920x1080 et en plein écran.
        /// </summary>
        private void OnDashboardViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawActivityChart();
            UpdateVisualIndicators();
        }

        /// <summary>
        /// Crée un conteneur de carte standard (fond sombre, bordure subtile, coins arrondis, ombre légère).
        /// Pourquoi : Uniformise l'aspect visuel de toutes les cartes du tableau de bord.
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
        /// Crée une ombre portée légère pour donner de la profondeur aux cartes et contrôles clés.
        /// Pourquoi : Ajoute du relief sans alourdir le thème sombre.
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
        /// Crée un ControlTemplate arrondi pour le bouton principal afin d'assurer un rendu net sans XAML.
        /// Pourquoi : Permet de contrôler précisément CornerRadius, Padding et état désactivé en pur code C#.
        /// </summary>
        private static ControlTemplate CreateRoundedButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "ButtonBorder";
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

            var disabledTrigger = new Trigger
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55));
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Convertit une chaîne hexadécimale (#RRGGBB) en structure Color WPF.
        /// Pourquoi : Centralise la déclaration des couleurs de thème en format hexadécimal lisible.
        /// </summary>
        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        /// <summary>
        /// Fige un Brush afin d'améliorer les performances WPF et permettre son partage sécurisé.
        /// Pourquoi : Un Brush figé est immuable, léger et sûr à réutiliser sur plusieurs contrôles.
        /// </summary>
        private static T FreezeBrush<T>(T brush) where T : Brush
        {
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }
            return brush;
        }

        /// <summary>
        /// Convertisseur booléen vers texte pour le bouton principal "Analyse rapide".
        /// Pourquoi : Adapte dynamiquement le libellé du bouton pendant la simulation d'analyse.
        /// </summary>
        private sealed class BooleanToScanLabelConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                bool isScanning = value is bool b && b;
                return isScanning ? "Analyse en cours..." : "Analyse rapide";
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
    }
}