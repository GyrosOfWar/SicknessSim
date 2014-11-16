using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
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
            bitmap.Clear(Colors.Black);
            img.Source = bitmap;

            CompositionTarget.Rendering += (s, t) => {
                DrawSimulation();
                simulation.Tick();
            };
        }

        public void DrawSimulation() {
            if (simulation.SimulationFinished) {
                return;
            }

            using (bitmap.GetBitmapContext()) {
                bitmap.Clear(Colors.Black);
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

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var coordinates = e.GetPosition(img);
            var person = new Person(new Vector((int) coordinates.X, (int) coordinates.Y), Status.Infectious,
                                    simulation.Rng) {TimeInfected = simulation.Time};
            simulation.AddPerson(person);
        }
    }
}