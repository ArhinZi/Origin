namespace Origin.Source.Model.NewWorld
{
    // Іменовані біти стану тайла (рівно 16 прапорців).
    public enum TileFlagBit : byte
    {
        Exists = 0,
        HasConstruction = 1,
        HasConstructionOver = 2,
        IsRamp = 3,
        HasConstructionShape = 4,
        HasConstructionRotation = 5,
        HasFluid = 6,
        IsFluidStatic = 7,
        IsFluidBlocker = 8,
        HasVegetation = 9,
        IsWalkable = 10,
        Reserved11 = 11,
        Reserved12 = 12,
        Reserved13 = 13,
        Reserved14 = 14,
        Reserved15 = 15
    }

    // Компактне 16-бітне сховище булевих станів тайла.
    public struct TileFlags16
    {
        private ushort _bits;

        // Універсальні доступи за індексом біта.
        public readonly bool Get(TileFlagBit bit)
        {
            ushort mask = (ushort)(1 << (byte)bit);
            return (_bits & mask) != 0;
        }

        public void Set(TileFlagBit bit, bool state)
        {
            ushort mask = (ushort)(1 << (byte)bit);
            if (state)
                _bits |= mask;
            else
                _bits &= (ushort)~mask;
        }

        // Іменовані прапорці для компактного і читабельного використання в Tile.
        public bool Exists { readonly get => Get(TileFlagBit.Exists); set => Set(TileFlagBit.Exists, value); }
        public bool HasConstruction { readonly get => Get(TileFlagBit.HasConstruction); set => Set(TileFlagBit.HasConstruction, value); }
        public bool HasConstructionOver { readonly get => Get(TileFlagBit.HasConstructionOver); set => Set(TileFlagBit.HasConstructionOver, value); }
        public bool IsRamp { readonly get => Get(TileFlagBit.IsRamp); set => Set(TileFlagBit.IsRamp, value); }
        public bool HasConstructionShape { readonly get => Get(TileFlagBit.HasConstructionShape); set => Set(TileFlagBit.HasConstructionShape, value); }
        public bool HasConstructionRotation { readonly get => Get(TileFlagBit.HasConstructionRotation); set => Set(TileFlagBit.HasConstructionRotation, value); }
        public bool HasFluid { readonly get => Get(TileFlagBit.HasFluid); set => Set(TileFlagBit.HasFluid, value); }
        public bool IsFluidStatic { readonly get => Get(TileFlagBit.IsFluidStatic); set => Set(TileFlagBit.IsFluidStatic, value); }
        public bool IsFluidBlocker { readonly get => Get(TileFlagBit.IsFluidBlocker); set => Set(TileFlagBit.IsFluidBlocker, value); }
        public bool HasVegetation { readonly get => Get(TileFlagBit.HasVegetation); set => Set(TileFlagBit.HasVegetation, value); }
        public bool IsWalkable { readonly get => Get(TileFlagBit.IsWalkable); set => Set(TileFlagBit.IsWalkable, value); }

        // Сире значення для серіалізації/відладки.
        public readonly ushort RawValue => _bits;
        public void SetRawValue(ushort value) => _bits = value;
    }
}
