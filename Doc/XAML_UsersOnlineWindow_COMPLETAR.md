# XAML Completo para UsersOnlineWindow.xaml

Copiar este contenido completo en el archivo `Views\UsersOnlineWindow.xaml`:

```xaml
<Window
    x:Class="GestionTime.Desktop.Views.UsersOnlineWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="Usuarios Online"
    Width="400"
    Height="600">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="#0FA7B6" Padding="20,16">
            <StackPanel>
                <TextBlock Text="Usuarios Online" FontSize="20" FontWeight="SemiBold" Foreground="White"/>
                <TextBlock x:Name="TxtSubtitle" Text="Actualizando..." FontSize="13" Foreground="#E0F7FA" Margin="0,4,0,0"/>
            </StackPanel>
        </Border>
        <Grid Grid.Row="1" Padding="12">
            <StackPanel x:Name="LoadingPanel" VerticalAlignment="Center" HorizontalAlignment="Center" Visibility="Collapsed">
                <ProgressRing IsActive="True" Width="48" Height="48" Foreground="#0FA7B6"/>
                <TextBlock Text="Cargando usuarios..." Margin="0,12,0,0" FontSize="14" Foreground="#666666" HorizontalAlignment="Center"/>
            </StackPanel>
            <StackPanel x:Name="ErrorPanel" VerticalAlignment="Center" HorizontalAlignment="Center" Visibility="Collapsed" Padding="24">
                <TextBlock x:Name="TxtError" Text="Error al cargar usuarios" Margin="0,12,0,0" FontSize="14" Foreground="#666666" HorizontalAlignment="Center" TextWrapping="Wrap" TextAlignment="Center" MaxWidth="300"/>
            </StackPanel>
            <ScrollViewer x:Name="UsersScrollViewer" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
                <ItemsControl x:Name="UsersListView" Margin="0,8"/>
            </ScrollViewer>
        </Grid>
    </Grid>
</Window>
```

Este archivo contiene el XAML mínimo para que compile correctamente con InitializeComponent().
