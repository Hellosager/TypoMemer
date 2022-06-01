using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using TypoMemer.Models;

namespace TypoMemer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    // try this: https://blog.magnusmontin.net/2015/03/31/implementing-global-hot-keys-in-wpf/
    public partial class App : Application
    {

        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isExit;

        private MongoClient mongoClient;
        private IMongoDatabase database;
        public static IMongoCollection<Word> wordCollection;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();
            MainWindow.Closing += MainWindow_Closing;

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = TypoMemer.Properties.Resources.mycon;
            _notifyIcon.Visible = true;

            CreateContextMenu();

            LoadDictionary();
        }

        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip =
              new System.Windows.Forms.ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("MainWindow...").Click += (s, e) => ShowMainWindow();
            _notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApplication();
        }

        private void LoadDictionary() 
        {
            string[] args = Environment.GetCommandLineArgs();
            Debug.WriteLine("Connecting via " + args[1]);
            mongoClient = new MongoClient(args[1]);
            database = mongoClient.GetDatabase("TypoMemer");
            wordCollection = database.GetCollection<Word>("germanWords");
           // var results = wordCollection.Find(word => word.word == "Sebastian").ToList();
        }

        private void ExitApplication()
        {
            _isExit = true;
            MainWindow.Close();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        public void ShowMainWindow()
        {
            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                MainWindow.Hide(); // A hidden window can be shown again, a closed one not
            }
        }
    }
}
