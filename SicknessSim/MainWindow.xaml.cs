using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SicknessSim {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window {
        private readonly WriteableBitmap bitmap;
        private readonly Simulation simulation;

        public MainWindow() {
            InitializeComponent();
            simulation = new Simulation(Constants.PopulationSize);
            bitmap = BitmapFactory.New(800, 800);
            img.Source = bitmap;
        }

        public void DrawSimulation() {
            using (bitmap.GetBitmapContext()) {
                bitmap.Clear();
                foreach (var person in simulation.Persons) {
                    var color = Colors.Green;

                    switch (person.Status) {
                        case Status.Healthy:
                            color = Constants.HealthyColor;
                            break;
                        case Status.Infectious:
                            color = Constants.InfectiousColor;
                            break;
                        case Status.Sick:
                            color = Constants.SickColor;
                            break;
                        case Status.Dead:
                            color = Constants.DeadColor;
                            break;
                        default:
                            Debug.Fail("uuuhhhh");
                            break;
                    }
                    //var r = (int)Constants.InfluenceRadius;
                    //bitmap.FillEllipseCentered((int)person.Position.X, (int)person.Position.Y, r, r, Color.FromArgb(50, 255, 255, 0));
                    bitmap.FillEllipseCentered((int) person.Position.X, (int) person.Position.Y, 2, 2, color);
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) {
            CompositionTarget.Rendering += (s, t) => {
                DrawSimulation();
                simulation.Tick();
            };
        }
    }
}