using System;
// Alias pour éviter tous les conflits
using WpfApp = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace Compress_Kami
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : WpfApp
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configuration de l'application
            this.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;

            // Gestion des exceptions non gérées
            this.DispatcherUnhandledException += (sender, args) =>
            {
                WpfMessageBox.Show(
                    $"Une erreur critique s'est produite :\n{args.Exception.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}