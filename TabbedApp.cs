
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class TabbedApp
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    private sealed class MainForm : Form
    {
        public MainForm()
        {
            Text = "Приложение с вкладками";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(480, 320);
            Size = new Size(800, 520);
            Font = new Font("Segoe UI", 10F);

            using (Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("AppIcon.ico"))
            {
                if (stream == null)
                    throw new InvalidOperationException("Не найден ресурс AppIcon.ico.");

                using (Icon icon = new Icon(stream))
                    this.Icon = (Icon)icon.Clone();
            }

            Image wallpaper;
            using (Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Wallpaper.jpg"))
            {
                if (stream == null)
                    throw new InvalidOperationException("Не найден ресурс Wallpaper.jpg.");

                using (Image image = Image.FromStream(stream))
                    wallpaper = new Bitmap(image);
            }
            FormClosed += (sender, args) => wallpaper.Dispose();

            var tabs = new BufferedTabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 6),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(1, 32),
                Multiline = false
            };

            Action updateTabWidths = () =>
            {
                if (tabs.TabPages.Count == 0 || tabs.ClientSize.Width <= 0)
                    return;

                int itemWidth = Math.Max(1, tabs.ClientSize.Width / tabs.TabPages.Count);
                if (tabs.ItemSize.Width != itemWidth)
                    tabs.ItemSize = new Size(itemWidth, tabs.ItemSize.Height);
            };
            tabs.SizeChanged += (sender, args) => updateTabWidths();

            tabs.DrawItem += (sender, args) =>
            {
                GraphicsState state = args.Graphics.Save();
                try
                {
                    args.Graphics.SetClip(args.Bounds);
                    DrawWallpaper(args.Graphics, wallpaper, tabs.ClientSize, 0, 0);

                    bool selected = (args.State & DrawItemState.Selected) != 0;
                    Rectangle buttonBounds = args.Bounds;
                    buttonBounds.Inflate(-2, -2);
                    using (var fill = new SolidBrush(
                        Color.FromArgb(selected ? 115 : 65, Color.Black)))
                        args.Graphics.FillRectangle(fill, buttonBounds);

                    using (var outerBorder = new Pen(Color.FromArgb(190, Color.Black)))
                        args.Graphics.DrawRectangle(outerBorder, buttonBounds);

                    string text = tabs.TabPages[args.Index].Text;
                    using (var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    })
                    using (var shadow = new SolidBrush(Color.FromArgb(220, Color.Black)))
                    using (var foreground = new SolidBrush(Color.White))
                    {
                        RectangleF textBounds = args.Bounds;
                        textBounds.Offset(1, 1);
                        args.Graphics.DrawString(text, Font, shadow, textBounds, format);
                        textBounds.Offset(-1, -1);
                        args.Graphics.DrawString(text, Font, foreground, textBounds, format);
                    }
                }
                finally
                {
                    args.Graphics.Restore(state);
                }
            };

            for (int i = 1; i <= 10; i++)
            {
                var page = new WallpaperTabPage(i.ToString(), wallpaper, tabs);
                tabs.TabPages.Add(page);
            }

            Controls.Add(tabs);
            updateTabWidths();
        }

    }

    private sealed class WallpaperTabPage : TabPage
    {
        private readonly Image wallpaper;
        private readonly TabControl owner;

        public WallpaperTabPage(string title, Image wallpaper, TabControl owner)
            : base(title)
        {
            this.wallpaper = wallpaper;
            this.owner = owner;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,
                true);
            UpdateStyles();
            BackColor = Color.White;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            DrawWallpaper(
                e.Graphics,
                wallpaper,
                owner.ClientSize,
                -owner.DisplayRectangle.Left,
                -owner.DisplayRectangle.Top);
        }
    }

    private static void DrawWallpaper(
        Graphics graphics, Image wallpaper, Size viewport, float offsetX, float offsetY)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return;

        float scale = Math.Max(
            (float)viewport.Width / wallpaper.Width,
            (float)viewport.Height / wallpaper.Height);
        float width = wallpaper.Width * scale;
        float height = wallpaper.Height * scale;

        GraphicsState state = graphics.Save();
        try
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(
                wallpaper,
                (viewport.Width - width) / 2f + offsetX,
                (viewport.Height - height) / 2f + offsetY,
                width,
                height);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private sealed class BufferedTabControl : TabControl
    {
        public BufferedTabControl()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,
                true);
            UpdateStyles();
        }
    }
}
