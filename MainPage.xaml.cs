using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Podometer;

public partial class MainPage : ContentPage
{
    private const double InitialThreshold = 1.5; // Umbral inicial de detección de pasos
    private const double AdjustmentFactor = 0.1; // Factor de ajuste para el umbral
    private double currentThreshold = InitialThreshold; // Umbral actual
    private bool isStepDetected = false;
    private int stepCount = 0;
    private const int MovingAverageWindowSize = 5; // Tamaño de la ventana del promedio móvil
    private List<double> accelerationValues = new List<double>();
    private readonly HttpClient _httpClient;
    private TimeSpan walkingTime = TimeSpan.Zero;
    private double caloriasQuemadas;



    public MainPage()
    {
        InitializeComponent();
        
        Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
        Accelerometer.Start(SensorSpeed.UI);
        _httpClient = new HttpClient();
        Reiniciar.IsVisible = false;
        

    }
 

    private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {

        double acceleration = Math.Sqrt(e.Reading.Acceleration.X * e.Reading.Acceleration.X +
                                        e.Reading.Acceleration.Y * e.Reading.Acceleration.Y +
                                        e.Reading.Acceleration.Z * e.Reading.Acceleration.Z);


        accelerationValues.Add(acceleration);


        // Mantener el tamaño de la lista de valores de aceleración dentro del límite establecido por el tamaño de la ventana
        if (accelerationValues.Count > MovingAverageWindowSize)
        {
            accelerationValues.RemoveAt(0); // Eliminar el valor más antiguo
        }


        double averagedAcceleration = CalculateMovingAverage(accelerationValues);


        ProcessAcceleration(acceleration);

        Dispatcher.Dispatch(() =>
        {
            StepLabel.Text = $"Pasos: {stepCount}";
            Tiempo.Text = $"Tiempo caminando: {walkingTime.ToString(@"hh\:mm\:ss")}";

        });
    }
    private double CalculateMovingAverage(List<double> values)
    {
        // Calcular el promedio de los valores en la lista
        double sum = values.Sum();
        return sum / values.Count;
    }
    private void ProcessAcceleration(double acceleration)
    {
        if (!isStepDetected && acceleration > currentThreshold)
        {
            // Se detectó un paso
            stepCount++;
            isStepDetected = true;
            // Ajustar el umbral hacia arriba
            currentThreshold += AdjustmentFactor;
        }
        else if (isStepDetected && acceleration <= currentThreshold)
        {
            // Restablecer el indicador de detección de paso cuando la aceleración vuelve por debajo del umbral
            isStepDetected = false;
            // Ajustar el umbral hacia abajo
            currentThreshold -= AdjustmentFactor;
            walkingTime += TimeSpan.FromSeconds(1);


            //por cada paso dado quiero que se actualize el contador de calorias

            double Drecorrida = calcularDistancia(Preferences.Get("Genero", string.Empty), double.Parse(Preferences.Get("Altura", string.Empty)), stepCount);
            double KcaloriasQuemadas = CalcularKCaloriasQuemadas(stepCount, Drecorrida, double.Parse(Preferences.Get("Altura", string.Empty)));
            caloriasQuemadas = Math.Round(KcaloriasQuemadas / 1000, 0);
            Dispatcher.Dispatch(() =>
            {

                Calorias.Text = $"Calorias quemadas: {caloriasQuemadas}";

            });
        }

        //si el umbral actual es menor a 1.2 o mayor a 2.0 que quite de la lista el valor mas nuevo
        if (currentThreshold < 1.2)
        {
            accelerationValues.RemoveAt(5);
            currentThreshold += AdjustmentFactor;

        }
        if (currentThreshold > 2.0)
        {
            accelerationValues.RemoveAt(5);
            currentThreshold -= AdjustmentFactor;
        }
    }

    protected double calcularDistancia(string genero, double altura, int pasos)
    {
        double zancada;
        altura = altura / 100;

        if (genero == "Masculino")
        {
            zancada = (double)altura * 0.415;

            return (double)(pasos * zancada);
        }
        if (genero == "Femenino")
        {
            zancada = (double)altura * 0.413;

            return (double)(pasos * zancada);
        }
        else
        {
            zancada = (double)altura * 0.414;

            return (double)(pasos * zancada);
        }
    }

    //funcion para calcular calorias quemadas
    public double CalcularKCaloriasQuemadas(int pasos, double distancia, double peso)
    {
        // Supongamos una velocidad promedio de 0.8 metros por paso
        double velocidadPromedio = 0.8;

        // Supongamos que una persona quema alrededor de 0.05 calorías por metro recorrido por kilogramo de peso corporal
        double factorCalorias = 0.05;

        // Calcular la distancia total recorrida en metros
        double distanciaTotal = pasos * distancia;

        // Calcular las calorías quemadas
        double KcaloriasQuemadas = distanciaTotal * velocidadPromedio * peso * factorCalorias;

        return KcaloriasQuemadas;
    }

    private void Reiniciar_Clicked(object sender, EventArgs e)
    {

        stepCount = 0;
        walkingTime = TimeSpan.Zero;
        caloriasQuemadas = 0;

        Distancia.IsVisible = false;
        Calorias.IsVisible = false;
        Tiempo.IsVisible = false;

        Reiniciar.IsVisible = false;

    }



    private async void Stop_Click(object sender, EventArgs e)
    { 

        if (Preferences.Get("Genero", string.Empty) != null && Preferences.Get("Altura", string.Empty)!=null)
        {
            double Drecorrida = calcularDistancia(Preferences.Get("Genero", string.Empty),double.Parse(Preferences.Get("Altura", string.Empty)), stepCount);

            Distancia.Text = "La distancia recorrida es de " + Math.Round(Drecorrida, 2).ToString() + " Metros";
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH:mm:ss");
            string distancia1 = (Math.Truncate(Drecorrida * 100) / 100).ToString();
            string distancia = distancia1.Replace(",", ".");

            Distancia.IsVisible = true;
            Calorias.IsVisible = true;
            Tiempo.IsVisible = true;


            await EnviarDatosPorPost(stepCount, distancia, walkingTime.ToString(), caloriasQuemadas.ToString(), fecha);


            Reiniciar.IsVisible = true;



        }

    }




    private async void Cargar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.Menu());
    }




    public async Task<bool> EnviarDatosPorPost(int pasos, string distancia, string tiempo, string calorias, string fecha)
    {


        try
        {
            var data = new Dictionary<string, object>
        {
            { "pasos", pasos },
            { "distancia", distancia },
            { "tiempo", tiempo},
            { "calorias", calorias },
            { "fecha", fecha }
        };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://192.168.1.35/RestPodometro/index.php");
            request.Content = content;

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (System.Exception ex)
        {
            // Manejar errores de conexión o de la API
            Console.WriteLine($"Error al enviar datos por POST: {ex.Message}");
            return false;
        }
    }




}
