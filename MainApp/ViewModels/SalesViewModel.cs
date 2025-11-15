using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using IPS.Core.Models;
using IPS.Services;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for sales dashboard and analytics
    /// </summary>
    public class SalesViewModel : BaseViewModel
    {
        private readonly SalesService _salesService;
        private readonly Dispatcher _dispatcher;
        private TimeInterval _selectedTimeInterval = TimeInterval.Last7Days;
        private DateTime _customStartDate = DateTime.Now.AddDays(-7);
        private DateTime _customEndDate = DateTime.Now;
        private SalesStatistics? _statistics;
        private bool _isCustomRangeVisible = false;
        private bool _isLoading = false;

        /// <summary>
        /// Available time interval options
        /// </summary>
        public ObservableCollection<TimeIntervalOption> TimeIntervals { get; } = new();

        /// <summary>
        /// Plot model for revenue chart
        /// </summary>
        public PlotModel RevenuePlotModel { get; set; }

        /// <summary>
        /// Top selling items
        /// </summary>
        public ObservableCollection<TopSellingItem> TopItems { get; } = new();

        /// <summary>
        /// Order history list
        /// </summary>
        public ObservableCollection<OrderHistory> OrderHistory { get; } = new();

        /// <summary>
        /// Revenue by system breakdown
        /// </summary>
        public ObservableCollection<SystemRevenue> SystemRevenue { get; } = new();

        /// <summary>
        /// Currently selected time interval
        /// </summary>
        public TimeInterval SelectedTimeInterval
        {
            get => _selectedTimeInterval;
            set
            {
                _selectedTimeInterval = value;
                OnPropertyChanged();
                IsCustomRangeVisible = value == TimeInterval.CustomRange;
                _ = RefreshDataAsync(); // Fire and forget for property setter
            }
        }

        /// <summary>
        /// Custom start date for custom range
        /// </summary>
        public DateTime CustomStartDate
        {
            get => _customStartDate;
            set
            {
                _customStartDate = value;
                OnPropertyChanged();
                // Don't auto-refresh when date changes - user must click Apply
            }
        }

        /// <summary>
        /// Custom end date for custom range
        /// </summary>
        public DateTime CustomEndDate
        {
            get => _customEndDate;
            set
            {
                _customEndDate = value;
                OnPropertyChanged();
                // Don't auto-refresh when date changes - user must click Apply
            }
        }

        /// <summary>
        /// Whether custom date range inputs are visible
        /// </summary>
        public bool IsCustomRangeVisible
        {
            get => _isCustomRangeVisible;
            set
            {
                _isCustomRangeVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether sales data is currently loading
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        // Summary statistics properties
        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set { _totalRevenue = value; OnPropertyChanged(); }
        }

        private int _totalOrders;
        public int TotalOrders
        {
            get => _totalOrders;
            set { _totalOrders = value; OnPropertyChanged(); }
        }

        private decimal _averageOrderValue;
        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            set { _averageOrderValue = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to export data
        /// </summary>
        public IRelayCommand ExportDataCommand { get; }

        /// <summary>
        /// Command to refresh data
        /// </summary>
        public IRelayCommand RefreshDataCommand { get; }

        /// <summary>
        /// Command to apply custom date range
        /// </summary>
        public IRelayCommand ApplyCustomRangeCommand { get; }

        public SalesViewModel()
        {
            _salesService = new SalesService();
            _dispatcher = Dispatcher.CurrentDispatcher;

            ExportDataCommand = new RelayCommand(OnExportData);
            RefreshDataCommand = new RelayCommand(async () => await RefreshDataAsync());
            ApplyCustomRangeCommand = new RelayCommand(async () => await RefreshDataAsync());

            // Initialize OxyPlot model
            RevenuePlotModel = new PlotModel { Title = "Revenue Trend" };

            InitializeTimeIntervals();

            // Initial data load (synchronous in constructor to avoid blocking)
            RefreshDataSync();
        }

        private void InitializeTimeIntervals()
        {
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.Today, DisplayName = "Today" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.Yesterday, DisplayName = "Yesterday" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.Last7Days, DisplayName = "Last 7 Days" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.Last30Days, DisplayName = "Last 30 Days" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.ThisMonth, DisplayName = "This Month" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.LastMonth, DisplayName = "Last Month" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.Last3Months, DisplayName = "Last 3 Months" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.ThisYear, DisplayName = "This Year" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.AllTime, DisplayName = "All Time" });
            TimeIntervals.Add(new TimeIntervalOption { Interval = TimeInterval.CustomRange, DisplayName = "Custom Range" });
        }


        /// <summary>
        /// Refresh sales data asynchronously with loading indicator
        /// </summary>
        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            Console.WriteLine("[SalesViewModel] RefreshDataAsync - Loading started");

            try
            {
                await Task.Run(() =>
                {
                    RefreshDataSync();
                });

                Console.WriteLine("[SalesViewModel] RefreshDataAsync - Loading completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesViewModel] RefreshDataAsync - Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Synchronous data refresh (used by async wrapper and constructor)
        /// </summary>
        private void RefreshDataSync()
        {
            var (startDate, endDate) = _salesService.GetTimeRange(
                SelectedTimeInterval,
                CustomStartDate,
                CustomEndDate
            );

            _statistics = _salesService.GetSalesStatistics(startDate, endDate);

            // Update on UI thread
            _dispatcher.Invoke(() =>
            {
                // Update summary statistics
                TotalRevenue = _statistics.TotalRevenue;
                TotalOrders = _statistics.TotalOrders;
                AverageOrderValue = _statistics.AverageOrderValue;

                // Update chart
                UpdateChart(_statistics.DailySalesData);

                // Update top items
                TopItems.Clear();
                foreach (var item in _statistics.TopItems)
                {
                    TopItems.Add(item);
                }

                // Update order history
                var orders = _salesService.GetOrderHistory(startDate, endDate);
                OrderHistory.Clear();
                foreach (var order in orders)
                { 
                    OrderHistory.Add(order);
                }

                // Update system revenue
                SystemRevenue.Clear();
                foreach (var kvp in _statistics.RevenueBySystem)
                {
                    SystemRevenue.Add(new SystemRevenue
                    {
                        SystemName = kvp.Key,
                        Revenue = kvp.Value,
                        Percentage = _statistics.TotalRevenue > 0 ? (double)(kvp.Value / _statistics.TotalRevenue * 100) : 0
                    });
                }

                Console.WriteLine($"[SalesViewModel] Data refreshed for {SelectedTimeInterval}: {TotalOrders} orders, ${TotalRevenue:F2} revenue");
            });
        }

        private void UpdateChart(List<DailySales> dailySales)
        {
            // Clear the plot model
            RevenuePlotModel.Series.Clear();
            RevenuePlotModel.Axes.Clear();

            // Create line series
            var lineSeries = new LineSeries
            {
                Title = "Revenue",
                Color = OxyColors.DodgerBlue,
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                MarkerFill = OxyColors.White,
                MarkerStroke = OxyColors.DodgerBlue,
                MarkerStrokeThickness = 2
            };

            // Add data points
            for (int i = 0; i < dailySales.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(i, (double)dailySales[i].Revenue));
            }

            // Add series to plot model
            RevenuePlotModel.Series.Add(lineSeries);

            // Configure axes
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Date",
                Key = "DateAxis",
                ItemsSource = dailySales.Select(d => d.Date.ToString("MM/dd")).ToList()
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Revenue ($)",
                Key = "ValueAxis",
                MinimumPadding = 0.1,
                MaximumPadding = 0.1
            };

            RevenuePlotModel.Axes.Add(categoryAxis);
            RevenuePlotModel.Axes.Add(valueAxis);

            // Refresh the plot
            RevenuePlotModel.InvalidatePlot(true);
        }

        private void OnExportData()
        {
            try
            {
                var fileName = $"SalesData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var writer = new System.IO.StreamWriter(filePath))
                {
                    // Write header
                    writer.WriteLine("Order ID,Order Label,Date & Time,Total Amount,Payment Method,Status,Items");

                    // Write order data
                    foreach (var order in OrderHistory)
                    {
                        var itemsSummary = string.Join("; ", order.Items.Select(i => $"{i.ItemName} x{i.Quantity}"));
                        writer.WriteLine($"\"{order.OrderId}\",\"{order.OrderLabel}\",\"{order.OrderDateTime:yyyy-MM-dd HH:mm}\",{order.TotalAmount},\"{order.PaymentMethod}\",\"{order.Status}\",\"{itemsSummary}\"");
                    }
                }

                Console.WriteLine($"[SalesViewModel] Data exported to {filePath}");
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesViewModel] Export failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Time interval option for combo box
    /// </summary>
    public class TimeIntervalOption
    {
        public TimeInterval Interval { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// System revenue display model
    /// </summary>
    public class SystemRevenue
    {
        public string SystemName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }
}
