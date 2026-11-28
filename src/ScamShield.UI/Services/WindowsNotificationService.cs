using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ScamShield.UI.Services
{
    /// <summary>
    /// Service chargé d'afficher une notification Windows lorsqu'une menace est bloquée.
    /// Pourquoi : Informe immédiatement l'utilisateur avec un message sobre et compréhensible
    /// dès qu'un fichier dangereux est détecté et isolé, avec repli WPF sûr si nécessaire.
    /// </summary>
    public sealed class WindowsNotificationService
    {
        private const string AppDisplayName = "ScamShield";
        private static readonly FontFamily AppFontFamily = new("Segoe UI");

        private readonly IAgentClient? _agentClient;

        /// <summary>
        /// Initialise le service de notification et s'abonne optionnellement aux alertes de IAgentClient.
        /// Pourquoi : Permet de déclencher automatiquement une notification Windows quand une menace est bloquée.
        /// </summary>
        public WindowsNotificationService(IAgentClient? agentClient = null)
        {
            _agentClient = agentClient;
            if (_agentClient is not null)
            {
                _agentClient.ThreatAlertReceived += OnThreatAlertReceived;
            }
        }

        /// <summary>
        /// Déclenche une notification lorsqu'une menace est bloquée (zone Dangereux).
        /// Pourquoi : Évite d'inonder l'utilisateur pour des fichiers sains et met en avant les menaces bloquées.
        /// </summary>
        public void NotifyThreatBlocked(AgentAlertReport alert)
        {
            if (alert is null || alert.Zone != AgentThreatZone.Dangereux)
            {
                return;
            }

            string title = "Menace bloquée par ScamShield";
            string firstReason = alert.Reasons.FirstOrDefault()
                ?? "Un fichier dangereux a été repéré et mis en sécurité.";
            int scorePercent = (int)Math.Round(alert.RiskScore * 100.0);
            string body = $"{alert.FileName} (risque {scorePercent} %) — {firstReason}";

            if (!TryShowWinRtToastNotification(title, body))
            {
                ShowFallbackPopupNotification(title, alert.FileName, firstReason, scorePercent);
            }
        }

        /// <summary>
        /// Réagit à l'événement ThreatAlertReceived de IAgentClient.
        /// Pourquoi : Relie automatiquement les alertes reçues de l'Agent au déclencheur de notification.
        /// </summary>
        private void OnThreatAlertReceived(object? sender, AgentAlertReport alert)
        {
            NotifyThreatBlocked(alert);
        }

        /// <summary>
        /// Tente d'émettre une notification toast Windows via les types WinRT disponibles.
        /// Pourquoi : Utilise les notifications natives Windows quand l'environnement d'exécution le permet.
        /// </summary>
        private static bool TryShowWinRtToastNotification(string title, string message)
        {
            try
            {
                Type? managerType = Type.GetType(
                    "Windows.UI.Notifications.ToastNotificationManager, Windows, ContentType=WindowsRuntime");
                Type? toastType = Type.GetType(
                    "Windows.UI.Notifications.ToastNotification, Windows, ContentType=WindowsRuntime");
                Type? templateType = Type.GetType(
                    "Windows.UI.Notifications.ToastTemplateType, Windows, ContentType=WindowsRuntime");

                if (managerType is null || toastType is null || templateType is null)
                {
                    return false;
                }

                object text02Template = Enum.Parse(templateType, "ToastText02");
                MethodInfo? getTemplateContent = managerType.GetMethod(
                    "GetTemplateContent",
                    new[] { templateType });
                object? xmlDoc = getTemplateContent?.Invoke(null, new[] { text02Template });
                if (xmlDoc is null)
                {
                    return false;
                }

                MethodInfo? getElementsByTagName = xmlDoc.GetType().GetMethod(
                    "GetElementsByTagName",
                    new[] { typeof(string) });
                object? textNodes = getElementsByTagName?.Invoke(xmlDoc, new object[] { "text" });
                if (textNodes is null)
                {
                    return false;
                }

                PropertyInfo? countProp = textNodes.GetType().GetProperty("Count");
                MethodInfo? itemMethod = textNodes.GetType().GetMethod("Item", new[] { typeof(uint) });
                if (countProp is null || itemMethod is null)
                {
                    return false;
                }

                uint count = Convert.ToUInt32(countProp.GetValue(textNodes));
                if (count < 2)
                {
                    return false;
                }

                object? node0 = itemMethod.Invoke(textNodes, new object[] { 0u });
                object? node1 = itemMethod.Invoke(textNodes, new object[] { 1u });
                PropertyInfo? innerTextProp0 = node0?.GetType().GetProperty("InnerText");
                PropertyInfo? innerTextProp1 = node1?.GetType().GetProperty("InnerText");
                if (innerTextProp0 is null || innerTextProp1 is null)
                {
                    return false;
                }

                innerTextProp0.SetValue(node0, title);
                innerTextProp1.SetValue(node1, message);

                ConstructorInfo? toastCtor = toastType.GetConstructor(new[] { xmlDoc.GetType() });
                object? toast = toastCtor?.Invoke(new[] { xmlDoc });
                if (toast is null)
                {
                    return false;
                }

                MethodInfo? createNotifier = managerType.GetMethod(
                    "CreateToastNotifier",
                    new[] { typeof(string) });
                object? notifier = createNotifier?.Invoke(null, new object[] { AppDisplayName });
                MethodInfo? showMethod = notifier?.GetType().GetMethod("Show", new[] { toastType });
                if (showMethod is null)
                {
                    return false;
                }

                showMethod.Invoke(notifier, new[] { toast });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Affiche une notification visuelle discrète en bas à droite de l'écran WPF si WinRT est indisponible.
        /// Pourquoi : Garantit que l'utilisateur voit toujours l'alerte de menace bloquée, même en mode simulation.
        /// </summary>
        private static void ShowFallbackPopupNotification(
            string title,
            string fileName,
            string reason,
            int scorePercent)
        {
            Application? app = Application.Current;
            if (app is null)
            {
                return;
            }

            void ShowOnUiThread()
            {
                var bgWindow = (Color)ColorConverter.ConvertFromString("#0B0F17");
                var bgCard = (Color)ColorConverter.ConvertFromString("#121826");
                var borderDanger = (Color)ColorConverter.ConvertFromString("#EF4444");
                var textPrimary = (Color)ColorConverter.ConvertFromString("#F1F5F9");
                var textSecondary = (Color)ColorConverter.ConvertFromString("#94A3B8");

                var popupWindow = new Window
                {
                    Width = 360,
                    SizeToContent = SizeToContent.Height,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    FontFamily = AppFontFamily,
                    Opacity = 0
                };

                var card = new Border
                {
                    Background = new SolidColorBrush(bgCard),
                    BorderBrush = new SolidColorBrush(borderDanger),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(16),
                    Margin = new Thickness(12),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.45,
                        BlurRadius = 18,
                        ShadowDepth = 4,
                        Direction = 270
                    }
                };

                var stack = new StackPanel();

                var titleBlock = new TextBlock
                {
                    Text = title,
                    FontFamily = AppFontFamily,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(borderDanger)
                };
                stack.Children.Add(titleBlock);

                var fileBlock = new TextBlock
                {
                    Text = $"{fileName} • Risque {scorePercent} %",
                    FontFamily = AppFontFamily,
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(textPrimary),
                    Margin = new Thickness(0, 6, 0, 4),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stack.Children.Add(fileBlock);

                var reasonBlock = new TextBlock
                {
                    Text = reason,
                    FontFamily = AppFontFamily,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(textSecondary),
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(reasonBlock);

                var closeBtn = new Button
                {
                    Content = "Fermer",
                    FontFamily = AppFontFamily,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(bgWindow),
                    Background = new SolidColorBrush(borderDanger),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(12, 5, 12, 5),
                    Margin = new Thickness(0, 12, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                closeBtn.Click += (_, _) => popupWindow.Close();
                stack.Children.Add(closeBtn);

                card.Child = stack;
                popupWindow.Content = card;

                Rect workArea = SystemParameters.WorkArea;
                popupWindow.Left = workArea.Right - popupWindow.Width - 16;
                popupWindow.Top = workArea.Bottom - 180;

                popupWindow.Show();

                // Animation d'apparition fluide de 250 ms.
                popupWindow.BeginAnimation(
                    UIElement.OpacityProperty,
                    new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
            }

            if (app.Dispatcher.CheckAccess())
            {
                ShowOnUiThread();
            }
            else
            {
                app.Dispatcher.Invoke(ShowOnUiThread);
            }
        }
    }
}