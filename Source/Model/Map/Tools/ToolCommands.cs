using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source.Model.Map.Tools
{
    internal sealed class RemoveConstructionAreaCommand : IToolCommand
    {
        private readonly Point3 _start;
        private readonly Point3 _end;

        public RemoveConstructionAreaCommand(Point3 start, Point3 end)
        {
            _start = start;
            _end = end;
        }

        public void Execute(Site site)
        {
            for (int x = _start.X; x <= _end.X; x++)
            {
                for (int y = _start.Y; y <= _end.Y; y++)
                {
                    site.RemoveConstruction(new Point3(x, y, _start.Z));
                }
            }
        }
    }

    internal sealed class PlaceConstructionAreaCommand : IToolCommand
    {
        private readonly Point3 _start;
        private readonly Point3 _end;
        private readonly Construction _construction;
        private readonly Material _material;

        public PlaceConstructionAreaCommand(Point3 start, Point3 end, Construction construction, Material material)
        {
            _start = start;
            _end = end;
            _construction = construction;
            _material = material;
        }

        public void Execute(Site site)
        {
            for (int z = _start.Z; z <= _end.Z; z++)
            {
                for (int x = _start.X; x <= _end.X; x++)
                {
                    for (int y = _start.Y; y <= _end.Y; y++)
                    {
                        site.PlaceConstruction(new Point3(x, y, z), _construction, _material);
                    }
                }
            }
        }
    }
}
