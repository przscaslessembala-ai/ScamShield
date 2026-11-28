using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ScamShield.UI.ViewModels;
using ScamShield.UI.Views;

namespace ScamShield.UI
{
    /// <summary>
    /// Fenêtre dédiée à la navigation multi-vues de Joseph (Tableau de bord, Alertes par zone,
    /// Quarantaine, Paramètres et Assistant de dépannage), construite en pur code C# sans modifier MainViewModel.cs.
    /// Pourquoi : Permet de visualiser et tester le tableau de bord et les autres vues
    /// tout en préservant la fenêtre et le MainViewModel existants.
    /// </summary>
    public class NavigationWindow : Window
    {
        private static readonly FontFamily AppFontFamily = new("Segoe UI");

        private static readonly Color BgWindowColor = ColorFromHex("#0B0F17");
        private static readonly Color BgSidebarColor = ColorFromHex("#0F1522");
        private static readonly Color BgSurfaceColor = ColorFromHex("#121826");
        private static readonly Color BgSurfaceElevatedColor = ColorFromHex("#182032");
        private static readonly Color BorderSubtleColor = ColorFromHex("#243049");
        private static readonly Color TextPrimaryColor = ColorFromHex("#F1F5F9");
        private static readonly Color TextSecondaryColor = ColorFromHex("#94A3B8");
        private static readonly Color AccentCyanColor = ColorFromHex("#06B6D4");
        private static readonly Color AccentCyanHoverColor = ColorFromHex("#22D3EE");
        private static readonly Color StatusProtectedColor = ColorFromHex("#10B981");
        private static readonly Color StatusAtRiskColor = ColorFromHex("#EF4444");

        private static readonly Brush BgWindowBrush = FreezeBrush(new SolidColorBrush(BgWindowColor));
        private static readonly Brush BgSidebarBrush = FreezeBrush(new SolidColorBrush(BgSidebarColor));
        private static readonly Brush BgSurfaceBrush = FreezeBrush(new SolidColorBrush(BgSurfaceColor));
        private static readonly Brush BgSurfaceElevatedBrush = FreezeBrush(new SolidColorBrush(BgSurfaceElevatedColor));
        private static readonly Brush BorderSubtleBrush = FreezeBrush(new SolidColorBrush(BorderSubtleColor));
        private static readonly Brush TextPrimaryBrush = FreezeBrush(new SolidColorBrush(TextPrimaryColor));
        private static readonly Brush TextSecondaryBrush = FreezeBrush(new SolidColorBrush(TextSecondaryColor));
        private static readonly Brush AccentCyanBrush = FreezeBrush(new SolidColorBrush(AccentCyanColor));
        private static readonly Brush AccentCyanHoverBrush = FreezeBrush(new SolidColorBrush(AccentCyanHoverColor));
        private static readonly Brush StatusProtectedBrush = FreezeBrush(new SolidColorBrush(StatusProtectedColor));
        private static readonly Brush StatusAtRiskBrush = FreezeBrush(new SolidColorBrush(StatusAtRiskColor));

        private readonly Dictionary<ShellSection, (Border Card, SolidColorBrush BgBrush, SolidColorBrush BorderBrush, TextBlock LabelText)> _navButtons = [];
        private readonly Dictionary<ShellSection, UIElement> _sectionViews = [];

        private Border _contentHost = null!;
        private ContentControl _viewHost = null!;
        private Border _statusBadge = null!;
        private TextBlock _statusBadgeText = null!;
        private NavigationShellViewModel? _shellViewModel;

        /// <summary>
        /// Initialise la fenêtre multi-vues avec NavigationShellViewModel.
        /// Pourquoi : Fournit une fenêtre autonome où le tableau de bord s'affiche dès l'ouverture.
        /// </summary>
        public NavigationWindow()
        {
            Title = "ScamShield — Tableau de bord & Navigation";
            Width = 1240;
            Height = 740;
            MinWidth = 980;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = AppFontFamily;
            Foreground = TextPrimaryBrush;
            Background = BgWindowBrush;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;

            Content = BuildLayout();

            if (DataContext is null)
            {
                DataContext = new NavigationShellViewModel();
            }
        }

        private UIElement BuildLayout()
        {
            var rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230), MinWidth = 200, MaxWidth = 270 });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border sidebar = BuildSidebar();
            Grid.SetColumn(sidebar, 0);
            rootGrid.Children.Add(sidebar);

            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border topBar = BuildTopBar();
            Grid.SetRow(topBar, 0);
            rightGrid.Children.Add(topBar);

            _contentHost = new Border { Background = BgWindowBrush };
            _viewHost = new ContentControl();
            _contentHost.Child = _viewHost;
            Grid.SetRow(_contentHost, 1);
            rightGrid.Children.Add(_contentHost);

            Border footer = BuildFooter();
            Grid.SetRow(footer, 2);
            rightGrid.Children.Add(footer);

            Grid.SetColumn(rightGrid, 1);
            rootGrid.Children.Add(rightGrid);

            return rootGrid;
        }

        private Border BuildSidebar()
        {
            var sidebar = new Border
            {
                Background = BgSidebarBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(16, 22, 16, 18)
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = "SCAMSHIELD",
                FontFamily = AppFontFamily,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = AccentCyanHoverBrush
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Espace navigation • MVVM",
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(0, 2, 0, 22)
            });

            stack.Children.Add(CreateNavItem(ShellSection.TableauDeBord, "Tableau de bord"));
            stack.Children.Add(CreateNavItem(ShellSection.AlertesParZone, "Alertes par zone"));
            stack.Children.Add(CreateNavItem(ShellSection.Quarantaine, "Quarantaine"));
            stack.Children.Add(CreateNavItem(ShellSection.Parametres, "Paramètres"));
            stack.Children.Add(CreateNavItem(ShellSection.AssistantDepannage, "Assistant de dépannage"));

            sidebar.Child = stack;
            return sidebar;
        }

        private Border CreateNavItem(ShellSection section, string label)
        {
            var bgBrush = new SolidColorBrush(Colors.Transparent);
            var borderBrush = new SolidColorBrush(Colors.Transparent);

            var card = new Border
            {
                Background = bgBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 11, 14, 11),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            var text = new TextBlock
            {
                Text = label,
                FontFamily = AppFontFamily,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextSecondaryBrush
            };
            card.Child = text;

            card.MouseLeftButtonUp += (_, _) =>
            {
                if (_shellViewModel is not null && _shellViewModel.NavigateCommand.CanExecute(section))
                {
                    _shellViewModel.NavigateCommand.Execute(section);
                }
            };

            card.MouseEnter += (_, _) =>
            {
                if (_shellViewModel?.CurrentSection != section)
                {
                    bgBrush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(BgSurfaceElevatedColor, TimeSpan.FromMilliseconds(220)));
                }
            };

            card.MouseLeave += (_, _) =>
            {
                if (_shellViewModel?.CurrentSection != section)
                {
                    bgBrush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(Colors.Transparent, TimeSpan.FromMilliseconds(220)));
                }
            };

            _navButtons[section] = (card, bgBrush, borderBrush, text);
            return card;
        }

        private Border BuildTopBar()
        {
            var bar = new Border
            {
                Background = BgSurfaceBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 14, 24, 14),
                Effect = CreateSoftShadow(0.2, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusBadge = new Border
            {
                Background = FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 16, 185, 129))),
                BorderBrush = StatusProtectedBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 5, 12, 5),
                VerticalAlignment = VerticalAlignment.Center
            };

            _statusBadgeText = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = StatusProtectedBrush
            };
            _statusBadgeText.SetBinding(TextBlock.TextProperty, new Binding(nameof(NavigationShellViewModel.ProtectionStatusLabel)));
            _statusBadge.Child = _statusBadgeText;

            Grid.SetColumn(_statusBadge, 0);
            grid.Children.Add(_statusBadge);

            var summary = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                Foreground = TextSecondaryBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 14, 0),
                TextWrapping = TextWrapping.Wrap
            };
            summary.SetBinding(TextBlock.TextProperty, new Binding(nameof(NavigationShellViewModel.ProtectionSummary)));
            Grid.SetColumn(summary, 1);
            grid.Children.Add(summary);

            var refreshBtn = new Button
            {
                Content = "Actualiser l'état",
                FontFamily = AppFontFamily,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush,
                Background = BgSurfaceElevatedBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Cursor = Cursors.Hand,
                Template = CreateRoundedButtonTemplate()
            };
            refreshBtn.SetBinding(Button.CommandProperty, new Binding(nameof(NavigationShellViewModel.RefreshStatusCommand)));
            Grid.SetColumn(refreshBtn, 2);
            grid.Children.Add(refreshBtn);

            bar.Child = grid;
            return bar;
        }

        private static Border BuildFooter()
        {
            var footer = new Border
            {
                Background = BgSidebarBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 8, 24, 8)
            };

            var msg = new TextBlock
            {
                FontFamily = AppFontFamily,
                FontSize = 11.5,
                Foreground = TextSecondaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            msg.SetBinding(TextBlock.TextProperty, new Binding(nameof(NavigationShellViewModel.LastActivityMessage)));
            footer.Child = msg;

            return footer;
        }

        private void EnsureViews(NavigationShellViewModel vm)
        {
            if (_sectionViews.Count > 0)
            {
                return;
            }

            _sectionViews[ShellSection.TableauDeBord] = new DashboardView { DataContext = vm.DashboardViewModel };
            _sectionViews[ShellSection.AlertesParZone] = new AlertsView { DataContext = vm.AlertsViewModel };
            _sectionViews[ShellSection.Quarantaine] = new QuarantineView { DataContext = vm.QuarantineViewModel };
            _sectionViews[ShellSection.Parametres] = new SettingsView { DataContext = vm.SettingsViewModel };
            _sectionViews[ShellSection.AssistantDepannage] = new TroubleshootingView { DataContext = vm.TroubleshootingViewModel };
        }

        private void UpdateActiveView()
        {
            if (_shellViewModel is null || _viewHost is null)
            {
                return;
            }

            EnsureViews(_shellViewModel);

            if (_sectionViews.TryGetValue(_shellViewModel.CurrentSection, out UIElement? view))
            {
                _viewHost.Content = view;
                _contentHost?.BeginAnimation(
                    UIElement.OpacityProperty,
                    new DoubleAnimation(0.55, 1.0, TimeSpan.FromMilliseconds(250))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
            }

            foreach (KeyValuePair<ShellSection, (Border Card, SolidColorBrush BgBrush, SolidColorBrush BorderBrush, TextBlock LabelText)> entry in _navButtons)
            {
                bool active = entry.Key == _shellViewModel.CurrentSection;
                entry.Value.BgBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(active ? BgSurfaceElevatedColor : Colors.Transparent, TimeSpan.FromMilliseconds(220)));
                entry.Value.BorderBrush.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(active ? AccentCyanColor : Colors.Transparent, TimeSpan.FromMilliseconds(220)));
                entry.Value.LabelText.Foreground = active ? AccentCyanHoverBrush : TextSecondaryBrush;
            }
        }

        private void UpdateStatusVisual()
        {
            if (_shellViewModel is null || _statusBadge is null || _statusBadgeText is null)
            {
                return;
            }

            if (_shellViewModel.IsRiskState)
            {
                _statusBadge.Background = FreezeBrush(new SolidColorBrush(Color.FromArgb(45, 239, 68, 68)));
                _statusBadge.BorderBrush = StatusAtRiskBrush;
                _statusBadgeText.Foreground = StatusAtRiskBrush;
            }
            else
            {
                _statusBadge.Background = FreezeBrush(new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)));
                _statusBadge.BorderBrush = StatusProtectedBrush;
                _statusBadgeText.Foreground = StatusProtectedBrush;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is NavigationShellViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnShellPropertyChanged;
            }

            _shellViewModel = e.NewValue as NavigationShellViewModel;

            if (_shellViewModel is not null)
            {
                _shellViewModel.PropertyChanged += OnShellPropertyChanged;
                UpdateActiveView();
                UpdateStatusVisual();
            }
        }

        private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NavigationShellViewModel.CurrentSection))
            {
                UpdateActiveView();
            }
            else if (e.PropertyName == nameof(NavigationShellViewModel.IsRiskState) ||
                     e.PropertyName == nameof(NavigationShellViewModel.ProtectionStatus))
            {
                UpdateStatusVisual();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateActiveView();
            UpdateStatusVisual();
        }

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
            return template;
        }

        private static DropShadowEffect CreateSoftShadow(double opacity = 0.25, double blurRadius = 12)
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

        private static Color ColorFromHex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

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