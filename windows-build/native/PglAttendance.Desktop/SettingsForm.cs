using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PglAttendance.Desktop;

public sealed class SettingsForm : Form
{
    // Match MainForm palette
    private static readonly Color Bg          = Color.FromArgb(248, 249, 251);
    private static readonly Color Surface     = Color.White;
    private static readonly Color SurfaceHover= Color.FromArgb(241, 243, 245);
    private static readonly Color BorderHair  = Color.FromArgb(231, 233, 237);
    private static readonly Color BorderSoft  = Color.FromArgb(216, 220, 226);
    private static readonly Color TextPrimary = Color.FromArgb( 31,  35,  40);
    private static readonly Color TextMuted   = Color.FromArgb(101, 109, 118);
    private static readonly Color TextSubtle  = Color.FromArgb(139, 148, 158);
    private static readonly Color Accent      = Color.FromArgb(  9, 105, 218);
    private static readonly Color AccentHover = Color.FromArgb(  8,  87, 184);
    private static readonly Color WarnFg      = Color.FromArgb(154, 103,   0);
    private static readonly Color WarnBg      = Color.FromArgb(255, 248, 197);

    private static readonly Font FontTitle   = new("Segoe UI Semibold", 13.5F);
    private static readonly Font FontH2      = new("Segoe UI Semibold", 9.5F);
    private static readonly Font FontBody    = new("Segoe UI", 9F);
    private static readonly Font FontBodyBold= new("Segoe UI Semibold", 9F);
    private static readonly Font FontLabel   = new("Segoe UI", 8.5F);

    private readonly ServiceClient _svc;
    private readonly TextBox _hrmis = new();
    private readonly NumericUpDown _port = new();
    private readonly Button _save = new();
    private readonly Button _cancel = new();
    private readonly Label _hint = new();
    private int _initialPort = 4001;

    public SettingsForm(ServiceClient svc)
    {
        _svc = svc;
        Text = "Settings";
        ClientSize = new Size(580, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = FontBody;
        DoubleBuffered = true;

        // ---------- Header --------------------------------------------------
        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Surface, Padding = new Padding(28, 16, 28, 16) };
        header.Paint += (_, e) => DrawBottomHairline(e.Graphics, header);

        var titleLabel = new Label
        {
            Text = "Settings",
            Font = FontTitle,
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(28, 18),
        };
        var subLabel = new Label
        {
            Text = "Configure the device-receive port and HRMIS sync target.",
            Font = FontLabel,
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(28, 46),
        };
        header.Controls.Add(titleLabel);
        header.Controls.Add(subLabel);

        // ---------- Body ----------------------------------------------------
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(28, 24, 28, 8) };

        // HRMIS URL field
        var lblUrl = new Label { Text = "HRMIS API URL", Location = new Point(28, 30), AutoSize = true, Font = FontH2, ForeColor = TextPrimary };
        StyleTextField(_hrmis, new Point(28, 54), 524);
        var hintUrl = new Label
        {
            Text = "Records POST to  {url}/iclock/cdata.  Takes effect immediately — no restart.",
            Location = new Point(28, 90),
            AutoSize = true,
            Font = FontLabel,
            ForeColor = TextSubtle,
        };

        // Port field
        var lblPort = new Label { Text = "Listening port", Location = new Point(28, 132), AutoSize = true, Font = FontH2, ForeColor = TextPrimary };
        _port.Location = new Point(28, 156);
        _port.Width = 140;
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Value = 4001;
        _port.Font = FontBody;
        _port.BorderStyle = BorderStyle.FixedSingle;
        _port.BackColor = Surface;
        var hintPort = new Label
        {
            Text = "Devices send data to  http://<this-PC-IP>:<port>/iclock/cdata.",
            Location = new Point(28, 192),
            AutoSize = true,
            Font = FontLabel,
            ForeColor = TextSubtle,
        };

        // Live warning when port changes
        _hint.Location = new Point(28, 224);
        _hint.AutoSize = false;
        _hint.Width = 524;
        _hint.Height = 32;
        _hint.Font = FontLabel;
        _hint.ForeColor = WarnFg;
        _hint.BackColor = WarnBg;
        _hint.TextAlign = ContentAlignment.MiddleLeft;
        _hint.Padding = new Padding(12, 0, 12, 0);
        _hint.Text = "";
        _hint.Visible = false;
        _hint.Paint += (s, e) =>
        {
            var lb = (Label)s!;
            using var path = RoundedRect(new Rectangle(0, 0, lb.Width - 1, lb.Height - 1), 6);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(212, 167, 44));
            e.Graphics.DrawPath(pen, path);
        };

        _port.ValueChanged += (_, _) =>
        {
            if ((int)_port.Value != _initialPort)
            {
                _hint.Text = $"   Changing the port will restart the background service.";
                _hint.Visible = true;
            }
            else _hint.Visible = false;
        };

        body.Controls.Add(lblUrl);
        body.Controls.Add(_hrmis);
        body.Controls.Add(hintUrl);
        body.Controls.Add(lblPort);
        body.Controls.Add(_port);
        body.Controls.Add(hintPort);
        body.Controls.Add(_hint);

        // ---------- Footer --------------------------------------------------
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Surface, Padding = new Padding(28, 14, 28, 14) };
        footer.Paint += (_, e) => DrawTopHairline(e.Graphics, footer);

        StyleGhostButton(_cancel, "Cancel", 96);
        StylePrimaryButton(_save, "Save", 96);
        _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _save.Click += async (_, _) => await SaveAsync();
        AcceptButton = _save;
        CancelButton = _cancel;
        footer.Resize += (_, _) =>
        {
            _save.Location = new Point(footer.Width - _save.Width - 28, 14);
            _cancel.Location = new Point(_save.Left - _cancel.Width - 8, 14);
        };
        footer.Controls.Add(_cancel);
        footer.Controls.Add(_save);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);

        Load += async (_, _) => await LoadCurrentAsync();
    }

    // -------------------------------------------------------------------------
    private static void StyleTextField(TextBox box, Point loc, int width)
    {
        box.Location = loc;
        box.Width = width;
        box.Font = new("Segoe UI", 9.5F);
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Surface;
        box.ForeColor = TextPrimary;
    }

    private static void StyleGhostButton(Button b, string text, int width)
    {
        b.Text = text;
        b.Width = width;
        b.Height = 36;
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Surface;
        b.ForeColor = TextPrimary;
        b.Font = FontBody;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderColor = BorderSoft;
        b.FlatAppearance.MouseOverBackColor = SurfaceHover;
        b.UseVisualStyleBackColor = false;
    }

    private static void StylePrimaryButton(Button b, string text, int width)
    {
        b.Text = text;
        b.Width = width;
        b.Height = 36;
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Accent;
        b.ForeColor = Color.White;
        b.Font = FontBodyBold;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = AccentHover;
        b.UseVisualStyleBackColor = false;
    }

    private static void DrawBottomHairline(Graphics g, Panel p)
    {
        using var pen = new Pen(BorderHair);
        g.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
    }

    private static void DrawTopHairline(Graphics g, Panel p)
    {
        using var pen = new Pen(BorderHair);
        g.DrawLine(pen, 0, 0, p.Width, 0);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        var d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // -------------------------------------------------------------------------
    private async Task LoadCurrentAsync()
    {
        var cur = await _svc.GetSettingsAsync();
        if (cur is null) return;
        _hrmis.Text = cur.HrmisUrl;
        _port.Value = Math.Clamp(cur.Port, 1, 65535);
        _initialPort = cur.Port;
    }

    private async Task SaveAsync()
    {
        var url = _hrmis.Text.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var u)
            || (u.Scheme != "http" && u.Scheme != "https"))
        {
            MessageBox.Show(this, "Please enter a valid http(s) URL.", "Settings",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var newPort = (int)_port.Value;
        _save.Enabled = false;
        try
        {
            var ok = await _svc.UpdateSettingsAsync(url, newPort);
            if (!ok)
            {
                MessageBox.Show(this, "Could not save settings. Is the service running?",
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (newPort != _initialPort)
            {
                MessageBox.Show(this,
                    $"Port changed to {newPort}. The service is restarting — the dashboard will reconnect automatically.",
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        finally { _save.Enabled = true; }
    }
}
