namespace PhotoIt.Data
{
    public sealed record PhotoSlot
    {
        public PhotoSlot(
            int Col,
            int Row,
            PhotoType Type,
            int ColSpan = 1,
            int RowSpan = 1,
            string? PhotoId = null)
        {
            this.Col = Col;
            this.Row = Row;
            this.Type = Type;
            this.ColSpan = ColSpan;
            this.RowSpan = RowSpan;
            this.PhotoId = PhotoId;
        }

        public PhotoSlot() : this(1, 1, PhotoType.Square)
        {
        }

        public int Col { get; set; }
        public int Row { get; set; }
        public PhotoType Type { get; set; }
        public int ColSpan { get; set; }
        public int RowSpan { get; set; }
        public string? PhotoId { get; set; }
        public double? OffsetX { get; set; }
        public double? OffsetY { get; set; }
        public double PositionX { get; set; } = 50d;
        public double PositionY { get; set; } = 50d;
        public double Zoom { get; set; } = 1d;

        public void Deconstruct(
            out int Col,
            out int Row,
            out PhotoType Type,
            out int ColSpan,
            out int RowSpan,
            out string? PhotoId)
        {
            Col = this.Col;
            Row = this.Row;
            Type = this.Type;
            ColSpan = this.ColSpan;
            RowSpan = this.RowSpan;
            PhotoId = this.PhotoId;
        }
    }
}
