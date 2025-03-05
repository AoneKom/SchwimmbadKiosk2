using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using BCrypt.Net;

namespace SchwimmbadKiosk2
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionString = "Data Source=SchwimmbadKiosk.db;Version=3;";
        private bool passwordVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL
                    );";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            if (!UserExists("admin"))
            {
                RegisterUser("admin", "admin123");
            }
        }

        private bool UserExists(string username)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Bitte Benutzername und Passwort eingeben!";
                return;
            }

            if (RegisterUser(username, password))
            {
                lblStatus.Text = "Registrierung erfolgreich!";
            }
            else
            {
                lblStatus.Text = "Benutzer existiert bereits!";
            }
        }

        private bool RegisterUser(string username, string password)
        {
            if (UserExists(username))
            {
                return false;
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@passwordHash", passwordHash);
                    command.ExecuteNonQuery();
                    return true;
                }
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Bitte alle Felder ausfüllen!";
                return;
            }

            if (IsLoginValid(username, password))
            {
                lblWelcome.Text = $"Willkommen, {username}!";
                .Visibility = Visibility.Hidden;
                MainPanel.Visibility = Visibility.Visible;
            }
            else
            {
                lblStatus.Text = "Falsche Anmeldedaten!";
            }
        }

        private bool IsLoginValid(string username, string password)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT PasswordHash FROM Users WHERE Username = @username";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        return BCrypt.Net.BCrypt.Verify(password, result.ToString());
                    }
                    return false;
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            txtUsername.Clear();
            txtPassword.Clear();
            lblStatus.Text = "Erfolgreich abgemeldet.";

          
            MainPanel.Visibility = Visibility.Hidden;
        }

        private void TogglePassword(object sender, RoutedEventArgs e)
        {
            passwordVisible = !passwordVisible;
            txtPassword.Visibility = passwordVisible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
