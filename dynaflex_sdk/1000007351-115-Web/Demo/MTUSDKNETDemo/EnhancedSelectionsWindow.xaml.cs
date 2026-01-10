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

using MTUSDKNET;

namespace MTUSDKDemo
{
    /// <summary>
    /// Interaction logic for EnhancedSelectionsWindow.xaml
    /// </summary>
    public partial class EnhancedSelectionsWindow : Window
    {
        private DispatcherTimer m_dismissTimer;
        private int m_selectedIndex;
        private EventHandler m_dissmissTimerHandler;

        public EnhancedSelectionsWindow()
        {
            InitializeComponent();
        }

        public void init(string title, List<DirectoryEntry> enhancedSelectionList, long timeout)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.Title = title;

            int index = 0;

            foreach (DirectoryEntry directoryEntry in enhancedSelectionList)
            {
                Button selectionButton = new Button();
                selectionButton.Height = 40;
                selectionButton.Margin = new Thickness(20, 10, 20, 0);
                selectionButton.Content = directoryEntry.Label + " [ AID: " + directoryEntry.AID + " ]";
                string tipString = "Priority: " + directoryEntry.Priority + "\nKernel Identifier: " + directoryEntry.KernelIdentifier;
                if (directoryEntry.ProprietaryData != null)
                    tipString += "\nProprietary Data: " + MTParser.getHexString(directoryEntry.ProprietaryData);
                if (directoryEntry.IssuerIN != null)
                    tipString += "\nIssuer IN: " + MTParser.getHexString(directoryEntry.IssuerIN);
                if (directoryEntry.IssuerINE != null)
                    tipString += "\nIssuer INE: " + MTParser.getHexString(directoryEntry.IssuerINE);
                if (directoryEntry.IssuerCountryCodeAlpha2 != null)
                    tipString += "\nIssuer Country Code (alpha2): " + MTParser.getHexString(directoryEntry.IssuerCountryCodeAlpha2);
                if (directoryEntry.IssuerCountryCodeAlpha3 != null)
                    tipString += "\nIssuer Country Code (alpha3): " + MTParser.getHexString(directoryEntry.IssuerCountryCodeAlpha3);
                if (directoryEntry.CardProductDetails != null)
                    tipString += "\nCardProduct Details: " + MTParser.getHexString(directoryEntry.CardProductDetails);
                selectionButton.ToolTip = tipString;
                selectionButton.Tag = index++;
                selectionButton.Click += new RoutedEventHandler(selectionButtonClick);
                this.EnhancedSelectionPanel.Children.Add(selectionButton);
            }

            Button cancelButton = new Button();
            cancelButton.Height = 40;
            cancelButton.Margin = new Thickness(20, 30, 20, 10);
            cancelButton.Content = "Cancel";
            cancelButton.IsDefault = true;
            cancelButton.IsCancel = true;
            m_selectedIndex = 0;
            this.EnhancedSelectionPanel.Children.Add(cancelButton);

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
