namespace Podometer.Pages;

public partial class Menu : ContentPage
{

    public Menu()
    {
        InitializeComponent();
    }

    private async void Volver_Clicked(object sender, EventArgs e)
    {
        Preferences.Set("Altura", altura.Text);
        Preferences.Set("Peso", Peso.Text);
        await Navigation.PopAsync();
    }

    private void Genero_Changed(object sender, EventArgs e)
    {
        if (Genero.SelectedIndex == 0) { Preferences.Set("Genero", "Masculino"); }
        if (Genero.SelectedIndex == 1) { Preferences.Set("Genero", "Femenino"); }
        if (Genero.SelectedIndex == 2) { Preferences.Set("Genero", "Otro"); }

    }


}