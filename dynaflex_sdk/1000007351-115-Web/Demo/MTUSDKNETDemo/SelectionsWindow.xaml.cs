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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MTUSDKDemo
{
    /// <summary>
    /// Interaction logic for SelectionsWindow.xaml
    /// </summary>
    public partial class SelectionsWindow : Window
    {
        private DispatcherTimer m_dismissTimer;
        private int m_selectedIndex;
        private EventHandler m_dissmissTimerHandler;

        public SelectionsWindow()
        {
            InitializeComponent();
        }

        public void init(string title, List<string> selectionList, long timeout)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.Title = title;

            int index = 0;

            foreach (string selectionText in selectionList)
            {
                Button selectionButton = new Button();
                selectionButton.Height = 40;
                selectionButton.Margin = new Thickness(20, 10, 20, 0);
                selectionButton.Content = selectionText;
                selectionButton.Tag = index++;
                selectionButton.Click += new RoutedEventHandler(selectionButtonClick);
                this.SelectionPanel.Children.Add(selectionButton);
            }

            Button cancelButton = new Button();
            cancelButton.Height = 40;
            cancelButton.Margin = new Thickness(20, 30, 20, 10);
            cancelButton.Content = "Cancel";
            cancelButton.IsDefault = true;
            cancelButton.IsCancel = true;
            m_selectedIndex = 0;
            this.SelectionPanel.Children.Add(cancelButton);

            this.Height = (index * 50) + 140;

            m_dismissTimer = new DispatcherTimer();
            m_dismissTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)timeout);
            m_dissmissTimerHandler = new EventHandler(dismissTimerTick);
            m_dismissTimer.Tick += m_dissmissTimerHandler;
            m_dismissTimer.Start();
        }

        private void dismissTimerTick(object sender, EventArgs e)
        {
            try
            {
                m_dismissTimer.Tick -= m_dissmissTimerHandler;

                this.m_selectedIndex = -1;

                this.DialogResult = false;

                this.Close();
            }
            catch (Exception)
            {
            }
        }

        protected void selectionButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                m_dismissTimer.Tick -= m_dissmissTimerHandler;

                m_selectedIndex = (int)(sender as Button).Tag;

                this.DialogResult = true;

                this.Close();
            }
            catch (Exception)
            {
            }
        }

        public int getSelectedIndex()
        {
            return m_selectedIndex;
        }
    }
}
