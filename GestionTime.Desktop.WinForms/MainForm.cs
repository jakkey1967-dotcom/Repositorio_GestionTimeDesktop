using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace GestionTime.Desktop.WinForms
{
    public partial class MainForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiBaseUrl;
        private readonly string _userName;
        private readonly string _userEmail;
        private readonly string _userRole;

        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private TabControl tabControl;
        private TabPage tabPartes;
        private TabPage tabResumen;

        // Partes tab controls
        private DataGridView dgvPartes;
        private Button btnNuevaParte;
        private Button btnCerrarParte;
        private Button btnRefresh;
        private ComboBox cmbClientes;
        private ComboBox cmbGrupos;
        private ComboBox cmbTipos;

        public MainForm(HttpClient httpClient, ILogger logger, string apiBaseUrl, string userName, string userEmail, string userRole)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiBaseUrl = apiBaseUrl;
            _userName = userName;
            _userEmail = userEmail;
            _userRole = userRole;

            InitializeComponent();
            LoadInitialData();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form properties
            Text = $"GestionTime Desktop - {_userName}";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            CreateMenuStrip();
            CreateStatusStrip();
            CreateTabControl();

            ResumeLayout(false);
            PerformLayout();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(248, 249, 250),
                Font = new Font("Segoe UI", 10)
            };

            // File menu
            var fileMenu = new ToolStripMenuItem("&Archivo");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Nueva Parte", null, (s, e) => CrearNuevaParte()) { ShortcutKeys = Keys.Control | Keys.N });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Salir", null, (s, e) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 });

            // View menu
            var viewMenu = new ToolStripMenuItem("&Ver");
            viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Actualizar", null, (s, e) => RefreshData()) { ShortcutKeys = Keys.F5 });
            viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Partes de Hoy", null, (s, e) => FiltrarPartesHoy()));

            // Help menu
            var helpMenu = new ToolStripMenuItem("&Ayuda");
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&Acerca de", null, (s, e) => MostrarAcercaDe()));

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, helpMenu });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var lblUser = new ToolStripStatusLabel($"👤 Usuario: {_userName} ({_userRole})");
            var lblSeparator = new ToolStripStatusLabel(" | ");
            var lblTime = new ToolStripStatusLabel($"🕐 {DateTime.Now:HH:mm:ss}");

            // Update time every second
            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => lblTime.Text = $"🕐 {DateTime.Now:HH:mm:ss}";
            timer.Start();

            statusStrip.Items.AddRange(new ToolStripItem[] { lblUser, lblSeparator, lblTime });
            Controls.Add(statusStrip);
        }

        private void CreateTabControl()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            CreatePartesTab();
            CreateResumenTab();

            tabControl.TabPages.AddRange(new TabPage[] { tabPartes, tabResumen });
            Controls.Add(tabControl);
        }

        private void CreatePartesTab()
        {
            tabPartes = new TabPage("📋 Partes de Trabajo")
            {
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // Top panel with filters and buttons
            var topPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Filters
            var lblCliente = new Label
            {
                Text = "Cliente:",
                Location = new Point(10, 20),
                AutoSize = true
            };

            cmbClientes = new ComboBox
            {
                Location = new Point(60, 17),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblGrupo = new Label
            {
                Text = "Grupo:",
                Location = new Point(230, 20),
                AutoSize = true
            };

            cmbGrupos = new ComboBox
            {
                Location = new Point(275, 17),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblTipo = new Label
            {
                Text = "Tipo:",
                Location = new Point(415, 20),
                AutoSize = true
            };

            cmbTipos = new ComboBox
            {
                Location = new Point(450, 17),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Buttons
            btnNuevaParte = new Button
            {
                Text = "➕ Nueva Parte",
                Location = new Point(600, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNuevaParte.FlatAppearance.BorderSize = 0;
            btnNuevaParte.Click += (s, e) => CrearNuevaParte();

            btnCerrarParte = new Button
            {
                Text = "⏹️ Cerrar Parte",
                Location = new Point(730, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCerrarParte.FlatAppearance.BorderSize = 0;
            btnCerrarParte.Click += (s, e) => CerrarParteSeleccionada();

            btnRefresh = new Button
            {
                Text = "🔄 Actualizar",
                Location = new Point(860, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshData();

            topPanel.Controls.AddRange(new Control[] 
            {
                lblCliente, cmbClientes, lblGrupo, cmbGrupos, 
                lblTipo, cmbTipos, btnNuevaParte, btnCerrarParte, btnRefresh
            });

            // DataGridView for parts
            dgvPartes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9),
                RowHeadersWidth = 25
            };

            // Configure columns
            dgvPartes.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Grupo", HeaderText = "Grupo", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripción", Width = 300 },
                new DataGridViewTextBoxColumn { Name = "FechaInicio", HeaderText = "Inicio", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "FechaFin", HeaderText = "Fin", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "Duracion", HeaderText = "Duración", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 80 }
            });

            tabPartes.Controls.Add(dgvPartes);
            tabPartes.Controls.Add(topPanel);
        }

        private void CreateResumenTab()
        {
            tabResumen = new TabPage("📊 Resumen")
            {
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            var lblResumen = new Label
            {
                Text = "📊 Resumen de Actividad",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 122, 183),
                Location = new Point(20, 20),
                AutoSize = true
            };

            tabResumen.Controls.Add(lblResumen);
        }

        private async void LoadInitialData()
        {
            try
            {
                _logger.LogInformation("Cargando datos iniciales...");

                await Task.WhenAll(
                    LoadClientes(),
                    LoadGrupos(),
                    LoadTipos(),
                    LoadPartes()
                );

                _logger.LogInformation("Datos iniciales cargados correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando datos iniciales");
                MessageBox.Show($"Error cargando datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    
                    cmbClientes.BeginUpdate();
                    cmbClientes.Items.Clear();
                    cmbClientes.Items.Add(new CatalogItem { Id = 0, Nombre = "Todos los clientes" });
                    if (clientes != null)
                        cmbClientes.Items.AddRange(clientes.ToArray());
                    cmbClientes.DisplayMember = "Nombre";
                    cmbClientes.ValueMember = "Id";
                    cmbClientes.SelectedIndex = 0;
                    cmbClientes.EndUpdate();
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
                    
                    cmbGrupos.BeginUpdate();
                    cmbGrupos.Items.Clear();
                    cmbGrupos.Items.Add(new CatalogItem { Id = 0, Nombre = "Todos los grupos" });
                    if (grupos != null)
                        cmbGrupos.Items.AddRange(grupos.ToArray());
                    cmbGrupos.DisplayMember = "Nombre";
                    cmbGrupos.ValueMember = "Id";
                    cmbGrupos.SelectedIndex = 0;
                    cmbGrupos.EndUpdate();
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
                    
                    cmbTipos.BeginUpdate();
                    cmbTipos.Items.Clear();
                    cmbTipos.Items.Add(new CatalogItem { Id = 0, Nombre = "Todos los tipos" });
                    if (tipos != null)
                        cmbTipos.Items.AddRange(tipos.ToArray());
                    cmbTipos.DisplayMember = "Nombre";
                    cmbTipos.ValueMember = "Id";
                    cmbTipos.SelectedIndex = 0;
                    cmbTipos.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando tipos");
            }
        }

        private async Task LoadPartes()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/partes");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var partes = JsonConvert.DeserializeObject<List<ParteItem>>(content);
                    
                    dgvPartes.Rows.Clear();
                    if (partes != null)
                    {
                        foreach (var parte in partes)
                        {
                            dgvPartes.Rows.Add(
                                parte.Id,
                                parte.Cliente,
                                parte.Grupo,
                                parte.Tipo,
                                parte.Descripcion,
                                parte.FechaInicio?.ToString("dd/MM/yyyy HH:mm"),
                                parte.FechaFin?.ToString("dd/MM/yyyy HH:mm"),
                                parte.Duracion,
                                parte.Estado
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando partes");
            }
        }

        private void CrearNuevaParte()
        {
            try
            {
                var nuevaParteForm = new NuevaParteForm(_httpClient, _logger, _apiBaseUrl);
                if (nuevaParteForm.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando nueva parte");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CerrarParteSeleccionada()
        {
            if (dgvPartes.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una parte para cerrar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = dgvPartes.SelectedRows[0];
            var parteId = selectedRow.Cells["Id"].Value?.ToString();
            var estado = selectedRow.Cells["Estado"].Value?.ToString();

            if (estado == "Cerrada")
            {
                MessageBox.Show("La parte seleccionada ya está cerrada.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("¿Está seguro de que desea cerrar esta parte?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                // TODO: Implement close part API call
                MessageBox.Show("Funcionalidad de cerrar parte en desarrollo.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void RefreshData()
        {
            try
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "🔄 Cargando...";
                
                await LoadPartes();
                
                _logger.LogInformation("Datos actualizados correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando datos");
                MessageBox.Show($"Error actualizando datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "🔄 Actualizar";
            }
        }

        private void FiltrarPartesHoy()
        {
            // TODO: Implement filter for today's parts
            MessageBox.Show("Filtro de partes de hoy en desarrollo.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarAcercaDe()
        {
            MessageBox.Show("GestionTime Desktop v1.1.0\nSistema de Gestión de Tiempo\n\n© 2025 GestionTime Solutions", 
                          "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class CatalogItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        
        public override string ToString() => Nombre;
    }

    public class ParteItem
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Duracion { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}