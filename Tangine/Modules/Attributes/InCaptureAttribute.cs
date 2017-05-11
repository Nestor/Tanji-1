namespace Tangine.Modules
{
    public sealed class InDataCaptureAttribute : DataCaptureAttribute
    {
        public InDataCaptureAttribute(ushort id)
            : base(id, false)
        { }
        public InDataCaptureAttribute(string hash)
            : base(hash, false)
        { }
    }
}