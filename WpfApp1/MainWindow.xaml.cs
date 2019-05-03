using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TSP;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TspResult bestTspRes = null;
        private List<Location> parent1;
        private List<Location> parent2;
        private Stopwatch sw;
        private Task[] tasksArray;
        private int phase1Time;
        private int phase2Time;
        private Task Refresher;
        private bool needsRefresh = false;
        private int ThreadId;
        private bool isWorking = false;
        

        public MainWindow()
        {
            InitializeComponent();
            MyCanvas.SizeChanged += MyCanvas_SizeChanged;
            Phase1TimeControl.ValueChanged += Phase1ControlChanged;
            Phase2TimeControl.ValueChanged += Phase2ControlChanged;
        }

        private void Phase1ControlChanged(object sender, System.EventArgs e)
        {
            phase1Time = (int)Phase1TimeControl.Value;
        }

        private void Phase2ControlChanged(object sender, System.EventArgs e)
        {
            phase2Time = (int)Phase2TimeControl.Value;
        }

        private void MyCanvas_SizeChanged(object sender, System.EventArgs e)
        {
            List<Location> tour = null;
            if (bestTspRes != null)
            {
                tour = new List<Location>(bestTspRes.BestTour);
            }
            if (tour != null)
            {
                draw_graph(tour);
            }
        }

        private void btn_chooseFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                FileNameTextBox.Text = openFileDlg.FileName;
            }
        }

        private Ellipse DotFactory(Point p)
        {
            double dotSize = 10;
            Ellipse dot = new Ellipse();
            dot.Stroke = new SolidColorBrush(Colors.Red);
            dot.StrokeThickness = 3;
            dot.Height = dotSize;
            dot.Width = dotSize;
            dot.Fill = new SolidColorBrush(Colors.Red);
            dot.Margin = new Thickness(p.X - dotSize/2, p.Y - dotSize/2, 0, 0);
            return dot;
        }

        public void draw_graph(List<Location> tour)
        {
            MyCanvas.Children.Clear();
            double margin = 0.3 * 1000;
            double currentWidth = MyCanvas.ActualWidth;
            double currentHeight = MyCanvas.ActualHeight;

            double max_X = tour.Max(l => l.X);
            double max_Y = tour.Max(l => l.Y);

            double min_X = tour.Min(l => l.X);
            double min_Y = tour.Min(l => l.Y);

            PointCollection points = new PointCollection();

            foreach (var location in tour)
            {
                double newX = (location.X - min_X + margin / 2) * (currentWidth / (max_X - min_X + margin));
                double newY = (location.Y - min_Y + margin / 2) * (currentHeight / (max_Y - min_Y + margin));

                Point p = new Point(newX, newY);
                points.Add(p);
            }
            points.Add(points[0]);

            Polyline mypolyline = new Polyline();
            mypolyline.Points = points;
            mypolyline.Stroke = new SolidColorBrush(Colors.Black);
            mypolyline.StrokeThickness = 2;
            MyCanvas.Children.Add(mypolyline);

            foreach (var point in points)
            {
                MyCanvas.Children.Add(DotFactory(point));
            }

            OptimalTourTable.ItemsSource = tour;
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            //GUI 
            BtnStart.Visibility = Visibility.Collapsed;
            BtnExit.Visibility = Visibility.Hidden;
            BtnChooseFile.IsEnabled = false;
            LblStatus.Content = "Processing";
            TasksCountControl.IsEnabled = false;

            //variables
            phase1Time = (int)Phase1TimeControl.Value;
            phase2Time = (int)Phase2TimeControl.Value;
            isWorking = true;

            //Get locations from file
            bestTspRes = new TspResult();
            var dataModel = new DataModel(FileNameTextBox.Text);

            //Start timer
            sw = new Stopwatch();
            sw.Start();

            //Set initial values
            bestTspRes.Distance = Double.MaxValue;
            bestTspRes.SolutionCount = 1;
            
            //Init parents
            parent1 = new List<Location>(dataModel.Data);
            parent2 = new List<Location>(dataModel.Data);

            //Shuffle parents
            Task taskA = Task.Run(() => parent1.Shuffle());
            Task taskB = Task.Run(() => parent2.Shuffle());
            taskA.Wait();
            taskB.Wait();
            //Set one parent as best
            checkAndHandleIfBetter(parent1);
            checkAndHandleIfBetter(parent2);

            tasksArray = new Task[(int)TasksCountControl.Value];
            for (int i = 0; i < tasksArray.Length; i++)
            {
                if (i % 2 == 0)
                {
                    tasksArray[i] = Task.Run(() => phase1());
                }
                else
                {
                    tasksArray[i] = Task.Run(() => phase2());
                }

            }
            Refresher = Task.Run(() => refresh());
            BtnStop.Visibility = Visibility.Visible;
        }

        private void phase1()
        {
            List<Location> list1;
            List<Location> list2;
            while (sw.Elapsed.TotalSeconds < phase1Time)
            {
                lock (bestTspRes)
                {
                    bestTspRes.SolutionCount++;
                }
                lock (parent1) lock (parent2)
                {
                    list1 = new List<Location>(parent1);
                    list2 = new List<Location>(parent2);
                }

                List<Location> newlist1 = Utils.PMX(list1, list2);
                List<Location> newlist2 = Utils.PMX(list2, list1);
                
                checkAndHandleIfBetter(newlist1);
                checkAndHandleIfBetter(newlist2);
                lock (parent1) lock (parent2)
                {
                    parent1 = new List<Location>(newlist1);
                    parent2 = new List<Location>(newlist2);
                }
            }
        }

        private void phase2()
        {
            List<Location> list1;
            List<Location> list2;
            while (sw.Elapsed.TotalSeconds < phase2Time)
            {
                lock (bestTspRes)
                {
                    bestTspRes.SolutionCount++;
                }
                lock (parent1) lock (parent2)
                {
                    list1 = new List<Location>(parent1);
                    list2 = new List<Location>(parent2);
                }
                list1.SwapEdges();
                checkAndHandleIfBetter(list1);
                list2.SwapEdges();
                checkAndHandleIfBetter(list2);
                lock (parent1) lock (parent2)
                {
                    parent1 = new List<Location>(list1);
                    parent2 = new List<Location>(list2);
                }
            }
        }

        private void refresh()
        {
            while (isWorking)
            {
                List<Location> tour;
                lock (bestTspRes)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LblBestResult.Content = bestTspRes.Distance;
                        LblSolutionCount.Content = bestTspRes.SolutionCount;
                        LblThreadNumber.Content = ThreadId;
                    }));
                    if (needsRefresh)
                    {
                        needsRefresh = false;
                        tour = new List<Location>(bestTspRes.BestTour);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            draw_graph(tour);
                        }));
                    }
                }
                Thread.Sleep(500);
                if (sw.Elapsed.TotalSeconds > phase2Time && sw.Elapsed.TotalSeconds > phase1Time)
                    isWorking = false;
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                btn_stop_Click(null, null);
            }));
        }

        private void checkAndHandleIfBetter(List<Location> tour)
        {
            bool result = false;
            double distance = Utils.DistanceSum(tour);
            lock (bestTspRes)
            {
                if (bestTspRes.Distance > distance)
                {
                    bestTspRes.Distance = distance;
                    bestTspRes.BestTour = new List<Location>(tour);
                    result = true;
                    needsRefresh = true;
                    ThreadId = Thread.CurrentThread.ManagedThreadId;
                }
            }
        }

        private void btn_end_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            phase1Time = 0;
            phase2Time = 0;
            isWorking = false;

            BtnStop.Visibility = Visibility.Collapsed;
            BtnExit.Visibility = Visibility.Visible;
            BtnChooseFile.IsEnabled = true;
            LblStatus.Content = "Ready";
            TasksCountControl.IsEnabled = true;

            BtnStart.Visibility = Visibility.Visible;
        }
    }
}
