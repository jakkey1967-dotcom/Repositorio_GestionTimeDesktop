using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using GestionTime.Desktop.WinForms.Models;

namespace GestionTime.Desktop.WinForms
{
    public partial class NuevaParteForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiBaseUrl;

        private ComboBox cmbCliente;
        private ComboBox cmbGrupo;
        private ComboBox cmbTipo;
        private TextBox txtDescripcion;
        private DateTimePicker dtpFecha;
        private DateTimePicker dtpHoraInicio;
        private DateTimePicker dtpHoraFin;
        private Button btnGuardar;
        private Button btnCancelar;

        public NuevaParteForm(HttpClient httpClient, ILogger logger, string apiBaseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiBaseUrl = apiBaseUrl;
            
            InitializeComponent();
            LoadCatalogs();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Nueva Parte de Trabajo";
            Size = new Size(500, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var lblTitle = new Label
            {
                Text = "📝 Nueva Parte de Trabajo",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 122, 183),
                Size = new Size(400, 30),
                Location = new Point(50, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblCliente = new Label
            {
                Text = "👤 Cliente:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 80)
            };

            cmbCliente = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(300, 30),
                Location = new Point(140, 77),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblGrupo = new Label
            {
                Text = "📁 Grupo:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 130)
            };

            cmbGrupo = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(300, 30),
                Location = new Point(140, 127),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblTipo = new Label
            {
                Text = "🏷️ Tipo:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 180)
            };

            cmbTipo = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(300, 30),
                Location = new Point(140, 177),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblDescripcion = new Label
            {
                Text = "📝 Descripción:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 230)
            };

            txtDescripcion = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(400, 80),
                Location = new Point(30, 260),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var lblFecha = new Label
            {
                Text = "📅 Fecha:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 370)
            };

            dtpFecha = new DateTimePicker
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(150, 30),
                Location = new Point(140, 367),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            var lblHoraInicio = new Label
            {
                Text = "🕐 Hora Inicio:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(30, 420)
            };

            dtpHoraInicio = new DateTimePicker
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(120, 30),
                Location = new Point(140, 417),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now
            };

            var lblHoraFin = new Label
            {
                Text = "🕐 Hora Fin:",
                Font = new Font("Segoe UI", 11),
                Size = new Size(100, 25),
                Location = new Point(280, 420)
            };

            dtpHoraFin = new DateTimePicker
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(120, 30),
                Location = new Point(380, 417),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now.AddHours(1)
            };

            btnGuardar = new Button
            {
                Text = "💾 Guardar",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(120, 40),
                Location = new Point(200, 500),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Font = new Font("Segoe UI", 12),
                Size = new Size(120, 40),
                Location = new Point(340, 500),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] 
            {
                lblTitle, lblCliente, cmbCliente, lblGrupo, cmbGrupo, 
                lblTipo, cmbTipo, lblDescripcion, txtDescripcion,
                lblFecha, dtpFecha, lblHoraInicio, dtpHoraInicio, 
                lblHoraFin, dtpHoraFin, btnGuardar, btnCancelar
            });

            ResumeLayout(false);
        }

        private async void LoadCatalogs()
        {
            try
            {
                await Task.WhenAll(
                    LoadClientes(),
                    LoadGrupos(),
                    LoadTipos()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando catálogos");
                MessageBox.Show($"Error cargando catálogos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadClientes()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/catalog/clientes");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var clientes = JsonConvert.DeserializeObject<List<CatalogItem>>(content);
                    
                    cmbCliente.BeginUpdate();
                    cmbCliente.Items.Clear();
                    if (clientes != null)
                        cmbCliente.Items.AddRange(clientes.ToArray());
                    cmbCliente.DisplayMember = "Nombre";
                    cmbCliente.ValueMember = "Id";
                    if (cmbCliente.Items.Count > 0)
                        cmbCliente.SelectedIndex = 0;
                    cmbCliente.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando clientes");
            }
        }

        private async Task LoadGrupos()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/catalog/grupos");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var grupos = JsonConvert.DeserializeObject<List<CatalogItem>>(content);
                    
                    cmbGrupo.BeginUpdate();
                    cmbGrupo.Items.Clear();
                    if (grupos != null)
                        cmbGrupo.Items.AddRange(grupos.ToArray());
                    cmbGrupo.DisplayMember = "Nombre";
                    cmbGrupo.ValueMember = "Id";
                    if (cmbGrupo.Items.Count > 0)
                        cmbGrupo.SelectedIndex = 0;
                    cmbGrupo.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando grupos");
            }
        }

        private async Task LoadTipos()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/catalog/tipos");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tipos = JsonConvert.DeserializeObject<List<CatalogItem>>(content);
                    
                    cmbTipo.BeginUpdate();
                    cmbTipo.Items.Clear();
                    if (tipos != null)
                        cmbTipo.Items.AddRange(tipos.ToArray());
                    cmbTipo.DisplayMember = "Nombre";
                    cmbTipo.ValueMember = "Id";
                    if (cmbTipo.Items.Count > 0)
                        cmbTipo.SelectedIndex = 0;
                    cmbTipo.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando tipos");
            }
        }

        private async void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (ValidarFormulario())
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text = "💾 Guardando...";
                
                try
                {
                    await GuardarParte();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando parte");
                    MessageBox.Show($"Error guardando parte: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnGuardar.Enabled = true;
                    btnGuardar.Text = "💾 Guardar";
                }
            }
        }

        private bool ValidarFormulario()
        {
            if (cmbCliente.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCliente.Focus();
                return false;
            }

            if (cmbGrupo.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un grupo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbGrupo.Focus();
                return false;
            }

            if (cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un tipo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbTipo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Ingrese una descripción.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescripcion.Focus();
                return false;
            }

            if (dtpHoraFin.Value <= dtpHoraInicio.Value)
            {
                MessageBox.Show("La hora de fin debe ser posterior a la hora de inicio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpHoraFin.Focus();
                return false;
            }

            return true;
        }

        private async Task GuardarParte()
        {
            var cliente = (CatalogItem)cmbCliente.SelectedItem;
            var grupo = (CatalogItem)cmbGrupo.SelectedItem;
            var tipo = (CatalogItem)cmbTipo.SelectedItem;

            var fechaInicio = dtpFecha.Value.Date.Add(dtpHoraInicio.Value.TimeOfDay);
            var fechaFin = dtpFecha.Value.Date.Add(dtpHoraFin.Value.TimeOfDay);

            var nuevaParte = new
            {
                ClienteId = cliente.Id,
                GrupoId = grupo.Id,
                TipoId = tipo.Id,
                Descripcion = txtDescripcion.Text.Trim(),
                FechaInicio = fechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                FechaFin = fechaFin.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            var json = JsonConvert.SerializeObject(nuevaParte);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/partes", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error del servidor: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("Parte guardada exitosamente");
        }
    }
}
