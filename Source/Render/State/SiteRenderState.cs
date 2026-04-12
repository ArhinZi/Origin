using Origin.Source.Utils;
using System.Collections.Generic;

namespace Origin.Source.Render.State
{
    public sealed class SiteRenderState
    {
        private readonly Point3 _size;
        private readonly Dictionary<Point3, TileRenderState> _tileStates = [];
        private readonly HashSet<Point3> _dirtyTiles = [];

        public SiteRenderState(Point3 size)
        {
            _size = size;
        }

        public bool HasDirtyTiles => _dirtyTiles.Count > 0;

        public void Clear()
        {
            _tileStates.Clear();
            _dirtyTiles.Clear();
        }

        public void ClearDirty()
        {
            _dirtyTiles.Clear();
        }

        public bool TryGet(Point3 pos, out TileRenderState state)
        {
            return _tileStates.TryGetValue(pos, out state);
        }

        public TileRenderState GetOrCreate(Point3 pos)
        {
            if (!_tileStates.TryGetValue(pos, out var state))
            {
                state = new TileRenderState();
                _tileStates.Add(pos, state);
            }

            return state;
        }

        public void Remove(Point3 pos)
        {
            _tileStates.Remove(pos);
        }

        public void RemoveIfEmpty(Point3 pos)
        {
            if (_tileStates.TryGetValue(pos, out var state) && !state.HasAny)
                _tileStates.Remove(pos);
        }

        public void MarkDirty(Point3 pos)
        {
            if (pos.InBounds(Point3.Zero, _size))
                _dirtyTiles.Add(pos);
        }

        public void MarkDirtyAround(Point3 pos)
        {
            foreach (var offset in WorldUtils.FULL_NEIGHBOUR_PATTERN_1L(true))
            {
                var dirtyPos = pos + offset;
                if (dirtyPos.InBounds(Point3.Zero, _size))
                    _dirtyTiles.Add(dirtyPos);
            }
        }

        public void MarkDirty(IEnumerable<Point3> positions, bool includeNeighbours)
        {
            foreach (var pos in positions)
            {
                if (includeNeighbours)
                    MarkDirtyAround(pos);
                else
                    MarkDirty(pos);
            }
        }

        public List<Point3> ConsumeDirtyTiles()
        {
            List<Point3> result = [.. _dirtyTiles];
            _dirtyTiles.Clear();
            return result;
        }
    }
}
