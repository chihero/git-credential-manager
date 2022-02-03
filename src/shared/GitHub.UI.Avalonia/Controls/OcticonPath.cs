using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace GitHub.UI.Controls
{
    public class OcticonPath : Shape
    {
        private static readonly Dictionary<Octicon, Lazy<Geometry>> GeometryCache = CreateGeometryCache();

        public static readonly StyledProperty<Octicon> IconProperty =
            AvaloniaProperty.Register<OcticonImage, Octicon>(nameof(Icon));

        public Octicon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        protected override Geometry CreateDefiningGeometry() => GetGeometryForIcon(Icon);

        private static Dictionary<Octicon, Lazy<Geometry>> CreateGeometryCache()
        {
            return Enum.GetValues<Octicon>()
                .ToDictionary(
                    x => x,
                    x => new Lazy<Geometry>(
                        () => LoadGeometry(x)
                    )
                );
        }

        private static Geometry GetGeometryForIcon(Octicon icon)
        {
            if (!GeometryCache.TryGetValue(icon, out Lazy<Geometry> lazyGeometry))
            {
                throw new ArgumentOutOfRangeException(nameof(icon), @"Unknown icon");
            }

            return lazyGeometry.Value;
        }

        private static Geometry LoadGeometry(Octicon icon)
        {
            string iconName = Enum.GetName(icon);
            string pathData = Assets.OcticonPathData.ResourceManager.GetString(iconName);

            if (pathData is null)
            {
                throw new ArgumentException($@"Unable to locate path geometry for icon '{iconName}'", nameof(icon));
            }

            try
            {
                return PathGeometry.Parse(pathData);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
